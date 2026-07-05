using Panels;
using ErrorManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelMgrStart : MonoBehaviour
{
    private void Awake()
    {
        var em = ErrorManager.Instance;
    }

    private void Start()
    {
        UIManager._Instance.OpenPanel<LoginPanel>();
    }
}
