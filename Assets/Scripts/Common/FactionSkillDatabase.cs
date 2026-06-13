using System.Collections.Generic;

/// <summary>
/// Faction Skill Database - centralized management of all faction skill descriptions and configurations.
/// Eliminates the long hard-coded strings in FactionSelectionUI.
/// </summary>
public static class FactionSkillDatabase
{
    public static Dictionary<string, FactionInfo> GetAllFactionInfos()
    {
        return new Dictionary<string, FactionInfo>
        {
            { "TianWangDian", CreateTianWangDianInfo() },
            { "WuZhuangGuan", CreateWuZhuangGuanInfo() },
            { "FangCunShan", CreateFangCunShanInfo() }
        };
    }

    public static string GetFactionDescription(string factionKey)
    {
        var all = GetAllFactionInfos();
        if (all.TryGetValue(factionKey, out var info))
            return info.GetFullDescription();
        return "未知门派";
    }

    public static float GetBaseComboChance(string faction) => faction == "TianWangDian" ? 0.20f : 0f;
    public static float GetBaseStunChance(string faction) => faction == "WuZhuangGuan" ? 0.20f : 0f;
    public static float GetBaseCritRate(string faction) => faction == "FangCunShan" ? 0.10f : 0f;
    public static float GetBaseCritDamage(string faction) => faction == "FangCunShan" ? 1.70f : 1.50f;

    private static FactionInfo CreateTianWangDianInfo()
    {
        return new FactionInfo
        {
            factionName = "天王殿",
            masterName = "李天王",
            feature = "擅长连击，通过连续攻击压制敌人，体现天兵神将的迅猛攻势；基础连击率提升20%。",
            activeSkills = new List<SkillInfo>
            {
                new SkillInfo { name = "狮子搏兔", type = "物理攻击", mp = 30, cooldown = 1, description = "对目标连续攻击2次，每次造成100%攻击力的伤害；若两次攻击均命中，则下次攻击时连击概率提升10%（不可叠加，施加攻击后消耗）。" },
                new SkillInfo { name = "威震山河", type = "控制", mp = 20, cooldown = 2, description = "有80%概率使目标陷入错乱状态，无法区分敌我和施展技能，持续3回合；若效果命中，则自身下次攻击时连击概率提升10%（不可叠加，施加攻击后消耗）；此次攻击回复自身30%内力值。" }
            },
            assistSkills = new List<SkillInfo>
            {
                new SkillInfo { name = "枕戈待旦", type = "增益", mp = 50, cooldown = 1, description = "本回合内，自身攻击附加30%连击率提升，此技能不占用回合行动值。" },
                new SkillInfo { name = "不动如山", type = "防御", mp = 80, cooldown = 3, description = "回复自身30%气血，并进入列阵状态：免伤提升20%，为全体友方提升防御力，数值相当于自身防御力的 10%；在任意己方角色被敌方攻击时，自身有80%概率触发一次反击（造成相当于自身75%攻击力的固定伤害，不消耗行动值）；持续4回合。" }
            },
            passiveSkills = new List<SkillInfo>
            {
                new SkillInfo { name = "迅疾如风", type = "被动", description = "免伤提升30%；反击后使自身行动提前10%。" },
                new SkillInfo { name = "生生不息", type = "被动", description = "每次触发连击时，恢复自身15%最大生命值，并提升15%攻击力，持续2回合（不可叠加）。" },
                new SkillInfo { name = "游刃有余", type = "被动", description = "将溢出的暴击率按 1:1 比例转化为暴伤系数（每溢出 10% 暴击率转化为 10% 暴伤系数）。" },
                new SkillInfo { name = "蓄势待发", type = "被动", description = "连击可多次触发，每次触发连击时获得敛锋状态（提升自身10%伤害与5%防御忽视，并降低自身10%连击概率），可叠加（上限10层），持续到本回合结束；若回合结束时连击率大于等于50%且本回合内未触发连击，则额外发动一次不可多次触发的连击。" }
            }
        };
    }

    private static FactionInfo CreateWuZhuangGuanInfo()
    {
        return new FactionInfo
        {
            factionName = "五庄观",
            masterName = "镇元子",
            feature = "擅长晕击，以乾坤之力震慑敌人，彰显地仙之祖的掌控之道；基础晕击率提升20%。",
            activeSkills = new List<SkillInfo>
            {
                new SkillInfo { name = "雷霆一击", type = "物理攻击", mp = 30, cooldown = 1, description = "对目标造成180%攻击力的伤害；若目标已处于眩晕状态，则伤害提升30%（技能伤害系数提升至210%）并延长眩晕1回合。" },
                new SkillInfo { name = "袖里乾坤", type = "控制/回复", mp = 20, cooldown = 2, description = "有80%概率使目标陷入眩晕状态，无法行动，持续3回合；若效果命中，则自身下次攻击时晕击概率提升10%（不可叠加，施加攻击后消耗）；此次攻击回复自身30%内力值。" }
            },
            assistSkills = new List<SkillInfo>
            {
                new SkillInfo { name = "引雷控电", type = "增益", mp = 50, cooldown = 1, description = "本回合内，自身攻击附加30%晕击率提升，此技能不占用回合行动值。" },
                new SkillInfo { name = "五雷轰顶", type = "控制/推条", mp = 80, cooldown = 3, description = "对敌方全体造成120%攻击力的伤害，并有80%概率使其行动条减少30%。同时进入镇岳状态：晕击概率提高10%，且每次成功触发晕击时自身行动提前30%；持续4回合。" }
            },
            passiveSkills = new List<SkillInfo>
            {
                new SkillInfo { name = "混元道体", type = "被动", description = "免伤提升30%；受到攻击时，有15%概率使攻击者眩晕1回合。" },
                new SkillInfo { name = "天地同寿", type = "被动", description = "每次成功眩晕目标时，恢复自身15%最大生命值，并施加易伤效果（受到的伤害提高10%），持续2回合（不可叠加）。" },
                new SkillInfo { name = "成竹在胸", type = "被动", description = "将溢出的暴击率按 1:1 比例转化为暴伤系数（每溢出 10% 暴击率转化为 10% 暴伤系数）。" },
                new SkillInfo { name = "道玄缚祟", type = "被动", description = "攻击目标前，从未施加的负面效果中随机选择一个进行施加（除生命外的战斗属性降低10%，不可叠加），持续2回合，并对己方全体角色施加迅捷状态（提升20%速度，持续2回合，不可叠加）；敌方每存在一个负面效果（战斗属性降低系列），提升15%伤害，最多可提升75%。" }
            }
        };
    }

    private static FactionInfo CreateFangCunShanInfo()
    {
        return new FactionInfo
        {
            factionName = "方寸山",
            masterName = "菩提子",
            feature = "擅长暴击，以无上佛法一击制敌，体现菩提子的智慧与威能；基础暴击率提升10%，暴击伤害提升20%。",
            activeSkills = new List<SkillInfo>
            {
                new SkillInfo { name = "无相慧剑", type = "物理攻击", mp = 30, cooldown = 1, description = "对敌方单体造成150%攻击力的伤害；若暴击率大于等于75%，则此次攻击必暴击，且对其他所有敌人造成60%的溅射伤害。" },
                new SkillInfo { name = "禅心入梦", type = "控制", mp = 20, cooldown = 2, description = "对敌方单体施放，有80%概率使其陷入睡眠状态，无法行动，持续3回合（受到伤害会醒来）；若效果命中，则自身下次攻击时暴击概率提升10%（不可叠加，施加攻击后消耗）；此次攻击回复自身30%内力值。" }
            },
            assistSkills = new List<SkillInfo>
            {
                new SkillInfo { name = "空明心境", type = "增益", mp = 50, cooldown = 1, description = "本回合内，自身攻击附加30%暴击率提升，此技能不占用回合行动值。" },
                new SkillInfo { name = "明心见性", type = "驱散/增益", mp = 80, cooldown = 3, description = "驱散自身所有负面状态，并进入明心状态：暴击率提高10%，暴伤系数提高20%，且每次暴击时自身行动条增加20%；持续4回合。" }
            },
            passiveSkills = new List<SkillInfo>
            {
                new SkillInfo { name = "破妄之眼", type = "被动", description = "免伤提升30%；受到攻击时，有80%概率使目标命中率降低10%，持续2回合（不可叠加）。" },
                new SkillInfo { name = "慧灯永续", type = "被动", description = "每次触发暴击时，自身回复15%最大生命值，并提升15%命中率，持续2回合（不可叠加）。" },
                new SkillInfo { name = "得心应手", type = "被动", description = "将溢出的暴击率按 1:2 比例转化为暴伤系数（每溢出 10% 暴击率转化为 20% 暴伤系数）。" },
                new SkillInfo { name = "妙法承佑", type = "被动", description = "暴击时忽视敌方20%防御，并为己方全体角色施加相当于此次伤害30%的护盾（含无相慧剑溅射伤害）；暴击后使自身下一次攻击最终伤害提高20%（可叠加，上限60%，触发后消耗一层）。" }
            }
        };
    }
}

public class FactionInfo
{
    public string factionName;
    public string masterName;
    public string feature;
    public List<SkillInfo> activeSkills = new List<SkillInfo>();
    public List<SkillInfo> assistSkills = new List<SkillInfo>();
    public List<SkillInfo> passiveSkills = new List<SkillInfo>();

    public string GetFullDescription()
    {
        string desc = $"{factionName}·{masterName}\n\n" +
                      $"门派特色：{feature}\n\n" +
                      $"【主动技能】\n";

        foreach (var s in activeSkills)
            desc += $"{s.name} | {s.type} | {s.mp} MP | {s.cooldown}回合 | {s.description}\n";

        desc += "\n【辅助技能】\n";
        foreach (var s in assistSkills)
            desc += $"{s.name} | {s.type} | {s.mp} MP | {s.cooldown}回合 | {s.description}\n";

        desc += "\n【被动技能】\n";
        foreach (var s in passiveSkills)
            desc += $"{s.name} | {s.type} | {s.description}\n";

        return desc;
    }
}

public class SkillInfo
{
    public string name;
    public string type;
    public int mp;
    public int cooldown;
    public string description;
}
