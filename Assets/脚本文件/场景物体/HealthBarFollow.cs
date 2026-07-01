using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarFollow : MonoBehaviour
{
    public SmallMonster ObjectMonsterComponent;
    public GameObject ObjectMonster;
    private float Health;
    public Image HealthBar;
    private void Start()
    {
        ObjectMonsterComponent = ObjectMonster.GetComponent<SmallMonster>();
    }
    private void Update()
    {
        Health = ObjectMonsterComponent.HealthMax;
        HealthBar.fillAmount = Health / 500;
    }
}
