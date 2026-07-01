using BaseClasses;
using UnityEngine;
/// <summary>
/// 加载转圈动画面板，无需添加其他东西
/// 持续存在，无需销毁
/// </summary>
namespace Panels
{
    public class LoadAnimPanel : BasePanel
    {
        public override void Init(params object[] args)//打开加载面板预制体
        {
            gameObject.name = "LoadAnimPage";
            IsPersistence = true;//持续性存在
            PanelName = "LoadAnimPanel";
            GameObject o = ResMgr.LoadPanelPrefabs(PanelName);
            PanelObj = Instantiate(o);
            PanelObj.name = "LoadAnimPanel";
            PanelObj.transform.SetParent(transform);
        }
        public override void Close(params object[] args)
        {
            base.Close(args);
        }
    }
}