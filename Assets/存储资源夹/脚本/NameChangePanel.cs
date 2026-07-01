using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NameChangePanel : MonoBehaviour
{
    public GameObject Cover;
    public GameObject ChangeNamePanel;
    public Text NewName;
    public Text MyNameDemonstrate;
    public void OpenNameChangePanel()
    {
        Cover.SetActive(true);
        ChangeNamePanel.SetActive(true);
    }
    public void CloseNameChangePanel()
    {
        Cover.SetActive(false);
        ChangeNamePanel.SetActive(false);
    }
    public void EnsureName()
    {
        Cover.SetActive(false);
        ChangeNamePanel.SetActive(false);
        if(NewName.text!=null) MyNameDemonstrate.text = NewName.text;
    }
}
