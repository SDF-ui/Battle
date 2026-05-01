using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ForgeManager : MonoBehaviour
{
    [Header("锻造页面引用")]
    public ForgeInventoryUI forgeInventory;
    public TMP_Text itemNameText;
    public TMP_Text mainAttributeText;
    public TMP_Text subAttributesText;
    public TMP_Text forgingLevelText;
    public TMP_Text successRateText;
    public TMP_Text breakRateText;
    public TMP_Text forgeResultText; // 锻造结果文本

    [Header("选中的材料显示")]
    public Image selectedEquipmentIcon;
    public TMP_Text selectedEquipmentName;
    public Image selectedLuckStoneIcon;
    public TMP_Text selectedLuckStoneName;
    public Image selectedProtectStoneIcon;
    public TMP_Text selectedProtectStoneName;
    public Image selectedEnhanceStoneIcon;
    public TMP_Text selectedEnhanceStoneCount;

    [Header("按钮")]
    public Button forgeButton;
    public Button returnButton;

    [Header("顶部过滤按钮")]
    public Button equipmentFilterButton;
    public Button luckStoneFilterButton;
    public Button protectStoneFilterButton;
    public Button enhanceStoneFilterButton;

    private Item currentEquipment;
    private Item currentLuckStone;
    private Item currentProtectStone;
    private Item currentEnhanceStone;

    private ItemFilterType currentFilter = ItemFilterType.Equipment;

    // 双击检测相关
    private float lastClickTime_LuckStone;
    private float lastClickTime_ProtectStone;
    private float lastClickTime_EnhanceStone;

    // 锻造结果消息队列（存储消息及其协程）
    private class QueuedMessage
    {
        public string message;
        public Coroutine coroutine;
    }
    private List<QueuedMessage> resultMessages = new List<QueuedMessage>();
    private const int MAX_MESSAGES = 5;

    private readonly float[] baseSuccessRates = { 1.0f, 0.95f, 0.90f, 0.85f, 0.80f, 0.70f, 0.60f, 0.50f, 0.40f, 0.30f };
    private readonly float[] breakRates = { 0f, 0f, 0f, 0f, 0f, 0.10f, 0.20f, 0.30f, 0.40f, 0.50f };

    private void Start()
    {
        forgeButton.onClick.AddListener(OnForgeClicked);
        returnButton.onClick.AddListener(OnReturnClicked);

        equipmentFilterButton.onClick.AddListener(() => SetFilter(ItemFilterType.Equipment));
        luckStoneFilterButton.onClick.AddListener(() => SetFilter(ItemFilterType.LuckStone));
        protectStoneFilterButton.onClick.AddListener(() => SetFilter(ItemFilterType.ProtectStone));
        enhanceStoneFilterButton.onClick.AddListener(() => SetFilter(ItemFilterType.EnhanceStone));

        StartCoroutine(DelayedInitialize());
    }

    private IEnumerator DelayedInitialize()
    {
        yield return null;
        if (BackpackManager.Instance != null && BackpackManager.Instance.GetAllItems().Count == 0)
            yield return null;

        forgeInventory.Initialize(this);
        SetFilter(ItemFilterType.Equipment);
        RefreshEnhanceStone();
        RefreshProtectStone();
        UpdateUI();
    }

    private void SetFilter(ItemFilterType filter)
    {
        currentFilter = filter;
        forgeInventory.SetFilter(filter);
    }

    public void OnItemSelected(Item item, ItemFilterType type)
    {
        float currentTime = Time.time;
        float doubleClickInterval = 0.3f;

        switch (type)
        {
            case ItemFilterType.Equipment:
                currentEquipment = item;
                break;

            case ItemFilterType.LuckStone:
                if (currentLuckStone == item && currentTime - lastClickTime_LuckStone <= doubleClickInterval)
                    currentLuckStone = null;
                else
                    currentLuckStone = item;
                lastClickTime_LuckStone = currentTime;
                break;

            case ItemFilterType.ProtectStone:
                if (currentProtectStone == item && currentTime - lastClickTime_ProtectStone <= doubleClickInterval)
                    currentProtectStone = null;
                else
                    currentProtectStone = item;
                lastClickTime_ProtectStone = currentTime;
                break;

            case ItemFilterType.EnhanceStone:
                if (currentEnhanceStone == item && currentTime - lastClickTime_EnhanceStone <= doubleClickInterval)
                    currentEnhanceStone = null;
                else
                    currentEnhanceStone = item;
                lastClickTime_EnhanceStone = currentTime;
                break;
        }
        UpdateUI();
    }

    private void RefreshEnhanceStone()
    {
        var stones = BackpackManager.Instance.GetItemsByType(ItemType.Material)
            .Where(i => i.itemName.Contains("强化石")).ToList();
        if (stones.Count > 0)
            currentEnhanceStone = stones[0];
        else
            currentEnhanceStone = null;
        UpdateUI();
    }

    private void RefreshProtectStone()
    {
        if (currentProtectStone == null)
        {
            var stones = BackpackManager.Instance.GetItemsByType(ItemType.Material)
                .Where(i => i.itemName.Contains("防碎石")).ToList();
            if (stones.Count > 0)
                currentProtectStone = stones[0];
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        Color hasItemColor = Color.white;
        Color noItemColor = new Color(0x07 / 255f, 0x48 / 255f, 0x89 / 255f);

        // 装备信息
        if (currentEquipment != null)
        {
            itemNameText.text = currentEquipment.itemName;
            if (currentEquipment.basicAttributes != null && currentEquipment.basicAttributes.Count > 0)
                mainAttributeText.text = string.Join("\n", currentEquipment.basicAttributes.Select(a => $"{a.attributeName} +{a.value}"));
            else
                mainAttributeText.text = "无";

            if (currentEquipment.extraAttributes != null && currentEquipment.extraAttributes.Count > 0)
                subAttributesText.text = string.Join("\n", currentEquipment.extraAttributes.Select(a => $"{a.attributeName} +{a.value}"));
            else
                subAttributesText.text = "无";

            forgingLevelText.text = $"{currentEquipment.forgingLevel}";
            int nextLevel = currentEquipment.forgingLevel + 1;
            if (nextLevel <= 10)
            {
                float successRate = GetSuccessRate(currentEquipment.forgingLevel);
                float breakRate = GetBreakRate(currentEquipment.forgingLevel);
                successRateText.text = $"{successRate * 100:F0}%";
                breakRateText.text = $"{breakRate * 100:F0}%";
            }
            else
            {
                successRateText.text = "已满级";
                breakRateText.text = "无";
            }

            if (currentEquipment.icon != null)
                selectedEquipmentIcon.sprite = currentEquipment.icon;
            selectedEquipmentName.text = currentEquipment.itemName;
            selectedEquipmentIcon.color = hasItemColor;
        }
        else
        {
            itemNameText.text = "未选择装备";
            mainAttributeText.text = "-";
            subAttributesText.text = "-";
            forgingLevelText.text = "-";
            successRateText.text = "-";
            breakRateText.text = "-";
            selectedEquipmentIcon.sprite = null;
            selectedEquipmentName.text = "无";
            selectedEquipmentIcon.color = noItemColor;
        }

        // 幸运符
        if (currentLuckStone != null)
        {
            selectedLuckStoneIcon.sprite = currentLuckStone.icon;
            selectedLuckStoneName.text = currentLuckStone.itemName;
            selectedLuckStoneIcon.color = hasItemColor;
        }
        else
        {
            selectedLuckStoneIcon.sprite = null;
            selectedLuckStoneName.text = "幸运符";
            selectedLuckStoneIcon.color = noItemColor;
        }

        // 防碎石
        if (currentProtectStone != null)
        {
            selectedProtectStoneIcon.sprite = currentProtectStone.icon;
            selectedProtectStoneName.text = currentProtectStone.itemName;
            selectedProtectStoneIcon.color = hasItemColor;
        }
        else
        {
            selectedProtectStoneIcon.sprite = null;
            selectedProtectStoneName.text = "防碎石";
            selectedProtectStoneIcon.color = noItemColor;
        }

        // 强化石
        if (currentEnhanceStone != null)
        {
            selectedEnhanceStoneIcon.sprite = currentEnhanceStone.icon;
            selectedEnhanceStoneCount.text = "强化石";
            selectedEnhanceStoneIcon.color = hasItemColor;
        }
        else
        {
            selectedEnhanceStoneIcon.sprite = null;
            selectedEnhanceStoneCount.text = "强化石";
            selectedEnhanceStoneIcon.color = noItemColor;
        }

        bool canForge = currentEquipment != null && currentEquipment.forgingLevel < 10
                        && currentEnhanceStone != null && currentEnhanceStone.count >= 1;
        forgeButton.interactable = canForge;
    }

    private float GetSuccessRate(int currentLevel)
    {
        if (currentLevel >= 10) return 0;
        int index = currentLevel - 1;
        if (index < 0) index = 0;
        if (index >= baseSuccessRates.Length) return 0;
        float baseRate = baseSuccessRates[index];
        float bonus = 0f;
        if (currentLuckStone != null)
        {
            if (currentLuckStone.itemName.Contains("高级幸运符"))
                bonus = 0.20f;
            else if (currentLuckStone.itemName.Contains("幸运符"))
                bonus = 0.10f;
        }
        return Mathf.Min(baseRate + bonus, 1.0f);
    }

    private float GetBreakRate(int currentLevel)
    {
        if (currentLevel < 5) return 0f;
        return breakRates[currentLevel];
    }

    private void ShowResultMessage(string message)
    {
        // 创建新消息对象
        QueuedMessage newMsg = new QueuedMessage { message = message };
        // 启动协程，1秒后自动移除该消息
        newMsg.coroutine = StartCoroutine(RemoveMessageAfterDelay(newMsg));
        resultMessages.Add(newMsg);

        // 如果超过最大数量，移除最早的一条，并停止其协程
        if (resultMessages.Count > MAX_MESSAGES)
        {
            QueuedMessage oldest = resultMessages[0];
            if (oldest.coroutine != null)
                StopCoroutine(oldest.coroutine);
            resultMessages.RemoveAt(0);
        }

        UpdateResultText();
    }

    private IEnumerator RemoveMessageAfterDelay(QueuedMessage msg)
    {
        yield return new WaitForSeconds(1f);
        if (resultMessages.Contains(msg))
        {
            resultMessages.Remove(msg);
            UpdateResultText();
        }
    }

    private void UpdateResultText()
    {
        if (forgeResultText != null)
            forgeResultText.text = string.Join("\n", resultMessages.Select(m => m.message));
    }

    private void OnForgeClicked()
    {
        if (currentEquipment == null || currentEnhanceStone == null || currentEnhanceStone.count < 1)
        {
            ShowResultMessage("锻造条件不足");
            Debug.LogWarning("锻造条件不足");
            return;
        }

        int currentLv = currentEquipment.forgingLevel;
        if (currentLv >= 10)
        {
            ShowResultMessage("装备已达最高锻造等级");
            Debug.LogWarning("装备已达最高锻造等级");
            return;
        }

        float successChance = GetSuccessRate(currentLv);
        float breakChance = GetBreakRate(currentLv);

        bool isSuccess = Random.value < successChance;
        bool isBroken = false;
        bool consumeProtectStone = false;
        string resultMessage = "";

        if (!isSuccess)
        {
            bool willBreak = Random.value < breakChance;
            if (willBreak)
            {
                if (currentProtectStone != null)
                {
                    consumeProtectStone = true;
                    isBroken = false;
                    resultMessage = $"锻造失败，防碎石生效，装备未破碎";
                    Debug.Log(resultMessage);
                }
                else
                {
                    isBroken = true;
                    resultMessage = $"锻造失败，装备已破碎";
                    Debug.Log(resultMessage);
                }
            }
            else
            {
                resultMessage = $"锻造失败，装备未升级";
                Debug.Log(resultMessage);
            }
        }
        else
        {
            resultMessage = $"锻造成功！{currentEquipment.itemName} 升至 +{currentEquipment.forgingLevel + 1}";
            Debug.Log(resultMessage);
        }

        // 消耗材料
        BackpackManager.Instance.ReduceItemCount(currentEnhanceStone, 1);
        if (currentLuckStone != null)
            BackpackManager.Instance.ReduceItemCount(currentLuckStone, 1);
        if (consumeProtectStone)
            BackpackManager.Instance.ReduceItemCount(currentProtectStone, 1);

        if (isBroken)
        {
            BackpackManager.Instance.RemoveItem(currentEquipment);
            currentEquipment = null;
        }
        else if (isSuccess)
        {
            UpgradeEquipment(currentEquipment);
            BackpackManager.Instance.SaveToGameData();
            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();
        }

        ShowResultMessage(resultMessage);

        if (forgeInventory != null)
            forgeInventory.RefreshInventory();
        RefreshEnhanceStone();
        RefreshProtectStone();
        UpdateUI();

        if (FantasyStatusPanel.Instance != null)
            FantasyStatusPanel.Instance.RefreshStats();
    }

    private void UpgradeEquipment(Item item)
    {
        int newLevel = item.forgingLevel + 1;
        item.forgingLevel = newLevel;
        item.forgingLevelText = newLevel.ToString();
        item.RecalcStatsByForgeLevel();
    }

    private void OnReturnClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Demon Tower");
    }
}

public enum ItemFilterType
{
    Equipment,
    LuckStone,
    ProtectStone,
    EnhanceStone
}