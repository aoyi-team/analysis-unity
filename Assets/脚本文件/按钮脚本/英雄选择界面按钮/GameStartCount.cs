using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameStartCount : MonoBehaviour
{
    public  GameObject ObjectImage;
    public GameObject ObjectText;
    public TextMeshProUGUI  CountTimeText;
    private bool countdownStarted = false;
    private float  Counttime = 5f;
    public GameObject GameButton;
    private Button GameButtonObject;
    public Button[] CharacterAndSkinChangeButtons;
    public int SceneNumber;
    
    private void Start()
    {
        GameButtonObject = GameButton.GetComponent<Button>();
        GameButtonObject.onClick.AddListener(StarCountDown);

    }


    void Update()
    {
        if(countdownStarted )
        {
            Counttime -= Time.deltaTime;
            CountTimeText.text = Mathf.Round(Counttime).ToString();
            if (Counttime <= 0f) SceneManager.LoadScene(SceneNumber);

        }
    }
    public void StarCountDown()
    {
        ObjectImage.SetActive(true);
        CountTimeText = ObjectText.GetComponent<TextMeshProUGUI>();
        GameButton.SetActive(false);
        CharacterAndSkinChangeButtons[0].enabled = false;
        CharacterAndSkinChangeButtons[1].onClick.Invoke();
        countdownStarted = true;

    }
}
