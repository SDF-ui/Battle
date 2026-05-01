using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class ForgeInventoryUI : MonoBehaviour
{
    public GameObject itemSlotPrefab;
    public Transform contentParent;
    public ScrollRect scrollRect;

    private ForgeManager forgeManager;
    private ItemFilterType currentFilter;
    private List<Item> filteredItems = new List<Item>();
    private List<GameObject> slotObjects = new List<GameObject>();

    public void Initialize(ForgeManager manager)
    {
        forgeManager = manager;
        RefreshInventory();
    }

    public void SetFilter(ItemFilterType filter)
    {
        currentFilter = filter;
        RefreshInventory();
    }

    public void RefreshInventory()
    {
        if (BackpackManager.Instance == null)
        {
            Debug.LogError("BackpackManager.Instance is null!");
            return;
        }

        var allItems = BackpackManager.Instance.GetAllItems();

        // 筛选
        switch (currentFilter)
        {
            case ItemFilterType.Equipment:
                // 只显示锻造等级小于10的装备（未满级）
                filteredItems = allItems.Where(i => i.type == ItemType.Equipment && i.forgingLevel < 10).ToList();
                break;
            case ItemFilterType.LuckStone:
                filteredItems = allItems.Where(i => i.type == ItemType.Material &&
                    (i.itemName.Contains("幸运符") || i.itemName.Contains("高级幸运符"))).ToList();
                break;
            case ItemFilterType.ProtectStone:
                filteredItems = allItems.Where(i => i.type == ItemType.Material && i.itemName.Contains("防碎石")).ToList();
                break;
            case ItemFilterType.EnhanceStone:
                filteredItems = allItems.Where(i => i.type == ItemType.Material && i.itemName.Contains("强化石")).ToList();
                break;
        }

        // 1. 先将所有现有格子隐藏
        BackpackManager.Instance.CloseSlotObjects();
        foreach (var slot in slotObjects)
        {
            if (slot != null)
                slot.SetActive(false);
        }

        // 2. 按需显示/创建格子
        for (int i = 0; i < filteredItems.Count; i++)
        {
            GameObject slotObj;
            if (i < slotObjects.Count)
            {
                slotObj = slotObjects[i];
                slotObj.SetActive(true);
            }
            else
            {
                slotObj = Instantiate(itemSlotPrefab, contentParent);
                slotObjects.Add(slotObj);
            }

            var slotComp = slotObj.GetComponent<ForgeInventorySlot>();
            if (slotComp == null)
                slotComp = slotObj.AddComponent<ForgeInventorySlot>();
            slotComp.Setup(filteredItems[i], currentFilter, forgeManager);
        }

        // 3. 强制重新布局（确保隐藏的格子不占位）
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
    }
}