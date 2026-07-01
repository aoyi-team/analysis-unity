using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllUse_Deabled : MonoBehaviour
{
    public Component[] TargetComponent;

    public void DisabledThe_Component()
    {
        foreach (var Target in TargetComponent)
        {
            ((Behaviour)Target).enabled = false;
        }
    }
}
