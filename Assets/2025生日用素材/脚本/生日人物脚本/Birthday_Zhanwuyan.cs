using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Birthday_Zhanwuyan : MonoBehaviour
{
    private Rigidbody2D myRigid;
    public float speed;
    private Animator ZhanWuYan_Animator;
    void Start()
    {
        myRigid = GetComponent<Rigidbody2D>();
        ZhanWuYan_Animator = gameObject.GetComponent<Animator>();
    }
    void Update()
    {
        Run();
        Move();
    }
    public void Run()//×ßÂ·½Å±¾
    {
        float movedirx = Input.GetAxis("Horizontal");
        float movediry = Input.GetAxis("Vertical");
        Vector2 playervel = new Vector2(movedirx * speed, movediry * speed);
        myRigid.velocity = playervel;

    }
    private void Move()
    {
        if (WhetherMove())
        {
            ZhanWuYan_Animator.SetBool("isMove", true);
        }
        else if (!WhetherMove()) ZhanWuYan_Animator.SetBool("isMove", false);
    }
    private bool WhetherMove()
    {
        bool Keydown;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        { Keydown = true; return Keydown; }
        else { Keydown = false; return Keydown; }
    }
}
