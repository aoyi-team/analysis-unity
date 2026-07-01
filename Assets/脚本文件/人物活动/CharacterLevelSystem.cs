using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterLevelSystem : MonoBehaviour
{
    public int Nowlevel=1;
    /*private int FirstLevel = 5, SecondLevel = 10;
    private void FifthLevelUp()
    {

    }
    private void TenthLevelUp()
    {

    }*/
    public  GameObject LevelUpGameobject;
    private Animation LevelUpAnimation;
    public int PlayerScore = 0;
    public Text ScoreElpo;
    public Text ScoreFollow;
    private bool IsFind = false;
    private Transform GamePos;
    void Start()
    {
        LevelUpAnimation = LevelUpGameobject.GetComponent<Animation>();
        GamePos = gameObject.transform.Find("GamePos");
    }

    // Update is called once per frame
    void Update()
    {
        GameObject LeveAnim=null;
        int NowL= DolevelUp(PlayerScore, Nowlevel);
        SpriteRenderer NewSprite;
        Material SpriteMate;
        if (NowL !=Nowlevel)
        {
            LeveAnim = Instantiate(LevelUpGameobject, gameObject.transform.position, Quaternion.identity);
            LeveAnim.transform.SetParent(gameObject.transform);
            LeveAnim.transform.position = GamePos.position;
            NewSprite = LeveAnim.GetComponent<SpriteRenderer>();
            SpriteMate = NewSprite.material;
            Color color = SpriteMate.color;
            color.a = 0.8f;
            SpriteMate.color = color;
            Destroy(LeveAnim, LevelUpAnimation.clip.length);
        }
        Nowlevel = DolevelUp(PlayerScore, Nowlevel);
        ScoreFollow.text = PlayerScore.ToString();
        if (IsFind == false) FindTargetScoreEplo();
        if(IsFind) ScoreElpo.text = ScoreFollow.text;
    }
    private int DolevelUp(int NowSco,int Nowlev)
    {
        int TotalTargetScore = 0;
        for(int i=1;i<=Nowlev;i++)
        {
            TotalTargetScore += i * 222;
        }
        if (TotalTargetScore <= NowSco) Nowlev++;
        return Nowlev;
    }
    private void FindTargetScoreEplo()
    {
        GameObject Pscore;
        if ((Pscore = GameObject.FindWithTag("PersonalScore")) != null)
        {
            IsFind = true;
            ScoreElpo = Pscore.GetComponent<Text>();
        }
    }

}


