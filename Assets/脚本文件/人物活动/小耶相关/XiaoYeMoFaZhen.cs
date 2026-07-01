using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XiaoYeMoFaZhen : MonoBehaviour
{
    private Animation ThisAnim;
    public RuntimeAnimatorController BianXiongAnimator;
    private List<GameObject> enemiesInCircle = new List<GameObject>();
    public string TeamLabel;
    private void Start()
    {
        ThisAnim = gameObject.GetComponent<Animation>();
        StartCoroutine(BianXiong());
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Characters") 
        {
            if (collision.GetComponent<PlayerInfo>().Team != TeamLabel)
            {
                enemiesInCircle.Add(collision.gameObject);
                // 检测到敌人进入魔法阵
            }
        }

    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Characters") 
        {
            if (collision.GetComponent<PlayerInfo>().Team != TeamLabel)
            {
                enemiesInCircle.Remove(collision.gameObject);
                // 敌人离开魔法阵
            }
        }


    }
    IEnumerator BianXiong()
    {
        yield return new WaitForSeconds(ThisAnim.clip.length-0.05f);
        foreach (GameObject enemy in enemiesInCircle)
        {
            enemy.GetComponent<Animator>().runtimeAnimatorController = BianXiongAnimator;
            enemy.GetComponent<PlayerInfo>().NowState = CharacterState.BianXiong;
            enemy.GetComponent<PlayerInfo>().NowSpeed = (enemy.GetComponent<PlayerInfo>().OriginalSpeed)/2f;
        }
        Destroy(gameObject);
    }

}
