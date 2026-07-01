using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Async_Load : MonoBehaviour
{
    public bool IsFirstTrigger = false;//用于触发是否第一次连接
    public Image LoadBar;
    public Text LoadText;
    public string SceneName;
    public string Ip;//IP地址
    public int port;//端口号

    //Init port ip
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
    /// <summary>
    /// 异步登录
    /// 加载登录场景资源
    /// 再判断是否成功连接到服务器，If true激活场景
    /// </summary>
    /// <param name="Scenename"></param>
    /// <returns></returns>
    IEnumerator LoadSceneAsync(string Scenename)
    {
        int i = 0;
        NetWorkMgr.Instance.Connect(Ip, port);
        AsyncOperation asyncOperation=SceneManager.LoadSceneAsync(Scenename);
        asyncOperation.allowSceneActivation = false;
        while (!asyncOperation.isDone)//0.9progress资源加载完毕等待激活场景
        {
            float progress = asyncOperation.progress;
            LoadBar.fillAmount= progress;
            LoadText.text = (int)(progress * 100) + "%";
            if(asyncOperation.progress>=0.9f)
            {
                break;
            }
            yield return null;
        }
        while (NetWorkMgr.Instance.IsConnected() == false)
        {
            yield return new WaitForSeconds(0.5f);
            i++;
            if (i >= 3)//重连三次失败退出协程并报错
            {
                Debug.Log("Connected Fail");
                yield break;
            }
        }
        LoadBar.fillAmount = 1f;
        LoadText.text = "100%";
        asyncOperation.allowSceneActivation = true;
    }
}
