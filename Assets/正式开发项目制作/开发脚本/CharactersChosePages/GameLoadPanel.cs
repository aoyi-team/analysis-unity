using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//加载进度面板(和UImanager作用查分开)
public class GameLoadPanel : MonoBehaviour
{
    private GameObject panel;

    private Image loadBar;
    private Image[] LoadHeroPosters;

    private Image[] Up_AllplayerImgs;
    private Text loadBar_percentage;

    private List<PlayerData> playerData;

    private bool GameResourcesLoadFin;
    public List<PlayerData> _allPlayerInfo { get { return playerData; } }
    public void Init(Transform fatherRoot)
    {
        if (panel == null)
        {
            GameObject o = ResMgr.LoadPanelPrefabs("GameLoadPanel");
            panel = Instantiate(o);
            panel.SetActive(false);
        }
        panel.transform.SetParent(fatherRoot);
        BattleManager.Instance.OnTotalLoadProgressUpdated += UpdateLocalProgressBar;
        InitComoponent();
    }
    // 初始化组件引用
    private void InitComoponent()
    {
        //todo:组件绑定
    }

    //外部调用加载进入游戏
    public void LoadGame(List<PlayerData> playerInfos)
    {
        playerData = playerInfos;
        panel.SetActive(true);
        // todo;跳转场景，异步加载所有资源。初始化对象池等等(交给BattleData处理)
        BattleManager.Instance.HanleGameReady += CloseTheLoadPanel;
        BattleManager.Instance.Init(playerInfos);
    }

    //加载完毕关闭面板
    public void CloseTheLoadPanel()
    {

        panel.SetActive(false);
    }
    // 加载自己的进度条更新情况
    public void UpdateLocalProgressBar(float progress)
    {
        loadBar.fillAmount = progress;
        loadBar_percentage.text = $"{(int)(progress * 100)}%";
    }
}