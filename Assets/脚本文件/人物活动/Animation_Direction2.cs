using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animation_Direction2 : MonoBehaviour
{
    private Vector3 mousePosition; // 鼠标的世界坐标
    private float horizontalThreshold = 0.1f; // 水平方向的容错值
    Vector3 LowDirection = new Vector3(0, -1, 0);
    private bool isMirrored = false;
    private Animator PlayerAnimController;
    public Canvas ChildCanvas;
    public Vector3 scale;
    Vector3 ScreenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
    public Vector3 SymmetryPos;
    public Vector3 OriginalPos;

    void Update()
    {
    }
    void Start()
    {
        PlayerAnimController = gameObject.GetComponent<Animator>();
    }
    private void FixedUpdate()
    {
        if (WhetherMove())
        {
            PlayerAnimController.SetBool("isMove", true);
        }
        else if (!WhetherMove()) PlayerAnimController.SetBool("isMove", false);
        if (!isMirrored)//往右看
        {
            scale = transform.localScale;
            scale.x = 0.8f;
            transform.localScale = scale;
            ChildCanvas.gameObject.transform.localScale = scale;
            ChildCanvas.GetComponent<RectTransform>().anchoredPosition = OriginalPos;
        }
        if (isMirrored)//往左看
        {
            scale = transform.localScale;
            scale.x = -0.8f;
            transform.localScale = scale;
            ChildCanvas.gameObject.transform.localScale = scale;
            ChildCanvas.GetComponent<RectTransform>().anchoredPosition = SymmetryPos;
        }
        IsMirrored();
    }
    private void IsMirrored()
    {
        // 获取鼠标位置（屏幕坐标）并转换为世界坐标
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f; // 确保在 2D 平面内

        // 获取人物的当前世界坐标
        Vector3 playerPosition = transform.position;

        // 计算鼠标相对于人物的横向距离
        float horizontalDistance = mousePosition.x - playerPosition.x;

        // 根据横向距离决定朝向
        if (horizontalDistance > horizontalThreshold)
        {
            // 鼠标在右侧，人物面向右边
            isMirrored = false;
            transform.localScale = new Vector3(0.8f, 0.8f, 1); // 正常方向
        }
        else if (horizontalDistance < -horizontalThreshold)
        {
            // 鼠标在左侧，人物面向左边
            isMirrored = true;
            transform.localScale = new Vector3(-0.8f, 0.8f, 1); // 翻转 x 轴
        }
        // 在容错范围内不进行切换
    }
    private bool WhetherMove()
    {
        bool Keydown;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        { Keydown = true; return Keydown; }
        else { Keydown = false; return Keydown; }
    }
}
