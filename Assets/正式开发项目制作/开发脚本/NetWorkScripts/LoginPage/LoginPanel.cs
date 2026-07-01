using BaseClasses;
using MsgFramework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

namespace Panels
{

    public class LoginPanel : BasePanel//Panel组件就是挂载在PanelList下面的序列空物体，序列空物体下面挂载面板预制体
    {
        private Button DuoduoLoginBtn;//多多号登录切换按钮
        private Button NameLoginBtn;//角色名登录切换按钮
        private GameObject IdorNameCross;//检测ID或者名字是否为空
        private GameObject pwCross;//检测密码输入为空
        private Button LoginBtn;
        private Button RegisterBtn;
        private InputField idText;
        private InputField nameText;
        private InputField pwText;
        private int LoginWay = 0;//0-ID登录 1-角色名登录
        private bool pwCheck = false;//密码检测为空是否

        #region//加载场景绑定
        private int SceneNumber;
        private GameObject LoadScene;
        private Image LoadBar;
        private Text LoadPercentage;
        private float LoadAnimaFactor;
        private bool FirstTrigger = true;
        #endregion
        public override void Init(params object[] args)
        {
            PanelName = "LoginPanel";
            GameObject o = ResMgr.LoadPanelPrefabs(PanelName);
            PanelObj = Instantiate(o);
            PanelObj.name = "LoginPanel";
            PanelObj.transform.SetParent(gameObject.transform);
            IdorNameCross = transform.Find("LoginPanel/LoginPageBG/LoginPage/CrossOrTickImg/pwCross").gameObject;
            pwCross = transform.Find("LoginPanel/LoginPageBG/LoginPage/CrossOrTickImg/pwReCross").gameObject;
            NameLoginBtn = transform.Find("LoginPanel/LoginPageBG/TwoBtns/NameLoginButton").gameObject.GetComponent<Button>();
            DuoduoLoginBtn = transform.Find("LoginPanel/LoginPageBG/TwoBtns/IdLoginButton").gameObject.GetComponent<Button>();
            LoginBtn = transform.Find("LoginPanel/LoginBtn").GetComponent<Button>();
            LoadSceneInit();
            RegisterBtn = transform.Find("LoginPanel/LoginPageBG/RegBtn").GetComponent<Button>();
            idText = transform.Find("LoginPanel/LoginPageBG/LoginPage/IdInputFieldBG/DuodInputfield").GetComponent<InputField>();
            nameText = transform.Find("LoginPanel/LoginPageBG/LoginPage/NameInputfield/InputField").GetComponent<InputField>();
            pwText = transform.Find("LoginPanel/LoginPageBG/LoginPage/pwInputfieldBG/InputField").GetComponent<InputField>();
            gameObject.name = "LoginPage";
            LoginBtn.onClick.AddListener(OnLoginBtnClick);
            RegisterBtn.onClick.AddListener(OnRegisterBtnClick);
            DuoduoLoginBtn.onClick.AddListener(() => OnClickChangeLoginWay(0));
            NameLoginBtn.onClick.AddListener(() => OnClickChangeLoginWay(1));
            idText.onEndEdit.AddListener((text) => OnEndEditInfo());
            nameText.onEndEdit.AddListener((text) => OnEndEditInfo());
            pwText.onEndEdit.AddListener((text) => OnEndEditInfo());
            InitMsgListeners();
        }
        #region//场景加载模块
        private void LoadSceneInit()//初始化待绑定的加载场景
        {
            SceneNumber = 2;
            LoadAnimaFactor = 1.8f;
            GameObject LoadRoot = GameObject.Find("LoadRoot");
            Transform SecRoot = LoadRoot.transform.Find("LoadPage");
            LoadScene = SecRoot.gameObject;
            LoadBar = SecRoot.Find("Panel/LoadBarBG/LoadBar").gameObject.GetComponent<Image>();
            LoadPercentage = SecRoot.Find("Panel/PercentageNum").gameObject.GetComponent<Text>();
        }
        public IEnumerator LoadSceneAync()//异步加载场景，要向服务器发送获取好友信息的协议，初始化个人信息(获取个人个人角色名，个人头像及头像框ID等数据)
        {
            LoadScene.SetActive(true);
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneNumber);
            asyncLoad.allowSceneActivation = false;
            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                float CurrentProgress = LoadBar.fillAmount;
                LoadBar.fillAmount = Mathf.Lerp(CurrentProgress, progress, Time.deltaTime * LoadAnimaFactor);
                LoadPercentage.text = Mathf.Round(LoadBar.fillAmount * 100) + "%";
                if (LoadBar.fillAmount >= 0.9f && FirstTrigger)
                {
                    FirstTrigger = false;
                    asyncLoad.allowSceneActivation = true;
                }
                yield return null;
            }
            yield return null;
        }
        #endregion
        private void OnClickChangeLoginWay(int i)//切换登录方式
        {
            LoginWay = i;
        }
        public override void InitMsgListeners()//注册协议监听
        {
            NetWorkMgr.AddMsgListener("MsgLoginProf", OnMsgLoginProf);
        }
        /*public override void Open(params object[] args)
        {
            PanelName = "LoginPanel";
            GameObject o = ResMgr.LoadPanelPrefabs(PanelName);
            PanelObj = Instantiate(o);
            PanelObj.name = "LoginPanel";
            PanelObj.transform.SetParent(gameObject.transform);
        }*/
        public void OnLoginBtnClick()//登录按钮
        {
            if (string.IsNullOrEmpty(pwText.text))
            {
                pwCross.SetActive(true);
                pwCheck = false;
            }
            if (LoginWay == 0)
            {
                if (string.IsNullOrEmpty(idText.text))
                {
                    IdorNameCross.SetActive(true);
                    return;
                }
            }
            if (LoginWay == 1)
            {
                if (string.IsNullOrEmpty(nameText.text))
                {
                    IdorNameCross.SetActive(true);
                    return;
                }
            }
            if (!pwCheck)
            {
                return;
            }
            UIManager._Instance.OpenPanel<LoadAnimPanel>();
            MsgLoginProf msgLoginProf = new MsgLoginProf();
            msgLoginProf.LoginMehod = LoginWay;
            msgLoginProf.Id = idText.text;
            msgLoginProf.Name = nameText.text;
            msgLoginProf.pw = pwText.text;
            NetWorkMgr.Send(msgLoginProf);
        }
        public void OnRegisterBtnClick()//注册面板打开
        {
            UIManager._Instance.OpenPanel<RegisterPanel>();
        }
        public void OnMsgLoginProf(MsgBase msgBase)//收到登录协议//result=0则跳转
        {
            MsgLoginProf MsgBase = (MsgLoginProf)msgBase;
            if (MsgBase.result == 1)
            {
                if (MsgBase.ErrType == 0)
                {
                    UIManager._Instance.ClosePanel<LoadAnimPanel>();
                    pwCross.SetActive(true);
                    return;
                }
                if (MsgBase.ErrType == 1)
                {
                    UIManager._Instance.ClosePanel<LoadAnimPanel>();
                    IdorNameCross.SetActive(true);
                    return;
                }
            }
            PlayerBasicInfoMgr.Instance.UpdatePlayerName(MsgBase.Name);
            PlayerBasicInfoMgr.Instance.UpdatePlayerId(MsgBase.Id);
            Debug.Log($"登录成功:{PlayerBasicInfoMgr.Instance.GetID()}");
            UIManager._Instance.ClosePanel<LoadAnimPanel>();
            StartCoroutine(LoadSceneAync());

        }
        public void OnEndEditInfo()//用于清除当前的叉号
        {
            if (!string.IsNullOrEmpty(pwText.text))
            {
                pwCross.SetActive(false);
                pwCheck = true;
            }
            if (LoginWay == 0)
            { if (!string.IsNullOrEmpty(idText.text)) IdorNameCross.SetActive(false); }
            if (LoginWay == 1)
            {
                if (!string.IsNullOrEmpty(nameText.text)) IdorNameCross.SetActive(false);
            }
        }
        public override void OnClose()//解除协议监听
        {
            NetWorkMgr.RemoveMsgListener("MsgLoginProf", OnMsgLoginProf);
        }
    }
}