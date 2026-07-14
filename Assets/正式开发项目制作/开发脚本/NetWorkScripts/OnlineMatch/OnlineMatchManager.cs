using System.Threading;
using UnityEngine;

namespace Panels
{
    public interface IOnlineMatchConnector
    {
        bool IsMatching { get; }
        bool IsWaiting { get; }
        void StartQuickMatch(GameModes mode, int heroId, int skinId);
        void CancelMatch();
    }

    public sealed class OnlineMatchManager : MonoBehaviour, IOnlineMatchConnector
    {
        public static OnlineMatchManager Instance { get; private set; }

        private bool isMatching;
        private bool isWaiting;
        private string currentTicketId;
        private string currentAccessToken;
        private bool cancelRequested;
        private bool isPolling;
        private int pollGeneration;
        private const float PollIntervalSeconds = 2f;
        private const int StartMatchMaxAttempts = 4;
        private const int StartMatchRetryDelayMs = 1500;
        private const int MaxTransientPollFailures = 3;
        private int transientPollFailures;
        private string handledMatchedRoomId;
        private bool isStartingMatchedRoom;
        private CancellationTokenSource matchedRoomCancellation;

        public bool IsMatching => isMatching || isStartingMatchedRoom;
        public bool IsWaiting => isWaiting;

        public static bool ShouldAcceptStartResult(
            int generation,
            int currentGeneration,
            bool matching,
            bool waiting,
            bool canceled)
        {
            return generation == currentGeneration
                && matching
                && waiting
                && !canceled;
        }

        public static bool CanCancelMatch(bool matching, bool waiting, bool startingMatchedRoom)
        {
            return matching || waiting || startingMatchedRoom;
        }

        public static bool IsCurrentGeneration(int generation, int currentGeneration)
        {
            return generation == currentGeneration;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[OnlineMatchManager] Awake");
        }

        public async void StartQuickMatch(GameModes mode, int heroId, int skinId)
        {
            if (isMatching || isWaiting || isStartingMatchedRoom)
            {
                Debug.LogWarning($"[OnlineMatchManager] 当前正在在线匹配或启动在线房间，isMatching={isMatching}, isWaiting={isWaiting}, isStartingMatchedRoom={isStartingMatchedRoom}");
                return;
            }

            string accessToken = SupabaseBackendProvider.GetSavedAccessToken();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                Debug.LogWarning("[OnlineMatchManager] 缺少 Supabase access token，请先登录后再进行在线匹配");
                return;
            }

            currentAccessToken = accessToken;

            PlayerBasicInfoMgr.Instance.CurrentNetworkMode = NetworkMode.SupabaseOnline;
            PlayerBasicInfoMgr.Instance.SetCurrentGamemode(mode);
            PlayerBasicInfoMgr.Instance.UpdateHeroCache(heroId, skinId);

            isMatching = true;
            isWaiting = true;
            cancelRequested = false;
            isPolling = false;
            pollGeneration++;
            transientPollFailures = 0;
            handledMatchedRoomId = null;
            isStartingMatchedRoom = false;
            currentTicketId = null;
            Debug.Log($"[OnlineMatchManager] 在线匹配入口已启动，mode={mode}, heroId={heroId}, skinId={skinId}");

            int generation = pollGeneration;
            OnlineMatchApiResult<OnlineMatchResponse> result =
                await StartMatchWithRetryAsync(mode, heroId, skinId, generation, accessToken);

            if (!IsMatchFlowActive(generation))
            {
                await CancelLateMatchTicketIfNeededAsync(accessToken, result);
                return;
            }

            if (result == null || !result.Success)
            {
                FailMatch($"启动在线匹配失败：{result?.ErrorMessage ?? "未知错误"}");
                return;
            }

            HandleMatchResponse(result.Data);
        }

        public async void CancelMatch()
        {
            if (!CanCancelMatch(isMatching, isWaiting, isStartingMatchedRoom))
            {
                return;
            }

            cancelRequested = true;
            string ticketId = currentTicketId;
            string accessToken = currentAccessToken;
            CancellationTokenSource roomCancellation = matchedRoomCancellation;
            isMatching = false;
            isWaiting = false;
            isPolling = false;
            pollGeneration++;
            handledMatchedRoomId = null;
            isStartingMatchedRoom = false;
            currentTicketId = null;
            currentAccessToken = null;
            matchedRoomCancellation = null;
            roomCancellation?.Cancel();

            if (!string.IsNullOrWhiteSpace(ticketId) && !string.IsNullOrWhiteSpace(accessToken))
            {
                OnlineMatchApiResult<OnlineMatchCancelResponse> result =
                    await OnlineMatchApiClient.CancelMatchAsync(accessToken, ticketId);
                if (!result.Success)
                {
                    Debug.LogWarning($"[OnlineMatchManager] 取消在线匹配请求失败：{result.ErrorMessage}");
                }
            }

            Debug.Log("[OnlineMatchManager] 已取消在线匹配");
        }

        private async void PollMatchStatus()
        {
            int generation = pollGeneration;
            while (IsPollingActive(generation))
            {
                await System.Threading.Tasks.Task.Delay(System.TimeSpan.FromSeconds(PollIntervalSeconds));
                if (!IsPollingActive(generation))
                {
                    StopPollingIfCurrent(generation);
                    return;
                }

                OnlineMatchApiResult<OnlineMatchResponse> result =
                    await OnlineMatchApiClient.GetStatusAsync(currentAccessToken, currentTicketId);
                if (!IsPollingActive(generation))
                {
                    StopPollingIfCurrent(generation);
                    return;
                }

                if (!result.Success)
                {
                    if (IsTransientMatchApiFailure(result) && transientPollFailures < MaxTransientPollFailures)
                    {
                        transientPollFailures++;
                        Debug.LogWarning($"[OnlineMatchManager] 轮询在线匹配状态临时失败，继续重试 {transientPollFailures}/{MaxTransientPollFailures}：{result.ErrorMessage}");
                        continue;
                    }

                    FailMatch($"轮询在线匹配状态失败：{result.ErrorMessage}");
                    return;
                }

                transientPollFailures = 0;
                HandleMatchResponse(result.Data);
            }

            StopPollingIfCurrent(generation);
        }

        private void StopPollingIfCurrent(int generation)
        {
            if (IsCurrentGeneration(generation, pollGeneration))
            {
                isPolling = false;
            }
        }

        private bool IsPollingActive(int generation)
        {
            return generation == pollGeneration
                && isMatching
                && isWaiting
                && !cancelRequested
                && !string.IsNullOrWhiteSpace(currentTicketId);
        }

        private bool IsMatchFlowActive(int generation)
        {
            return ShouldAcceptStartResult(
                generation,
                pollGeneration,
                isMatching,
                isWaiting,
                cancelRequested);
        }

        private async System.Threading.Tasks.Task<OnlineMatchApiResult<OnlineMatchResponse>> StartMatchWithRetryAsync(
            GameModes mode,
            int heroId,
            int skinId,
            int generation,
            string accessToken)
        {
            OnlineMatchApiResult<OnlineMatchResponse> lastResult = null;
            for (int attempt = 1; attempt <= StartMatchMaxAttempts; attempt++)
            {
                OnlineMatchApiResult<OnlineMatchResponse> result =
                    await OnlineMatchApiClient.StartMatchAsync(accessToken, mode, heroId, skinId);
                lastResult = result;

                if (!IsMatchFlowActive(generation) || result.Success)
                {
                    return result;
                }

                if (!IsTransientMatchApiFailure(result) || attempt == StartMatchMaxAttempts)
                {
                    return result;
                }

                Debug.LogWarning($"[OnlineMatchManager] 启动在线匹配临时失败，准备恢复重试 {attempt}/{StartMatchMaxAttempts}：{result.ErrorMessage}");
                await System.Threading.Tasks.Task.Delay(StartMatchRetryDelayMs);
                if (!IsMatchFlowActive(generation))
                {
                    return lastResult;
                }
            }

            return lastResult;
        }

        private static async System.Threading.Tasks.Task CancelLateMatchTicketIfNeededAsync(
            string accessToken,
            OnlineMatchApiResult<OnlineMatchResponse> result)
        {
            string ticketId = result?.Data?.TicketId;
            if (result == null
                || !result.Success
                || string.IsNullOrWhiteSpace(accessToken)
                || string.IsNullOrWhiteSpace(ticketId))
            {
                return;
            }

            OnlineMatchApiResult<OnlineMatchCancelResponse> cancelResult =
                await OnlineMatchApiClient.CancelMatchAsync(accessToken, ticketId);
            if (!cancelResult.Success)
            {
                Debug.LogWarning($"[OnlineMatchManager] 清理迟到匹配 ticket 失败，ticketId={ticketId}：{cancelResult.ErrorMessage}");
            }
        }

        private static bool IsTransientMatchApiFailure(OnlineMatchApiResult<OnlineMatchResponse> result)
        {
            if (result == null || result.Success)
            {
                return false;
            }

            if (result.StatusCode == 0 || result.StatusCode == 408 || result.StatusCode == 429 || result.StatusCode >= 500)
            {
                return true;
            }

            string message = result.ErrorMessage ?? string.Empty;
            return message.IndexOf("timeout", System.StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("timed out", System.StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("temporarily unavailable", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void HandleMatchResponse(OnlineMatchResponse response)
        {
            if (response == null)
            {
                FailMatch("在线匹配响应为空");
                return;
            }

            currentTicketId = response.TicketId;
            if (response.Status == "waiting")
            {
                Debug.Log($"[OnlineMatchManager] 已进入在线匹配队列，ticketId={currentTicketId}");
                if (!isPolling)
                {
                    isPolling = true;
                    PollMatchStatus();
                }
                return;
            }

            if (response.Status == "matched")
            {
                if (IsMatchedRoomAlreadyHandled(response.RoomId))
                {
                    Debug.Log($"[OnlineMatchManager] 已处理当前在线匹配房间，跳过重复 matched 响应，roomId={response.RoomId}");
                    return;
                }

                isMatching = false;
                isWaiting = false;
                isPolling = false;
                pollGeneration++;
                currentTicketId = null;
                handledMatchedRoomId = response.RoomId;
                isStartingMatchedRoom = true;
                CancellationTokenSource roomCancellation = new CancellationTokenSource();
                matchedRoomCancellation = roomCancellation;
                int connectionGeneration = pollGeneration;
                PlayerBasicInfoMgr.Instance.UpdateRoomID(response.RoomId);
                Debug.Log($"[OnlineMatchManager] 在线匹配成功，roomId={response.RoomId}, role={response.Role}, connectionMode={SupabaseConfig.Instance.OnlineConnectionMode}, provider={response.Room?["relay_provider"]}");
                _ = StartMatchedOnlineRoomAsync(response, connectionGeneration, roomCancellation);
                return;
            }

            FailMatch($"在线匹配结束，状态={response.Status}");
        }

        private void FailMatch(string message)
        {
            isMatching = false;
            isWaiting = false;
            isPolling = false;
            pollGeneration++;
            currentTicketId = null;
            Debug.LogWarning($"[OnlineMatchManager] {message}");
        }

        private bool IsMatchedRoomAlreadyHandled(string roomId)
        {
            return !string.IsNullOrWhiteSpace(roomId)
                && string.Equals(handledMatchedRoomId, roomId, System.StringComparison.OrdinalIgnoreCase);
        }

        private async System.Threading.Tasks.Task StartMatchedOnlineRoomAsync(
            OnlineMatchResponse response,
            int generation,
            CancellationTokenSource cancellation)
        {
            bool started = false;
            bool wasCanceled = false;
            string roomId = response?.RoomId;
            try
            {
                started = await OnlineConnectionLauncher.StartMatchedRoomAsync(
                    response,
                    PlayerBasicInfoMgr.Instance.GameMode,
                    PlayerBasicInfoMgr.Instance.HeroCache.heroId,
                    PlayerBasicInfoMgr.Instance.HeroCache.skinId,
                    cancellation.Token);
            }
            catch (System.OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                wasCanceled = true;
                Debug.Log($"[OnlineMatchManager] 已取消在线房间启动，roomId={roomId}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[OnlineMatchManager] 启动在线连接异常：{ex}");
            }
            finally
            {
                bool ownsCurrentState = generation == pollGeneration
                    && ReferenceEquals(matchedRoomCancellation, cancellation);
                if (ownsCurrentState)
                {
                    matchedRoomCancellation = null;
                    isStartingMatchedRoom = false;
                    if (!started
                        && string.Equals(handledMatchedRoomId, roomId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        handledMatchedRoomId = null;
                    }
                }

                cancellation.Dispose();
            }

            if (!started && !wasCanceled)
            {
                Debug.LogWarning("[OnlineMatchManager] 在线匹配已成功，但在线连接启动失败");
            }
        }

        private void OnDestroy()
        {
            pollGeneration++;
            CancellationTokenSource roomCancellation = matchedRoomCancellation;
            matchedRoomCancellation = null;
            roomCancellation?.Cancel();

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
