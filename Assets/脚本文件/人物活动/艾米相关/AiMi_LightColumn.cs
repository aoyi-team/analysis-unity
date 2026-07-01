using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AiMi_LightColumn : MonoBehaviour
{
    public int damageAmount = 40; // 每次造成的伤害
    public float damageInterval = 0.05f; // 伤害间隔
    public  GameObject AttackParticle;
    public float ParticleMover;//粒子半径显示
    private LightColumn_Health TargetHealth;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Box") || collision.CompareTag("SmallMonster") || collision.CompareTag("SmallBox"))
        {

            TargetHealth = collision.GetComponent<LightColumn_Health>();
            TargetHealth.StartTakingDamage(damageAmount, damageInterval);

        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Box") || collision.CompareTag("SmallMonster") || collision.CompareTag("SmallBox"))
        {
            TargetHealth = collision.GetComponent<LightColumn_Health>();
            TargetHealth.StopTakingDamage();
        }
    }
}
