using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class SetButtonNative : MonoBehaviour
{
    private Button ThisButton;
    private Image targetImage;
    public Button[] TwoButtons;
    public Sprite[] This_Images;
    public Sprite[] Opposite_Sprites;
    public int ThisIndex;
    public GameObject[] Show_InputField;
    private void Awake()
    {
        ThisButton = GetComponent<Button>();
        targetImage = GetComponent<Image>();
        targetImage.SetNativeSize();
    }
    private void Update()
    {
        targetImage.SetNativeSize();
    }
    public void OnChangeButton()
    {
        ThisButton.interactable = false;
        ThisButton.GetComponent<Image>().sprite = This_Images[1];
        if (ThisIndex == 0)
        {
            TwoButtons[1].interactable = true;
            TwoButtons[1].GetComponent<Image>().sprite = Opposite_Sprites[0];
        }
        else
        {
            TwoButtons[0].interactable = true;
            TwoButtons[0].GetComponent<Image>().sprite = Opposite_Sprites[0];
        }
    }
    public void Shouw_InputField()
    {
        if (ThisIndex == 0) Show_InputField[1].SetActive(false);
        else Show_InputField[0].SetActive(false);
        Show_InputField[ThisIndex].SetActive(true);
    }
}
