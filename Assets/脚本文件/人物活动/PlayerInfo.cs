using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public enum CharacterState { Normal,BianXiong,Stunned,Silence,Dashing,BePushed,Displacement,AoYing
}
public class PlayerInfo : MonoBehaviour
{
    [Header("玩家信息或中立生物信息")]
    public RuntimeAnimatorController ThisPlayerRunAnimator;
    public string Team;
    public float OriginalSpeed;
    public float NowSpeed;
    public CharacterState NowState;
    public bool StartStopBePushed=false;
    public float DashLeftTime;
    private Rigidbody2D GameObjectRgid;
    public int WhoDashLayer;
    public GameObject LongYanYinji;
    public int FullHealth;//便于修改等
    public int NowHealth;
    private void Start()
    {
        GameObjectRgid = GetComponent<Rigidbody2D>();
    }
    private void Update()
    {
        if (StartStopBePushed)
        {
            StartStopBePushed = false;
            StartCoroutine(StopBePushed(DashLeftTime));

        }
    }
    IEnumerator StopBePushed(float DashLefT)
    {
        yield return new WaitForSeconds(DashLefT);
        GameObjectRgid.velocity = Vector2.zero;
        NowState = CharacterState.Normal;

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (NowState == CharacterState.BePushed)
        {
            if (collision.gameObject.tag == "SmallMonster")
            {
                Instantiate(LongYanYinji, gameObject.transform.position, Quaternion.identity);

            }
            if (collision.gameObject.tag == "Characters"&&collision.gameObject.layer!=WhoDashLayer)
            {
                GameObject Yinji = Instantiate(LongYanYinji, gameObject.transform.position, Quaternion.identity);
                Yinji.GetComponent<YinjiBoom>().WhoDash = WhoDashLayer;
            }
            if (collision.gameObject.tag == "Box" || collision.gameObject.tag == "SmallBox")
            {
                Instantiate(LongYanYinji, gameObject.transform.position, Quaternion.identity);
            }
        }
    }

}
