using BaseClasses;
using MsgFramework;
using UnityEngine;
using UnityEngine.UI;
namespace Panels
{

    public class RegisterPanel : BasePanel
    {
        private InputField pwText;//密码输入框
        private Text pwTipInfo;//密码旁文本提示
                               //private GameObject pwTipInfoObj;//密码文本提示框
        private InputField pwReText;//密码重复输入
        private Text pwReTipInfo;//重复输入旁文本提示
                                 //private GameObject pwReTipInfoObj;//重复输入旁文本框
        private Button finishRegBtn;//完成注册按钮
        private Button alreadyHaveBtn;//已拥有账号(用于关闭注册界面)
        private GameObject pwtickImage;//pw打勾Img
        private GameObject pwcrossImage;//pw打叉Img
        private GameObject pwRetickImage;//pwRe打勾Img
        private GameObject pwRecrossImage;//pwRe打叉Img
        private bool pwCheck = false;
        private bool pwRegCheck = false;
        /*public override void Open(params object[] args)//打开注册面板
        {
            PanelName = "RegisterPanel";
            GameObject o=ResMgr.LoadPanelPrefabs(PanelName);
            PanelObj = Instantiate(o);
            PanelObj.name = "RegisterPanel";
            PanelObj.transform.SetParent(transform);
        }*/
        public override void Close(params object[] args)//关闭注册面板
        {
            base.Close(args);
        }
        public override void Init(params object[] args)//注册面板初始化
        {
            PanelName = "RegisterPanel";
            GameObject o = ResMgr.LoadPanelPrefabs(PanelName);
            PanelObj = Instantiate(o);
            PanelObj.name = "RegisterPanel";
            PanelObj.transform.SetParent(transform);
            IsPersistence = true;
            pwText = transform.Find("RegisterPanel/RegisterPageBG/pwInputBG/pwInputfield").GetComponent<InputField>();
            pwReText = transform.Find("RegisterPanel/RegisterPageBG/pwReInputBG/pwReInputfield").GetComponent<InputField>();
            finishRegBtn = transform.Find("RegisterPanel/RegisterPageBG/RegisterBtn").GetComponent<Button>();
            alreadyHaveBtn = transform.Find("RegisterPanel/RegisterPageBG/BackBtn").GetComponent<Button>();
            pwTipInfo = transform.Find("RegisterPanel/TipsInfos/pwTipInfo/Infoholder").GetComponent<Text>();
            //pwTipInfoObj = pwTipInfo.transform.parent.gameObject;
            pwReTipInfo = transform.Find("RegisterPanel/TipsInfos/pwReTipInfo/Infoholder").GetComponent<Text>();
            //pwReTipInfoObj = pwReTipInfo.transform.parent.gameObject;
            pwtickImage = transform.Find("RegisterPanel/CrossOrTickImg/pwImg/Tick").gameObject;
            pwcrossImage = transform.Find("RegisterPanel/CrossOrTickImg/pwImg/Cross").gameObject;
            pwRetickImage = transform.Find("RegisterPanel/CrossOrTickImg/pwReImg/Tick").gameObject;
            pwRecrossImage = transform.Find("RegisterPanel/CrossOrTickImg/pwReImg/Cross").gameObject;
            gameObject.name = "RegisterPage";
            finishRegBtn.onClick.AddListener(OnClickFinRegBtn);
            alreadyHaveBtn.onClick.AddListener(() =>UIManager._Instance.ClosePanel<RegisterPanel>());
            pwText.onValueChanged.AddListener((text) => OnPasswordChange("pwInputfield"));
            pwReText.onEndEdit.AddListener((text) => OnPasswordChange("pwReInputfield"));
            InitMsgListeners();
        }
        public override void InitMsgListeners()
        {
            NetWorkMgr.AddMsgListener("MsgRegisterProf", OnMsgRegisterProf);
        }
        public void OnClickFinRegBtn()//注册按钮回调
        {
            if (!PasswordCheck()) return;
            UIManager._Instance.OpenPanel<LoadAnimPanel>();
            MsgRegisterProf msg = new();
            msg.pw = pwText.text;
            NetWorkMgr.Send(msg);
        }
        private bool PasswordCheck()//发送协议时候检查
        {
            if (pwCheck == false)
            {
                return false;
            }
            if (pwRegCheck == false)
            {
                return false;
            }
            return true;

        }
        public void OnPasswordChange(string TextName)//pwTextField-OnValuedChanged//pwReField-Onendedit
        {
            if (TextName == pwText.name)//检测密码框
            {
                if (pwReText.text != pwText.text && !string.IsNullOrEmpty(pwReText.text))
                {
                    pwReTipInfo.text = "两次密码输入不一致!\r\n";
                    pwReTipInfo.transform.parent.gameObject.SetActive(true);
                    pwRetickImage.SetActive(false);
                    pwRecrossImage.SetActive(true);
                    pwRegCheck = false;
                    return;
                }
                if (string.IsNullOrEmpty(pwText.text))
                {
                    pwTipInfo.text = "密码不能为空!";
                    pwTipInfo.transform.parent.gameObject.SetActive(true);
                    pwtickImage.SetActive(false);
                    pwcrossImage.SetActive(true);
                    pwCheck = false;
                    return;
                }
                if (!(pwText.text.Length < 11 && pwText.text.Length > 5))
                {
                    pwTipInfo.text = "密码长度不符合!";
                    pwTipInfo.transform.parent.gameObject.SetActive(true);
                    pwtickImage.SetActive(false);
                    pwcrossImage.SetActive(true);
                    pwCheck = false;
                    return;
                }
                pwTipInfo.transform.parent.gameObject.SetActive(false);
                pwcrossImage.SetActive(false);
                pwtickImage.SetActive(true);
                pwCheck = true;
            }
            if (TextName == pwReText.name)//检测密码重复输入框
            {
                if (string.IsNullOrEmpty(pwText.text))
                {
                    pwReTipInfo.transform.parent.gameObject.SetActive(false);
                    pwRetickImage.SetActive(false);
                    pwRecrossImage.SetActive(true);
                    return;
                }
                if (pwReText.text != pwText.text)
                {
                    pwReTipInfo.text = "两次密码输入不一致!\r\n";
                    pwReTipInfo.transform.parent.gameObject.SetActive(true);
                    pwRetickImage.SetActive(false);
                    pwRecrossImage.SetActive(true);
                    pwRegCheck = false;
                    return;
                }
                pwReTipInfo.transform.parent.gameObject.SetActive(false);
                pwRecrossImage.SetActive(false);
                pwRetickImage.SetActive(true);
                pwRegCheck = true;
            }
        }
        public void OnMsgRegisterProf(MsgBase msgBase)//收到服务端的注册协议反馈
        {
            MsgRegisterProf msg = (MsgRegisterProf)msgBase;
            if (msg.result == 1)//失败
            {
                UIManager._Instance.ClosePanel<LoadAnimPanel>();
                return;
            }
            UIManager._Instance.ClosePanel<LoadAnimPanel>();
            PlayerBasicInfoMgr.Instance.UpdatePlayerId(msg.Id);
            UIManager._Instance.ClosePanel<RegisterPanel>();
            UIManager._Instance.ClosePanel<LoginPanel>();
            UIManager._Instance.OpenPanel<UploadNamePanel>();
        }
        public override void OnClose()
        {
            NetWorkMgr.RemoveMsgListener("MsgRegisterProf", OnMsgRegisterProf);
        }
    }
}