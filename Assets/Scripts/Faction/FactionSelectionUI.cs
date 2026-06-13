using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class FactionSelectionUI : MonoBehaviour
{
    [Header("Faction Buttons")]
    public Button tianWangDianButton;
    public Button wuZhuangGuanButton;
    public Button fangCunShanButton;

    [Header("Description Text")]
    public TMP_Text descriptionText;

    [Header("Action Buttons")]
    public Button switchButton;
    public Button returnButton;

    [Header("Prompt Text")]
    public TMP_Text promptText;

    [Header("Faction Data")]
    [SerializeField] private string currentSelectedFaction = "TianWangDian";

    private Dictionary<string, string> factionDescriptions = new Dictionary<string, string>();
    private TMP_Text tianWangDianText;
    private TMP_Text wuZhuangGuanText;
    private TMP_Text fangCunShanText;

    void Start()
    {
        tianWangDianText = tianWangDianButton.GetComponentInChildren<TMP_Text>();
        wuZhuangGuanText = wuZhuangGuanButton.GetComponentInChildren<TMP_Text>();
        fangCunShanText = fangCunShanButton.GetComponentInChildren<TMP_Text>();

        // Use FactionSkillDatabase to get descriptions
        factionDescriptions["TianWangDian"] = FactionSkillDatabase.GetFactionDescription("TianWangDian");
        factionDescriptions["WuZhuangGuan"] = FactionSkillDatabase.GetFactionDescription("WuZhuangGuan");
        factionDescriptions["FangCunShan"] = FactionSkillDatabase.GetFactionDescription("FangCunShan");

        // Add button listeners
        tianWangDianButton.onClick.AddListener(() => SelectFaction("TianWangDian"));
        wuZhuangGuanButton.onClick.AddListener(() => SelectFaction("WuZhuangGuan"));
        fangCunShanButton.onClick.AddListener(() => SelectFaction("FangCunShan"));

        switchButton.onClick.AddListener(SwitchFaction);
        returnButton.onClick.AddListener(ReturnToGame);

        // Legacy hardcoded content has been migrated to FactionSkillDatabase
        LoadCurrentFaction();
        UpdateFactionDisplay();

        if (promptText != null)
            promptText.text = "请选择你的门派";
    }

    void SelectFaction(string factionKey)
    {
        currentSelectedFaction = factionKey;
        UpdateFactionDisplay();
    }

    void UpdateFactionDisplay()
    {
        if (descriptionText != null && factionDescriptions.ContainsKey(currentSelectedFaction))
        {
            descriptionText.text = factionDescriptions[currentSelectedFaction];
        }

        // Update button highlights
        // ...existing highlight logic would go here
    }

    void SwitchFaction()
    {
        GameData.playerFaction = currentSelectedFaction;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
            Debug.Log($"Switched to faction: {currentSelectedFaction} and saved");
        }
        else
        {
            Debug.LogWarning("SaveManager.Instance does not exist, unable to save");
        }

        string displayName = currentSelectedFaction switch
        {
            "TianWangDian" => "天王殿",
            "WuZhuangGuan" => "五庄观",
            "FangCunShan" => "方寸山",
            _ => currentSelectedFaction
        };

        if (promptText != null)
            promptText.text = $"已切换到{displayName}！";
    }

    void LoadCurrentFaction()
    {
        if (!string.IsNullOrEmpty(GameData.playerFaction))
            currentSelectedFaction = GameData.playerFaction;
    }

    void ReturnToGame()
    {
        SceneController.LoadDemonTower();
    }
}