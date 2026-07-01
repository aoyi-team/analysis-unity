using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;

public class ShangjinLoad : MonoBehaviour
{
    public float scalerFactor = 1.1f;
    public float transitionSpeed = 0.1f;
    public int OffsetX = 5;
    public int OffsetY = 5;
    private RectTransform This_Rect;
    private Vector2 Original_Vc;
    private Vector2 Original_Sc;
    private Animator ShanjinBtn_Animator;
    void Start()
    {
        This_Rect = GetComponent<RectTransform>();
        Original_Vc = This_Rect.anchoredPosition;
        Original_Sc = This_Rect.localScale;
        ShanjinBtn_Animator = GetComponent<Animator>();
    }
    public void OnMouseEnter()
    {
        Vector2 Newpos = Original_Vc + new Vector2(OffsetX, OffsetY);
        Vector2 NewSca = Original_Sc * scalerFactor;
        ShanjinBtn_Animator.Play("Shanjin_MouseIn_Animation");
        This_Rect.anchoredPosition = Newpos;
        This_Rect.localScale = NewSca;
    }
    public void OnMouseExit()
    {
        This_Rect.anchoredPosition = Original_Vc;
        This_Rect.localScale = Original_Sc;
        ShanjinBtn_Animator.Play("Shanjin_Solid_Animation");
    }

    /*  public void Loadlevel(int sceneIdex)
      {
          StartCoroutine(AsyncLoadLevel(sceneIdex));
      }
      IEnumerator AsyncLoadLevel(int sceneIdex)
      {
          AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIdex);
          ScreenObject.SetActive(true);
          while(!operation .isDone )
          {
              float progress = operation.progress;
              Progressbar.fillAmount = progress;
              yield return null;
          }
      }//加载进度条
    */
}
