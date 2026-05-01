using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

// 背包格子组件
public class ForgeInventorySlot : MonoBehaviour
{
    private Item item;
    private ItemFilterType filterType;
    private ForgeManager manager;

    public Image iconImage;
    public TMP_Text countText;
    public Button button;

    public void Setup(Item item, ItemFilterType filter, ForgeManager mgr)
    {
        this.item = item;
        this.filterType = filter;
        this.manager = mgr;

        if (iconImage == null) iconImage = GetComponent<Image>();
        if (button == null) button = GetComponent<Button>();
        if (countText == null) countText = GetComponentInChildren<TMP_Text>();

        if (item.icon != null)
            iconImage.sprite = item.icon;
        countText.text = item.count > 1 ? item.count.ToString() : "";

        // 清除旧监听，避免重复
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            if (manager != null)
                manager.OnItemSelected(item, filterType);
            else
                Debug.LogError("ForgeManager is null in ForgeInventorySlot");
        });
    }
}