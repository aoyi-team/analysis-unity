using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;

public class UI_Base_Button_DynamicEffect:MonoBehaviour
{
    private bool IsBouncing;//是否处于弹射
    public virtual void Button_Bounce(float ScaleUp_Facotr,float Animator_interval)//连续弹射两下
    {
        if (IsBouncing) return;
        IsBouncing = true;
        Vector3 OriginalVtor3 = transform.localScale;
        gameObject.transform.DOScale(OriginalVtor3 * ScaleUp_Facotr, Animator_interval).OnComplete(() =>
        {
            gameObject.transform.DOScale(OriginalVtor3 , Animator_interval).OnComplete(() => 
            {
                gameObject.transform.DOScale(OriginalVtor3 * ScaleUp_Facotr, Animator_interval).OnComplete(() =>
                {
                    gameObject.transform.DOScale(OriginalVtor3, Animator_interval);
                });
            });
            });
        IsBouncing = false;
    }
    public virtual void InvertColor()//按钮反色方法，暂时没必要，对应的反色颜色可以直接去ps进行搜索，然后在unity里面查找颜色代码，在按下的时候修改就行了。
    {
    }
    public virtual void Button_Anchored_UpMove(RectTransform Button_Trans,float Duration,Vector3 HavorOffset)//UI上移
    {
        Vector3 OriginalPosition = Button_Trans.anchoredPosition;
        Button_Trans.DOAnchorPos(OriginalPosition + HavorOffset, Duration);
    }
    public virtual void Button_Anchored_Recover(RectTransform Button_Trans, float Duration, Vector3 HavorOffset)//UI下移动
    {
        Vector3 OriginalPosition = Button_Trans.anchoredPosition;
        Button_Trans.DOAnchorPos(OriginalPosition - HavorOffset, Duration);
    }
}
