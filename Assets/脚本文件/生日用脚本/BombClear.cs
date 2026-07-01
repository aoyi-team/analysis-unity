using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombClear : MonoBehaviour
{
    public GameObject[] FourLetterBombs;
    public bool TheFourLetters = false;
    public GameObject[] TwoLetterBombs;
    public GameObject NexTlETTER;
    public GameObject XinShow;
    private int i = 0;
    private void Update()
    {
        if (TheFourLetters == true)
        {
            TheFourLetters = false;
            StartCoroutine(ClearTheBomb());
        }
    }
    IEnumerator ClearTheBomb()
    {
        i++;
        if (i == 1)
        {
            foreach (GameObject FatherGame in FourLetterBombs)
            {
                foreach (Transform Child in FatherGame.transform)
                {
                    Child.gameObject.GetComponent<Animator>().Play("DiLeiBoomAnim");
                    Destroy(Child.gameObject, 0.4f);
                }
            }
        }

        if (i == 2)
        {
            foreach (GameObject FatherGame in TwoLetterBombs)
            {
                foreach (Transform Child in FatherGame.transform)
                {
                    Child.gameObject.GetComponent<Animator>().Play("DiLeiBoomAnim");
                    Destroy(Child.gameObject, 0.4f);
                }
            }
        }
        yield return new WaitForSeconds(0.5f);
        if(i==1) NexTlETTER.SetActive(true);
        if (i == 2) XinShow.SetActive(true);
    }
}
