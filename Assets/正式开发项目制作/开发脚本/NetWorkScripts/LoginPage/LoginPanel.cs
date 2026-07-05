using BaseClasses;
using MsgFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Panels
{
    public class LoginPanel : BasePanel
    {
        private Button DuoduoLoginBtn;
        private Button NameLoginBtn;
        private GameObject IdorNameCross;
        private GameObject pwCross;
        private Button LoginBtn;
        private Button RegisterBtn;
        private InputField idText;
        private InputField nameText;
        private InputField pwText;
        private int LoginWay = 0; // 0-账号登录，1-用户名登录
        private Toggle rememberPwToggle;

        // 网络模式选择
        private Dropdown modeDropdown;
        private NetworkMode selectedMode = NetworkMode.SupabaseOnline;

        // 不同模式下的可选输入框（如预制体中未提供则复用已有输入框）
        private InputField ipText;     // 本地服务器 IP
        private InputField emailText;  // Supabase 邮箱
        private InputField nickText;   // 局域网昵称

        #region //加载场景
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

            // 模式选择下拉框（如预制体中存在则使用，否则不显示）
            Transform dropdownTrans = transform.Find("LoginPanel/LoginPageBG/NetworkModeDropdown");
            if (dropdownTrans != null)
            {
                modeDropdown = dropdownTrans.GetComponent<Dropdown>();
                if (modeDropdown != null)
                {
                    modeDropdown.ClearOptions();
                    modeDropdown.AddOptions(new List<string> { "Supabase 在线" });
                    modeDropdown.value = 0;
                    modeDropdown.interactable = false;
                    modeDropdown.onValueChanged.AddListener(OnModeChanged);
                }
            }

            // 可选输入框
            ipText = GetOptionalInputField("LoginPanel/LoginPageBG/IpInputFieldBG/IpInputfield");
            emailText = GetOptionalInputField("LoginPanel/LoginPageBG/EmailInputFieldBG/EmailInputfield");
            nickText = GetOptionalInputField("LoginPanel/LoginPageBG/NickInputFieldBG/NickInputfield");

            gameObject.name = "LoginPage";
            selectedMode = NetworkMode.SupabaseOnline;
            PlayerBasicInfoMgr.Instance.CurrentNetworkMode = selectedMode;
            BackendProviderFactory.Create(selectedMode);

            LoginBtn.onClick.AddListener(OnLoginBtnClick);
            RegisterBtn.onClick.AddListener(OnRegisterBtnClick);
            DuoduoLoginBtn.onClick.AddListener(() => OnClickChangeLoginWay(0));
            NameLoginBtn.onClick.AddListener(() => OnClickChangeLoginWay(1));
            DuoduoLoginBtn.gameObject.SetActive(true);
            NameLoginBtn.gameObject.SetActive(true);
            idText.onEndEdit.AddListener((text) => OnEndEditInfo());
            nameText.onEndEdit.AddListener((text) => OnEndEditInfo());
            pwText.onEndEdit.AddListener((text) => OnEndEditInfo());

            rememberPwToggle = transform.Find("LoginPanel/Img_01/Toggle")?.GetComponent<Toggle>();

            LoadSavedCredentials();
            RefreshInputVisibility();
        }

        #region //加载场景
        private void LoadSceneInit()
        {
            SceneNumber = 2;
            LoadAnimaFactor = 1.8f;
            GameObject LoadRoot = GameObject.Find("LoadRoot");
            Transform SecRoot = LoadRoot.transform.Find("LoadPage");
            LoadScene = SecRoot.gameObject;
            LoadBar = SecRoot.Find("Panel/LoadBarBG/LoadBar").gameObject.GetComponent<Image>();
            LoadPercentage = SecRoot.Find("Panel/PercentageNum").gameObject.GetComponent<Text>();
        }

        public IEnumerator LoadSceneAync()
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

        private void OnClickChangeLoginWay(int loginWay)
        {
            LoginWay = loginWay;
            RefreshInputVisibility();
        }

        private void OnModeChanged(int value)
        {
            selectedMode = NetworkMode.SupabaseOnline;
            PlayerBasicInfoMgr.Instance.CurrentNetworkMode = selectedMode;
            RefreshInputVisibility();
        }

        private InputField GetOptionalInputField(string path)
        {
            Transform t = transform.Find(path);
            if (t == null) return null;
            return t.GetComponent<InputField>();
        }

        private void RefreshInputVisibility()
        {
            bool local = selectedMode == NetworkMode.LocalServer;
            bool lan = selectedMode == NetworkMode.LanClient || selectedMode == NetworkMode.LanHost;
            bool supa = selectedMode == NetworkMode.SupabaseOnline;

            SetActiveIfExists(idText, local || (supa && LoginWay == 0));
            SetActiveIfExists(nameText, lan || (supa && LoginWay == 1));
            SetActiveIfExists(pwText, local || supa);
            SetActiveIfExists(ipText, local);
            SetActiveIfExists(nickText, lan);
            SetActiveIfExists(emailText, false);
        }

        private void SetActiveIfExists(InputField field, bool active)
        {
            if (field != null)
                field.gameObject.SetActive(active);
        }

        private string GetNickName()
        {
            if (nickText != null && !string.IsNullOrWhiteSpace(nickText.text))
                return nickText.text.Trim();
            if (nameText != null && !string.IsNullOrWhiteSpace(nameText.text))
                return nameText.text.Trim();
            return string.Empty;
        }

        private string GetAccount()
        {
            if (idText != null && !string.IsNullOrWhiteSpace(idText.text))
                return idText.text.Trim();
            if (emailText != null && !string.IsNullOrWhiteSpace(emailText.text))
                return emailText.text.Trim();
            return string.Empty;
        }

        private string GetUserName()
        {
            if (nameText != null && !string.IsNullOrWhiteSpace(nameText.text))
                return nameText.text.Trim();
            return string.Empty;
        }

        private string GetLoginIdentifier()
        {
            return LoginWay == 1 ? GetUserName() : GetAccount();
        }

        public async void OnLoginBtnClick()
        {
            selectedMode = NetworkMode.SupabaseOnline;
            PlayerBasicInfoMgr.Instance.CurrentNetworkMode = selectedMode;

            if (string.IsNullOrEmpty(GetLoginIdentifier()) || string.IsNullOrEmpty(pwText.text))
            {
                IdorNameCross.SetActive(true);
                pwCross.SetActive(string.IsNullOrEmpty(pwText.text));
                return;
            }

            UIManager._Instance.OpenPanel<LoadAnimPanel>();

            try
            {
                PlayerBasicInfoMgr.Instance.CurrentNetworkMode = selectedMode;
                IBackendProvider backend = BackendProviderFactory.Create(selectedMode);
                var credentials = new SupabaseLoginCredentials
                {
                    Email = LoginWay == 0 ? GetAccount() : null,
                    Username = LoginWay == 1 ? GetUserName() : null,
                    UseUsernameLogin = LoginWay == 1,
                    Password = pwText.text
                };

                LoginResult result = await backend.LoginAsync(credentials);
                if (!result.Success)
                {
                    UIManager._Instance.ClosePanel<LoadAnimPanel>();
                    Debug.LogWarning($"[LoginPanel] 登录失败：{result.ErrorMessage}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(result.UserId))
                {
                    UIManager._Instance.ClosePanel<LoadAnimPanel>();
                    Debug.LogError("[LoginPanel] 登录成功但 UserId 为空，无法进入游戏");
                    return;
                }

                PlayerBasicInfo info = await backend.GetPlayerInfoAsync(result.UserId);
                if (string.IsNullOrWhiteSpace(info.UserId))
                {
                    UIManager._Instance.ClosePanel<LoadAnimPanel>();
                    Debug.LogError("[LoginPanel] GetPlayerInfo 返回的 UserId 为空");
                    return;
                }

                PlayerBasicInfoMgr.Instance.CurrentPlayer = info;
                PlayerBasicInfoMgr.Instance.UpdatePlayerId(info.UserId);
                PlayerBasicInfoMgr.Instance.UpdatePlayerName(info.UserName);

                Debug.Log($"[LoginPanel] 登录成功：{info.UserId}");
                UIManager._Instance.ClosePanel<LoadAnimPanel>();

                if (rememberPwToggle != null && rememberPwToggle.isOn)
                    SaveCredentials();

                StartCoroutine(LoadSceneAync());
            }
            catch (Exception ex)
            {
                UIManager._Instance.ClosePanel<LoadAnimPanel>();
                Debug.LogError($"[LoginPanel] 登录异常：{ex}");
            }
        }

        public void OnRegisterBtnClick()
        {
            UIManager._Instance.OpenPanel<RegisterPanel>();
        }

        public void OnEndEditInfo()
        {
            if (!string.IsNullOrEmpty(pwText.text))
            {
                pwCross.SetActive(false);
            }
            if (!string.IsNullOrEmpty(GetLoginIdentifier())) IdorNameCross.SetActive(false);
        }

        public override void InitMsgListeners()
        {
            // 登录结果改由 LocalBackendProvider 统一处理，避免双重响应。
        }

        private void SaveCredentials()
        {
            PlayerPrefs.SetInt("LoginWay", LoginWay);
            PlayerPrefs.SetString("LoginAccount", GetLoginIdentifier());
            PlayerPrefs.SetString("LoginPassword", pwText.text);
            PlayerPrefs.SetInt("RememberPw", 1);
            PlayerPrefs.Save();
        }

        private void LoadSavedCredentials()
        {
            if (PlayerPrefs.GetInt("RememberPw", 0) == 0) return;

            LoginWay = PlayerPrefs.GetInt("LoginWay", 0);
            string savedAccount = PlayerPrefs.GetString("LoginAccount", "");
            string savedPassword = PlayerPrefs.GetString("LoginPassword", "");

            if (!string.IsNullOrEmpty(savedAccount))
            {
                if (LoginWay == 1 && nameText != null)
                    nameText.text = savedAccount;
                else if (idText != null)
                    idText.text = savedAccount;
            }
            if (!string.IsNullOrEmpty(savedPassword) && pwText != null)
                pwText.text = savedPassword;

            if (rememberPwToggle != null)
                rememberPwToggle.isOn = true;

            OnClickChangeLoginWay(LoginWay);
        }

        public override void OnClose()
        {
            if (modeDropdown != null)
                modeDropdown.onValueChanged.RemoveListener(OnModeChanged);
        }
    }
}
