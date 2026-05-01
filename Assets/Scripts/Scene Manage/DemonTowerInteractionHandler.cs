using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class DemonTowerInteractionHandler : MonoBehaviour
{
    [Header("UI References")]
    public GameObject backpackButton;
    public TMP_Text currentFloorText; // 用于显示当前层数

    // private bool isLoadingScene = false;

    void Start()
    {
        if (backpackButton != null)
        {
            UnityEngine.UI.Button btn = backpackButton.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
                btn.onClick.AddListener(OpenBackpack);
        }
    }

    void Update()
    {
        if (currentFloorText != null)
            currentFloorText.text = "当前层数: " + GameData.currentFloor;
    }

    public void OpenBackpack()
    {
        SceneManager.LoadScene("Backpack");
    }

    public void OnSaveButtonClick()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
        else
            Debug.LogWarning("SaveManager 不存在");
    }

    public void OnLoadButtonClick()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.LoadGame();
        else
            Debug.LogWarning("SaveManager 不存在");
    }

    public void OpenFaction()
    {
        SceneManager.LoadScene("Faction");
    }

    public void OpenAttribute()
    {
        SceneManager.LoadScene("Attribute");
    }

    public void OpenForge()
    {
        SceneManager.LoadScene("Forge");
    }

    public void OnQuitButtonClick()
    {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}