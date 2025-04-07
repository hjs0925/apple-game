using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ModeSelector : MonoBehaviour
{
    public Button addModeButton;
    public Button subtractModeButton;
    public Button apModeButton;
    public Button gpModeButton;
    
    public TextMeshProUGUI selectedModeText;
    
    private GameControl.GameMode selectedMode = GameControl.GameMode.Add;
    
    private void Start()
    {
        // 저장된 모드 불러오기
        if (PlayerPrefs.HasKey("GameMode"))
        {
            int modeIndex = PlayerPrefs.GetInt("GameMode");
            if (System.Enum.IsDefined(typeof(GameControl.GameMode), modeIndex))
            {
                selectedMode = (GameControl.GameMode)modeIndex;
            }
        }
        
        // 버튼 이벤트 설정
        addModeButton.onClick.AddListener(() => SelectMode(GameControl.GameMode.Add));
        subtractModeButton.onClick.AddListener(() => SelectMode(GameControl.GameMode.Subtract));
        apModeButton.onClick.AddListener(() => SelectMode(GameControl.GameMode.AP));
        gpModeButton.onClick.AddListener(() => SelectMode(GameControl.GameMode.GP));
        
        // 현재 선택된 모드 표시
        UpdateSelectedModeUI();
    }
    
    private void SelectMode(GameControl.GameMode mode)
    {
        selectedMode = mode;
        GameControl.SaveGameMode(mode);
        UpdateSelectedModeUI();
    }
    
    private void UpdateSelectedModeUI()
    {
        if (selectedModeText != null)
        {
            switch (selectedMode)
            {
                case GameControl.GameMode.Add:
                    selectedModeText.text = "선택된 모드: 더하기";
                    break;
                case GameControl.GameMode.Subtract:
                    selectedModeText.text = "선택된 모드: 빼기";
                    break;
                case GameControl.GameMode.AP:
                    selectedModeText.text = "선택된 모드: 등차수열";
                    break;
                case GameControl.GameMode.GP:
                    selectedModeText.text = "선택된 모드: 등비수열";
                    break;
            }
        }
    }
} 