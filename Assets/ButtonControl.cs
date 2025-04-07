using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonControl : MonoBehaviour
{
    public GameObject modeSelectionPanel;
    public GameObject titlePanel;
    
    private void Start()
    {
        if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
        if (titlePanel != null) titlePanel.SetActive(true);
    }
    
    public void OnStartButton()
    {
        if (modeSelectionPanel != null) modeSelectionPanel.SetActive(true);
        if (titlePanel != null) titlePanel.SetActive(false);
    }
    
    public void OnStartGameButton()
    {
        SceneManager.LoadScene("Main");
    }
    
    public void OnBackButton()
    {
        if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
        if (titlePanel != null) titlePanel.SetActive(true);
    }
    
    public void OnHelpButton()
    {
        SceneManager.LoadScene("Help");
    }
    public void OnHelp1Button()
    {
        SceneManager.LoadScene("Help 1");
    }
    
    public void OnTitleButton()
    {
        SceneManager.LoadScene("Title");
    }

    public void OnQuitButton()
    {
        Application.Quit(0);
    }
}
