using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
public static class GameData
{
    // 玩家基础属性
    public static int playerLevel = 70;
    public static int unallocatedPoints = 0;               // 新增：未分配属性点
    public static int playerAllocatedCON = 0;
    public static int playerAllocatedINT = 0;
    public static int playerAllocatedSTR = 0;
    public static int playerAllocatedAGI = 0;
    public static int playerExtraCON = 0;
    public static int playerExtraINT = 0;
    public static int playerExtraSTR = 0;
    public static int playerExtraAGI = 0;
    public static string playerFaction = "WuZhuangGuan";

    public static int currentFloor = 1; // 当前层数

    // 背包数据
    public static Item[] backpackSlots = new Item[64];
    public static Item[] equippedItems = new Item[7];
    public static Item[] artifactSlots = new Item[3];

    // 新增：计算已分配的总点数
    public static int TotalAllocatedPoints => playerAllocatedCON + playerAllocatedINT + playerAllocatedSTR + playerAllocatedAGI;

    // 新增：已使用人参果数量（最多120）
    public static int ginsengFruitUsedCount = 0;
    // 修改：允许的最大分配点数 = 等级点数 + 已使用人参果数
    public static int MaxAllowedAllocatedPoints => (playerLevel - 1) * 4 + ginsengFruitUsedCount;



    // 新增：校验当前分配是否合法，若不合法则自动修正（例如加载旧存档时）
    public static void ValidateAllocatedPoints()
    {
        int max = MaxAllowedAllocatedPoints;
        int current = TotalAllocatedPoints;
        if (current > max)
        {
            // 超出的部分从最高属性中削减（可自定义削减策略）
            int excess = current - max;
            // 简单策略：依次从各属性中减去，直到消除超限
            if (playerAllocatedSTR >= excess) playerAllocatedSTR -= excess;
            else if (playerAllocatedINT >= excess) playerAllocatedINT -= excess;
            else if (playerAllocatedAGI >= excess) playerAllocatedAGI -= excess;
            else if (playerAllocatedCON >= excess) playerAllocatedCON -= excess;
            else
            {
                // 多属性共同削减
                int[] values = { playerAllocatedSTR, playerAllocatedINT, playerAllocatedAGI, playerAllocatedCON };
                int total = values.Sum();
                if (total > 0)
                {
                    playerAllocatedSTR = Mathf.RoundToInt(playerAllocatedSTR * (1 - (float)excess / total));
                    playerAllocatedINT = Mathf.RoundToInt(playerAllocatedINT * (1 - (float)excess / total));
                    playerAllocatedAGI = Mathf.RoundToInt(playerAllocatedAGI * (1 - (float)excess / total));
                    playerAllocatedCON = Mathf.RoundToInt(playerAllocatedCON * (1 - (float)excess / total));
                }
            }
            Debug.LogWarning($"属性点超限，已自动修正：移除 {excess} 点");
        }

        // 确保非负
        playerAllocatedSTR = Mathf.Max(0, playerAllocatedSTR);
        playerAllocatedINT = Mathf.Max(0, playerAllocatedINT);
        playerAllocatedAGI = Mathf.Max(0, playerAllocatedAGI);
        playerAllocatedCON = Mathf.Max(0, playerAllocatedCON);
    }

    public static void CleanupInvalidItems()
    {
        for (int i = 0; i < backpackSlots.Length; i++)
            if (backpackSlots[i] != null && string.IsNullOrEmpty(backpackSlots[i].itemName))
                backpackSlots[i] = null;

        for (int i = 0; i < equippedItems.Length; i++)
            if (equippedItems[i] != null && string.IsNullOrEmpty(equippedItems[i].itemName))
                equippedItems[i] = null;

        for (int i = 0; i < artifactSlots.Length; i++)
            if (artifactSlots[i] != null && string.IsNullOrEmpty(artifactSlots[i].itemName))
                artifactSlots[i] = null;
    }

    public static void CopyToBackpack(Item[] target)
    {
        for (int i = 0; i < target.Length && i < backpackSlots.Length; i++)
            target[i] = backpackSlots[i];
    }

    public static void CopyFromBackpack(Item[] source)
    {
        for (int i = 0; i < source.Length && i < backpackSlots.Length; i++)
            backpackSlots[i] = source[i];
    }

    public static void CopyToEquipped(Item[] target)
    {
        for (int i = 0; i < target.Length && i < equippedItems.Length; i++)
            target[i] = equippedItems[i];
    }

    public static void CopyFromEquipped(Item[] source)
    {
        for (int i = 0; i < source.Length && i < equippedItems.Length; i++)
            equippedItems[i] = source[i];
    }

    public static void CopyToArtifact(Item[] target)
    {
        for (int i = 0; i < target.Length && i < artifactSlots.Length; i++)
            target[i] = artifactSlots[i];
    }

    public static void CopyFromArtifact(Item[] source)
    {
        for (int i = 0; i < source.Length && i < artifactSlots.Length; i++)
            artifactSlots[i] = source[i];
    }

    /// <summary>
    /// 添加奖励道具到背包（直接操作 GameData 背包数组）
    /// </summary>
    /// <param name="item">要添加的道具</param>
    /// <returns>是否添加成功（背包未满）</returns>
    public static bool AddRewardItem(Item item)
    {
        if (item == null) return false;

        // 先尝试堆叠：查找相同id且类型为Material/Consumable的物品
        for (int i = 0; i < backpackSlots.Length; i++)
        {
            if (backpackSlots[i] != null && backpackSlots[i].id == item.id && backpackSlots[i].type == item.type)
            {
                backpackSlots[i].count += item.count;
                return true;
            }
        }

        // 没有找到可堆叠的，查找空位
        for (int i = 0; i < backpackSlots.Length; i++)
        {
            if (backpackSlots[i] == null)
            {
                backpackSlots[i] = item;
                return true;
            }
        }

        Debug.LogWarning("背包已满，无法添加奖励道具");
        return false;
    }

}