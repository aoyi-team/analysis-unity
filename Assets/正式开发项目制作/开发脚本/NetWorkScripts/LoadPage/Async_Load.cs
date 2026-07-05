using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Aoyi.Mirror;

public class Async_Load : MonoBehaviour
{
    public bool IsFirstTrigger = false;
    public Image LoadBar;
    public Text LoadText;
    public string SceneName;
    public string Ip;
    public int port;

    private void Awake()
    {
        Ip = ServerConfig.ServerIp;
        port = ServerConfig.TcpPort;
    }

    private void Start()
    {
        AsyncLoadScene(SceneName);
    }
    public void AsyncLoadScene(string Scenename)
    {
        StartCoroutine(LoadSceneAsync(Scenename));
    }

    IEnumerator LoadSceneAsync(string Scenename)
    {
        bool mirrorActive = MirrorNetBridge.IsMirrorActive;

        // 默认跳过旧 TCP 服务器连接（项目已迁移到 Supabase/Mirror，不再需要旧 TCP）
        // 只有在明确需要旧 TCP 的场景下才连接
        if (mirrorActive)
        {
            Debug.Log("[Async_Load] Mirror 已活跃，跳过旧 TCP 连接");
        }
        else
        {
            Debug.Log("[Async_Load] 跳过旧 TCP 服务器连接（使用 Supabase/Mirror）");
        }

        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(Scenename);
        asyncOperation.allowSceneActivation = false;
        while (!asyncOperation.isDone)
        {
            float progress = asyncOperation.progress;
            LoadBar.fillAmount = progress;
            LoadText.text = (int)(progress * 100) + "%";
            if (asyncOperation.progress >= 0.9f)
            {
                break;
            }
            yield return null;
        }

        LoadBar.fillAmount = 1f;
        LoadText.text = "100%";
        asyncOperation.allowSceneActivation = true;
    }
}
