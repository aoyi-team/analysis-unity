
using UnityEngine;
namespace BaseClasses
{
    public class BasePanel : MonoBehaviour
    {
        //面板名称 
        public string PanelName;
        public GameObject PanelObj;//面板资源存储
        public bool IsPersistence = false;//是否持续化存在(是则只是setactive否则是destroy)
                                          //初始化
        public virtual void Init(params object[] args)//第一次打开面板时调用
        {

        }
        //关闭
        public virtual void Close(params object[] args)//可被按钮调用
        {

        }
        public virtual void Open(params object[] args)//后续打开面板时调用
        {

        }
        public virtual void OnClose()//解除监听事件绑定
        {

        }
        public virtual void InitMsgListeners()//绑定协议监听
        {

        }
    }
}