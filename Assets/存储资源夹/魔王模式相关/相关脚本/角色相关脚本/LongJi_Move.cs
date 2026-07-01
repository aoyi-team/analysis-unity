using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LongJi_Move : MonoBehaviour
{
    private Rigidbody2D myRigid;
    private Animator LongJiAnimator;
    public float DashSpeed;
    public float DashDurationTime;
    private Vector3 DashDirection;
    void Start()
    {
        myRigid = GetComponent<Rigidbody2D>();
        LongJiAnimator = gameObject.GetComponent<Animator>();
    }
    void Update()
    {
        Aoyi();
    }
    private void Aoyi()//Áú¼ª°ÂÒå
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Dash();
            StartCoroutine(StopDash());
        }
    }
    IEnumerator StopDash()
    {
        yield return new WaitForSeconds(DashDurationTime);
        myRigid.velocity = Vector2.zero;
    }
    private void Dash()
    {
        LongJiAnimator.Play("LongJi_Aoyi");
        Vector3 MousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        MousePosition.z = 0;
        DashDirection = MousePosition - transform.position;
        myRigid.velocity = DashDirection.normalized * DashSpeed;
    }
}
