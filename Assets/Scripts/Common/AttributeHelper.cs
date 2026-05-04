using System.Collections.Generic;

/// <summary>
/// 属性辅助工具类 - 统一处理装备/法宝属性的累加逻辑
/// 消除 CharacterStatsCalculator 和 FantasyStatusPanel 中的重复 switch 代码
/// </summary>
public static class AttributeHelper
{
    /// <summary>
    /// 累加一个物品属性到对应的累计变量中（用于装备）
    /// </summary>
    public static void AddAttribute(
        ItemAttribute attr,
        ref int con, ref int intel, ref int str, ref int agi,
        ref int hp, ref int mp, ref int atk, ref int def, ref int spd,
        ref int crit, ref int hit, ref int eva, ref int combo, ref int stun)
    {
        switch (attr.type)
        {
            case AttributeType.Constitution: con += attr.value; break;
            case AttributeType.Spirit: intel += attr.value; break;
            case AttributeType.Strength: str += attr.value; break;
            case AttributeType.Agility: agi += attr.value; break;
            case AttributeType.Health: hp += attr.value; break;
            case AttributeType.Mana: mp += attr.value; break;
            case AttributeType.Attack: atk += attr.value; break;
            case AttributeType.Defense: def += attr.value; break;
            case AttributeType.Speed: spd += attr.value; break;
            case AttributeType.CritRate: crit += attr.value; break;
            case AttributeType.HitRate: hit += attr.value; break;
            case AttributeType.EvasionRate: eva += attr.value; break;
            case AttributeType.ComboRate: combo += attr.value; break;
            case AttributeType.StunRate: stun += attr.value; break;
        }
    }

    /// <summary>
    /// 累加单个物品的所有属性（基础属性 + 额外属性）到累计变量中
    /// </summary>
    public static void AddItemAttributes(
        Item item,
        ref int con, ref int intel, ref int str, ref int agi,
        ref int hp, ref int mp, ref int atk, ref int def, ref int spd,
        ref int crit, ref int hit, ref int eva, ref int combo, ref int stun)
    {
        if (item == null) return;

        if (item.basicAttributes != null)
        {
            foreach (var attr in item.basicAttributes)
            {
                AddAttribute(attr,
                    ref con, ref intel, ref str, ref agi,
                    ref hp, ref mp, ref atk, ref def, ref spd,
                    ref crit, ref hit, ref eva, ref combo, ref stun);
            }
        }

        if (item.extraAttributes != null)
        {
            foreach (var attr in item.extraAttributes)
            {
                AddAttribute(attr,
                    ref con, ref intel, ref str, ref agi,
                    ref hp, ref mp, ref atk, ref def, ref spd,
                    ref crit, ref hit, ref eva, ref combo, ref stun);
            }
        }
    }

    /// <summary>
    /// 累加物品列表中的所有属性
    /// </summary>
    public static void AddItemsAttributes(
        IEnumerable<Item> items,
        ref int con, ref int intel, ref int str, ref int agi,
        ref int hp, ref int mp, ref int atk, ref int def, ref int spd,
        ref int crit, ref int hit, ref int eva, ref int combo, ref int stun)
    {
        if (items == null) return;

        foreach (var item in items)
        {
            AddItemAttributes(item,
                ref con, ref intel, ref str, ref agi,
                ref hp, ref mp, ref atk, ref def, ref spd,
                ref crit, ref hit, ref eva, ref combo, ref stun);
        }
    }

    /// <summary>
    /// 获取属性类型的中文名称
    /// </summary>
    public static string GetAttributeDisplayName(AttributeType type)
    {
        switch (type)
        {
            case AttributeType.Constitution: return "体质";
            case AttributeType.Spirit: return "灵力";
            case AttributeType.Strength: return "力量";
            case AttributeType.Agility: return "敏捷";
            case AttributeType.Health: return "生命";
            case AttributeType.Mana: return "内力";
            case AttributeType.Attack: return "攻击力";
            case AttributeType.Defense: return "防御力";
            case AttributeType.Speed: return "速度";
            case AttributeType.CritRate: return "暴击率";
            case AttributeType.ComboRate: return "连击率";
            case AttributeType.StunRate: return "晕击率";
            case AttributeType.HitRate: return "命中率";
            case AttributeType.EvasionRate: return "闪避率";
            default: return type.ToString();
        }
    }
}
