using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// 继承基类，仅处理对战按钮的差异化
public class ModesLoadButton : BaseButtonBehaviour
{
    [Header("对战按钮扩展配置")]
    [Tooltip("要加载的场景索引")]
    public int targetSceneIndex;

    // 可选：如果需要重写Start（一般不需要，基类已处理）
    protected override void Start()
    {
        base.Start(); // 必须调用基类Start，保证通用逻辑执行

        // 也可以在这里直接赋值动画名（替代Inspector配置）
        // mouseInAnimName = "DuizhanBtn_MouseIn_Animation";
        // mouseOutAnimName = "DuizhanBtn_Solid_Animation";
    }

    // 扩展点击逻辑：加载场景
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData); // 先执行基类的音效播放
    }
}