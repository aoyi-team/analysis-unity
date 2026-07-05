using BaseClasses;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIManager:MonoBehaviour
{/// <summary>
/// PanelMgr负责管理面板的打开和关闭
/// </summary>
    private static UIManager Instance;
    private Transform _panelRoot;
    public static UIManager _Instance {
        get {
            if(Instance==null)
            {
                GameObject UIManager = new GameObject("UIManager");
                Instance = UIManager.AddComponent<UIManager>();
                DontDestroyOnLoad(UIManager); // 跨场景不销毁（登录→大厅需保留）
            }
            if(Instance._panelRoot==null)
            {
                // 初始化面板根节点
                GameObject panelList = GameObject.Find("PanelList");
                if (panelList == null)
                {
                    panelList = new GameObject("PanelList");
                }
                Instance._panelRoot = panelList.transform;
            }
            return Instance;
        }
    }
    // 缓存所有面板（Key：面板类名，避免ToString()的命名空间冗余）
    private Dictionary<string, BasePanel> _panelCache = new Dictionary<string, BasePanel>();
    //打开面板
    public void OpenPanel<T>(params object[] args) where T: BasePanel
    {
        string panelName = typeof(T).Name;
        if (_panelCache.ContainsKey(panelName)) {
            BasePanel panel = _panelCache[panelName];
            if (!panel.PanelObj.activeSelf)
            {
                panel.PanelObj.SetActive(true);
                panel.Open(args);
            }
            return;
        }
        GameObject o= new GameObject();
        o.transform.SetParent(_panelRoot);
        T panelcomponent= o.AddComponent<T>();
        _panelCache.Add(panelName, panelcomponent);
        panelcomponent.Init(args);
    }
    //关闭入口放在UIManager
    public void ClosePanel<T>(params object[] args) where T:BasePanel//关闭面板
    {
        string panelName = typeof(T).Name;
        if (!_panelCache.ContainsKey(panelName))
        {
            Debug.LogWarning($"面板不存在：{panelName}");
            return;
        }
        BasePanel panel = _panelCache[panelName];
        panel.Close(args); // 先执行面板自定义Close逻辑
        // 非持久化面板：销毁+移除缓存
        if (!panel.IsPersistence)
        {
            panel.OnClose(); // 解绑消息/清理资源
            Destroy(panel.gameObject);
            _panelCache.Remove(panelName);
        }
        // 持久化面板：仅隐藏
        else
        {
            panel.PanelObj.SetActive(false);
        }
    }
    //外界调用添加本体，顺便执行目标初始化方法
    public void PanelAddSelf(BasePanel selfpanel)
    {
        if (!_panelCache.ContainsKey(selfpanel.PanelName))
        {
            _panelCache.Add(selfpanel.PanelName,selfpanel);
            selfpanel.gameObject.transform.SetParent(_panelRoot);
        }
    }
}