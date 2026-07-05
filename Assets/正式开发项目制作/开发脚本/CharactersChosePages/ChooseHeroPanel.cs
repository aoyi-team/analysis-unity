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
            LanQuickMatchManager.Instance?.CancelMatch();
            UIManager._Instance.ClosePanel<ChooseHeroPanel>();
        }

        private void onStartMatchClick()
        {
            Debug.Log($"[ChooseHeroPanel] onStartMatchClick 被点击, GameMode={GameMode}, heroId={_curHeroId}, skinId={_curSkinId}");
            NetworkMode netMode = PlayerBasicInfoMgr.Instance.CurrentNetworkMode;
            int heroId = _curHeroId;
            int skinId = _curSkinId;

            switch (netMode)
            {
                case NetworkMode.LocalServer:
                    // ԭ������������
                    MsgMatchRequest msg = new MsgMatchRequest();
                    msg.GameModes = GameMode;

                    string userIdStr = PlayerBasicInfoMgr.Instance.GetID();
                    int userId = 0;
                    if (!int.TryParse(userIdStr, out userId))
                    {
                        Debug.LogWarning($"[ChooseHeroPanel] 本地服务器模式下玩家ID无效：'{userIdStr}'，请检查登录是否成功");
                    }

                    msg.playerPack = new List<PlayerChooseCache>
                    {
                        new PlayerChooseCache()
                        {
                            userId = userId,
                            selectedHeroId = heroId
                        }
                    };
                    NetWorkMgr.Send(msg);
                    break;

                case NetworkMode.LanClient:
                case NetworkMode.LanHost:
                case NetworkMode.SupabaseOnline:
                    Debug.Log($"[ChooseHeroPanel] 开始局域网匹配, netMode={netMode}, mode={GameMode}");
                    StartLanQuickMatch(heroId, skinId);
                    break;

                default:
                    Debug.LogWarning($"[ChooseHeroPanel] 未知网络模式: {netMode}，无法开始匹配");
                    break;
            }
        }

        private void onCancelBtnClick()
        {
            NetworkMode netMode = PlayerBasicInfoMgr.Instance.CurrentNetworkMode;
            if (netMode == NetworkMode.LocalServer)
            {
                MsgExitRequest msg = new MsgExitRequest();
                msg.mode = GameMode;
                msg.PlayerList = new List<int>() { int.Parse(PlayerBasicInfoMgr.Instance.GetID()) };
                NetWorkMgr.Send(msg);
            }
            else
            {
                LanQuickMatchManager.Instance?.CancelMatch();
            }
        }

        private void StartLanQuickMatch(int heroId, int skinId)
        {
            LanQuickMatchManager lanMgr = FindObjectOfType<LanQuickMatchManager>();
            if (lanMgr == null)
            {
                GameObject go = new GameObject("LanQuickMatchManager");
                lanMgr = go.AddComponent<LanQuickMatchManager>();
                DontDestroyOnLoad(go);
            }
            lanMgr.StartQuickMatch(GameMode, heroId, skinId);
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
}
