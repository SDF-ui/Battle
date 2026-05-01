using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 用于计算角色最终战斗属性的工具类
/// </summary>
public static class CharacterStatsCalculator
{
    /// <summary>
    /// 最终属性计算结果
    /// </summary>
    public struct FinalStats
    {
        public int HP;           // 生命值
        public int MP;           // 内力值
        public int ATK;          // 攻击力
        public int DEF;          // 防御力
        public float SPD;        // 速度
        public float CritRate;   // 暴击率 (0~1)
        public float CritDamage;   // 暴击伤害倍率 (1.5 = 150%)
        public float HitRate;    // 命中率 (0~1)
        public float EvasionRate;// 闪避率 (0~1)
        public float ComboRate;  // 连击率 (0~1)
        public float StunRate;   // 晕击率 (0~1)

        // 额外的基础属性（最终基础属性值，用于界面显示）
        public int FinalCON;
        public int FinalINT;
        public int FinalSTR;
        public int FinalAGI;
    }

    /// <summary>
    /// 计算角色的最终属性
    /// </summary>
    /// <param name="baseSTR">基础力量（已包含初始10、分配点、额外道具）</param>
    /// <param name="baseINT">基础灵力</param>
    /// <param name="baseAGI">基础敏捷</param>
    /// <param name="baseCON">基础体质</param>
    /// <param name="level">角色等级</param>
    /// <param name="faction">门派（用于被动概率）</param>
    /// <param name="equipments">装备列表</param>
    /// <param name="artifacts">法宝列表</param>
    /// <returns>最终属性结构体</returns>
    public static FinalStats CalculateFinalStats(
        int baseSTR, int baseINT, int baseAGI, int baseCON,
        int level, string faction,
        IEnumerable<Item> equipments, IEnumerable<Item> artifacts)
    {
        // 装备战斗属性加成
        int equipHP = 0, equipMP = 0, equipATK = 0, equipDEF = 0, equipSPD = 0;
        int equipCRIT = 0, equipHIT = 0, equipEVA = 0;
        int equipCombo = 0, equipStun = 0;

        // 装备基础属性加成
        int equipCON = 0, equipINT = 0, equipSTR = 0, equipAGI = 0;

        // 法宝加成
        int artifactCON = 0, artifactINT = 0, artifactSTR = 0, artifactAGI = 0;
        int artifactHP = 0, artifactMP = 0, artifactATK = 0, artifactDEF = 0, artifactSPD = 0;
        int artifactCRIT = 0, artifactHIT = 0, artifactEVA = 0;
        int artifactCombo = 0, artifactStun = 0;

        int equipCritDamage = 0, artifactCritDamage = 0;

        // 处理装备
        foreach (var item in equipments)
        {
            if (item == null) continue;
            foreach (var attr in item.basicAttributes)
                AddEquipmentAttribute(attr, ref equipCON, ref equipINT, ref equipSTR, ref equipAGI,
                                      ref equipHP, ref equipMP, ref equipATK, ref equipDEF, ref equipSPD,
                                      ref equipCRIT, ref equipHIT, ref equipEVA, ref equipCombo, ref equipStun);
            foreach (var attr in item.extraAttributes)
                AddEquipmentAttribute(attr, ref equipCON, ref equipINT, ref equipSTR, ref equipAGI,
                                      ref equipHP, ref equipMP, ref equipATK, ref equipDEF, ref equipSPD,
                                      ref equipCRIT, ref equipHIT, ref equipEVA, ref equipCombo, ref equipStun);
        }

        // 处理法宝
        foreach (var item in artifacts)
        {
            if (item == null) continue;
            if (item.basicAttributes != null)
            {
                foreach (var attr in item.basicAttributes)
                    AddArtifactAttribute(attr, ref artifactCON, ref artifactINT, ref artifactSTR, ref artifactAGI,
                                         ref artifactHP, ref artifactMP, ref artifactATK, ref artifactDEF, ref artifactSPD,
                                         ref artifactCRIT, ref artifactHIT, ref artifactEVA, ref artifactCombo, ref artifactStun);
            }
            if (item.extraAttributes != null)
            {
                foreach (var attr in item.extraAttributes)
                    AddArtifactAttribute(attr, ref artifactCON, ref artifactINT, ref artifactSTR, ref artifactAGI,
                                         ref artifactHP, ref artifactMP, ref artifactATK, ref artifactDEF, ref artifactSPD,
                                         ref artifactCRIT, ref artifactHIT, ref artifactEVA, ref artifactCombo, ref artifactStun);
            }
        }

        // 最终基础属性
        int finalCON = baseCON + equipCON + artifactCON;
        int finalINT = baseINT + equipINT + artifactINT;
        int finalSTR = baseSTR + equipSTR + artifactSTR;
        int finalAGI = baseAGI + equipAGI + artifactAGI;

        // 计算战斗属性（依据 version 1.0.md）
        int hp = finalCON * 20 + level * 50 + 1000 + equipHP + artifactHP;
        int mp = finalINT * 5 + level * 20 + 300 + equipMP + artifactMP;
        int atk = finalSTR * 6 + level * 25 + 200 + equipATK + artifactATK;   // 基础攻击力增加200
        int def = finalCON * 4 + level * 15 + 120 + equipDEF + artifactDEF;   // 基础防御力增加120
        float spd = finalAGI * 0.75f + level * 0.5f + 500 + equipSPD + artifactSPD;  // 基础速度增加500

        // 门派被动概率（基础）
        float baseCombo = faction == "TianWangDian" ? 0.20f : 0f;
        float baseStun = faction == "WuZhuangGuan" ? 0.20f : 0f;
        float baseCrit = faction == "FangCunShan" ? 0.10f : 0f;
        float baseCritDamage = faction == "FangCunShan" ? 1.70f : 1.50f;

        // 百分比属性（基础部分）
        // 百分比属性基础部分（基础值改为20%）
        float baseCritRate = (finalSTR * 0.05f + finalINT * 0.05f) / 100f + level * 0.2f / 100f + 0.2f + baseCrit;  // 20%
        float baseHitRate = (finalSTR * 0.05f + finalAGI * 0.05f + finalINT * 0.05f) / 100f + level * 0.2f / 100f + 0.2f;
        float baseEvasion = (finalAGI * 0.05f) / 100f + level * 0.1f / 100f + 0.1f;

        // 最终百分比（加上装备/法宝提供的千分数）
        float finalCrit = baseCritRate + (equipCRIT + artifactCRIT) / 1000f;
        float finalCritDamage = baseCritDamage;
        float finalHit = baseHitRate + (equipHIT + artifactHIT) / 1000f;
        float finalEva = baseEvasion + (equipEVA + artifactEVA) / 1000f;
        float finalCombo = baseCombo + (equipCombo + artifactCombo) / 1000f;
        float finalStun = baseStun + (equipStun + artifactStun) / 1000f;

        return new FinalStats
        {
            HP = hp,
            MP = mp,
            ATK = atk,
            DEF = def,
            SPD = spd,
            CritRate = finalCrit,
            CritDamage = finalCritDamage,
            HitRate = finalHit,
            EvasionRate = finalEva,
            ComboRate = finalCombo,
            StunRate = finalStun,
            FinalCON = finalCON,
            FinalINT = finalINT,
            FinalSTR = finalSTR,
            FinalAGI = finalAGI
        };
    }

    // 处理装备属性（包含基础属性和战斗属性）
    private static void AddEquipmentAttribute(ItemAttribute attr,
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

    // 处理法宝属性
    private static void AddArtifactAttribute(ItemAttribute attr,
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
}