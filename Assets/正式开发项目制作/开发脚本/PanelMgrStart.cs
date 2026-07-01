using Panels;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelMgrStart : MonoBehaviour
{
    private void Start()
    {
        UIManager._Instance.OpenPanel<LoginPanel>();
    }
}
