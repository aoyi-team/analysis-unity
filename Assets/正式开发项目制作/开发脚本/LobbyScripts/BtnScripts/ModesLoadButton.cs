using Panels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// 对战按钮：进入 5v5v5 单人模式
/// </summary>
public class ModesLoadButton : BaseButtonBehaviour
{
    [Header("对战按钮扩展配置")]
    [Tooltip("要加载的场景索引")]
    public int targetSceneIndex;

    protected override void Start()
    {
        base.Start();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        // 进入 5v5v5 单人模式
        UIManager._Instance.OpenPanel<ChooseHeroPanel>(GameModes.paiwei_solo);
        PlayerBasicInfoMgr.Instance.SetCurrentGamemode(GameModes.paiwei_solo);
    }
}
