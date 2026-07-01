using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AttackCoolImage : MonoBehaviour
{
    //private GameObject TargetPlayer;
    //private bool IsFindPlayer = false;
    /*private void Update()//暂时测试，记得之后修改；
    {
        if (!IsFindPlayer)
        {
            GameObject[] Allplayers = GameObject.FindGameObjectsWithTag("Characters");
            foreach (GameObject Player in Allplayers)
            {
                if (LayerMask.LayerToName(Player.layer) == "Green")
                {
                    IsFindPlayer = true;
                    TargetPlayer = Player;
                    if (gameObject.tag == "AttackImage")
                    {
                        gameObject.GetComponent<Image>().sprite = TargetPlayer.GetComponent<PictureSave>().CoolImages[0];
                    }
                    else gameObject.GetComponent<Image>().sprite = TargetPlayer.GetComponent<PictureSave>().CoolImages[1];
                }
            }
            
        }
    }*/
}
