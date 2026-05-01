using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image iconImage;
    public TMP_Text countText;
    public ItemDetailPanel detailPanel;

    [Header("槽位类型")]
    public bool isEquipSlot;
    public bool isArtifactSlot;
    public EquipSlot equipSlotType;

    private Item currentItem;

    public void SetItem(Item item)
    {
        currentItem = item;
        if (item != null)
        {
            // 如果图标未加载且存在路径，则加载
            if (item.icon == null && !string.IsNullOrEmpty(item.iconPath))
            {
                item.icon = Resources.Load<Sprite>(item.iconPath);
            }
            iconImage.sprite = item.icon;
            countText.text = item.count > 1 ? item.count.ToString() : "";
            iconImage.enabled = true;
        }
        else
        {
            iconImage.enabled = false;
            countText.text = "";
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem != null && detailPanel != null)
        {
            SlotType sourceType = isEquipSlot ? SlotType.Equipment : (isArtifactSlot ? SlotType.Artifact : SlotType.None);
            detailPanel.SetItem(currentItem, sourceType, equipSlotType);
            detailPanel.gameObject.SetActive(true);
            detailPanel.transform.SetAsLastSibling();

            // 根据槽位位置调整详情面板的显示位置
            AdjustDetailPanelPosition();
        }
    }

    private void AdjustDetailPanelPosition()
    {
        // 获取相关组件
        RectTransform slotRect = GetComponent<RectTransform>();
        RectTransform detailRect = detailPanel.GetComponent<RectTransform>();
        if (slotRect == null || detailRect == null) return;

        // 获取 Canvas（假设 detailPanel 在 Canvas 下，用于坐标转换）
        Canvas canvas = detailPanel.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // 将槽位的世界坐标转换为屏幕坐标
        Vector2 slotScreenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, slotRect.position);

        // 屏幕中心
        float screenHalfWidth = Screen.width / 2f;

        // 详情面板的尺寸
        Vector2 detailSize = detailRect.sizeDelta;

        // 设置锚点为左上角 (0,1)，方便使用屏幕坐标定位
        detailRect.anchorMin = new Vector2(0, 1);
        detailRect.anchorMax = new Vector2(0, 1);
        detailRect.pivot = new Vector2(0, 1); // 以左上角为基准

        // 水平位置计算
        float xPos;
        if (slotScreenPos.x < screenHalfWidth)
        {
            // 物品在左半边，详情显示在右侧
            xPos = 640f; // 10像素偏移
            // if (xPos + detailSize.x > Screen.width)
            //     xPos = Screen.width - detailSize.x - 10; // 防止超出右边界
        }
        else
        {
            // 物品在右半边，详情显示在左侧
            xPos = -420f;
            // if (xPos < 0)
            //     xPos = 10; // 防止超出左边界
        }

        // 垂直位置：以槽位顶部为参考，并确保不超出屏幕上下
        float yPos = 120f; // 槽位顶部
        // if (yPos + detailSize.y > Screen.height)
        //     yPos = Screen.height - detailSize.y;
        // if (yPos < 0)
        //     yPos = 0;

        // 因为锚点在左上角，屏幕左上角坐标为 (0,0)，向下为正，所以 Y 需要取负
        detailRect.anchoredPosition = new Vector2(xPos, -yPos);
    }

    public void OnPointerEnter(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }
}

public enum SlotType { None, Equipment, Artifact }