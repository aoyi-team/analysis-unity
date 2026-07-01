using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamMarkTotal : MonoBehaviour
{
    public Image MarkBar;
    private GameObject[] TeamPlayers;
    private GameObject[] AllPlayers;
    public string TeamName;
    private int FullMark = 0;
    public Text MarkDemonstrate;
    void Start()
    {

        StartCoroutine(AddUpSameTeamPlayers());

    }
    void Update()
    {
        FullMark = MarkTotal();
        MarkBarLoad();
        MarkDemonstrate.text = FullMark.ToString();
    }
    private int MarkTotal()
    {
        int i = 0;
        if (TeamPlayers != null)
        {
            foreach (GameObject Player in TeamPlayers)
            {
                if (Player != null) i += Player.GetComponent<CharacterLevelSystem>().PlayerScore;
            }
            return i;

        }
        return 0;
    }
    private void MarkBarLoad()
    {
        if (FullMark <= 150000f)
        {
            float f = FullMark;
            MarkBar.fillAmount =f / 150000f;
        }
        if (FullMark > 150000f)
        {
            MarkBar.fillAmount = 1;
        }
    }
    IEnumerator AddUpSameTeamPlayers()
    {
        yield return new WaitForSeconds(0.05f);
        TeamPlayers = new GameObject[10];
        int i = 0;
        int layerMask = LayerMask.NameToLayer(TeamName);
        AllPlayers = GameObject.FindGameObjectsWithTag("Characters");
        foreach (GameObject TeamPlayer in AllPlayers)
        {
            if (TeamPlayer.layer == layerMask)
            {
                TeamPlayers[i] = TeamPlayer;
                i += 1;
            }

        }
    }
}
