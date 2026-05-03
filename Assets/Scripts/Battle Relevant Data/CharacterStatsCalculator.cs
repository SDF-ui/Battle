using System.Collections.Generic;

/// <summary>
/// 用于计算角色最终战斗属性的工具类
/// （已重构：使用 AttributeHelper 消除重复的 switch 代码）
/// </summary>
public static class CharacterStatsCalculator
{
    public struct FinalStats
    {
        public int HP;
        public int MP;
        public int ATK;
        public int DEF;
        public float SPD;
        public float CritRate;
        public float CritDamage;
        public float HitRate;
        public float EvasionRate;
        public float ComboRate;
        public float StunRate;

        public int FinalCON;
        public int FinalINT;
        public int FinalSTR;
        public int FinalAGI;
    }

    public static FinalStats CalculateFinalStats(
        int baseSTR, int baseINT, int baseAGI, int baseCON,
        int level, string faction,
        IEnumerable<Item> equipments, IEnumerable<Item> artifacts)
    {
        int equipHP = 0, equipMP = 0, equipATK = 0, equipDEF = 0, equipSPD = 0;
        int equipCRIT = 0, equipHIT = 0, equipEVA = 0;
        int equipCombo = 0, equipStun = 0;
        int equipCON = 0, equipINT = 0, equipSTR = 0, equipAGI = 0;

        int artifactCON = 0, artifactINT = 0, artifactSTR = 0, artifactAGI = 0;
        int artifactHP = 0, artifactMP = 0, artifactATK = 0, artifactDEF = 0, artifactSPD = 0;
        int artifactCRIT = 0, artifactHIT = 0, artifactEVA = 0;
        int artifactCombo = 0, artifactStun = 0;

        // 使用统一的 AttributeHelper 累加装备属性
        AttributeHelper.AddItemsAttributes(equipments,
            ref equipCON, ref equipINT, ref equipSTR, ref equipAGI,
            ref equipHP, ref equipMP, ref equipATK, ref equipDEF, ref equipSPD,
            ref equipCRIT, ref equipHIT, ref equipEVA, ref equipCombo, ref equipStun);

        // 使用统一的 AttributeHelper 累加法宝属性
        AttributeHelper.AddItemsAttributes(artifacts,
            ref artifactCON, ref artifactINT, ref artifactSTR, ref artifactAGI,
            ref artifactHP, ref artifactMP, ref artifactATK, ref artifactDEF, ref artifactSPD,
                        ref artifactCRIT, ref artifactHIT, ref artifactEVA, ref artifactCombo, ref artifactStun);

        int finalCON = baseCON + equipCON + artifactCON;
        int finalINT = baseINT + equipINT + artifactINT;
        int finalSTR = baseSTR + equipSTR + artifactSTR;
        int finalAGI = baseAGI + equipAGI + artifactAGI;

        int hp = finalCON * 20 + level * 50 + 1000 + equipHP + artifactHP;
        int mp = finalINT * 5 + level * 20 + 300 + equipMP + artifactMP;
        int atk = finalSTR * 6 + level * 25 + 200 + equipATK + artifactATK;
        int def = finalCON * 4 + level * 15 + 120 + equipDEF + artifactDEF;
        float spd = finalAGI * 0.75f + level * 0.5f + 500 + equipSPD + artifactSPD;

        float baseCombo = FactionSkillDatabase.GetBaseComboChance(faction);
        float baseStun = FactionSkillDatabase.GetBaseStunChance(faction);
        float baseCrit = FactionSkillDatabase.GetBaseCritRate(faction);
        float baseCritDamage = FactionSkillDatabase.GetBaseCritDamage(faction);

        float baseCritRate = (finalSTR * 0.05f + finalINT * 0.05f) / 100f + level * 0.2f / 100f + 0.2f + baseCrit;
        float baseHitRate = (finalSTR * 0.05f + finalAGI * 0.05f + finalINT * 0.05f) / 100f + level * 0.2f / 100f + 0.2f;
        float baseEvasion = (finalAGI * 0.05f) / 100f + level * 0.1f / 100f + 0.1f;

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
}