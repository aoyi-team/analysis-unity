using Panels;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ShangjinLoadButton : BaseButtonBehaviour
{
    [Header("赏金按钮扩展配置")]
    public int targetSceneIndex;

    // 可选：赋值动画名
    protected override void Start()
    {
        base.Start();
        // mouseInAnimName = "Shanjin_MouseIn_Animation";
        // mouseOutAnimName = "Shanjin_Solid_Animation";
    }

    // 扩展点击逻辑：加载赏金场景（如需异步加载，可在这里实现）
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        UIManager._Instance.OpenPanel<ChooseHeroPanel>(GameModes.dantiao);
        PlayerBasicInfoMgr.Instance.SetCurrentGamemode(GameModes.dantiao);
    }

}