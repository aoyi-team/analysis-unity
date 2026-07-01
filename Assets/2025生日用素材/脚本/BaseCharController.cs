using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//抽象角色类
public abstract class BaseCharController :MonoBehaviour
{
    public GameObject ClosetBox;
    public abstract void Attack();
    //角色行走方式
}
