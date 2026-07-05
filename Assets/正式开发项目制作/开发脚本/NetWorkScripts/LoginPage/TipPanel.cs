using BaseClasses;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// TipPanel
/// 用于简单的显示提示信息
/// 小方框提示文本还是用别的形式来实现
/// 例如做在预制体里面，用setactive来实现
/// </summary>
namespace Panels
{
    public class TipPanel : BasePanel
    {

        public override void Init(params object[] args)
        {
            base.Init(args);
        }
        public override void Close(params object[] args)
        {
            base.Close(args);
        }
    }
}