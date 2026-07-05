using BaseClasses;
using MsgFramework;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Panels
{
    public class RegisterPanel : BasePanel
    {
        private InputField pwText;
        private Text pwTipInfo;
        private InputField pwReText;
        private Text pwReTipInfo;
        private Button finishRegBtn;
        private Button alreadyHaveBtn;
        private GameObject pwtickImage;
        private GameObject pwcrossImage;
        private GameObject pwRetickImage;
        private GameObject pwRecrossImage;
        private bool pwCheck = false;
        private bool pwRegCheck = false;

        // Supabase 注册用的可选邮箱输入框
        private InputField emailText;

        public override void Close(params object[] args)
        {
            base.Close(args);
        }

        public override void Init(params object[] args)
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
            pwReTipInfo = transform.Find("RegisterPanel/TipsInfos/pwReTipInfo/Infoholder").GetComponent<Text>();
            pwtickImage = transform.Find("RegisterPanel/CrossOrTickImg/pwImg/Tick").gameObject;
            pwcrossImage = transform.Find("RegisterPanel/CrossOrTickImg/pwImg/Cross").gameObject;
            pwRetickImage = transform.Find("RegisterPanel/CrossOrTickImg/pwReImg/Tick").gameObject;
            pwRecrossImage = transform.Find("RegisterPanel/CrossOrTickImg/pwReImg/Cross").gameObject;

            emailText = GetOptionalInputField("RegisterPanel/RegisterPageBG/EmailInputBG/EmailInputfield");

            gameObject.name = "RegisterPage";
            PlayerBasicInfoMgr.Instance.CurrentNetworkMode = NetworkMode.SupabaseOnline;
            BackendProviderFactory.Create(NetworkMode.SupabaseOnline);

            finishRegBtn.onClick.AddListener(OnClickFinRegBtn);
            alreadyHaveBtn.onClick.AddListener(() => UIManager._Instance.ClosePanel<RegisterPanel>());
            pwText.onValueChanged.AddListener((text) => OnPasswordChange("pwInputfield"));
            pwReText.onEndEdit.AddListener((text) => OnPasswordChange("pwReInputfield"));
            InitMsgListeners();
        }

        private InputField GetOptionalInputField(string path)
        {
            Transform t = transform.Find(path);
            if (t == null) return null;
            return t.GetComponent<InputField>();
        }

        public override void InitMsgListeners()
        {
            // 现在注册只走 Supabase Auth，不再监听本地服务器注册回调。
        }

        public async void OnClickFinRegBtn()
        {
            if (!PasswordCheck()) return;

            PlayerBasicInfoMgr.Instance.CurrentNetworkMode = NetworkMode.SupabaseOnline;

            UIManager._Instance.OpenPanel<LoadAnimPanel>();

            try
            {
                await RegisterSupabaseAsync();
            }
            catch (Exception ex)
            {
                UIManager._Instance.ClosePanel<LoadAnimPanel>();
                Debug.LogError($"[RegisterPanel] 注册异常：{ex}");
            }
        }

        private async Task RegisterSupabaseAsync()
        {
            string email = emailText != null ? emailText.text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(email))
            {
                UIManager._Instance.ClosePanel<LoadAnimPanel>();
                Debug.LogWarning("[RegisterPanel] Supabase 注册需要邮箱");
                return;
            }

            IBackendProvider backend = PlayerBasicInfoMgr.Instance.CurrentBackend;
            if (backend == null)
            {
                backend = BackendProviderFactory.Create(NetworkMode.SupabaseOnline);
            }

            RegisterResult result = await backend.RegisterAsync(new SupabaseLoginCredentials
            {
                Email = email,
                Password = pwText.text
            });

            if (!result.Success)
            {
                UIManager._Instance.ClosePanel<LoadAnimPanel>();
                Debug.LogWarning($"[RegisterPanel] 注册失败：{result.ErrorMessage}");
                return;
            }

            UIManager._Instance.ClosePanel<LoadAnimPanel>();
            UIManager._Instance.ClosePanel<RegisterPanel>();
            Debug.Log("[RegisterPanel] Supabase 注册成功，请使用该账号登录后进入游戏");
        }

        private bool PasswordCheck()
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

        public void OnPasswordChange(string TextName)
        {
            if (TextName == pwText.name)
            {
                if (pwReText.text != pwText.text && !string.IsNullOrEmpty(pwReText.text))
                {
                    pwReTipInfo.text = "两次输入的密码不一致!\r\n";
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
            if (TextName == pwReText.name)
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
                    pwReTipInfo.text = "两次输入的密码不一致!\r\n";
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

        public void OnMsgRegisterProf(MsgBase msgBase)
        {
            UIManager._Instance.ClosePanel<LoadAnimPanel>();
            Debug.LogWarning("[RegisterPanel] 已忽略本地注册回调：当前只允许 Supabase 注册/登录");
        }

        public override void OnClose()
        {
            NetWorkMgr.RemoveMsgListener("MsgRegisterProf", OnMsgRegisterProf);
        }
    }
}
