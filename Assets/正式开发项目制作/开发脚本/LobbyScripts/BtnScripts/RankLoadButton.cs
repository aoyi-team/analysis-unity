using Panels;
using UnityEngine;
using UnityEngine.EventSystems;

public class RankLoadButton : BaseButtonBehaviour
{
    // 排行榜按钮无额外扩展逻辑，仅配置动画名即可
    // 如需扩展点击逻辑，重写OnButtonClick即可

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        UIManager._Instance.OpenPanel<ChooseHeroPanel>(GameModes.paiwei);
        PlayerBasicInfoMgr.Instance.SetCurrentGamemode(GameModes.paiwei);
    }
}