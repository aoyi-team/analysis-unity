using BaseClasses;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using static Cinemachine.CinemachineTriggerAction.ActionSettings;
using MsgFramework;

namespace Panels
{
    /// <summary>
    /// Ӣ��ѡ����壨������LobbyScene�У�����Open/Close����߼���
    /// </summary>
    public class ChooseHeroPanel : BasePanel
    {
        [Header("���ؽ�������")]
        private GameObject loadPanel;

        [Header("ҳ���������")]
        public Button ClosePageBtn;
        public Button GameStartBtn;
        public Button CancelMatchBtn;

        #region �������
        [Header("Ӣ���������")]
        private Image heroSkinSprite; // Ӣ��Ƥ������
        private Image heroSkinSpriteBg;//Ӣ��Ƥ����������
        private RectTransform heroSkinRect; // ����RectTransform�������ƶ���
        private CanvasGroup heroSkinCanvasGroup; // ���ڽ���

        [Header("ְҵ�л���ť")]
        private List<Button> CareerBtns;//ְҵѡ��ť
        private List<Image> CareerChoosedBtn;//��ѡ��ʱ��ͼ��

        [Header("ģʽͼ�����")]
        private Image modeIcon; // ģʽͼ��
        private GameModes GameMode;
        private HeroSelectionMatchController matchController;
        //private string defaultModeIconPath = "ModeIcons/Default"; // Ĭ��ģʽͼ��·��������ʵ����Դ·��������

        [Header("Ӣ�۰�ť���")]
        private List<Button> heroBtnList = new List<Button>(); // Ӣ��ѡ��ť�б�
        private Dictionary<int, Dictionary<int, SkinData>> heroSkinDic = new Dictionary<int, Dictionary<int, SkinData>>();
        private Dictionary<int,int> heroSkinCount=new Dictionary<int, int>();
        private Dictionary<int,int> ChoosedHeroAndSkin =new Dictionary<int, int>();
        private int _curHeroId;      // ��ǰѡ��Ӣ��ID
        private int _curSkinId;      // ��ǰѡ��Ƥ��ID��Ĭ��ȡ��һ��Ƥ��)
        #endregion

        [Header("Ƥ���л�UI")]
        public Transform skinBtnGroup; // Ƥ����ť�����壨��������Ƥ���л���ť��
        private List<Button> _skinBtnList = new List<Button>(); // Ƥ����ť�б�

        #region ��������
        private readonly float fadeDuration = 0.3f; // ����ʱ��
        private readonly float moveDuration = 0.45f; // �ƶ�ʱ��
        private readonly int movePixel = 135; // �ƶ����������������ң�
        #endregion

        //����Ƥ����Դ{�������ܸ��ݰ汾����������Ƥ���������Դ����}
        private void LoadSkinResources()
        {
            heroSkinCount=SkinConfig.GetHeroSkinCountMap();
            foreach(var o in heroSkinCount)
            {
                StartCoroutine(ResMgr.PreLoadHeroSkinDic(o.Key,o.Value));
            }
        }

        //���Լ�����UIManager
        private void Awake()
        {
            UIManager._Instance.PanelAddSelf(this);
            Init();
            LoadSkinResources();
            ResMgr.PreLoadModesIcons();
        }
        public override void Init(params object[] args)
        {
            base.Init(args);
            // ��ʼ������Ҫ���ã�����UI�㼶����·����
            InitComponentRef();
            // ��ʼ��Ӣ�۰�ť����
            //InitHeroBtnListeners();
            // ��ʼ���ϴ�ѡ�е�Ӣ��Ƥ�������Ŷ���
            InitLastSelectedHeroSkin();
            // ������ѡ��ҳ�洫ʱ���嶥��ǰƥ��/�ȴ�״̬
            ResetLanQuickMatchState();
            InitMatchController();
            InitBtnClick();
            InitMsgListener();
        }
        #region ��ʼ��Ӣ�۰�ť����
        private void InitHeroBtnListeners()
        {

        }
        #endregion

        #region �����ʼ��
        private void InitComponentRef()
        {
            loadPanel = new GameObject("GameLoadPanel");
            loadPanel.AddComponent<GameLoadPanel>();
            loadPanel.GetComponent<GameLoadPanel>().Init(loadPanel.transform);
            DontDestroyOnLoad(loadPanel);

            //����־û�
            IsPersistence = true;

            // �������
            heroSkinSpriteBg = transform.Find("ChooseHeroPanel/HeroSkinBG").GetComponent<Image>();
            heroSkinSprite = transform.Find("ChooseHeroPanel/HeroSkinBG/HeroSkinSprite").GetComponent<Image>();
            heroSkinRect = heroSkinSprite.GetComponent<RectTransform>();
            heroSkinCanvasGroup = heroSkinSprite.GetComponent<CanvasGroup>();
            if (heroSkinCanvasGroup == null)
            {
                heroSkinCanvasGroup = heroSkinSprite.gameObject.AddComponent<CanvasGroup>();
            }
            heroSkinCanvasGroup.alpha = 0; // ��ʼ͸��

            // ģʽͼ��
            modeIcon = transform.Find("ChooseHeroPanel/ModesIconSprite").GetComponent<Image>();

            // Ӣ�۰�ť��ʾ�������谴ť��HeroBtnGroup�£�����ΪHeroBtn_1��HeroBtn_2...��
            Transform heroBtnGroup = transform.Find("ChooseHeroPanel/SecondPanel/SecondBg_01/Scroll View/Viewport/Content");
            /*foreach (Transform btnTrans in heroBtnGroup)
            {
                Button heroBtn = btnTrans.Find("HeroIcon").GetComponent<Button>();
                if (heroBtn != null)
                {
                    heroBtnList.Add(heroBtn);
                    // ���谴ť����Я��Ӣ��ID����HeroBtn_01��
                    if (int.TryParse(btnTrans.name.Split('_')[1], out int heroId))
                    {
                        // �󶨰�ť����¼�����Ӣ��ID��
                        heroBtn.onClick.AddListener(() => OnHeroBtnClick(heroId));
                    }
                }
            }

            // ��ʼ��Ӣ��Ƥ��·��ӳ�䣨����ʵ����Դ·�����ã�
            InitHeroSkinPathDic();
            // ========== ������Ƥ����ť��ʼ�� ==========
            if (skinBtnGroup != null)
            {
                foreach (Transform btnTrans in skinBtnGroup)
                {
                    Button skinBtn = btnTrans.GetComponent<Button>();
                    if (skinBtn != null)
                    {
                        _skinBtnList.Add(skinBtn);

                        // ����Ƥ����ť����Ϊ SkinBtn_10101��Ӣ��101��Ƥ��10101��
                        if (int.TryParse(btnTrans.name.Split('_')[1], out int skinId))
                        {
                            // ��Ƥ����ť����¼�
                            skinBtn.onClick.AddListener(() => OnSkinBtnClick(skinId));

                            // ��ѡ����ʼ��ʱ����δ����Ƥ����ť/�û�
                            skinBtn.interactable = IsSkinUnlocked(skinId);
                            if (!skinBtn.interactable)
                            {
                                Image btnImage = skinBtn.GetComponent<Image>();
                                if (btnImage != null)
                                {
                                    btnImage.color = Color.gray; // δ�����û�
                                }
                            }
                        }
                    }
                }
            }*/
        }
        /// <summary>
        /// ��ʼ��Ӣ��-Ƥ��ӳ�䣨��Ƥ���汾��
        /// </summary>
        private void InitHeroSkinPathDic()
        {
            // �ɼ������Ӹ���Ӣ��...
        }
        private void InitBtnClick()
        {
            if (ClosePageBtn == null) Debug.LogError("[ChooseHeroPanel] ClosePageBtn 未在 Inspector 中赋值！");
            if (GameStartBtn == null) Debug.LogError("[ChooseHeroPanel] GameStartBtn 未在 Inspector 中赋值！");
            if (CancelMatchBtn == null) Debug.LogError("[ChooseHeroPanel] CancelMatchBtn 未在 Inspector 中赋值！");

            if (ClosePageBtn != null) ClosePageBtn.onClick.AddListener(OnCloseBtnClick);
            if (GameStartBtn != null) GameStartBtn.onClick.AddListener(onStartMatchClick);
            if (CancelMatchBtn != null) CancelMatchBtn.onClick.AddListener(onCancelBtnClick);
        }
        private void InitMsgListener()
        {
            NetWorkMgr.AddMsgListener("MsgMatchSuccess", OnMsgMatchSuccess);

        }

        private void InitMatchController()
        {
            matchController = new HeroSelectionMatchController(
                () => PlayerBasicInfoMgr.Instance.CurrentNetworkMode,
                () => PlayerBasicInfoMgr.Instance.GetID(),
                NetWorkMgr.Send,
                StartLanQuickMatch,
                () => LanQuickMatchManager.Instance?.CancelMatch(),
                StartOnlineMatch,
                () => OnlineMatchManager.Instance?.CancelMatch());
        }
        #endregion

        #region ģʽͼ�����
        private void LoadModeIcon(GameModes mode)
        {
            GameMode = mode;
            // ����Ĭ��ģʽͼ�꣨����չ�����ݵ�ǰ��Ϸģʽ���ض�Ӧͼ�꣩
            Sprite modeSprite = ResMgr.LoadModeIcon(mode);
            if (modeSprite != null)
            {
                modeIcon.sprite = modeSprite;
                modeIcon.SetNativeSize();
            }
        }
        #endregion

        #region Ӣ��Ƥ�������붯��
        /// <summary>
        /// ��ʼ���ϴ�ѡ�е�Ӣ��Ƥ����������֧�ֶ�Ƥ����
        /// </summary>
        private void InitLastSelectedHeroSkin()
        {
            // �ӱ��ػ����ȡ�ϴ�ѡ�е�Ӣ��ID/Ƥ��ID������ȡĬ�ϣ�
            _curHeroId = PlayerBasicInfoMgr.Instance.HeroCache.heroId;
            _curSkinId = PlayerBasicInfoMgr.Instance.HeroCache.skinId;
        }
        private void LastSelectHeroAnim()
        {
            // �����ϴ�ѡ�е�Ƥ�������Ŷ���
            LoadHeroSkinAndPlayAnim(_curHeroId, _curSkinId);

            // ��ʼ��Ƥ����ťѡ��̬
            //UpdateSkinBtnSelectState(_curSkinId);
        }

        /// <summary>
        /// ����Ӣ��ָ��Ƥ�������Ŷ�����������֧��ָ��Ƥ��ID��
        /// </summary>
        private void LoadHeroSkinAndPlayAnim(int heroId, int skinId)
        {
            /*if (!heroSkinDic.ContainsKey(heroId) || !heroSkinDic[heroId].ContainsKey(skinId))
            {
                Debug.LogError($"Ӣ��{heroId}��Ƥ��{skinId}������");
                return;
            }

            // ��ȡƤ������
            SkinData skinData = heroSkinDic[heroId][skinId];*/

            // 1. ����Ƥ��ͼƬ������ԭ��ResMgr��
            Sprite skinSprite = ResMgr.GetPoster(heroId,skinId);
            Sprite skinSpriteBg= ResMgr.GetPosterBg(heroId, skinId);
            if (skinSprite == null || skinSpriteBg==null )
            {
                Debug.LogError($"����Ƥ��ʧ��");
                return;
            }
            heroSkinSprite.sprite = skinSprite;
            heroSkinSpriteBg.sprite = skinSpriteBg;
            heroSkinSprite.SetNativeSize();
            heroSkinSpriteBg.SetNativeSize();

            // 2. ��������λ�ú�͸���ȣ�ԭ���߼���
            ResetHeroSkinTransform();

            // 3. ���Ž���+�ƶ�������ԭ���߼���
            StartCoroutine(HeroSkinMoveAndFadeAnim());
        }

        /// <summary>
        /// ���������λ�ú�͸���ȣ�����ǰ׼����
        /// </summary>
        private void ResetHeroSkinTransform()
        {
            // ��ʼλ�ã�����ƫ��100���أ�Ϊ���������ƶ���׼����
            heroSkinRect.anchoredPosition = new Vector2(-movePixel, heroSkinRect.anchoredPosition.y);
            heroSkinCanvasGroup.alpha = 0;
        }
        /// <summary>
        /// ���Ƥ���Ƿ����������������
        /// </summary>
        private bool IsSkinUnlocked(int skinId)
        {
            // ����Ƥ��ID��ǰ��λ=Ӣ��ID��ʾ�����ɰ���Ĺ��������
            int heroId = skinId / 100; // 10101 �� 101��10202 �� 102

            if (heroSkinDic.ContainsKey(heroId) && heroSkinDic[heroId].ContainsKey(skinId))
            {
                return heroSkinDic[heroId][skinId].isUnlocked;
            }
            return false;
        }
        /// <summary>
        /// ��ȡӢ�۵�Ĭ��Ƥ��ID������������
        /// </summary>
        private int GetHeroDefaultSkinId(int heroId)
        {
            if (heroSkinDic.ContainsKey(heroId))
            {
                // ȡ��һ��������Ƥ����ΪĬ��
                foreach (var skinKvp in heroSkinDic[heroId])
                {
                    if (skinKvp.Value.isUnlocked)
                    {
                        return skinKvp.Key;
                    }
                }
            }
            return 0;
        }
        /// <summary>
        /// ����Ƥ����ťѡ��̬��������ǰѡ�е�Ƥ����
        /// </summary>
        private void UpdateSkinBtnSelectState(int selectSkinId)
        {
            foreach (var skinBtn in _skinBtnList)
            {
                // ������ť�󶨵�Ƥ��ID
                if (int.TryParse(skinBtn.gameObject.name.Split('_')[1], out int btnSkinId))
                {
                    // ѡ��̬������/����/��ɫ��ʾ��������ɫ���֣�
                    Image btnImage = skinBtn.GetComponent<Image>();
                    if (btnImage != null)
                    {
                        btnImage.color = (btnSkinId == selectSkinId) ? Color.white : Color.gray;
                    }
                }
            }
        }
        /// <summary>
        /// Ƥ����ť����¼����л�Ƥ����
        /// </summary>
        private void OnSkinBtnClick(int skinId)
        {
            // ������ǰƤ����Ӧ��Ӣ��ID
            int heroId = skinId / 100;

            // У�飺Ӣ��/Ƥ���Ƿ���� + Ƥ���Ƿ����
            if (!heroSkinDic.ContainsKey(heroId) || !heroSkinDic[heroId].ContainsKey(skinId) || !IsSkinUnlocked(skinId))
            {
                Debug.LogWarning($"Ƥ��{skinId}�����ã�δ����/�����ڣ�");
                return;
            }

            // ���µ�ǰѡ�е�Ƥ��ID
            _curSkinId = skinId;

            // ����ѡ�е�Ƥ�������Ŷ���������ԭ�ж����߼���
            LoadHeroSkinAndPlayAnim(heroId, skinId);

            // ��ѡ������Ƥ����ťѡ��̬�����������ǰѡ�е�Ƥ����ť��
            UpdateSkinBtnSelectState(skinId);
        }
        /// <summary>
        /// Ӣ��Ƥ������+�ƶ�����Э��
        /// </summary>
        /// <returns></returns>
        private IEnumerator HeroSkinMoveAndFadeAnim()
        {
            float elapsedTime = 0;
            Vector2 startPos = heroSkinRect.anchoredPosition;
            Vector2 targetPos = new Vector2(0, startPos.y); // Ŀ��λ�ã�ԭλ�ã������ƶ�100���أ�

            while (elapsedTime < moveDuration)
            {
                // �ƶ��߼���0.5�룩
                float moveT = elapsedTime / moveDuration;
                heroSkinRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, moveT);

                // �����߼���0.3������ɣ�
                if (elapsedTime < fadeDuration)
                {
                    float fadeT = elapsedTime / fadeDuration;
                    heroSkinCanvasGroup.alpha = Mathf.Lerp(0, 1, fadeT);
                }
                else
                {
                    heroSkinCanvasGroup.alpha = 1;
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // ������������������״̬
            heroSkinRect.anchoredPosition = targetPos;
            heroSkinCanvasGroup.alpha = 1;
        }
        #endregion

        #region ��ť�¼�
        /// <summary>
        /// Ӣ�۰�ť����¼����������л�Ӣ�ۺ�����Ƥ��ΪĬ�ϣ�
        /// </summary>
        private void OnHeroBtnClick(int heroId)
        {
            if (_curHeroId == heroId) return;

            // ���µ�ǰӢ��ID
            _curHeroId = heroId;
            // ���õ�ǰƤ��Ϊ��Ӣ�۵�Ĭ��Ƥ��
            _curSkinId = GetHeroDefaultSkinId(heroId);

            // ���浽���ػ���
            PlayerPrefs.SetInt("LastSelectedHeroId", _curHeroId);
            PlayerPrefs.SetInt($"LastSelectedSkinId_{_curHeroId}", _curSkinId);
            PlayerPrefs.Save();

            // ����Ӣ��Ĭ��Ƥ�������Ŷ���
            LoadHeroSkinAndPlayAnim(_curHeroId, _curSkinId);

            // ����Ƥ����ťѡ��̬
            UpdateSkinBtnSelectState(_curSkinId);
        }
        #endregion

        #region ����������ڣ�����BasePanel��
        public override void Open(params object[] args)
        {
            LastSelectHeroAnim();
            GameModes mode = (GameModes)args[0];
            LoadModeIcon(mode);
        }

        public override void Close(params object[] args)
        {
            // �������ر��߼�����ʵ��
        }

        private void OnDestroy()
        {
            ResMgr.UnloadUnusedHeroPoster();
            // ������ť��������ѡ��
            foreach (var btn in heroBtnList)
            {
                btn.onClick.RemoveAllListeners();
            }
        }
        #endregion
        #region //ҳ������¼���ť��
        private void OnCloseBtnClick()
        {
            Debug.Log("[ChooseHeroPanel] 点击关闭按钮，退出当前匹配状态");
            matchController?.CancelMatch(GameMode);
            UIManager._Instance.ClosePanel<ChooseHeroPanel>();
        }

        private void onStartMatchClick()
        {
            Debug.Log($"[ChooseHeroPanel] onStartMatchClick 被点击, GameMode={GameMode}, heroId={_curHeroId}, skinId={_curSkinId}");
            HeroMatchStartResult result = matchController.StartMatch(GameMode, _curHeroId, _curSkinId);
            if (result == HeroMatchStartResult.UnsupportedNetworkMode)
            {
                Debug.LogWarning($"[ChooseHeroPanel] 未知网络模式: {PlayerBasicInfoMgr.Instance.CurrentNetworkMode}，无法开始匹配");
            }
        }

        private void onCancelBtnClick()
        {
            matchController.CancelMatch(GameMode);
        }

        private void StartLanQuickMatch(GameModes mode, int heroId, int skinId)
        {
            LanQuickMatchManager lanMgr = FindObjectOfType<LanQuickMatchManager>();
            if (lanMgr == null)
            {
                GameObject go = new GameObject("LanQuickMatchManager");
                lanMgr = go.AddComponent<LanQuickMatchManager>();
                DontDestroyOnLoad(go);
            }
            lanMgr.StartQuickMatch(mode, heroId, skinId);
        }

        private void StartOnlineMatch(GameModes mode, int heroId, int skinId)
        {
            OnlineMatchManager onlineMgr = FindObjectOfType<OnlineMatchManager>();
            if (onlineMgr == null)
            {
                GameObject go = new GameObject("OnlineMatchManager");
                onlineMgr = go.AddComponent<OnlineMatchManager>();
                DontDestroyOnLoad(go);
            }
            onlineMgr.StartQuickMatch(mode, heroId, skinId);
        }

        private void ResetLanQuickMatchState()
        {
            LanQuickMatchManager lanMgr = FindObjectOfType<LanQuickMatchManager>();
            if (lanMgr != null && (lanMgr.IsMatching || lanMgr.IsWaiting))
            {
                Debug.Log("[ChooseHeroPanel] 重置局域网匹配状态");
                lanMgr.CancelMatch();
            }
        }

        #endregion

        public override void OnClose()
        {
            NetWorkMgr.RemoveMsgListener("MsgMatchSuccess", OnMsgMatchSuccess);
        }

        #region Э���������
        public void OnMsgMatchSuccess(MsgBase ms)
        {
            MsgMatchSuccess msg = (MsgMatchSuccess)ms;
            Debug.Log("ƥ��ɹ�");
            PlayerBasicInfoMgr.Instance.UpdateRoomID(msg.roomId);
            string myBattleId = PlayerBasicInfoMgr.Instance.GetBattleId();
            foreach(var player in msg.playerInfos)
            {
                if(player.userId == myBattleId)
                {
                    PlayerBasicInfoMgr.Instance.UpdateTeamId(player.teamId);
                    break;
                }
            }
            loadPanel.GetComponent<GameLoadPanel>().LoadGame(msg.playerInfos);


        }
        #endregion
    }
    public enum HeroMatchStartResult
    {
        LocalServer,
        QuickMatch,
        OnlineMatch,
        UnsupportedNetworkMode
    }

    public sealed class HeroSelectionMatchController
    {
        private readonly System.Func<NetworkMode> getNetworkMode;
        private readonly System.Func<string> getUserId;
        private readonly System.Action<MsgBase> sendMessage;
        private readonly System.Action<GameModes, int, int> startQuickMatch;
        private readonly System.Action cancelQuickMatch;
        private readonly System.Action<GameModes, int, int> startOnlineMatch;
        private readonly System.Action cancelOnlineMatch;

        public HeroSelectionMatchController(
            System.Func<NetworkMode> getNetworkMode,
            System.Func<string> getUserId,
            System.Action<MsgBase> sendMessage,
            System.Action<GameModes, int, int> startQuickMatch,
            System.Action cancelQuickMatch,
            System.Action<GameModes, int, int> startOnlineMatch,
            System.Action cancelOnlineMatch)
        {
            this.getNetworkMode = getNetworkMode ?? throw new System.ArgumentNullException(nameof(getNetworkMode));
            this.getUserId = getUserId ?? throw new System.ArgumentNullException(nameof(getUserId));
            this.sendMessage = sendMessage ?? throw new System.ArgumentNullException(nameof(sendMessage));
            this.startQuickMatch = startQuickMatch ?? throw new System.ArgumentNullException(nameof(startQuickMatch));
            this.cancelQuickMatch = cancelQuickMatch ?? throw new System.ArgumentNullException(nameof(cancelQuickMatch));
            this.startOnlineMatch = startOnlineMatch ?? throw new System.ArgumentNullException(nameof(startOnlineMatch));
            this.cancelOnlineMatch = cancelOnlineMatch ?? throw new System.ArgumentNullException(nameof(cancelOnlineMatch));
        }

        public HeroMatchStartResult StartMatch(GameModes mode, int heroId, int skinId)
        {
            NetworkMode networkMode = getNetworkMode();
            if (networkMode == NetworkMode.LocalServer)
            {
                sendMessage(BuildLocalMatchRequest(mode, heroId, getUserId()));
                return HeroMatchStartResult.LocalServer;
            }

            if (UsesQuickMatch(networkMode))
            {
                startQuickMatch(mode, heroId, skinId);
                return HeroMatchStartResult.QuickMatch;
            }

            if (UsesOnlineMatch(networkMode))
            {
                startOnlineMatch(mode, heroId, skinId);
                return HeroMatchStartResult.OnlineMatch;
            }

            return HeroMatchStartResult.UnsupportedNetworkMode;
        }

        public void CancelMatch(GameModes mode)
        {
            NetworkMode networkMode = getNetworkMode();
            if (networkMode == NetworkMode.LocalServer)
            {
                sendMessage(BuildExitRequest(mode, getUserId()));
                return;
            }

            if (UsesOnlineMatch(networkMode))
            {
                cancelOnlineMatch();
                return;
            }

            cancelQuickMatch();
        }

        public static bool UsesQuickMatch(NetworkMode networkMode)
        {
            return networkMode == NetworkMode.LanClient
                || networkMode == NetworkMode.LanHost;
        }

        public static bool UsesOnlineMatch(NetworkMode networkMode)
        {
            return networkMode == NetworkMode.SupabaseOnline;
        }

        public static MsgMatchRequest BuildLocalMatchRequest(GameModes mode, int heroId, string userIdText)
        {
            return new MsgMatchRequest
            {
                GameModes = mode,
                playerPack = new List<PlayerChooseCache>
                {
                    new PlayerChooseCache
                    {
                        userId = ParseUserIdOrZero(userIdText),
                        selectedHeroId = heroId
                    }
                }
            };
        }

        public static MsgExitRequest BuildExitRequest(GameModes mode, string userIdText)
        {
            return new MsgExitRequest
            {
                mode = mode,
                PlayerList = new List<int> { ParseUserIdOrZero(userIdText) }
            };
        }

        public static int ParseUserIdOrZero(string userIdText)
        {
            return int.TryParse(userIdText, out int userId) ? userId : 0;
        }
    }

    // OnlineMatchManager moved to NetWorkScripts/OnlineMatch/OnlineMatchManager.cs.
    // Keep the legacy implementation out of the build to avoid duplicate Panels types.
#if false
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

        public bool IsMatching => isMatching;
        public bool IsWaiting => isWaiting;

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
            if (isMatching || isWaiting)
            {
                Debug.LogWarning($"[OnlineMatchManager] 当前正在在线匹配或等待中，isMatching={isMatching}, isWaiting={isWaiting}");
                return;
            }

            currentAccessToken = SupabaseBackendProvider.GetSavedAccessToken();
            if (string.IsNullOrWhiteSpace(currentAccessToken))
            {
                Debug.LogWarning("[OnlineMatchManager] 缺少 Supabase access token，请先登录后再进行在线匹配");
                return;
            }

            PlayerBasicInfoMgr.Instance.CurrentNetworkMode = NetworkMode.SupabaseOnline;
            PlayerBasicInfoMgr.Instance.SetCurrentGamemode(mode);
            PlayerBasicInfoMgr.Instance.UpdateHeroCache(heroId, skinId);

            isMatching = true;
            isWaiting = true;
            cancelRequested = false;
            isPolling = false;
            pollGeneration++;
            currentTicketId = null;
            Debug.Log($"[OnlineMatchManager] 在线匹配入口已启动，mode={mode}, heroId={heroId}, skinId={skinId}");

            OnlineMatchApiResult<OnlineMatchResponse> result =
                await OnlineMatchApiClient.StartMatchAsync(currentAccessToken, mode, heroId, skinId);

            if (!isMatching || cancelRequested)
            {
                return;
            }

            if (!result.Success)
            {
                FailMatch($"启动在线匹配失败：{result.ErrorMessage}");
                return;
            }

            HandleMatchResponse(result.Data);
        }

        public async void CancelMatch()
        {
            if (!isMatching && !isWaiting)
            {
                return;
            }

            cancelRequested = true;
            string ticketId = currentTicketId;
            string accessToken = currentAccessToken;
            isMatching = false;
            isWaiting = false;
            isPolling = false;
            pollGeneration++;

            if (!string.IsNullOrWhiteSpace(ticketId) && !string.IsNullOrWhiteSpace(accessToken))
            {
                OnlineMatchApiResult<OnlineMatchCancelResponse> result =
                    await OnlineMatchApiClient.CancelMatchAsync(accessToken, ticketId);
                if (!result.Success)
                {
                    Debug.LogWarning($"[OnlineMatchManager] 取消在线匹配请求失败：{result.ErrorMessage}");
                }
            }

            currentTicketId = null;
            currentAccessToken = null;
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
                    isPolling = false;
                    return;
                }

                OnlineMatchApiResult<OnlineMatchResponse> result =
                    await OnlineMatchApiClient.GetStatusAsync(currentAccessToken, currentTicketId);
                if (!IsPollingActive(generation))
                {
                    isPolling = false;
                    return;
                }

                if (!result.Success)
                {
                    FailMatch($"轮询在线匹配状态失败：{result.ErrorMessage}");
                    return;
                }

                HandleMatchResponse(result.Data);
            }

            isPolling = false;
        }

        private bool IsPollingActive(int generation)
        {
            return generation == pollGeneration
                && isMatching
                && isWaiting
                && !cancelRequested
                && !string.IsNullOrWhiteSpace(currentTicketId);
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
                isMatching = false;
                isWaiting = false;
                isPolling = false;
                pollGeneration++;
                currentTicketId = null;
                PlayerBasicInfoMgr.Instance.UpdateRoomID(response.RoomId);
                Debug.Log($"[OnlineMatchManager] 在线匹配成功，roomId={response.RoomId}, role={response.Role}, connectionMode={SupabaseConfig.Instance.OnlineConnectionMode}, provider={response.Room?["relay_provider"]}");
                _ = StartMatchedOnlineRoomAsync(response);
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

        private async System.Threading.Tasks.Task StartMatchedOnlineRoomAsync(OnlineMatchResponse response)
        {
            bool started = await OnlineConnectionLauncher.StartMatchedRoomAsync(
                response,
                PlayerBasicInfoMgr.Instance.GameMode,
                PlayerBasicInfoMgr.Instance.HeroCache.heroId,
                PlayerBasicInfoMgr.Instance.HeroCache.skinId);

            if (!started)
            {
                Debug.LogWarning("[OnlineMatchManager] 在线匹配已成功，但在线连接启动失败");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
#endif
}
