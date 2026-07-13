using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// 用于游戏内的场景跳转管理器
/// </summary>
public class SceneMgr : MonoBehaviour
{
    public static event Action LoadedFini;
    private static SceneMgr instance;
    public static SceneMgr Instance { get { 
            if(instance == null)
            {
                GameObject o = new GameObject("SceneMgr");
                instance= o.AddComponent<SceneMgr>();
                DontDestroyOnLoad(o);
            }
            return instance; } }

    // Unity的场景加载是异步的，不能直接调用SceneManager.LoadScene("sceneName")，会导致卡顿，所以需要使用协程来加载场景
    public AsyncOperation LoadSceneByName(GameModes mode)
    {
        return SceneManager.LoadSceneAsync(GameSceneCatalog.GetBattleScene(mode));
    }

    #region 旧版加载
    public void LoadSceneBySceneIndex(GameModes mode)
    {
        StartCoroutine(LoadSceneAsync(GameSceneCatalog.GetBattleScene(mode)));
    }
    IEnumerator LoadSceneAsync(string mapName)
    {
        AsyncOperation asyc = SceneManager.LoadSceneAsync(mapName);
        asyc.allowSceneActivation = false;
        while (asyc.progress < 0.9f)
        {
            float progress = asyc.progress / 0.9f;
            Debug.Log($"当前加载进度{progress}");
            yield return null;
        }
        asyc.allowSceneActivation = true;
        yield return null;
    }

    #endregion
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded-= OnSceneLoaded;
    }
    /// <summary>
    /// 如果是单挑或者排位地图，场景加载完通知观察者
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="mode"></param>
    private void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        if(GameSceneCatalog.IsBattleScene(scene.name))
        {
            LoadedFini?.Invoke();
        }
    }
}
