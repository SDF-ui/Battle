using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;

public class ItemDetailPanel : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text itemNameText;
    public TMP_Text descriptionText;

    [Header("属性容器")]
    public Transform extraAttributeContainer;   // 放置额外属性（ExtraInfo Container）
    public Transform basicAttributeContainer;   // 放置基础属性（Basic Attribute Container）
    public GameObject attributeRowPrefab;        // 属性行预设（需包含两个TMP_Text子物体）

    [Header("额外信息")]
    public TMP_Text requireLevelText;            // 需求等级
    public TMP_Text forgingLevelText;            // 锻造等级

    [Header("操作按钮")]
    public Button useButton;
    public Button unequipButton;

    private Item currentItem;

    private SlotType currentSlotType;
    private bool isFromEquipSlot; // 标记当前物品是否来自装备槽    
    private EquipSlot currentEquipSlot; // 如果来自装备槽，记录部位

    private Coroutine longPressCoroutine;

    void Awake()
    {
        unequipButton.onClick.AddListener(OnUnequipClicked);
        gameObject.SetActive(false); // 确保默认关闭

        // 为 useButton 添加长按支持
        SetupButtonLongPress(useButton);
    }

    public void SetItem(Item item, SlotType slotType = SlotType.None, EquipSlot equipSlot = EquipSlot.Weapon)
    {
        currentItem = item;
        currentSlotType = slotType;
        currentEquipSlot = equipSlot;

        // 根据来源显示按钮
        useButton.gameObject.SetActive(item != null && slotType == SlotType.None);
        unequipButton.gameObject.SetActive(item != null && (slotType == SlotType.Equipment || slotType == SlotType.Artifact));

        if (item == null) return;

        // 清空两个容器
        ClearContainer(extraAttributeContainer);
        ClearContainer(basicAttributeContainer);

        // 基础信息
        itemNameText.text = item.itemName;
        // 如果是人参果，动态追加已使用数量
        if (item.itemName.Contains("人参果"))
        {
            int used = GameData.ginsengFruitUsedCount;
            int remaining = 120 - used;
            descriptionText.text = $"{item.description}\n<color=#FFD700>已使用：{used}/120";
        }
        else
        {
            descriptionText.text = item.description;
        }

        // 填充额外属性
        if (item.extraAttributes != null)
        {
            foreach (var attr in item.extraAttributes)
                CreateAttributeRow(extraAttributeContainer, attr);
        }

        // 填充基础属性
        if (item.basicAttributes != null)
        {
            foreach (var attr in item.basicAttributes)
                CreateAttributeRow(basicAttributeContainer, attr);
        }

        // 额外信息
        requireLevelText.text = $"需求等级：{item.requireLevelText}";
        forgingLevelText.text = $"锻造等级：{item.forgingLevelText}";
    }



    private void CreateAttributeRow(Transform parent, ItemAttribute attr)
    {
        GameObject row = Instantiate(attributeRowPrefab, parent);
        TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();
        if (texts.Length >= 2)
        {
            // 假设第一个TMP_Text用于属性名称，第二个用于数值
            texts[0].text = attr.attributeName;
            texts[1].text = attr.valueText;
        }
        else
        {
            Debug.LogWarning("属性行预设缺少足够的TMP_Text组件");
        }
    }

    private void SetupButtonLongPress(Button btn)
    {
        if (btn == null) return;

        EventTrigger trigger = btn.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) =>
        {
            longPressCoroutine = StartCoroutine(LongPressCoroutine());
        });
        trigger.triggers.Add(pointerDown);

        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) =>
        {
            if (longPressCoroutine != null)
            {
                StopCoroutine(longPressCoroutine);
                longPressCoroutine = null;
            }
        });
        trigger.triggers.Add(pointerUp);
    }

    private IEnumerator LongPressCoroutine()
    {
        UseItem();
        yield return new WaitForSeconds(0.5f);
        while (true)
        {
            UseItem();
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void UseItem()
    {
        if (currentItem == null || currentSlotType != SlotType.None) return;

        if (currentItem.type == ItemType.Consumable)
        {
            UseConsumable();
        }
        else if (currentItem.type == ItemType.Equipment)
        {
            BackpackManager.Instance.EquipItem(currentItem, currentItem.equipSlot);
            FindObjectOfType<FantasyStatusPanel>()?.RefreshEquipment();
            ClosePanel();
        }
        else if (currentItem.type == ItemType.Artifact)
        {
            if (BackpackManager.Instance.EquipArtifact(currentItem))
            {
                FindObjectOfType<FantasyStatusPanel>()?.RefreshArtifacts();
            }
            else
            {
                Debug.LogWarning("法宝槽已满，无法装备");
                // 可在此显示提示
            }
            ClosePanel();
        }
    }

    private void UseConsumable()
    {
        if (currentItem == null) return;

        if (currentItem.itemName.Contains("人参果"))
        {
            if (GameData.ginsengFruitUsedCount >= 120)
            {
                Debug.Log("已经使用了120个人参果，无法继续使用！");
                // 达到上限，关闭面板并停止长按
                ClosePanel();
                return;
            }

            if (BackpackManager.Instance == null)
            {
                Debug.LogError("BackpackManager 不存在！");
                return;
            }

            if (BackpackManager.Instance.ReduceItemCount(currentItem, 1))
            {
                GameData.ginsengFruitUsedCount++;
                GameData.unallocatedPoints++;
                if (SaveManager.Instance != null)
                    SaveManager.Instance.SaveGame();

                // 安全刷新 UI
                var statusPanel = FindObjectOfType<FantasyStatusPanel>();
                if (statusPanel != null) statusPanel.RefreshStats();

                var charAttr = FindObjectOfType<CharacterAttribute>();
                if (charAttr != null) charAttr.RefreshUI();

                // 刷新当前物品的显示（数量、已使用次数等）
                SetItem(currentItem, currentSlotType, currentEquipSlot);

                // 如果物品数量变为 0，关闭面板并停止长按
                if (currentItem.count <= 0)
                {
                    Debug.Log("人参果已用完，关闭面板");
                    ClosePanel();
                }

                Debug.Log($"使用人参果，已使用 {GameData.ginsengFruitUsedCount}/120，未分配点数 +1");
            }
            else
            {
                Debug.Log("人参果数量不足或使用失败");
                // 使用失败（可能物品不存在或数量不足），关闭面板
                ClosePanel();
            }
        }
        else
        {
            Debug.Log($"使用消耗品：{currentItem.itemName}，暂未实现效果");
        }
    }

    void OnUseClicked()
    {
        // 短按直接调用 UseItem
        // UseItem();
    }

    void OnUnequipClicked()
    {
        if (currentItem == null) return;

        if (currentSlotType == SlotType.Equipment)
        {
            BackpackManager.Instance.UnequipItem(currentEquipSlot);
            FindObjectOfType<FantasyStatusPanel>()?.RefreshEquipment();
        }
        else if (currentSlotType == SlotType.Artifact)
        {
            int index = BackpackManager.Instance.FindArtifactSlot(currentItem);
            if (index >= 0)
            {
                BackpackManager.Instance.UnequipArtifact(index);
                FindObjectOfType<FantasyStatusPanel>()?.RefreshArtifacts();
            }
        }
        ClosePanel();
    }


    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);
    }

    public void ClosePanel()
    {
        // 停止长按协程，防止关闭面板后继续调用
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
        gameObject.SetActive(false);
    }
}