using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ItemAttribute
{
    public string attributeName;
    public string valueText;
    public AttributeType type;
    public int value;
}

[System.Serializable]
public class Item
{
    public int id;
    public string itemName;
    public string description;
    public ItemType type;
    public int count;
    [System.NonSerialized] public Sprite icon;          // 图标不序列化
    public string iconPath;                              // 保存图标资源路径

    public List<ItemAttribute> basicAttributes;
    public List<ItemAttribute> extraAttributes;
    public string requireLevelText;
    public string forgingLevelText;
    public int requireLevel;
    public int forgingLevel;
    public EquipSlot equipSlot;
    public ArtifactEffect artifactEffect = ArtifactEffect.None;

    // 在 Item 类中添加
    [System.NonSerialized] public List<ItemAttribute> originalBasicAttributes;
    [System.NonSerialized] public List<ItemAttribute> originalExtraAttributes;

    public const int TONG_TIAN_REWARD_ID = 20000;  // 通天认可令固定ID

    // 在 Item 类中添加或修改以下方法

    /// <summary>
    /// 备份原始基础属性（锻造前调用一次即可）
    /// </summary>
    public void BackupOriginalBasicAttributes()
    {
        if (originalBasicAttributes == null && basicAttributes != null)
        {
            originalBasicAttributes = new List<ItemAttribute>();
            foreach (var attr in basicAttributes)
                originalBasicAttributes.Add(new ItemAttribute
                {
                    attributeName = attr.attributeName,
                    valueText = attr.valueText,
                    type = attr.type,
                    value = attr.value
                });
        }
    }

    /// <summary>
    /// 备份原始附加属性（可选，用于其他用途，但锻造不改变它）
    /// </summary>
    public void BackupOriginalExtraAttributes()
    {
        if (originalExtraAttributes == null && extraAttributes != null)
        {
            originalExtraAttributes = new List<ItemAttribute>();
            foreach (var attr in extraAttributes)
                originalExtraAttributes.Add(new ItemAttribute
                {
                    attributeName = attr.attributeName,
                    valueText = attr.valueText,
                    type = attr.type,
                    value = attr.value
                });
        }
    }

    /// <summary>
    /// 根据当前锻造等级重新计算基础属性（附加属性不受影响）
    /// </summary>
    public void RecalcStatsByForgeLevel()
    {
        // 确保原始基础属性已备份
        if (originalBasicAttributes == null && basicAttributes != null)
            BackupOriginalBasicAttributes();

        // 如果没有原始备份或没有基础属性，则直接返回
        if (originalBasicAttributes == null || originalBasicAttributes.Count == 0)
            return;

        float multiplier = GetForgeMultiplier(forgingLevel);

        // 只重新计算基础属性
        if (basicAttributes != null)
        {
            // 确保 basicAttributes 数量与原始备份一致
            for (int i = 0; i < basicAttributes.Count && i < originalBasicAttributes.Count; i++)
            {
                int newValue = Mathf.RoundToInt(originalBasicAttributes[i].value * multiplier);
                basicAttributes[i].value = newValue;
                basicAttributes[i].valueText = $"+{newValue}";
            }
        }

        // 附加属性保持不变，无需重新计算
    }

    /// <summary>
    /// 获取锻造等级对应的属性倍率（累计倍率）
    /// </summary>
    public static float GetForgeMultiplier(int forgeLevel)
    {
        // 累计提升百分比：前5级每级14%，后4级递增15%、16%、17%、22%，总和140%
        // 索引0：锻造等级0（未强化）倍率1.0
        // 索引1：锻造等级1（强化到2级）倍率1.14
        // 索引2：锻造等级2（强化到3级）倍率1.28
        // 索引3：锻造等级3（强化到4级）倍率1.42
        // 索引4：锻造等级4（强化到5级）倍率1.56
        // 索引5：锻造等级5（强化到6级）倍率1.70
        // 索引6：锻造等级6（强化到7级）倍率1.85
        // 索引7：锻造等级7（强化到8级）倍率2.01
        // 索引8：锻造等级8（强化到9级）倍率2.18
        // 索引9：锻造等级9（强化到10级）倍率2.40
        float[] cumulativeBonus = { 0f, 0.14f, 0.28f, 0.42f, 0.56f, 0.70f, 0.85f, 1.01f, 1.18f, 1.40f };
        forgeLevel -= 1;
        if (forgeLevel < 0) forgeLevel = 0;
        if (forgeLevel > 9) forgeLevel = 9;
        return 1f + cumulativeBonus[forgeLevel];
    }
}

public enum ItemType { Consumable, Equipment, Material, Task, Artifact }

public enum AttributeType
{
    Constitution, Spirit, Strength, Agility,
    Health, Mana,
    Attack, Defense, Speed,
    ComboRate, StunRate, CritRate, HitRate, EvasionRate
}

public enum EquipSlot { Weapon, Armor, Necklace, Helmet, Bracelet, Belt, Boots }

public enum ArtifactEffect
{
    None, FenTianZhu, LeiShenChui, PoJunFu, XuanBingJia,
    JinGangSan, HuXinJing, FengLeiYi, LingFengPei, LunHuiJing, ZhenHunFan
}

