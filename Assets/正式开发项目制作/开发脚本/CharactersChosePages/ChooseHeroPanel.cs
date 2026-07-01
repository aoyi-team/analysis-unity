using BaseClasses;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using static Cinemachine.CinemachineTriggerAction.ActionSettings;
using MsgFramework;

/// <summary>
/// 游戏模式排位=0，赏金=1
/// </summary>
public enum GameModes
{
    paiwei = 0, shengcun = 1, dantiao = 2
}
namespace Panels
{
    /// <summary>
    /// 英雄选择面板（挂载在LobbyScene中，无需Open/Close面板逻辑）
    /// </summary>
    public class ChooseHeroPanel : BasePanel
    {
        [Header("加载界面配置")]
        private GameObject loadPanel;

        [Header("页面相关引用")]
        public Button ClosePageBtn;
        public Button GameStartBtn;
        public Button CancelMatchBtn;

        #region 组件引用
        [Header("英雄立绘相关")]
        private Image heroSkinSprite; // 英雄皮肤立绘
        private Image heroSkinSpriteBg;//英雄皮肤海报背景
        private RectTransform heroSkinRect; // 立绘RectTransform（用于移动）
        private CanvasGroup heroSkinCanvasGroup; // 用于渐显

        [Header("职业切换按钮")]
        private List<Button> CareerBtns;//职业选择按钮
        private List<Image> CareerChoosedBtn;//被选择时的图标

        [Header("模式图标相关")]
        private Image modeIcon; // 模式图标
        private GameModes GameMode;
        //private string defaultModeIconPath = "ModeIcons/Default"; // 默认模式图标路径（根据实际资源路径调整）

        [Header("英雄按钮相关")]
        private List<Button> heroBtnList = new List<Button>(); // 英雄选择按钮列表
        private Dictionary<int, Dictionary<int, SkinData>> heroSkinDic = new Dictionary<int, Dictionary<int, SkinData>>();
        private Dictionary<int,int> heroSkinCount=new Dictionary<int, int>();
        private Dictionary<int,int> ChoosedHeroAndSkin =new Dictionary<int, int>();
        private int _curHeroId;      // 当前选中英雄ID
        private int _curSkinId;      // 当前选中皮肤ID（默认取第一个皮肤)
        #endregion

        [Header("皮肤切换UI")]
        public Transform skinBtnGroup; // 皮肤按钮父物体（挂载所有皮肤切换按钮）
        private List<Button> _skinBtnList = new List<Button>(); // 皮肤按钮列表

        #region 动画参数
        private readonly float fadeDuration = 0.3f; // 渐显时长
        private readonly float moveDuration = 0.45f; // 移动时长
        private readonly int movePixel = 135; // 移动像素数（从左往右）
        #endregion

        //加载皮肤资源{后续可能根据版本内容来进行皮肤数组的资源载入}
        private void LoadSkinResources()
        {
            heroSkinCount=SkinConfig.GetHeroSkinCountMap();
            foreach(var o in heroSkinCount)
            {
                StartCoroutine(ResMgr.PreLoadHeroSkinDic(o.Key,o.Value));
            }
        }

        //把自己塞入UIManager
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
            // 初始化组件引用（根据UI层级调整路径）
            InitComponentRef();
            // 初始化英雄按钮监听
            //InitHeroBtnListeners();
            // 初始化上次选中的英雄皮肤并播放动画
            InitLastSelectedHeroSkin();
            InitBtnClick();
            InitMsgListener();
        }
        #region 初始化英雄按钮监听
        private void InitHeroBtnListeners()
        {

        }
        #endregion

        #region 组件初始化
        private void InitComponentRef()
        {
            loadPanel = new GameObject("GameLoadPanel");
            loadPanel.AddComponent<GameLoadPanel>();
            loadPanel.GetComponent<GameLoadPanel>().Init(loadPanel.transform);
            DontDestroyOnLoad(loadPanel);

            //组件持久化
            IsPersistence = true;

            // 立绘组件
            heroSkinSpriteBg = transform.Find("ChooseHeroPanel/HeroSkinBG").GetComponent<Image>();
            heroSkinSprite = transform.Find("ChooseHeroPanel/HeroSkinBG/HeroSkinSprite").GetComponent<Image>();
            heroSkinRect = heroSkinSprite.GetComponent<RectTransform>();
            heroSkinCanvasGroup = heroSkinSprite.GetComponent<CanvasGroup>();
            if (heroSkinCanvasGroup == null)
            {
                heroSkinCanvasGroup = heroSkinSprite.gameObject.AddComponent<CanvasGroup>();
            }
            heroSkinCanvasGroup.alpha = 0; // 初始透明

            // 模式图标
            modeIcon = transform.Find("ChooseHeroPanel/ModesIconSprite").GetComponent<Image>();

            // 英雄按钮（示例：假设按钮在HeroBtnGroup下，命名为HeroBtn_1、HeroBtn_2...）
            Transform heroBtnGroup = transform.Find("ChooseHeroPanel/SecondPanel/SecondBg_01/Scroll View/Viewport/Content");
            /*foreach (Transform btnTrans in heroBtnGroup)
            {
                Button heroBtn = btnTrans.Find("HeroIcon").GetComponent<Button>();
                if (heroBtn != null)
                {
                    heroBtnList.Add(heroBtn);
                    // 假设按钮名称携带英雄ID（如HeroBtn_01）
                    if (int.TryParse(btnTrans.name.Split('_')[1], out int heroId))
                    {
                        // 绑定按钮点击事件（传英雄ID）
                        heroBtn.onClick.AddListener(() => OnHeroBtnClick(heroId));
                    }
                }
            }

            // 初始化英雄皮肤路径映射（根据实际资源路径配置）
            InitHeroSkinPathDic();
            // ========== 新增：皮肤按钮初始化 ==========
            if (skinBtnGroup != null)
            {
                foreach (Transform btnTrans in skinBtnGroup)
                {
                    Button skinBtn = btnTrans.GetComponent<Button>();
                    if (skinBtn != null)
                    {
                        _skinBtnList.Add(skinBtn);

                        // 假设皮肤按钮命名为 SkinBtn_10101（英雄101的皮肤10101）
                        if (int.TryParse(btnTrans.name.Split('_')[1], out int skinId))
                        {
                            // 绑定皮肤按钮点击事件
                            skinBtn.onClick.AddListener(() => OnSkinBtnClick(skinId));

                            // 可选：初始化时隐藏未解锁皮肤按钮/置灰
                            skinBtn.interactable = IsSkinUnlocked(skinId);
                            if (!skinBtn.interactable)
                            {
                                Image btnImage = skinBtn.GetComponent<Image>();
                                if (btnImage != null)
                                {
                                    btnImage.color = Color.gray; // 未解锁置灰
                                }
                            }
                        }
                    }
                }
            }*/
        }
        /// <summary>
        /// 初始化英雄-皮肤映射（多皮肤版本）
        /// </summary>
        private void InitHeroSkinPathDic()
        {
            // 可继续添加更多英雄...
        }
        private void InitBtnClick()
        {
            ClosePageBtn.onClick.AddListener(OnCloseBtnClick);
            GameStartBtn.onClick.AddListener(onStartMatchClick);
            CancelMatchBtn.onClick.AddListener(onCancelBtnClick);
        }
        private void InitMsgListener()
        {
            NetWorkMgr.AddMsgListener("MsgMatchSuccess", OnMsgMatchSuccess);

        }
        #endregion

        #region 模式图标加载
        private void LoadModeIcon(GameModes mode)
        {
            GameMode = mode;
            // 加载默认模式图标（可扩展：根据当前游戏模式加载对应图标）
            Sprite modeSprite = ResMgr.LoadModeIcon(mode);
            if (modeSprite != null)
            {
                modeIcon.sprite = modeSprite;
                modeIcon.SetNativeSize();
            }
        }
        #endregion

        #region 英雄皮肤加载与动画
        /// <summary>
        /// 初始化上次选中的英雄皮肤（升级：支持多皮肤）
        /// </summary>
        private void InitLastSelectedHeroSkin()
        {
            // 从本地缓存获取上次选中的英雄ID/皮肤ID（无则取默认）
            _curHeroId = PlayerBasicInfoMgr.Instance.HeroCache.heroId;
            _curSkinId = PlayerBasicInfoMgr.Instance.HeroCache.skinId;
        }
        private void LastSelectHeroAnim()
        {
            // 加载上次选中的皮肤并播放动画
            LoadHeroSkinAndPlayAnim(_curHeroId, _curSkinId);

            // 初始化皮肤按钮选中态
            //UpdateSkinBtnSelectState(_curSkinId);
        }

        /// <summary>
        /// 加载英雄指定皮肤并播放动画（升级：支持指定皮肤ID）
        /// </summary>
        private void LoadHeroSkinAndPlayAnim(int heroId, int skinId)
        {
            /*if (!heroSkinDic.ContainsKey(heroId) || !heroSkinDic[heroId].ContainsKey(skinId))
            {
                Debug.LogError($"英雄{heroId}的皮肤{skinId}不存在");
                return;
            }

            // 获取皮肤数据
            SkinData skinData = heroSkinDic[heroId][skinId];*/

            // 1. 加载皮肤图片（复用原有ResMgr）
            Sprite skinSprite = ResMgr.GetPoster(heroId,skinId);
            Sprite skinSpriteBg= ResMgr.GetPosterBg(heroId, skinId);
            if (skinSprite == null || skinSpriteBg==null )
            {
                Debug.LogError($"加载皮肤失败");
                return;
            }
            heroSkinSprite.sprite = skinSprite;
            heroSkinSpriteBg.sprite = skinSpriteBg;
            heroSkinSprite.SetNativeSize();
            heroSkinSpriteBg.SetNativeSize();

            // 2. 重置立绘位置和透明度（原有逻辑）
            ResetHeroSkinTransform();

            // 3. 播放渐显+移动动画（原有逻辑）
            StartCoroutine(HeroSkinMoveAndFadeAnim());
        }

        /// <summary>
        /// 重置立绘的位置和透明度（动画前准备）
        /// </summary>
        private void ResetHeroSkinTransform()
        {
            // 初始位置：向左偏移100像素（为从左往右移动做准备）
            heroSkinRect.anchoredPosition = new Vector2(-movePixel, heroSkinRect.anchoredPosition.y);
            heroSkinCanvasGroup.alpha = 0;
        }
        /// <summary>
        /// 检查皮肤是否解锁（辅助方法）
        /// </summary>
        private bool IsSkinUnlocked(int skinId)
        {
            // 解析皮肤ID：前两位=英雄ID（示例，可按你的规则调整）
            int heroId = skinId / 100; // 10101 → 101，10202 → 102

            if (heroSkinDic.ContainsKey(heroId) && heroSkinDic[heroId].ContainsKey(skinId))
            {
                return heroSkinDic[heroId][skinId].isUnlocked;
            }
            return false;
        }
        /// <summary>
        /// 获取英雄的默认皮肤ID（辅助方法）
        /// </summary>
        private int GetHeroDefaultSkinId(int heroId)
        {
            if (heroSkinDic.ContainsKey(heroId))
            {
                // 取第一个解锁的皮肤作为默认
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
        /// 更新皮肤按钮选中态（高亮当前选中的皮肤）
        /// </summary>
        private void UpdateSkinBtnSelectState(int selectSkinId)
        {
            foreach (var skinBtn in _skinBtnList)
            {
                // 解析按钮绑定的皮肤ID
                if (int.TryParse(skinBtn.gameObject.name.Split('_')[1], out int btnSkinId))
                {
                    // 选中态：高亮/缩放/变色（示例：用颜色区分）
                    Image btnImage = skinBtn.GetComponent<Image>();
                    if (btnImage != null)
                    {
                        btnImage.color = (btnSkinId == selectSkinId) ? Color.white : Color.gray;
                    }
                }
            }
        }
        /// <summary>
        /// 皮肤按钮点击事件（切换皮肤）
        /// </summary>
        private void OnSkinBtnClick(int skinId)
        {
            // 解析当前皮肤对应的英雄ID
            int heroId = skinId / 100;

            // 校验：英雄/皮肤是否存在 + 皮肤是否解锁
            if (!heroSkinDic.ContainsKey(heroId) || !heroSkinDic[heroId].ContainsKey(skinId) || !IsSkinUnlocked(skinId))
            {
                Debug.LogWarning($"皮肤{skinId}不可用（未解锁/不存在）");
                return;
            }

            // 更新当前选中的皮肤ID
            _curSkinId = skinId;

            // 加载选中的皮肤并播放动画（复用原有动画逻辑）
            LoadHeroSkinAndPlayAnim(heroId, skinId);

            // 可选：更新皮肤按钮选中态（比如高亮当前选中的皮肤按钮）
            UpdateSkinBtnSelectState(skinId);
        }
        /// <summary>
        /// 英雄皮肤渐显+移动动画协程
        /// </summary>
        /// <returns></returns>
        private IEnumerator HeroSkinMoveAndFadeAnim()
        {
            float elapsedTime = 0;
            Vector2 startPos = heroSkinRect.anchoredPosition;
            Vector2 targetPos = new Vector2(0, startPos.y); // 目标位置：原位置（向右移动100像素）

            while (elapsedTime < moveDuration)
            {
                // 移动逻辑（0.5秒）
                float moveT = elapsedTime / moveDuration;
                heroSkinRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, moveT);

                // 渐显逻辑（0.3秒内完成）
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

            // 动画结束后修正最终状态
            heroSkinRect.anchoredPosition = targetPos;
            heroSkinCanvasGroup.alpha = 1;
        }
        #endregion

        #region 按钮事件
        /// <summary>
        /// 英雄按钮点击事件（升级：切换英雄后重置皮肤为默认）
        /// </summary>
        private void OnHeroBtnClick(int heroId)
        {
            if (_curHeroId == heroId) return;

            // 更新当前英雄ID
            _curHeroId = heroId;
            // 重置当前皮肤为该英雄的默认皮肤
            _curSkinId = GetHeroDefaultSkinId(heroId);

            // 保存到本地缓存
            PlayerPrefs.SetInt("LastSelectedHeroId", _curHeroId);
            PlayerPrefs.SetInt($"LastSelectedSkinId_{_curHeroId}", _curSkinId);
            PlayerPrefs.Save();

            // 加载英雄默认皮肤并播放动画
            LoadHeroSkinAndPlayAnim(_curHeroId, _curSkinId);

            // 更新皮肤按钮选中态
            UpdateSkinBtnSelectState(_curSkinId);
        }
        #endregion

        #region 面板生命周期（适配BasePanel）
        public override void Open(params object[] args)
        {
            LastSelectHeroAnim();
            GameModes mode = (GameModes)args[0];
            LoadModeIcon(mode);
        }

        public override void Close(params object[] args)
        {
            // 无需面板关闭逻辑，空实现
        }

        private void OnDestroy()
        {
            ResMgr.UnloadUnusedHeroPoster();
            // 清理按钮监听（可选）
            foreach (var btn in heroBtnList)
            {
                btn.onClick.RemoveAllListeners();
            }
        }
        #endregion
        #region //页面组件事件按钮绑定
        private void OnCloseBtnClick()
        {
            UIManager._Instance.ClosePanel<ChooseHeroPanel>();
        }
        private void onStartMatchClick()
        {
            MsgMatchRequest msg=new MsgMatchRequest();
            msg.GameModes = GameMode;
            msg.playerPack = new List<PlayerChooseCache> { new PlayerChooseCache() { userId = int.Parse(PlayerBasicInfoMgr.Instance.GetID()), selectedHeroId = PlayerBasicInfoMgr.Instance.HeroCache.heroId} };
            NetWorkMgr.Send(msg);
        }
        private void onCancelBtnClick()
        {
            MsgExitRequest msg=new MsgExitRequest();
            msg.mode=GameMode;
            msg.PlayerList = new List<int>(){int.Parse(PlayerBasicInfoMgr.Instance.GetID())};
            NetWorkMgr.Send(msg);
        }
        #endregion

        public override void OnClose()
        {
            NetWorkMgr.RemoveMsgListener("MsgMatchSuccess", OnMsgMatchSuccess);
        }

        #region 协议监听处理
        public void OnMsgMatchSuccess(MsgBase ms)
        {
            MsgMatchSuccess msg = (MsgMatchSuccess)ms;
            Debug.Log("匹配成功");
            PlayerBasicInfoMgr.Instance.UpdateRoomID(msg.roomId);
            foreach(var player in msg.playerInfos)
            {
                if(player.userId == PlayerBasicInfoMgr.Instance.GetID())
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