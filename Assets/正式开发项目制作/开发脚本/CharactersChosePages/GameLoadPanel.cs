using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//���ؽ������(��UImanager���ò�ֿ�)
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
    // ��ʼ���������
    private void InitComoponent()
    {
        //todo:�����
    }

    //外部调用加载进入游戏
    public void LoadGame(List<PlayerData> playerInfos)
    {
        playerData = playerInfos;

        var ctx = BattleContext.Capture();
        if (ctx == null)
        {
            Debug.LogError("[GameLoadPanel] BattleContext 构建失败，无法进入战斗");
            return;
        }
        ctx.AllPlayers = playerInfos;

        panel.SetActive(true);
        BattleManager.Instance.HanleGameReady += CloseTheLoadPanel;
        BattleManager.Instance.Init(ctx);
    }

    //������Ϲر����
    public void CloseTheLoadPanel()
    {

        panel.SetActive(false);
    }
    // �����Լ��Ľ������������
    public void UpdateLocalProgressBar(float progress)
    {
        loadBar.fillAmount = progress;
        loadBar_percentage.text = $"{(int)(progress * 100)}%";
    }
}