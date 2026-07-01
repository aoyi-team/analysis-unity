using BaseClasses;
using MsgFramework;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Panels
{

    public class UploadNamePanel : BasePanel
    {
        private InputField nameText;//提交更新后的名字
        private Text tipsInfo;//用于显示提示信息
        private bool NameCheck = false;//名称是否符合规则
        private Button UploadNameBtn;//确认名称按钮

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
            PanelName = "UploadNamePanel";
            GameObject o = ResMgr.LoadPanelPrefabs(PanelName);
            PanelObj = Instantiate(o);
            PanelObj.name = "UploadNamePanel";
            PanelObj.transform.SetParent(transform);
            LoadSceneInit();
            UploadNameBtn = transform.Find("UploadNamePanel/UploadPageBG/UploadBtn").GetComponent<Button>();
            nameText = transform.Find("UploadNamePanel/UploadPageBG/nameInputBG/nameInputfield").GetComponent<InputField>();
            tipsInfo = transform.Find("UploadNamePanel/TipsInfos/NameTipInfo/Infoholder").GetComponent<Text>();
            tipsInfo.text = "起一个昵称,可用于账号\r\n登录哦!";
            gameObject.name = "UploadPage";
            UploadNameBtn.onClick.AddListener(OnUploadNameClick);
            nameText.onValueChanged.AddListener((text) => OnValuedChanged());
            nameText.onEndEdit.AddListener((o) => OnEndEditName());
            nameText.onValidateInput += OnValidateInput;
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
        /*public override void Open(params object[] args)
        {
            PanelName = "UploadNamePanel";
            GameObject o = ResMgr.LoadPanelPrefabs(PanelName);
            PanelObj = Instantiate(o);
            PanelObj.name = "UploadNamePanel";
            PanelObj.transform.SetParent(transform);
        }*/
        public void OnUploadNameClick()//上传名字协议
        {
            if (NameCheck == false) return;
            UIManager._Instance.OpenPanel<LoadAnimPanel>();
            MsgUpdateloadName msg = new();
            msg.Id = PlayerBasicInfoMgr.Instance.GetID();
            msg.Name = nameText.text;
            NetWorkMgr.Send(msg);
        }
        public void OnMsgUpdateloadName(MsgBase msgbase)//监听消息结果
        {
            MsgUpdateloadName msg = (MsgUpdateloadName)msgbase;
            if (msg.result == 1)
            {
                tipsInfo.text = "名称已存在!";
                tipsInfo.transform.parent.gameObject.SetActive(true);
                UIManager._Instance.ClosePanel<LoadAnimPanel>();
                return;
            }
            PlayerBasicInfoMgr.Instance.UpdatePlayerName(msg.Name);
            UIManager._Instance.ClosePanel<LoadAnimPanel>();
            StartCoroutine(LoadSceneAync());
        }
        public void OnValuedChanged()//用于实时检测输入情况
        {
            if (string.IsNullOrEmpty(nameText.text))
            {
                tipsInfo.transform.parent.gameObject.SetActive(true);
                tipsInfo.text = "起一个昵称,可用于账号\r\n登录哦!";
                NameCheck = false;
                return;
            }
            if (!(nameText.text.Length <= 6))
            {
                tipsInfo.transform.parent.gameObject.SetActive(true);
                tipsInfo.text = "名称不能多于6个字!";
                NameCheck = false;
                return;
            }
            tipsInfo.transform.parent.gameObject.SetActive(false);
            NameCheck = true;
        }
        public void OnEndEditName()//用于关闭消息提示面板
        {
            tipsInfo.transform.parent.gameObject.SetActive(false);
        }
        private char OnValidateInput(string text, int charIndex, char addedChar)//过滤除中文数字以外的字符
        {
            // 如果输入的字符是中文或数字，则允许；否则返回空字符
            if (Regex.IsMatch(addedChar.ToString(), @"[\u4e00-\u9fa5\d]") && addedChar != ' ')
            {
                return addedChar;
            }
            return '\0'; // 返回空字符表示禁止输入
        }
        public override void OnClose()
        {
            NetWorkMgr.RemoveMsgListener("MsgUpdateloadName", OnMsgUpdateloadName);
        }
        public override void InitMsgListeners()
        {
            NetWorkMgr.AddMsgListener("MsgUpdateloadName", OnMsgUpdateloadName);
        }
    }
}