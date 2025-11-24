using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialUIManager : MonoBehaviour
{
    [SerializeField] private GameObject tutorialUI;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    private string title;
    private string body;

    private void Awake()
    {
        tutorialUI.SetActive(false);
    }

    public void Show(string title, string body)
    {
        tutorialUI.SetActive(true);

        this.title = title;
        this.body = body;

        if(this.title != null) titleText.text = this.title;
        
        if(this.body != null) bodyText.text = this.body;
    
    }
    public void Hide()
    {
        tutorialUI?.SetActive(false);
    }
}
