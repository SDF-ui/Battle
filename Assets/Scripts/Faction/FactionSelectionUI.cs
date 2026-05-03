using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class FactionSelectionUI : MonoBehaviour
{
    [Header("门派按钮")]
    public Button tianWangDianButton;
    public Button wuZhuangGuanButton;
    public Button fangCunShanButton;

    [Header("描述文本")]
    public TMP_Text descriptionText;

    [Header("操作按钮")]
    public Button switchButton;
    public Button returnButton;

    [Header("提示文本")]
    public TMP_Text promptText;

    [Header("门派数据")]
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

        InitDescriptions();

        tianWangDianButton.onClick.AddListener(() => ShowFactionInfo("TianWangDian"));
        wuZhuangGuanButton.onClick.AddListener(() => ShowFactionInfo("WuZhuangGuan"));
        fangCunShanButton.onClick.AddListener(() => ShowFactionInfo("FangCunShan"));

        switchButton.onClick.AddListener(SwitchToSelectedFaction);
        returnButton.onClick.AddListener(ReturnToPreviousScene);

        string playerFaction = GameData.playerFaction;
        if (string.IsNullOrEmpty(playerFaction)) playerFaction = "TianWangDian";
        ShowFactionInfo(playerFaction);

        if (promptText != null) promptText.gameObject.SetActive(false);
    }

    private void InitDescriptions()
    {
        var allInfos = FactionSkillDatabase.GetAllFactionInfos();
        foreach (var kv in allInfos)
            factionDescriptions[kv.Key] = kv.Value.GetFullDescription();
        return; // 旧版硬编码内容已迁移到 FactionSkillDatabase
    }

    private void ShowFactionInfo(string factionKey)
    {
        currentSelectedFaction = factionKey;
        if (descriptionText != null && factionDescriptions.ContainsKey(factionKey))
            descriptionText.text = factionDescriptions[factionKey];

        if (tianWangDianText != null)
            tianWangDianText.fontStyle = (factionKey == "TianWangDian") ? FontStyles.Bold : FontStyles.Normal;
        if (wuZhuangGuanText != null)
            wuZhuangGuanText.fontStyle = (factionKey == "WuZhuangGuan") ? FontStyles.Bold : FontStyles.Normal;
        if (fangCunShanText != null)
            fangCunShanText.fontStyle = (factionKey == "FangCunShan") ? FontStyles.Bold : FontStyles.Normal;
    }

    private void SwitchToSelectedFaction()
    {
        GameData.playerFaction = currentSelectedFaction;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
            Debug.Log($"已切换到门派: {currentSelectedFaction} 并保存");
        }
        else
        {
            Debug.LogWarning("SaveManager.Instance 不存在，无法保存");
        }

        if (promptText != null)
        {
            string displayName = currentSelectedFaction switch
            {
                "TianWangDian" => "天王殿",
                "WuZhuangGuan" => "五庄观",
                "FangCunShan" => "方寸山",
                _ => currentSelectedFaction
            };
            promptText.text = $"已切换到{displayName}！";
            promptText.gameObject.SetActive(true);
            StopCoroutine(HidePromptAfterDelay());
            StartCoroutine(HidePromptAfterDelay());
        }
    }

    private IEnumerator HidePromptAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private void ReturnToPreviousScene()
    {
        SceneManager.LoadScene("Demon Tower");
    }
}