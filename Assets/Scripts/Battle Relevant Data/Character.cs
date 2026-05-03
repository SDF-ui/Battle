using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Linq;
using System.Collections;
using TMPro;

public enum SkillType { Attack, Control, Buff, Defense, Heal }

[System.Serializable]
public class Skill
{
    public string skillName;
    public SkillType type;
    public int mpCost;
    public int cooldown;
    public int currentCooldown;
    public bool isFreeAction;
    public int skillID;
    public string description;
}

[System.Serializable]
public class AttributeDebuff
{
    public string attribute;
    public float reducePercent;
    public int remainingTurns;
}

public class Character : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public Sprite queueIconSprite;
    public TMP_Text damageText;
    public GameObject targetIcon;
    public Button targetClickButton;

    [Header("UI Sliders")]
    public Slider hpSlider;
    public Slider mpSlider;
    public Slider shieldSlider;
    public Slider actionSlider;

    [Header("动画")]
    public Animator animator;
    public float attackHitTime = 1.2f;
    public float skillAnimationTime = 1.5f;

    public Transform visualTransform;

    [Header("特效")]
    public GameObject stunEffectObject;
    public GameObject confuseEffectObject;
    public GameObject sleepEffectObject;
    public GameObject hitEffectObject;

    private GameObject currentStunEffect;
    private GameObject currentConfuseEffect;
    private GameObject currentSleepEffect;
    private GameObject currentHitEffect;
    private Transform effectsParent;

    [Header("基本属性")]
    public string characterName;
    public int level = 1;
    [Header("属性分配（不含初始10）")]
    public int allocatedCON = 0;
    public int allocatedINT = 0;
    public int allocatedSTR = 0;
    public int allocatedAGI = 0;
    [Header("额外道具加成（上限120）")]
    public int extraCON = 0;
    public int extraINT = 0;
    public int extraSTR = 0;
    public int extraAGI = 0;

    [Header("当前装备/法宝")]
    public List<Item> equippedEquipments = new List<Item>();
    public List<Item> equippedArtifacts = new List<Item>();

    [Header("门派与技能")]
    public string faction;
    public List<Skill> skills = new List<Skill>();

    [Header("当前战斗状态")]
    public int currentHP;
    public int currentMP;
    public float currentActionValue = 0f;
    public bool isDefending = false;
    public bool isStunned = false;
    public bool isConfused = false;
    public float defenseReduction = 0.5f;
    public float arrayFormationReduction = 0.0f;
    public float arrayFormationCounterChance = 0.0f;
    public bool isInArrayFormation = false;
    public int arrayFormationRemaining = 0;

    public int stunRemaining = 0;
    public int confuseRemaining = 0;
    public int defenseRemaining = 0;

    public bool heartMirrorUsed = false;
    public int windWingStacks = 0;
    public int jinGangSanShield = 0;
    public bool fengLeiYiTriggeredThisTurn = false;
    public bool isEliteOrBoss = false;
    public bool isCustomStats = false;   // 是否使用手动设置的属性（跳过 RecalcStats 覆盖）
    public Character summoner;   // 记录该角色是由谁召唤的（用于清除召唤物）

    // ★ 通用被动属性
    public float damageReductionPercent = 0f;   // 免伤百分比（0.5表示50%免伤）
    public float reflectDamagePercent = 0f;     // 反弹伤害百分比（0.1表示反弹10%）
    public bool immuneToControl = false;        // 是否免疫控制（眩晕/错乱/睡眠）
    public float controlToPushMultiplier = 0f;  // 控制转化为行动条推迟的乘数（如0.5表示每回合推迟50%行动条）
    public float tempComboBonus = 0f;
    public float tempStunBonus = 0f;
    public float tempCritRateBonus = 0f;

    public float xianXianCritBonus = 0f;   // 陷仙结界提供的暴击率加成
    public float tempAttackBonus = 0f;
    public float tempDefBonus = 0f;
    public float tempHitBonus = 0f;
    public float tempEvaBonus = 0f;
    public float counterChance = 0f;

    public bool baGuaZhenYueActive = false;
    public int baGuaZhenYueRemaining = 0;
    public float controlChanceBonus = 0f;

    public bool mingXinActive = false;
    public int mingXinRemaining = 0;
    public float mingXinCritRateBonus = 0f;
    public float mingXinCritDamageBonus = 0f;

    public bool lionComboBuffActive = false;
    public int lionComboBuffRemaining = 0;
    public float lionComboBuffValue = 0.1f;

    // ★ 威震山河：下次攻击连击率加成（与狮子搏兔分开追踪，但效果共用lionComboBuffValue）
    public bool weiZhenBuffActive = false;
    public bool weiZhenBuffConsumed = false;

    public bool lastAttackHit = false;
    public int lastDamageDealt = 0;

    public int lastMainDamageDealt = 0;   // 新增：记录主攻击伤害（不含连击）

    public float nextCritRateBonus = 0f;
    public int nextCritRateBonusRemaining = 0;

    public float chanXinCritBonus = 0f;

    public const float HUIYAN_PO_WANG_PENETRATION = 0.2f;

    public bool shengShengAttackBonusActive = false;
    public int shengShengAttackBonusRemaining = 0;
    public float shengShengAttackBonusValue = 0.15f;

    public bool isImmobilizeDefense = false;

    public bool hunYuanDaoTiTriggeredThisTurn = false;
    public bool xiuLiStunBuffActive = false;
    public int xiuLiStunBuffRemaining = 0;
    public float xiuLiStunBuffValue = 0.1f;
    public float damageTakenIncrease = 0f;
    public int damageTakenIncreaseRemaining = 0;
    public float hitRateDecrease = 0f;
    public int hitRateDecreaseRemaining = 0;
    public bool poWangTriggeredThisTurn = false;

    public bool allowMultipleComboThisTurn = false;
    public int lianFengStacks = 0;
    public float lianFengDamageBonus = 0f;
    public float lianFengArmorPen = 0f;
    public float lianFengComboPenalty = 0f;

    public List<AttributeDebuff> attributeDebuffs = new List<AttributeDebuff>();
    public bool xunJieActive = false;
    public int xunJieRemaining = 0;
    public float xunJieSpeedBonus = 0.20f; // 迅捷速度加成，默认20%

    // 慧灯永续命中加成
    public float huiDengHitBonus = 0f;
    public int huiDengHitBonusRemaining = 0;

    // ★ 基础属性缓存（改为 public 以便外部直接赋值，用于通天教主等Boss）
    public int cachedMaxHP, cachedMaxMP, cachedATK, cachedDEF;
    public float cachedSPD, cachedCritRate, cachedHitRate, cachedEvasionRate;
    public float cachedCritDamage = 1.5f;

    public int extraDEF = 0;
    public int extraDEFRemaining = 0;

    // ★ 中毒/灼伤系统：每层独立剩余回合
    private List<int> burnRemainingTurns = new List<int>();
    private List<int> poisonRemainingTurns = new List<int>();

    // ★ 蓄力系统
    public bool isCharging = false;
    public int currentChargeTurns = 0;
    public Skill chargedSkill = null;

    // ★ 阶段相关字段（用于多阶段Boss如通天教主）
    public int currentPhase = 1;
    public int maxPhase = 1;
    public float[] phaseMaxHPs;
    public float[] phaseMaxMPs;
    public float[] phaseAttack;
    public float[] phaseDefense;
    public float[] phaseSpeed;

    // ★ 诛仙剑护盾持续时间（回合数）
    public int zhuXianShieldRemainingTurns = 0;   // 0表示没有诛仙剑护盾

    // ★ 玄武玄甲护盾持续时间（回合数）
    public int xuanWuShieldRemainingTurns = 0;    // 0表示没有玄甲护盾

    // ★ 通天教主专属：虚影死亡攻击力加成层数
    public int shadowDeathAttackBonusStacks = 0;
    public float shadowDeathAttackBonusValue = 0f;   // 每层+5%

    // ★ 追加攻击计数器（用于二/三阶段）
    public int consecutiveAttackCount = 0;

    // ★ 朱雀涅槃：是否已使用过复活
    public bool hasUsedPhoenixRebirth = false;

    // ★ 白虎：逐血追击 — 每损失1%血量，增加1%攻击力
    public float baiHuLostHpAttackBonus = 0f;

    // ★ 白虎：虎煞噬魂 — 无敌状态
    public bool isInvincible = false;           // 是否处于无敌状态（白虎专用）
    public int invincibleRemaining = 0;         // 无敌剩余回合数（白虎专用）
    public bool hasUsedInvincibleRevive = false; // 是否已使用过虎煞噬魂无敌触发

    // ★ 朱雀：朱雀涅槃 — 复活后无敌状态（与白虎解耦）
    public bool isPhoenixInvincible = false;    // 朱雀涅槃后是否处于无敌状态
    public int phoenixInvincibleRemaining = 0;  // 朱雀涅槃后无敌剩余回合数

    // ★ 青龙：龙之逆鳞 — 追加攻击计数器
    public int qingLongConsecutiveAttacks = 0;

    // ★ 玄武：玄龟之佑 — 低血量回血是否已使用
    public bool xuanWuLowHpHealUsed = false;

    // ★ 四象灵尊阵亡惩罚速度加成层数（每层+25%，最大值0.75）
    public float deathPenaltySpeedBonus = 0f;

    // ★ 标记是否已消耗下次攻击加成（用于威震山河/袖里乾坤/禅心入梦/狮子搏兔的"施加攻击后消耗"）
    public bool nextAttackBonusConsumed = false;

    // ★ 本次暴击总伤害缓存（用于方寸山妙法承佑护盾计算，包含溅射伤害）
    public int currentCritTotalDamage = 0;

    // ★ 方寸山妙法承佑：暴击后下次攻击伤害加成层数（每层+20%，上限3层，攻击后消耗一层）
    public int nextAttackDamageBonusStacks = 0;

    public int MaxHP => cachedMaxHP;
    public int MaxMP => cachedMaxMP;
    public int BaseATK => cachedATK;

    public int DEF
    {
        get
        {
            int def = cachedDEF + extraDEF;

            // ★ 四象灵尊镇岳阵防御加成（四神兽专属）
            if (!string.IsNullOrEmpty(characterName) &&
                (characterName == "青龙" || characterName == "白虎" ||
                 characterName == "朱雀" || characterName == "玄武"))
            {
                BattleManager bm = FindObjectOfType<BattleManager>();
                if (bm != null)
                {
                    float defBonusPercent = FourSymbolAura.GetDefenseBonusPercent(bm);
                    if (defBonusPercent > 0)
                    {
                        // 找到玄武的防御力作为基准（用 cachedDEF 避免递归）
                        foreach (var e in bm.enemyParty)
                        {
                            if (e.characterName == "玄武" && !e.IsDead())
                            {
                                def += Mathf.RoundToInt(e.cachedDEF * defBonusPercent);
                                break;
                            }
                        }
                    }
                }
            }

            var debuff = attributeDebuffs.Find(d => d.attribute == "防御");
            if (debuff != null)
                def = Mathf.RoundToInt(def * (1f - debuff.reducePercent));
            return def;
        }
    }

    public float SPD => cachedSPD;
    public float BaseCritRate => cachedCritRate;

    public float HitRate
    {
        get
        {
            float hit = cachedHitRate + tempHitBonus + huiDengHitBonus - (hitRateDecrease > 0 ? hitRateDecrease : 0f);
            var debuff = attributeDebuffs.Find(d => d.attribute == "命中");
            if (debuff != null)
                hit -= debuff.reducePercent;
            return hit;
        }
    }

    public float EvasionRate
    {
        get
        {
            float eva = cachedEvasionRate + tempEvaBonus;
            var debuff = attributeDebuffs.Find(d => d.attribute == "闪避");
            if (debuff != null)
                eva -= debuff.reducePercent;
            return eva;
        }
    }

    public float CurrentSpeed
    {
        get
        {
            float speed = SPD;
            var debuff = attributeDebuffs.Find(d => d.attribute == "速度");
            if (debuff != null)
                speed *= (1f - debuff.reducePercent);
            return speed * (1f + 0.1f * windWingStacks + deathPenaltySpeedBonus) * GetSpeedMultiplier() * (xunJieActive ? 1f + xunJieSpeedBonus : 1f);
        }
    }

    public bool isSleep = false;
    public int sleepRemaining = 0;
    public int overHealShield = 0;

    public int GetFinalATK()
    {
        float baiHuBonus = 0f;
        if (characterName == "白虎")
        {
            float hpPercent = MaxHP > 0 ? (float)currentHP / MaxHP : 1f;
            baiHuBonus = Mathf.Clamp01(1f - hpPercent);
        }

        // ★ 妙法承佑：暴击后下次攻击伤害加成（每层+20%，上限60%）
        float miaoFaBonus = faction == "FangCunShan" ? nextAttackDamageBonusStacks * 0.20f : 0f;

        float multiplier = 1f + tempAttackBonus + (shengShengAttackBonusActive ? shengShengAttackBonusValue : 0f) + shadowDeathAttackBonusValue + baiHuBonus + miaoFaBonus;

        // ★ 四象灵尊杀伐阵攻击力加成（白虎存活时，所有四神兽受益）
        if (!string.IsNullOrEmpty(characterName) &&
            (characterName == "青龙" || characterName == "白虎" ||
             characterName == "朱雀" || characterName == "玄武"))
        {
            BattleManager bm = FindObjectOfType<BattleManager>();
            if (bm != null)
                multiplier += FourSymbolAura.GetAttackBonusPercent(bm);
        }

        int baseATK = BaseATK;
        var debuff = attributeDebuffs.Find(d => d.attribute == "攻击");
        if (debuff != null)
            baseATK = Mathf.RoundToInt(baseATK * (1f - debuff.reducePercent));
        return Mathf.RoundToInt(baseATK * multiplier);
    }

    public float GetFinalCritRate()
    {
        float raw = cachedCritRate + tempCritRateBonus + (mingXinActive ? mingXinCritRateBonus : 0f) + xianXianCritBonus + chanXinCritBonus;

        if (nextCritRateBonusRemaining > 0)
            raw += nextCritRateBonus;
        var debuff = attributeDebuffs.Find(d => d.attribute == "暴击");
        if (debuff != null)
            raw -= debuff.reducePercent;
        return raw; // 不限制100%，允许显示溢出
    }

    public float GetOverCritRate()
    {
        float raw = cachedCritRate + tempCritRateBonus + (mingXinActive ? mingXinCritRateBonus : 0f);
        if (nextCritRateBonusRemaining > 0)
            raw += nextCritRateBonus;
        var debuff = attributeDebuffs.Find(d => d.attribute == "暴击");
        if (debuff != null)
            raw -= debuff.reducePercent;
        return Mathf.Max(0f, raw - 1f);
    }

    public float GetFinalCritDamage()
    {
        float baseCritDmg = cachedCritDamage + (mingXinActive ? mingXinCritDamageBonus : 0f);

        // 溢出暴击率转暴伤系数（根据门派被动）
        float overCrit = GetOverCritRate();
        if (faction == "TianWangDian" || faction == "WuZhuangGuan")
        {
            // 游刃有余/成竹在胸：1:1转化
            baseCritDmg += overCrit * 1f;
        }
        else if (faction == "FangCunShan")
        {
            // 得心应手：1:2转化
            baseCritDmg += overCrit * 2f;
        }

        return baseCritDmg;
    }

    public float GetDamageMultiplierAgainstStunned(Character target)
    {
        if (faction == "WuZhuangGuan" && target.isStunned)
            return 1.1f;
        return 1f;
    }

    public float GetTotalComboChance()
    {
        float baseVal = totalComboChance + tempComboBonus
            + (lionComboBuffActive ? lionComboBuffValue : 0f)
            + (weiZhenBuffActive ? lionComboBuffValue : 0f);
        var debuff = attributeDebuffs.Find(d => d.attribute == "连击");
        if (debuff != null)
            baseVal -= debuff.reducePercent;
        return Mathf.Max(-1.0f, baseVal - lianFengComboPenalty);
    }

    public float GetTotalStunChance()
    {
        float baseVal = totalStunChance + tempStunBonus + (baGuaZhenYueActive ? controlChanceBonus : 0f) + (xiuLiStunBuffActive ? xiuLiStunBuffValue : 0f);
        var debuff = attributeDebuffs.Find(d => d.attribute == "晕击");
        if (debuff != null)
            baseVal -= debuff.reducePercent;
        return Mathf.Max(0f, baseVal);
    }

    private float speedMultiplier = 1f;
    private List<TempBuff> buffs = new List<TempBuff>();
    private float totalComboChance;
    private float totalStunChance;
    private Coroutine damageCoroutine;

    void Start()
    {
        RecalcStats();
    }

    public void RecalcStats()
    {
        // 如果使用了手动自定义属性（如通天教主），则跳过自动计算，避免覆盖
        if (isCustomStats) return;

        int totalAllocated = allocatedCON + allocatedINT + allocatedSTR + allocatedAGI;
        int maxAlloc = (level - 1) * 4;
        if (totalAllocated > maxAlloc)
        {
            Debug.LogWarning($"{characterName} 分配点数 ({totalAllocated}) 超过等级允许 ({maxAlloc})，将自动削减");
        }

        int baseCON = 10 + allocatedCON + extraCON;
        int baseINT = 10 + allocatedINT + extraINT;
        int baseSTR = 10 + allocatedSTR + extraSTR;
        int baseAGI = 10 + allocatedAGI + extraAGI;

        var stats = CharacterStatsCalculator.CalculateFinalStats(
            baseSTR, baseINT, baseAGI, baseCON,
            level, faction,
            equippedEquipments, equippedArtifacts);

        cachedMaxHP = stats.HP;
        cachedMaxMP = stats.MP;
        cachedATK = stats.ATK;
        cachedDEF = stats.DEF;
        cachedSPD = stats.SPD;
        cachedCritRate = stats.CritRate;
        cachedHitRate = stats.HitRate;
        cachedEvasionRate = stats.EvasionRate;
        cachedCritDamage = stats.CritDamage;

        totalComboChance = stats.ComboRate;
        totalStunChance = stats.StunRate;

        ResetBattleStates();
    }

    private void ResetBattleStates()
    {
        windWingStacks = 0;
        jinGangSanShield = 0;
        fengLeiYiTriggeredThisTurn = false;
        heartMirrorUsed = false;
        baGuaZhenYueActive = false;
        baGuaZhenYueRemaining = 0;
        controlChanceBonus = 0f;
        mingXinActive = false;
        mingXinRemaining = 0;
        mingXinCritRateBonus = 0f;
        mingXinCritDamageBonus = 0f;
        nextCritRateBonus = 0f;
        nextCritRateBonusRemaining = 0;
        lionComboBuffActive = false;
        lionComboBuffRemaining = 0;
        weiZhenBuffActive = false;
        weiZhenBuffConsumed = false;
        shengShengAttackBonusActive = false;
        shengShengAttackBonusRemaining = 0;
        xiuLiStunBuffActive = false;
        xiuLiStunBuffRemaining = 0;
        hunYuanDaoTiTriggeredThisTurn = false;
        damageTakenIncrease = 0f;
        damageTakenIncreaseRemaining = 0;
        hitRateDecrease = 0f;
        hitRateDecreaseRemaining = 0;
        poWangTriggeredThisTurn = false;
        isInArrayFormation = false;
        arrayFormationReduction = 0f;
        arrayFormationCounterChance = 0f;
        arrayFormationRemaining = 0;
        overHealShield = 0;
        extraDEF = 0;
        extraDEFRemaining = 0;
        chanXinCritBonus = 0f;

        allowMultipleComboThisTurn = false;
        lianFengStacks = 0;
        lianFengDamageBonus = 0f;
        lianFengArmorPen = 0f;
        lianFengComboPenalty = 0f;
        attributeDebuffs.Clear();
        xunJieActive = false;
        xunJieRemaining = 0;
        xunJieSpeedBonus = 0.20f;

        huiDengHitBonus = 0f;
        huiDengHitBonusRemaining = 0;

        // 清空中毒/灼伤
        burnRemainingTurns.Clear();
        poisonRemainingTurns.Clear();

        // 重置蓄力
        isCharging = false;
        currentChargeTurns = 0;
        chargedSkill = null;

        // 重置阶段相关字段
        currentPhase = 1;
        maxPhase = 1;
        phaseMaxHPs = null;
        phaseMaxMPs = null;
        phaseAttack = null;
        phaseDefense = null;
        phaseSpeed = null;

        // 诛仙剑护盾
        zhuXianShieldRemainingTurns = 0;

        // 玄甲护盾（不重置，运行时动态管理）
        xuanWuShieldRemainingTurns = 0;

        // 陷仙剑结界
        xianXianCritBonus = 0f;

        // 通天教主专属
        shadowDeathAttackBonusStacks = 0;
        shadowDeathAttackBonusValue = 0f;
        consecutiveAttackCount = 0;

        // 朱雀涅槃标记（不重置，用于跨阶段保留状态）
        // hasUsedPhoenixRebirth = false;

        // 白虎逐血追击（不重置，持续到战斗结束）
        // baiHuLostHpAttackBonus = 0f;

        // 白虎虎煞噬魂无敌（不重置，持续到战斗结束）
        // isInvincible = false;
        // invincibleRemaining = 0;
        // hasUsedInvincibleRevive = false;

        // 朱雀涅槃后无敌（不重置，持续到战斗结束）
        // isPhoenixInvincible = false;
        // phoenixInvincibleRemaining = 0;

        // 青龙追加攻击计数器（不重置）
        // qingLongConsecutiveAttacks = 0;

        // 玄武低血量回血（不重置）
        // xuanWuLowHpHealUsed = false;

        // 阵亡惩罚速度加成（不重置，持续到战斗结束）
        // deathPenaltySpeedBonus = 0f;

        // 下次攻击加成消耗标记（不重置，战斗开始时重置一次即可）
        nextAttackBonusConsumed = false;
    }

    public void Initialize()
    {
        RecalcStats();
        currentHP = MaxHP;
        currentMP = MaxMP;
        currentActionValue = 0f;
        isDefending = false;
        isStunned = false;
        isConfused = false;
        stunRemaining = 0;
        confuseRemaining = 0;
        defenseRemaining = 0;
        speedMultiplier = 1f;
        defenseReduction = 0.5f;
        isInArrayFormation = false;
        arrayFormationRemaining = 0;
        buffs.Clear();
        InitFactionAndSkills();

        // ★ 门派免伤被动实装（迅疾如风/混元道体/破妄之眼各提供30%免伤）
        if (!string.IsNullOrEmpty(faction) && !isCustomStats)
        {
            if (faction == "TianWangDian" || faction == "WuZhuangGuan" || faction == "FangCunShan")
            {
                damageReductionPercent = 0.3f;
            }
        }

        if (animator == null) animator = GetComponent<Animator>();

        if (damageText != null)
            damageText.gameObject.SetActive(false);

        Transform effects = transform.Find("Effects");
        if (effects == null)
        {
            GameObject effectsObj = new GameObject("Effects");
            effectsObj.transform.SetParent(transform);
            effectsParent = effectsObj.transform;
        }
        else
        {
            effectsParent = effects;
        }

        // ★ 动画速度提升为2倍
        if (animator != null)
            animator.speed = 2f;

        // ★ 攻击持续时间减半
        attackHitTime *= 0.5f;
        skillAnimationTime *= 0.5f;

        ShowStunEffect(false);
        ShowConfuseEffect(false);
        ShowSleepEffect(false);
        ShowHitEffect(false);

        GetEffectObjects();
        if (stunEffectObject != null) stunEffectObject.SetActive(false);
        if (confuseEffectObject != null) confuseEffectObject.SetActive(false);
        if (sleepEffectObject != null) sleepEffectObject.SetActive(false);
        if (hitEffectObject != null) hitEffectObject.SetActive(false);

        // 清空状态列表
        burnRemainingTurns.Clear();
        poisonRemainingTurns.Clear();
        isCharging = false;
        currentChargeTurns = 0;
        chargedSkill = null;
    }

    private void InitFactionAndSkills()
    {
        // 如果使用了手动自定义属性（如通天教主），则跳过避免覆盖
        if (isCustomStats) return;
        skills.Clear();
        skills.Add(new Skill
        {
            skillName = "普通攻击",
            type = SkillType.Attack,
            mpCost = 0,
            cooldown = 0,
            currentCooldown = 0,
            isFreeAction = false,
            skillID = 0,
            description = "对敌人造成100%攻击力的伤害；本次攻击回复10%内力"
        });

        if (faction == "TianWangDian")
        {
            skills.Add(new Skill
            {
                skillName = "狮子搏兔",
                type = SkillType.Attack,
                mpCost = 30,
                cooldown = 2,
                currentCooldown = 0,
                isFreeAction = false,
                skillID = 101,
                description = "对目标连续攻击2次，每次造成100%攻击力的伤害；若两次攻击均命中，则下次攻击时连击概率提升10%（不可叠加，施加攻击后消耗）。"
            });
            skills.Add(new Skill
            {
                skillName = "威震山河",
                type = SkillType.Control,
                mpCost = 20,
                cooldown = 2,
                currentCooldown = 0,
                isFreeAction = false,
                skillID = 102,
                description = "有80%概率使目标陷入“错乱”状态，无法施展任何技能，持续3回合；若效果命中，则自身下次攻击时连击概率提升10%；此次攻击回复自身30%内力值。"
            });
            skills.Add(new Skill
            {
                skillName = "枕戈待旦",
                type = SkillType.Buff,
                mpCost = 50,
                cooldown = 2,
                currentCooldown = 0,
                isFreeAction = true,
                skillID = 103,
                description = "本回合内，自身攻击附加30%连击率提升，此技能不占用回合行动值。"
            });
            skills.Add(new Skill
            {
                skillName = "不动如山",
                type = SkillType.Defense,
                mpCost = 80,
                cooldown = 4,
                currentCooldown = 0,
                isFreeAction = false,
                skillID = 104,
                description = "回复自身30%气血，并进入“列阵”状态：免伤提升20%，并对己方全体角色提升相当于自身10%的防御；在任意己方角色被敌方攻击时，自身有80%概率触发一次反击（造成相当于自身75%攻击力的固定伤害，不消耗行动值）；持续4回合。"
            });
        }
        else if (faction == "WuZhuangGuan")
        {
            skills.Add(new Skill
            {
                skillName = "雷霆一击",
                type = SkillType.Attack,
                mpCost = 30,
                cooldown = 2,
                currentCooldown = 0,
                isFreeAction = false,
                skillID = 201,
                description = "对目标造成180%攻击力的伤害；若目标已处于眩晕状态，则伤害提升30%（技能伤害系数提升至210%）并延长眩晕1回合。"
            });
            skills.Add(new Skill
            {
                skillName = "袖里乾坤",
                type = SkillType.Control,
                mpCost = 20,
                cooldown = 2,
                currentCooldown = 0,
                isFreeAction = false,
                skillID = 202,
                description = "有80%概率使目标陷入“眩晕”状态，无法行动，持续3回合；若效果命中，则自身下次攻击时晕击概率提升10%；此次攻击回复自身30%内力值。"
            });
            skills.Add(new Skill
            {
                skillName = "引雷控电",
                type = SkillType.Buff,
                mpCost = 50,
                cooldown = 2,
                currentCooldown = 0,
                isFreeAction = true,
                skillID = 203,
                description = "本回合内，自身攻击附加30%晕击率提升，此技能不占用回合行动值。"
            });
            skills.Add(new Skill
            {
                skillName = "五雷轰顶",
                type = SkillType.Control,
                mpCost = 80,
                cooldown = 4,
                currentCooldown = 0,
                isFreeAction = false,
                skillID = 204,
                description = "对敌方全体造成180%攻击力的伤害，并有80%概率使其行动条减少30%。同时进入“镇岳”状态：晕击概率提高10%，且每次成功触发晕击时自身行动提前30%；持续4回合。"
            });
        }
        else if (faction == "FangCunShan")
        {
            skills.Add(new Skill
            {
                skillName = "无相慧剑",
                type = SkillType.Attack,
                mpCost = 30,
                cooldown = 2,
                currentCooldown = 0,
                isFreeAction = false,
                skillID = 301,
                description = "对敌方单体造成150%攻击力的伤害；若暴击率大于等于75%，则此次攻击必暴击，且对其他所有敌人造成60%的溅射伤害。"
            });
            skills.Add(new Skill
            {
                skillName = "禅心入梦",
                type = SkillType.Control,
                mpCost = 20,
                cooldown = 2,
                currentCooldown = 0,
                isFreeAction = false,
                skillID = 302,
                description = "有80%概率使目标陷入“睡眠”状态，无法行动，持续3回合（受到伤害会醒来）；若效果命中，则自身下次攻击时暴击概率提升10%；此次攻击回复自身30%内力值。"
            });
            skills.Add(new Skill
            {
                skillName = "空明心境",
                type = SkillType.Buff,
                mpCost = 50,
                cooldown = 2,
                currentCooldown = 0,
                isFreeAction = true,
                skillID = 303,
                description = "本回合内，自身攻击附加30%暴击率提升，此技能不占用回合行动值。"
            });
            skills.Add(new Skill
            {
                skillName = "明心见性",
                type = SkillType.Buff,
                mpCost = 80,
                cooldown = 4,
                currentCooldown = 0,
                isFreeAction = false,
                skillID = 304,
                description = "驱散自身所有负面状态，并进入“明心”状态：暴击率提高10%，暴伤系数提高20%，且每次暴击时自身行动条增加20%；持续4回合。"
            });
        }
    }

    // ★ 灼伤/中毒方法
    public void ApplyBurn(int turns)
    {
        if (IsDead() || turns <= 0) return;
        if (burnRemainingTurns.Count < 2)
            burnRemainingTurns.Add(turns);
        else
        {
            int minIndex = 0;
            int minValue = burnRemainingTurns[0];
            for (int i = 1; i < burnRemainingTurns.Count; i++)
            {
                if (burnRemainingTurns[i] < minValue)
                {
                    minValue = burnRemainingTurns[i];
                    minIndex = i;
                }
            }
            if (turns > minValue)
                burnRemainingTurns[minIndex] = turns;
        }
    }

    public void ApplyPoison(int turns)
    {
        if (IsDead() || turns <= 0) return;
        if (poisonRemainingTurns.Count < 2)
            poisonRemainingTurns.Add(turns);
        else
        {
            int minIndex = 0;
            int minValue = poisonRemainingTurns[0];
            for (int i = 1; i < poisonRemainingTurns.Count; i++)
            {
                if (poisonRemainingTurns[i] < minValue)
                {
                    minValue = poisonRemainingTurns[i];
                    minIndex = i;
                }
            }
            if (turns > minValue)
                poisonRemainingTurns[minIndex] = turns;
        }
    }

    // ★ 清除所有灼伤和中毒
    public void ClearAllBurns() => burnRemainingTurns.Clear();
    public void ClearAllPoisons() => poisonRemainingTurns.Clear();

    // ★ 单独清除灼伤或中毒（保留层数，仅清除效果）
    public void ClearBurns() => burnRemainingTurns.Clear();
    public void ClearPoisons() => poisonRemainingTurns.Clear();

    // ★ 回合开始时处理灼伤/中毒：先结算伤害，再减少回合数
    public void ProcessBurnAndPoisonAtTurnStart(BattleManager battleManager)
    {
        // 灼伤伤害（基于当前层数）
        if (burnRemainingTurns.Count > 0)
        {
            int burnDamage = Mathf.RoundToInt(MaxHP * 0.05f * burnRemainingTurns.Count);
            TakeDamage(burnDamage, false);
            if (battleManager != null)
                battleManager.AddTurnResultMessage($"{characterName} 受到灼伤 {burnDamage} 伤害");
        }
        // 中毒伤害
        if (poisonRemainingTurns.Count > 0)
        {
            int poisonDamage = Mathf.RoundToInt(MaxHP * 0.05f * poisonRemainingTurns.Count);
            TakeDamage(poisonDamage, false);
            if (battleManager != null)
                battleManager.AddTurnResultMessage($"{characterName} 受到中毒 {poisonDamage} 伤害");
        }

        // 减少所有灼伤层的剩余回合数，移除已结束的层
        for (int i = burnRemainingTurns.Count - 1; i >= 0; i--)
        {
            burnRemainingTurns[i]--;
            if (burnRemainingTurns[i] <= 0)
                burnRemainingTurns.RemoveAt(i);
        }
        // 减少所有中毒层的剩余回合数
        for (int i = poisonRemainingTurns.Count - 1; i >= 0; i--)
        {
            poisonRemainingTurns[i]--;
            if (poisonRemainingTurns[i] <= 0)
                poisonRemainingTurns.RemoveAt(i);
        }
    }

    public bool TryEnterNextPhase(BattleManager battleManager)
    {
        if (currentPhase >= maxPhase) return false;
        if (currentHP > 0) return false;

        currentPhase++;
        Debug.Log($"{characterName} 进入第 {currentPhase} 阶段！");

        if (phaseMaxHPs != null && phaseMaxHPs.Length >= currentPhase)
        {
            cachedMaxHP = Mathf.RoundToInt(phaseMaxHPs[currentPhase - 1]);
            currentHP = cachedMaxHP;
        }
        if (phaseMaxMPs != null && phaseMaxMPs.Length >= currentPhase)
        {
            cachedMaxMP = Mathf.RoundToInt(phaseMaxMPs[currentPhase - 1]);
            currentMP = cachedMaxMP;
        }
        if (phaseAttack != null && phaseAttack.Length >= currentPhase)
            cachedATK = Mathf.RoundToInt(phaseAttack[currentPhase - 1]);
        if (phaseDefense != null && phaseDefense.Length >= currentPhase)
            cachedDEF = Mathf.RoundToInt(phaseDefense[currentPhase - 1]);
        if (phaseSpeed != null && phaseSpeed.Length >= currentPhase)
            cachedSPD = phaseSpeed[currentPhase - 1];

        foreach (var skill in skills)
            skill.currentCooldown = 0;

        if (battleManager != null)
            battleManager.ClearSummonedUnits(this);

        currentActionValue = 0f;
        isCharging = false;
        currentChargeTurns = 0;
        chargedSkill = null;

        // 重置追加攻击计数器
        consecutiveAttackCount = 0;

        battleManager.AddTurnResultMessage($"{characterName} 进入第 {currentPhase} 阶段！");
        return true;
    }

    public void TakeDamage(int damage, bool isCrit = false)
    {
        int totalDamage = damage;

        // ★ 白虎虎煞噬魂：无敌状态下，所有受到的伤害归零
        if (isInvincible)
        {
            if (totalDamage > 0)
            {
                BattleManager bm = FindObjectOfType<BattleManager>();
                if (bm != null)
                    bm.AddTurnResultMessage($"{characterName} 处于无敌状态，免疫 {totalDamage} 点伤害");
            }
            return;
        }

        // ★ 朱雀涅槃后：无敌状态下，所有受到的伤害归零
        if (isPhoenixInvincible)
        {
            if (totalDamage > 0)
            {
                BattleManager bm = FindObjectOfType<BattleManager>();
                if (bm != null)
                    bm.AddTurnResultMessage($"{characterName} 处于涅槃无敌状态，免疫 {totalDamage} 点伤害");
            }
            return;
        }

        ShowHitEffect(true);
        if (totalDamage > 0) ShowDamage(totalDamage, isCrit);
        else return;

        if (overHealShield > 0)
        {
            int absorb = Mathf.Min(overHealShield, totalDamage);
            overHealShield -= absorb;
            totalDamage -= absorb;
            if (totalDamage <= 0) return;
        }
        if (jinGangSanShield > 0)
        {
            int absorb = Mathf.Min(jinGangSanShield, totalDamage);
            jinGangSanShield -= absorb;
            totalDamage -= absorb;
            if (totalDamage <= 0) return;
        }
        if (damageTakenIncrease > 0)
        {
            totalDamage = Mathf.RoundToInt(totalDamage * (1 + damageTakenIncrease));
        }
        currentHP -= totalDamage;
        if (currentHP < 0) currentHP = 0;

        // ★ 白虎虎煞噬魂：受到致命伤害时触发无敌
        if (characterName == "白虎" && currentHP <= 0 && !hasUsedInvincibleRevive)
        {
            hasUsedInvincibleRevive = true;
            // 清空自身所有负面及控制效果
            ClearConfuse();
            ClearStun();
            ClearSleep();
            attributeDebuffs.Clear();
            burnRemainingTurns.Clear();
            poisonRemainingTurns.Clear();
            isInvincible = true;
            invincibleRemaining = 3;
            currentHP = 1; // 锁定为1血不死
            BattleManager bm = FindObjectOfType<BattleManager>();
            if (bm != null)
                bm.AddTurnResultMessage($"白虎虎煞噬魂触发，清空所有负面及控制效果，进入无敌状态3回合！");
            return;
        }

        // ★ 玄武低血量回血：受伤时判断
        if (characterName == "玄武" && !xuanWuLowHpHealUsed && (float)currentHP / MaxHP < 0.3f && currentHP > 0)
        {
            xuanWuLowHpHealUsed = true;
            int lostHP = MaxHP - currentHP;
            int healAmount = Mathf.RoundToInt(lostHP * 0.8f);
            int actualHeal = Heal(healAmount);
            BattleManager bm = FindObjectOfType<BattleManager>();
            if (bm != null)
                bm.AddTurnResultMessage($"玄武玄龟之佑触发，恢复已损失血量的80%（{actualHeal}点）");
        }

        // ★ 朱雀涅槃：受到致命伤害时，若有任意敌人处于灼烧状态，立即触发其全部灼烧效果
        if (characterName == "朱雀" && currentHP <= 0 && !hasUsedPhoenixRebirth)
        {
            BattleManager bm = FindObjectOfType<BattleManager>();
            if (bm != null)
            {
                // 检查是否有任意敌人处于灼烧状态
                bool anyTargetBurning = false;
                foreach (var target in bm.playerParty)
                {
                    if (target != null && target.burnRemainingTurns.Count > 0)
                    {
                        anyTargetBurning = true;
                        // 立即触发全部灼烧伤害
                        int burnDamage = Mathf.RoundToInt(target.MaxHP * 0.05f * target.burnRemainingTurns.Count);
                        target.TakeDamage(burnDamage, false);
                        bm.AddTurnResultMessage($"朱雀涅槃触发灼烧效果，{target.characterName} 受到 {burnDamage} 点灼烧伤害");
                        target.ClearAllBurns();
                        break;
                    }
                }
                if (anyTargetBurning && bm != null)
                {
                    hasUsedPhoenixRebirth = true;
                    int reviveHP = Mathf.RoundToInt(MaxHP * 0.75f);
                    currentHP = reviveHP;
                    // 清空自身所有负面及控制效果
                    ClearConfuse();
                    ClearStun();
                    ClearSleep();
                    attributeDebuffs.Clear();
                    burnRemainingTurns.Clear();
                    poisonRemainingTurns.Clear();
                    // 免疫所有伤害和控制，持续1回合
                    isPhoenixInvincible = true;
                    phoenixInvincibleRemaining = 1;
                    bm.AddTurnResultMessage($"朱雀涅槃触发！消耗敌人灼烧层数，以 {reviveHP} 点生命复活，清空负面及控制效果，免疫1回合！");
                }
            }
        }

        // 阶段转换处理
        if (currentHP <= 0 && currentPhase < maxPhase)
        {
            BattleManager bm = FindObjectOfType<BattleManager>();
            if (bm != null && TryEnterNextPhase(bm))
            {
                return;
            }
        }

        // 死亡且没有下一阶段时，清除自己召唤的召唤物（如虚影）
        if (currentHP <= 0 && currentPhase == maxPhase)
        {
            BattleManager bm = FindObjectOfType<BattleManager>();
            if (bm != null)
            {
                bm.ClearSummonedUnits(this);
            }
        }
    }

    private void ShowDamage(int damage, bool isCrit = false)
    {
        if (damageText == null) return;
        damageText.text = "-" + damage;
        if (isCrit)
        {
            damageText.fontSize = 42;
            damageText.color = Color.red;
            damageText.fontStyle = FontStyles.Bold;
        }
        else
        {
            damageText.fontSize = 36;
            damageText.color = Color.red;
        }
        damageText.gameObject.SetActive(true);
        if (damageCoroutine != null) StopCoroutine(damageCoroutine);
        damageCoroutine = StartCoroutine(HideDamageAfterDelay(0.8f));
    }

    private IEnumerator HideDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (damageText != null) damageText.gameObject.SetActive(false);
    }

    public bool IsDead() => currentHP <= 0;

    public void AddActionValue(float deltaTime)
    {
        if (IsDead()) return;
        currentActionValue += CurrentSpeed * deltaTime;
        if (currentActionValue > 12000f) currentActionValue = 12000f;
    }

    public void SpendAction()
    {
        currentActionValue -= 500f;
    }

    public float TimeToNextTurn()
    {
        if (currentActionValue >= 500f) return 0f;
        return (500f - currentActionValue) / CurrentSpeed;
    }

    public void ReduceCooldowns()
    {
        foreach (var skill in skills)
            if (skill.currentCooldown > 0)
                skill.currentCooldown--;
    }

    public void ReduceStatusDurations()
    {
        if (stunRemaining > 0) { stunRemaining--; if (stunRemaining <= 0) ClearStun(); }
        if (confuseRemaining > 0) { confuseRemaining--; if (confuseRemaining <= 0) ClearConfuse(); }
        if (sleepRemaining > 0) { sleepRemaining--; if (sleepRemaining <= 0) ClearSleep(); }
        if (defenseRemaining > 0)
        {
            defenseRemaining--;
            if (defenseRemaining <= 0) { isDefending = false; counterChance = 0f; defenseReduction = 0.5f; }
        }
        if (arrayFormationRemaining > 0)
        {
            arrayFormationRemaining--;
            if (arrayFormationRemaining <= 0) { isInArrayFormation = false; arrayFormationReduction = 0f; arrayFormationCounterChance = 0f; }
        }
        // ★ 狮子搏兔/威震山河buff（不依赖倒计时，由攻击消耗）
        if (shengShengAttackBonusActive) { shengShengAttackBonusRemaining--; if (shengShengAttackBonusRemaining <= 0) shengShengAttackBonusActive = false; }
        // ★ 袖里乾坤/禅心入梦buff（不依赖倒计时，由攻击消耗）
        // xiuLiStunBuffActive 和 chanXinCritBonus 在 ConsumeNextAttackBonus 中消耗
        if (damageTakenIncreaseRemaining > 0) { damageTakenIncreaseRemaining--; if (damageTakenIncreaseRemaining <= 0) damageTakenIncrease = 0f; }
        if (hitRateDecreaseRemaining > 0) { hitRateDecreaseRemaining--; if (hitRateDecreaseRemaining <= 0) hitRateDecrease = 0f; }
        if (baGuaZhenYueActive) { baGuaZhenYueRemaining--; if (baGuaZhenYueRemaining <= 0) { baGuaZhenYueActive = false; controlChanceBonus = 0f; } }
        if (mingXinActive) { mingXinRemaining--; if (mingXinRemaining <= 0) { mingXinActive = false; mingXinCritRateBonus = 0f; mingXinCritDamageBonus = 0f; } }
        if (extraDEFRemaining > 0) { extraDEFRemaining--; if (extraDEFRemaining <= 0) extraDEF = 0; }

        if (nextCritRateBonusRemaining > 0)
        {
            nextCritRateBonusRemaining--;
            if (nextCritRateBonusRemaining <= 0)
                nextCritRateBonus = 0f;
        }

        if (huiDengHitBonusRemaining > 0)
        {
            huiDengHitBonusRemaining--;
            if (huiDengHitBonusRemaining <= 0)
                huiDengHitBonus = 0f;
        }

        for (int i = attributeDebuffs.Count - 1; i >= 0; i--)
        {
            attributeDebuffs[i].remainingTurns--;
            if (attributeDebuffs[i].remainingTurns <= 0)
                attributeDebuffs.RemoveAt(i);
        }
        if (xunJieRemaining > 0) { xunJieRemaining--; if (xunJieRemaining <= 0) xunJieActive = false; }

        hunYuanDaoTiTriggeredThisTurn = false;
        poWangTriggeredThisTurn = false;

        // ★ 注意：灼伤/中毒不再在这里结算伤害和减少回合数，因为已经在 OnTurnStart 中处理了
        // 但为了保持 ReduceStatusDurations 的完整性，这里不处理灼伤/中毒相关代码。
        // 原本的灼伤/中毒伤害和回合减少已移至 ProcessBurnAndPoisonAtTurnStart。

        // ★ 诛仙剑护盾持续时间减少
        if (zhuXianShieldRemainingTurns > 0)
        {
            zhuXianShieldRemainingTurns--;
            if (zhuXianShieldRemainingTurns <= 0)
            {
                overHealShield = 0;
                BattleManager bm = FindObjectOfType<BattleManager>();
                if (bm != null)
                {
                    bm.AddTurnResultMessage($"{characterName} 的诛仙剑护盾消失了");
                }
            }
        }

        // ★ 玄甲护盾持续时间减少
        if (xuanWuShieldRemainingTurns > 0)
        {
            xuanWuShieldRemainingTurns--;
            if (xuanWuShieldRemainingTurns <= 0)
            {
                overHealShield = 0;
                BattleManager bm = FindObjectOfType<BattleManager>();
                if (bm != null)
                {
                    bm.AddTurnResultMessage($"{characterName} 的玄甲护盾消失了");
                }
            }
        }

        // ★ 白虎虎煞噬魂无敌状态回合减少
        if (isInvincible && invincibleRemaining > 0)
        {
            invincibleRemaining--;
            if (invincibleRemaining <= 0)
            {
                isInvincible = false;
                BattleManager bm = FindObjectOfType<BattleManager>();
                if (bm != null)
                {
                    bm.AddTurnResultMessage($"{characterName} 的无敌状态结束，因虎煞噬魂契约而死亡！");
                    // ★ 触发四象阵亡惩罚（移除光环、其他神兽增速）
                    bm.OnFourSymbolDeath(this);
                }
                currentHP = 0; // 状态结束时死亡
            }
        }

        // ★ 朱雀涅槃后无敌状态回合减少
        if (isPhoenixInvincible && phoenixInvincibleRemaining > 0)
        {
            phoenixInvincibleRemaining--;
            if (phoenixInvincibleRemaining <= 0)
            {
                isPhoenixInvincible = false;
                // 朱雀无敌到期正常结束，没有额外惩罚
            }
        }
    }

    public void ClearTempBuffs()
    {
        tempComboBonus = 0f;
        tempStunBonus = 0f;
        tempCritRateBonus = 0f;
        tempAttackBonus = 0f;
        tempDefBonus = 0f;
        tempHitBonus = 0f;
        tempEvaBonus = 0f;

        allowMultipleComboThisTurn = false;
        lianFengStacks = 0;
        lianFengDamageBonus = 0f;
        lianFengArmorPen = 0f;
        lianFengComboPenalty = 0f;

        // ★ 每回合重置妙法承佑"下次攻击"标记（层数由 ConsumeNextAttackBonus 消耗，不在此重置）
    }

    public void ApplySpeedBuff(float multiplier, float duration)
    {
        buffs.Add(new TempBuff(multiplier, duration));
        UpdateSpeedMultiplier();
    }

    public void UpdateBuffs(float deltaTime)
    {
        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            buffs[i].remainingTime -= deltaTime;
            if (buffs[i].remainingTime <= 0)
                buffs.RemoveAt(i);
        }
        UpdateSpeedMultiplier();
    }

    private void UpdateSpeedMultiplier()
    {
        float multiplier = 1f;
        foreach (var buff in buffs)
            multiplier *= buff.multiplier;
        speedMultiplier = multiplier;
    }

    private float GetSpeedMultiplier() => speedMultiplier;

    public void ApplyStun(int duration)
    {
        if (IsDead()) return;
        // ★ 无敌期间免疫控制
        if (isInvincible || isPhoenixInvincible)
        {
            BattleManager bm = FindObjectOfType<BattleManager>();
            if (bm != null) bm.AddTurnResultMessage($"{characterName} 处于无敌状态，免疫眩晕");
            return;
        }
        if (immuneToControl)
        {
            // 免疫眩晕，转化为行动条推迟
            float pushAmount = 500f * controlToPushMultiplier * duration;
            currentActionValue -= pushAmount;
            BattleManager bm = FindObjectOfType<BattleManager>();
            if (bm != null) bm.AddTurnResultMessage($"{characterName} 免疫眩晕，行动条推迟{pushAmount}");
            return;
        }
        isStunned = true;
        stunRemaining = duration;
        ShowStunEffect(true);
    }
    public void ClearStun()
    {
        isStunned = false;
        stunRemaining = 0;
        ShowStunEffect(false);
    }

    public void ApplyConfuse(int duration)
    {
        if (IsDead()) return;
        // ★ 无敌期间免疫控制
        if (isInvincible || isPhoenixInvincible)
        {
            BattleManager bm = FindObjectOfType<BattleManager>();
            if (bm != null) bm.AddTurnResultMessage($"{characterName} 处于无敌状态，免疫错乱");
            return;
        }
        if (immuneToControl)
        {
            float pushAmount = 500f * controlToPushMultiplier * duration;
            currentActionValue -= pushAmount;
            BattleManager bm = FindObjectOfType<BattleManager>();
            if (bm != null) bm.AddTurnResultMessage($"{characterName} 免疫错乱，行动条推迟{pushAmount}");
            return;
        }
        isConfused = true;
        confuseRemaining = duration;
        ShowConfuseEffect(true);
    }
    public void ClearConfuse()
    {
        isConfused = false;
        confuseRemaining = 0;
        ShowConfuseEffect(false);
    }

    public void ApplySleep(int duration)
    {
        if (IsDead()) return;
        // ★ 无敌期间免疫控制
        if (isInvincible || isPhoenixInvincible)
        {
            BattleManager bm = FindObjectOfType<BattleManager>();
            if (bm != null) bm.AddTurnResultMessage($"{characterName} 处于无敌状态，免疫睡眠");
            return;
        }
        if (immuneToControl)
        {
            float pushAmount = 500f * controlToPushMultiplier * duration;
            currentActionValue -= pushAmount;
            BattleManager bm = FindObjectOfType<BattleManager>();
            if (bm != null) bm.AddTurnResultMessage($"{characterName} 免疫睡眠，行动条推迟{pushAmount}");
            return;
        }
        isSleep = true;
        sleepRemaining = duration;
        ShowSleepEffect(true);
    }

    public void ClearSleep()
    {
        isSleep = false;
        sleepRemaining = 0;
        ShowSleepEffect(false);
    }

    public void CheckHeartMirror(BattleManager battleManager)
    {
        if (heartMirrorUsed) return;
        foreach (var art in equippedArtifacts)
        {
            if (art != null && art.artifactEffect == ArtifactEffect.HuXinJing && (float)currentHP / MaxHP < 0.3f)
            {
                int heal = Mathf.RoundToInt(MaxHP * 0.3f);
                int actualHeal = Heal(heal);
                Debug.Log($"{characterName} 触发护心镜，恢复 {heal} 生命{(heal > actualHeal ? $"，溢出{heal - actualHeal}转化为护盾" : "")}");
                battleManager.AddTurnResultMessage($"{characterName} 触发护心镜，恢复 {actualHeal} 生命{(heal > actualHeal ? $"，溢出{heal - actualHeal}转化为护盾" : "")}");
                heartMirrorUsed = true;
                break;
            }
        }
    }

    public void ApplyJinGangSanShield(BattleManager battleManager)
    {
        jinGangSanShield = 0;
        foreach (var art in equippedArtifacts)
        {
            if (art != null && art.artifactEffect == ArtifactEffect.JinGangSan)
            {
                jinGangSanShield = Mathf.RoundToInt(MaxHP * 0.2f);
                Debug.Log($"{characterName} 获得金刚伞护盾 {jinGangSanShield}");
                battleManager.AddTurnResultMessage($"{characterName} 获得金刚伞护盾 {jinGangSanShield}");
                break;
            }
        }
    }

    public void PlayAttackAnimation()
    {
        if (animator != null) animator.SetTrigger("Attack");
    }

    public void PlaySkillAnimation()
    {
        if (animator != null) animator.SetTrigger("Skill");
    }

    public void PlayHitAnimation()
    {
        if (animator != null) animator.SetTrigger("Hit");
    }

    public IEnumerator PlayHitAnimationCoroutine(bool isDefending = false)
    {
        if (isDefending) PlayDefenseAnimation();
        else PlayHitAnimation();
        yield return null;
        if (animator == null) yield break;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(stateInfo.length);
    }

    public void PlayEvasionAnimation()
    {
        if (animator != null) animator.SetTrigger("Evasion");
    }

    public IEnumerator PlayEvasionAnimationCoroutine()
    {
        PlayEvasionAnimation();
        yield return null;
        if (animator == null) yield break;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(stateInfo.length);
    }

    public void PlayDefenseAnimation()
    {
        if (animator != null) animator.SetTrigger("Defense");
    }

    public bool HasArtifactEffect(ArtifactEffect effect)
    {
        return equippedArtifacts.Exists(a => a != null && a.artifactEffect == effect);
    }

    public bool HasAnyBuff()
    {
        return tempAttackBonus > 0 || tempDefBonus > 0 || tempCritRateBonus > 0 || tempHitBonus > 0 || tempEvaBonus > 0 ||
               shengShengAttackBonusActive || mingXinActive;
    }

    public void DispelRandomBuff()
    {
        if (tempAttackBonus > 0) tempAttackBonus = 0f;
        else if (tempCritRateBonus > 0) tempCritRateBonus = 0f;
        else if (shengShengAttackBonusActive) shengShengAttackBonusActive = false;
        else if (mingXinActive) mingXinActive = false;
    }

    public void OnTurnStart()
    {
        // ★ 禅心入梦的暴击加成不再在回合开始时转移到tempCritRateBonus
        // 改为在 GetFinalCritRate() 中直接判断 chanXinCritBonus > 0

        // ★ 在回合开始时处理灼伤/中毒
        BattleManager bm = FindObjectOfType<BattleManager>();
        if (bm != null)
            ProcessBurnAndPoisonAtTurnStart(bm);

        // ★ 四神兽回合开始被动
        if (bm != null)
            ProcessFourSymbolPassivesAtTurnStart(bm);
    }

    /// <summary>
    /// 四神兽回合开始被动效果
    /// </summary>
    private void ProcessFourSymbolPassivesAtTurnStart(BattleManager bm)
    {
        switch (characterName)
        {
            case "青龙":
                break;

            case "白虎":

                break;

            case "朱雀":

                break;

            case "玄武":
                // 玄武回春：恢复全体友方10%最大生命值并驱散一个负面效果和控制效果
                foreach (var ally in bm.enemyParty)
                {
                    if (ally != null && !ally.IsDead())
                    {
                        int turtleHeal = Mathf.RoundToInt(ally.MaxHP * 0.1f);
                        int actualTurtleHeal = ally.Heal(turtleHeal);
                        // 驱散一个负面效果
                        if (ally.attributeDebuffs.Count > 0)
                        {
                            ally.attributeDebuffs.RemoveAt(ally.attributeDebuffs.Count - 1);
                        }
                        // 驱散控制效果
                        if (ally.isStunned) ally.ClearStun();
                        if (ally.isConfused) ally.ClearConfuse();
                        if (ally.isSleep) ally.ClearSleep();
                    }
                }
                bm.AddTurnResultMessage($"玄武回春触发，全体友方恢复10%生命并驱散负面效果和控制效果");

                // 玄武被动：血量低于30%时，回复已损失血量的80%（每次战斗限一次）
                if (!xuanWuLowHpHealUsed && (float)currentHP / MaxHP < 0.3f)
                {
                    xuanWuLowHpHealUsed = true;
                    int lostHP = MaxHP - currentHP;
                    int healAmount = Mathf.RoundToInt(lostHP * 0.8f);
                    int actualHeal = Heal(healAmount);
                    bm.AddTurnResultMessage($"玄武玄龟之佑触发，恢复已损失血量的80%（{actualHeal}点）");
                }
                break;
        }
    }

    public void OnComboTriggered(BattleManager battleManager)
    {
        if (faction == "TianWangDian")
        {
            int heal = Mathf.RoundToInt(MaxHP * 0.15f);
            int actualHeal = Heal(heal);
            battleManager.AddTurnResultMessage($"{characterName} 生生不息触发，回复{heal}生命{(heal > actualHeal ? $"，溢出{heal - actualHeal}转化为护盾" : "")}，攻击力+15%");
            shengShengAttackBonusActive = true;
            shengShengAttackBonusRemaining = 2;
        }
    }

    public void OnComboTriggeredForLianFeng(BattleManager battleManager)
    {
        if (faction != "TianWangDian") return;
        lianFengStacks++;
        lianFengDamageBonus = lianFengStacks * 0.1f;
        lianFengArmorPen = lianFengStacks * 0.05f;
        lianFengComboPenalty = lianFengStacks * 0.10f;
        battleManager.AddTurnResultMessage($"{characterName} 获得敛锋 {lianFengStacks} 层：伤害+{lianFengDamageBonus * 100:F0}%，防御忽视+{lianFengArmorPen * 100:F0}%，连击率-{lianFengComboPenalty * 100:F0}%");
    }

    public void OnAttacked(Character attacker, BattleManager battleManager)
    {
        if (faction == "WuZhuangGuan")
        {
            if (Random.value < 0.15f)
            {
                attacker.ApplyStun(2);
                battleManager.AddTurnResultMessage($"{characterName} 混元道体触发，{attacker.characterName}被眩晕");
                hunYuanDaoTiTriggeredThisTurn = true;
            }
        }

        if (faction == "FangCunShan" && !poWangTriggeredThisTurn)
        {
            if (Random.value < 0.8f)
            {
                attacker.hitRateDecrease = 0.1f;
                attacker.hitRateDecreaseRemaining = 2;
                battleManager.AddTurnResultMessage($"{characterName} 破妄之眼触发，{attacker.characterName}命中率降低10%");
                poWangTriggeredThisTurn = true;
            }
        }
    }

    public void OnStunSuccess(Character target, BattleManager battleManager)
    {
        if (baGuaZhenYueActive)
        {
            float increase = 500f * 0.3f;
            currentActionValue += increase;
            battleManager.AddTurnResultMessage($"{characterName} 镇岳状态触发，行动条增加30%");
        }

        if (faction == "WuZhuangGuan")
        {
            int heal = Mathf.RoundToInt(MaxHP * 0.15f);
            int actualHeal = Heal(heal);
            battleManager.AddTurnResultMessage($"{characterName} 天地同寿触发，回复{heal}生命{(heal > actualHeal ? $"，溢出{heal - actualHeal}转化为护盾" : "")}");
            target.damageTakenIncrease = 0.1f;
            target.damageTakenIncreaseRemaining = 2;
            battleManager.AddTurnResultMessage($"{target.characterName} 受到伤害提高10%，持续2回合");
        }
    }

    public void ApplyDaoXuanFuSuiEffects(Character target, BattleManager battleManager)
    {
        if (faction != "WuZhuangGuan") return;
        ApplyRandomAttributeDebuff(target, battleManager);
        ApplyXunJieToAllies(battleManager);
    }

    private readonly string[] debuffAttributesList = { "攻击", "防御", "速度", "命中", "闪避", "暴击", "晕击", "连击" };
    public void ApplyRandomAttributeDebuff(Character target, BattleManager battleManager)
    {
        HashSet<string> existingAttributes = new HashSet<string>(target.attributeDebuffs.Select(d => d.attribute));
        List<string> available = debuffAttributesList.Where(attr => !existingAttributes.Contains(attr)).ToList();

        string selectedAttr;
        if (available.Count > 0)
        {
            selectedAttr = available[Random.Range(0, available.Count)];
        }
        else
        {
            selectedAttr = debuffAttributesList[Random.Range(0, debuffAttributesList.Length)];
        }

        target.AddOrRefreshAttributeDebuff(selectedAttr, 0.1f, 2);
        battleManager.AddTurnResultMessage($"{characterName} 道玄缚祟触发，使 {target.characterName} 的{selectedAttr}降低10%，持续2回合");
    }

    public void AddOrRefreshAttributeDebuff(string attribute, float reducePercent, int duration)
    {
        var existing = attributeDebuffs.Find(d => d.attribute == attribute);
        if (existing != null)
        {
            existing.remainingTurns = duration;
        }
        else
        {
            attributeDebuffs.Add(new AttributeDebuff { attribute = attribute, reducePercent = reducePercent, remainingTurns = duration });
        }
    }

    public void ApplyXunJieToAllies(BattleManager battleManager)
    {
        foreach (var ally in battleManager.playerParty)
        {
            if (ally != null && !ally.IsDead())
            {
                ally.xunJieActive = true;
                ally.xunJieRemaining = 2;
                ally.xunJieSpeedBonus = 0.20f; // 迅捷加速20%
            }
        }
        battleManager.AddTurnResultMessage($"己方全体获得迅捷状态，速度+20%，持续2回合");
    }

    /// <summary>
    /// 消耗"下次攻击加成"buff（威震山河/袖里乾坤/禅心入梦/狮子搏兔）
    /// 调用时机：角色成功造成伤害后
    /// </summary>
    public void ConsumeNextAttackBonus()
    {
        if (nextAttackBonusConsumed) return;
        nextAttackBonusConsumed = true;

        // 消耗狮子搏兔的连击加成
        lionComboBuffActive = false;
        lionComboBuffRemaining = 0;

        // 消耗威震山河的连击加成
        weiZhenBuffActive = false;
        weiZhenBuffConsumed = true;

        // 消耗袖里乾坤的晕击加成
        xiuLiStunBuffActive = false;
        xiuLiStunBuffRemaining = 0;

        // 消耗禅心入梦的暴击加成
        chanXinCritBonus = 0f;
    }

    /// <summary>
    /// 消耗妙法承佑一层伤害加成（与下次攻击加成消耗解耦，每次攻击命中都独立检查）
    /// </summary>
    public void ConsumeMiaoFaBonus()
    {
        if (faction == "FangCunShan" && nextAttackDamageBonusStacks > 0)
        {
            nextAttackDamageBonusStacks--;
        }
    }

    public void OnCrit(Character target, BattleManager battleManager, int skillID)
    {
        if (faction == "FangCunShan")
        {
            int heal = Mathf.RoundToInt(MaxHP * 0.15f);
            int actualHeal = Heal(heal);
            battleManager.AddTurnResultMessage($"{characterName} 慧灯永续触发，回复{heal}生命{(heal > actualHeal ? $"，溢出{heal - actualHeal}转化为护盾" : "")}");
            huiDengHitBonus = 0.15f;
            huiDengHitBonusRemaining = 2;
            battleManager.AddTurnResultMessage($"命中率提高15%，持续2回合");

            // ★ 妙法承佑：暴击后使自身下一次攻击伤害提高20%（可叠加，上限60%）
            nextAttackDamageBonusStacks = Mathf.Min(nextAttackDamageBonusStacks + 1, 3);
            battleManager.AddTurnResultMessage($"妙法承佑：攻击伤害提高{nextAttackDamageBonusStacks * 20}%（{nextAttackDamageBonusStacks}层，上限60%）");
        }
        if (mingXinActive)
        {
            float increase = 500f * 0.2f;
            currentActionValue += increase;
            battleManager.AddTurnResultMessage($"{characterName} 明心见性触发，行动条增加20%");
        }
    }

    public string GetDetailedInfo()
    {
        StringBuilder sb = new StringBuilder();

        float fleeChance = 0.3f;
        BattleManager battleManager = GameObject.FindObjectOfType<BattleManager>();
        if (battleManager != null)
        {
            fleeChance = battleManager.fleeBaseChance + (this.CurrentSpeed / 500f);
        }

        string factionName = string.IsNullOrEmpty(faction) ? "无门派" : faction;
        sb.AppendLine($"{characterName} ({factionName}) Lv.{level}");

        int totalShield = jinGangSanShield + overHealShield;
        int maxShield = MaxHP;

        int baseATK = BaseATK;
        int finalATK = GetFinalATK();
        int attackBonus = finalATK - baseATK;
        string attackStr = attackBonus != 0 ? $"{finalATK} ({attackBonus})" : $"{baseATK}";

        int baseDEF = cachedDEF + extraDEF;
        int finalDEF = DEF;
        int defBonus = finalDEF - baseDEF;
        string defenseStr = defBonus != 0 ? $"{finalDEF} ({defBonus})" : $"{finalDEF}";

        float baseSPD = SPD;
        float finalSpeed = CurrentSpeed;
        int speedBonus = Mathf.RoundToInt(finalSpeed - baseSPD);
        string speedStr = speedBonus != 0 ? $"{finalSpeed:F0} (+{speedBonus})" : $"{finalSpeed:F0}";

        float baseComboRaw = totalComboChance;
        float finalCombo = GetTotalComboChance();
        float comboBonusTotal = finalCombo - baseComboRaw;
        string comboStr = comboBonusTotal != 0 ? $"{finalCombo * 100:F1}% ({comboBonusTotal * 100:+0.0;-0.0}%)" : $"{finalCombo * 100:F1}%";

        float baseStunRaw = totalStunChance;
        float finalStun = GetTotalStunChance();
        float stunBonusTotal = finalStun - baseStunRaw;
        string stunStr = stunBonusTotal != 0 ? $"{finalStun * 100:F1}% ({stunBonusTotal * 100:+0.0;-0.0}%)" : $"{finalStun * 100:F1}%";

        float baseCritRaw = cachedCritRate;
        float finalCrit = GetFinalCritRate();
        float critBonusTotal = finalCrit - baseCritRaw;
        string critStr = critBonusTotal != 0 ? $"{finalCrit * 100:F1}% ({critBonusTotal * 100:+0.0;-0.0}%)" : $"{finalCrit * 100:F1}%";

        float baseHitRaw = cachedHitRate;
        float finalHit = HitRate;
        float hitBonusTotal = finalHit - baseHitRaw;
        string hitStr = hitBonusTotal != 0 ? $"{finalHit * 100:F1}% ({hitBonusTotal * 100:+0.0;-0.0}%)" : $"{finalHit * 100:F1}%";

        float baseEvaRaw = cachedEvasionRate;
        float finalEva = EvasionRate;
        float evaBonusTotal = finalEva - baseEvaRaw;
        string evaStr = evaBonusTotal != 0 ? $"{finalEva * 100:F1}% ({evaBonusTotal * 100:+0.0;-0.0}%)" : $"{finalEva * 100:F1}%";

        float critDamageBonus = GetFinalCritDamage() * 100f;
        string critDamageStr = $"{critDamageBonus:F0}%";

        string defenseReductionStr = $"{defenseReduction * 100:F0}%";
        float baseCounter = counterChance * 100f;
        string counterStr = $"{baseCounter:F1}%";
        string fleeStr = $"{fleeChance * 100:F1}%";

        const int colWidth4 = 25;

        sb.AppendFormat("{0,-" + colWidth4 + "}{1,-" + colWidth4 + "}{2,-" + colWidth4 + "}{3,-" + colWidth4 + "}\n",
            $"生命: {currentHP}/{MaxHP}",
            $"内力: {currentMP}/{MaxMP}",
            $"护盾: {totalShield}/{maxShield}",
            $"行动: {currentActionValue / 500f * 100:F0}%");

        string penetrationStr = "0%";
        sb.AppendFormat("{0,-" + colWidth4 + "}{1,-" + colWidth4 + "}{2,-" + colWidth4 + "}{3,-" + colWidth4 + "}\n",
            $"攻击: {attackStr}",
            $"穿透: {penetrationStr}",
            $"防御: {defenseStr}",
            $"减伤: {defenseReductionStr}");

        sb.AppendFormat("{0,-" + colWidth4 + "}{1,-" + colWidth4 + "}{2,-" + colWidth4 + "}{3,-" + colWidth4 + "}\n",
            $"连击: {comboStr}",
            $"晕击: {stunStr}",
            $"暴击: {critStr}",
            $"暴伤: {critDamageStr}");

        sb.AppendFormat("{0,-" + colWidth4 + "}{1,-" + colWidth4 + "}{2,-" + colWidth4 + "}{3,-" + colWidth4 + "}\n",
            $"命中: {hitStr}",
            $"闪避: {evaStr}",
            $"速度: {speedStr}",
            $"逃跑: {fleeStr}");

        // ★ 修改反伤显示：通用反弹百分比
        string reflectDesc = reflectDamagePercent > 0 ? $"{reflectDamagePercent * 100:F0}%" : "0%";
        // 列阵反击伤害（原有）
        int formationReflectDamage = 0;
        float reflectMult = 0f;
        if (isInArrayFormation)
        {
            reflectMult = 0.6f;
            if (faction == "TianWangDian") reflectMult = 0.9f;
            formationReflectDamage = Mathf.RoundToInt(GetFinalATK() * reflectMult);
        }
        string formationReflectStr = formationReflectDamage > 0 ? $"{formationReflectDamage}" : "0";

        // ★ 计算当前总免伤
        float totalDamageReduction = damageReductionPercent;
        if (isInArrayFormation)
            totalDamageReduction += arrayFormationReduction;
        else if (isDefending)
            totalDamageReduction += defenseReduction;
        string totalDamageReductionStr = $"{totalDamageReduction * 100:F0}%";

        sb.AppendFormat("{0,-" + colWidth4 + "}{1,-" + colWidth4 + "}{2,-" + colWidth4 + "}{3,-" + colWidth4 + "}\n",
        $"反击: {counterStr}",
        $"反伤: {reflectDesc}",
        $"列阵: {formationReflectStr}",
        $"免伤: {totalDamageReductionStr}");

        sb.AppendLine("状态效果:");
        List<string> effectDescriptions = new List<string>();

        if (isConfused) effectDescriptions.Add($"错乱：无法施展任何技能，普通攻击随机选择目标（包括队友）。剩余{confuseRemaining}回合");
        if (isStunned) effectDescriptions.Add($"眩晕：无法行动。剩余{stunRemaining}回合");
        if (isSleep) effectDescriptions.Add($"睡眠：无法行动，受到任何伤害后立即醒来。剩余{sleepRemaining}回合");
        if (isInvincible) effectDescriptions.Add($"白虎虎煞噬魂无敌：免疫所有伤害和控制。剩余{invincibleRemaining}回合");
        if (isPhoenixInvincible) effectDescriptions.Add($"朱雀涅槃无敌：免疫所有伤害和控制。剩余{phoenixInvincibleRemaining}回合");
        if (isDefending) effectDescriptions.Add($"防御中：所受伤害降低{defenseReduction * 100:F0}%。剩余{defenseRemaining}回合");
        if (isInArrayFormation) effectDescriptions.Add($"列阵（不动如山）：所受伤害降低{arrayFormationReduction * 100:F0}%，被攻击时{arrayFormationCounterChance * 100:F0}%概率反击（造成{reflectMult * 100:F0}%攻击力的固定伤害）；剩余{arrayFormationRemaining}回合");
        if (baGuaZhenYueActive) effectDescriptions.Add($"镇岳（五雷轰顶）：晕击概率提高{controlChanceBonus * 100:F0}%，成功晕击时拉条30%。剩余{baGuaZhenYueRemaining}回合");
        if (mingXinActive) effectDescriptions.Add($"明心（明心见性）：暴击率提高{mingXinCritRateBonus * 100:F0}%，暴击伤害提高{mingXinCritDamageBonus * 100:F0}%，暴击拉条20%；剩余{mingXinRemaining}回合");
        if (lionComboBuffActive) effectDescriptions.Add($"狮子搏兔：下次攻击连击率提高{lionComboBuffValue * 100:F0}%。");
        if (weiZhenBuffActive) effectDescriptions.Add($"威震山河：下次攻击连击率提高{lionComboBuffValue * 100:F0}%。");
        if (shengShengAttackBonusActive) effectDescriptions.Add($"生生不息：攻击力提高{shengShengAttackBonusValue * 100:F0}%。剩余{shengShengAttackBonusRemaining}回合");
        if (xiuLiStunBuffActive) effectDescriptions.Add($"袖里乾坤：下次攻击晕击率提高{xiuLiStunBuffValue * 100:F0}%。");
        if (damageTakenIncreaseRemaining > 0) effectDescriptions.Add($"天地同寿：受到的伤害提高{damageTakenIncrease * 100:F0}%。剩余{damageTakenIncreaseRemaining}回合");
        if (hitRateDecreaseRemaining > 0) effectDescriptions.Add($"破妄之眼：命中率降低{hitRateDecrease * 100:F0}%。剩余{hitRateDecreaseRemaining}回合");
        if (tempComboBonus > 0) effectDescriptions.Add($"枕戈待旦：连击率+{tempComboBonus * 100:F0}%（仅本回合）");
        if (tempStunBonus > 0) effectDescriptions.Add($"引雷控电：晕击率+{tempStunBonus * 100:F0}%（仅本回合）");
        if (tempCritRateBonus > 0) effectDescriptions.Add($"空明心境：暴击率+{tempCritRateBonus * 100:F0}%（仅本回合）");
        if (tempAttackBonus > 0) effectDescriptions.Add($"临时攻击增益：攻击力+{tempAttackBonus * 100:F0}%（仅本回合）");
        if (tempDefBonus > 0) effectDescriptions.Add($"临时防御增益：防御力+{tempDefBonus * 100:F0}%（仅本回合）");
        if (tempHitBonus > 0) effectDescriptions.Add($"临时命中增益：命中率+{tempHitBonus * 100:F0}%（仅本回合）");
        if (tempEvaBonus > 0) effectDescriptions.Add($"临时闪避增益：闪避率+{tempEvaBonus * 100:F0}%（仅本回合）");
        if (nextCritRateBonus > 0) effectDescriptions.Add($"慧灯永续：下次攻击暴击率+{nextCritRateBonus * 100:F0}%");
        if (huiDengHitBonus > 0) effectDescriptions.Add($"慧灯永续：命中率+{huiDengHitBonus * 100:F0}%");
        if (chanXinCritBonus > 0) effectDescriptions.Add($"禅心入梦：下次攻击暴击率+10%");
        if (xianXianCritBonus > 0) effectDescriptions.Add($"陷仙结界：暴击率+{xianXianCritBonus * 100:F0}%");

        if (lianFengStacks > 0) effectDescriptions.Add($"敛锋：伤害+{lianFengDamageBonus * 100:F0}%，防御忽视+{lianFengArmorPen * 100:F0}%，连击率-{lianFengComboPenalty * 100:F0}% (本回合)");
        if (xunJieActive) effectDescriptions.Add($"迅捷：速度+{xunJieSpeedBonus * 100:F0}%。剩余{xunJieRemaining}回合");
        foreach (var debuff in attributeDebuffs)
        {
            effectDescriptions.Add($"{debuff.attribute}降低{debuff.reducePercent * 100:F0}%。剩余{debuff.remainingTurns}回合");
        }

        // ★ 结界效果显示
        if (characterName == "诛仙剑")
        {
            effectDescriptions.Add($"诛仙结界：每次行动后为全体友方施加最大生命值5%的护盾（持续2回合）");
        }
        else if (characterName == "陷仙剑")
        {
            effectDescriptions.Add($"陷仙结界：全体友方暴击率+15%");
        }
        else if (characterName == "绝仙剑")
        {
            effectDescriptions.Add($"绝仙结界：全体友方攻击附带灼烧效果（2回合）");
        }
        else if (characterName == "戮仙剑")
        {
            effectDescriptions.Add($"戮仙结界：全体友方攻击附带中毒效果（2回合）");
        }

        // ★ 通用被动显示（反伤、免疫控制）
        if (reflectDamagePercent > 0)
            effectDescriptions.Add($"反伤 {reflectDamagePercent * 100:F0}%");
        if (immuneToControl)
            effectDescriptions.Add($"免疫控制（被控时行动条推迟 {controlToPushMultiplier * 100:F0}% * 持续回合数）");

        // ★ 新增中毒/灼伤显示
        if (burnRemainingTurns.Count > 0)
        {
            effectDescriptions.Add($"灼伤 {burnRemainingTurns.Count} 层，每层剩余回合：{string.Join(",", burnRemainingTurns)}");
        }
        if (poisonRemainingTurns.Count > 0)
        {
            effectDescriptions.Add($"中毒 {poisonRemainingTurns.Count} 层，每层剩余回合：{string.Join(",", poisonRemainingTurns)}");
        }
        if (isCharging && chargedSkill != null)
        {
            effectDescriptions.Add($"蓄力中：{chargedSkill.skillName}，剩余 {currentChargeTurns} 回合");
        }
        if (zhuXianShieldRemainingTurns > 0)
        {
            effectDescriptions.Add($"诛仙剑护盾：吸收 {overHealShield} 伤害，剩余 {zhuXianShieldRemainingTurns} 回合");
        }
        if (xuanWuShieldRemainingTurns > 0)
        {
            effectDescriptions.Add($"玄甲护体：吸收 {overHealShield} 伤害，剩余 {xuanWuShieldRemainingTurns} 回合");
        }
        if (shadowDeathAttackBonusStacks > 0)
        {
            effectDescriptions.Add($"虚影祭献：攻击力+{shadowDeathAttackBonusValue * 100:F0}%（{shadowDeathAttackBonusStacks}层）");
        }

        if (effectDescriptions.Count == 0) sb.AppendLine("  无");
        else foreach (var desc in effectDescriptions) sb.AppendLine($"  {desc}");

        sb.AppendLine("主动技能:");
        if (skills.Count == 0) sb.AppendLine("  无");
        else
        {
            foreach (var skill in skills)
            {
                string cdText = skill.currentCooldown > 0 ? $"（冷却 {skill.currentCooldown} 回合）" : "（可用）";
                sb.AppendLine($"  - {skill.skillName} {cdText}，消耗 {skill.mpCost}MP，{skill.description}");
            }
        }

        sb.AppendLine("被动技能:");
        switch (faction)
        {
            case "TianWangDian":
                string youRenYouYuDesc = "";
                float overCritTW = GetOverCritRate();
                if (overCritTW > 0)
                    youRenYouYuDesc = $"（溢出 {overCritTW * 100:F0}% 暴击率，已转换为 {overCritTW * 100:F0}% 暴伤系数）";
                sb.AppendLine($"  - 游刃有余：将溢出的暴击率按 1:1 比例转化为暴伤系数。{youRenYouYuDesc}");
                sb.AppendLine($"  - 迅疾如风：免伤提升30%；反击后使自身行动提前10%。");
                sb.AppendLine($"  - 生生不息：每次触发连击时，恢复自身15%最大生命值，并提升15%攻击力，持续2回合（不可叠加）。当前剩余 {shengShengAttackBonusRemaining} 回合。");
                sb.AppendLine($"  - 蓄势待发：连击可多次触发，每次触发连击时获得“敛锋”状态（提升自身10%伤害与5%防御忽视，并降低自身10%连击概率），可叠加（上限10层），持续到本回合结束；若回合结束时连击率大于等于50%且本回合内未触发连击，则额外发动一次不可多次触发的连击。当前敛锋层数：{lianFengStacks}。");
                break;
            case "WuZhuangGuan":
                string chengZhuZaiXiongDesc = "";
                float overCritWZ = GetOverCritRate();
                if (overCritWZ > 0)
                    chengZhuZaiXiongDesc = $"（溢出 {overCritWZ * 100:F0}% 暴击率，已转换为 {overCritWZ * 100:F0}% 暴伤系数）";
                sb.AppendLine($"  - 成竹在胸：将溢出的暴击率按 1:1 比例转化为暴伤系数。{chengZhuZaiXiongDesc}");
                sb.AppendLine($"  - 混元道体：免伤提升30%；受到攻击时，有15%概率使攻击者眩晕1回合（本回合已触发：{(hunYuanDaoTiTriggeredThisTurn ? "是" : "否")}）。");
                string tianDiTongShouStatus = damageTakenIncreaseRemaining > 0 ? $"目标易伤 {damageTakenIncrease * 100:F0}% 剩余 {damageTakenIncreaseRemaining} 回合" : "未激活";
                sb.AppendLine($"  - 天地同寿：每次成功眩晕目标时，恢复自身15%最大生命值，并施加“易伤”效果（受到的伤害提高10%），持续2回合（不可叠加）。{tianDiTongShouStatus}");
                sb.AppendLine($"  - 道玄缚祟：攻击目标前，从未施加的负面效果中随机选择一个进行施加（除生命外的战斗属性降低10%，不可叠加），持续2回合，并对己方全体角色施加“迅捷”状态（提升20%速度，持续2回合，不可叠加）；敌方每存在一个负面效果（战斗属性降低系列），提升15%伤害，最多可提升75%。");
                break;
            case "FangCunShan":
                string deXinYingShouDesc = "";
                float overCritFC = GetOverCritRate();
                if (overCritFC > 0)
                    deXinYingShouDesc = $"（溢出 {overCritFC * 100:F0}% 暴击率，已转换为 {overCritFC * 2 * 100:F0}% 暴伤系数）";
                sb.AppendLine($"  - 得心应手：将溢出的暴击率按 1:2 比例转化为暴伤系数。{deXinYingShouDesc}");
                string poWangStatus = hitRateDecreaseRemaining > 0 ? $"目标命中-{hitRateDecrease * 100:F0}% 剩余 {hitRateDecreaseRemaining} 回合" : "未激活";
                sb.AppendLine($"  - 破妄之眼：免伤提升30%；受到攻击时，有80%概率使目标命中率降低10%，持续2回合（不可叠加）。{poWangStatus}");
                sb.AppendLine($"  - 慧灯永续：每次触发暴击时，自身回复15%最大生命值，并提升15%命中率，持续2回合（不可叠加）。当前命中加成{huiDengHitBonus * 100:F0}%。");
                sb.AppendLine($"  - 妙法承佑：暴击时忽视敌方20%防御，并为己方全体角色施加相当于此次伤害30%的护盾（含无相慧剑溅射伤害）；暴击后使自身下一次攻击伤害提高20%（可叠加，上限60%，触发后消耗一层）。当前层数：{nextAttackDamageBonusStacks}。");
                break;
            default:
                // 根据角色名字显示被动
                switch (characterName)
                {
                    case "通天教主":
                        sb.AppendLine($"  - 天生圣体：免疫常规控制，被控时行动条推迟{controlToPushMultiplier * 100:F0}% * 持续回合数；常驻免伤{damageReductionPercent * 100:F0}%，反弹{reflectDamagePercent * 100:F0}%伤害；");
                        sb.AppendLine($"  - 虚影祭献：每有一把虚影被消灭，扣除自身最大生命值10%，提升10%攻击力（当前{shadowDeathAttackBonusStacks}层，+{shadowDeathAttackBonusValue * 100:F0}%攻击）");
                        // ★ 新增：剑意如潮被动
                        if (currentPhase >= 2)
                        {
                            if (currentPhase == 2)
                                sb.AppendLine($"  - 剑意如潮（二阶段）：每三次主动攻击后追加一次普通攻击。");
                            else if (currentPhase == 3)
                                sb.AppendLine($"  - 剑意如潮（三阶段）：每两次主动攻击后追加一次普通攻击，并填充自身50%行动条。");
                        }
                        else
                        {
                            sb.AppendLine($"  - 剑意如潮：未激活（进入二阶段后生效）");
                        }
                        break;
                    case "诛仙剑":
                    case "陷仙剑":
                    case "绝仙剑":
                    case "戮仙剑":
                        sb.AppendLine($"  - 不灭剑意：免疫常规控制，被控时行动条推迟{controlToPushMultiplier * 100:F0}% * 持续回合数；常驻免伤{damageReductionPercent * 100:F0}%，反弹{reflectDamagePercent * 100:F0}%伤害；");
                        sb.AppendLine($"  - 协同作战：行动后，使通天教主行动条填充10%");
                        break;
                    case "青龙":
                        sb.AppendLine($"  - 龙之逆鳞：每次攻击时，减少目标10%行动条；攻击后自身行动条填充10%。每三次主动攻击后追加一次普通攻击，并填充自身30%行动条。");
                        sb.AppendLine($"  - 御风阵（光环）：全体友方每次攻击时，减少目标10%行动条。");
                        sb.AppendLine($"  - 四象联动（被动）：任意神兽行动时，其余三只获得10%行动条填充。");
                        sb.AppendLine($"  - 阵亡惩罚（被动）：阵亡时，其余存活神兽速度提升15%。");
                        break;
                    case "白虎":
                        sb.AppendLine($"  - 虎煞噬魂：暴击时额外造成目标最大生命值15%的真实伤害。当前血量每损失1%，获得1%攻击力加成，持续到战斗结束。当前攻击力加成：{Mathf.Clamp01(1f - (MaxHP > 0 ? (float)currentHP / MaxHP : 1f)) * 100:F0}%。受到致命伤害时获得3回合无敌状态：清空自身所有负面及控制效果，免疫所有伤害和控制效果，状态结束时死亡。无敌状态：{(isInvincible ? $"激活中（剩余{invincibleRemaining}回合）" : "未激活")}。");
                        sb.AppendLine($"  - 杀伐阵（光环）：全体友方攻击力提升15%。");
                        sb.AppendLine($"  - 四象联动（被动）：任意神兽行动时，其余三只获得10%行动条填充。");
                        sb.AppendLine($"  - 阵亡惩罚（被动）：阵亡时，其余存活神兽速度提升15%。");
                        break;
                    case "朱雀":
                        string rebirthStatus = hasUsedPhoenixRebirth ? "已使用" : "未使用";
                        string phoenixInvStatus = isPhoenixInvincible ? $"激活中（剩余{phoenixInvincibleRemaining}回合）" : "未激活";
                        sb.AppendLine($"  - 朱雀涅槃：受到致命伤害时，若任意敌方角色处于灼烧状态，立即触发其全部灼烧效果，并以75%最大生命值复活（每次战斗限一次）；复活后清空自身所有负面及控制效果，免疫所有伤害和控制效果，持续1回合。状态：{rebirthStatus}。涅槃无敌：{phoenixInvStatus}。");
                        sb.AppendLine($"  - 焚野阵（光环）：全体友方攻击时附带灼烧效果，持续2回合。");
                        sb.AppendLine($"  - 四象联动（被动）：任意神兽行动时，其余三只获得10%行动条填充。");
                        sb.AppendLine($"  - 阵亡惩罚（被动）：阵亡时，其余存活神兽速度提升15%。");
                        break;
                    case "玄武":
                        string lowHpHealStatus = xuanWuLowHpHealUsed ? "已使用" : "未使用";
                        sb.AppendLine($"  - 玄武回春：每回合开始时恢复全体友方10%最大生命值并驱散一个负面效果和控制效果；血量低于30%时，回复已损失血量的80%（每次战斗限一次）。低血量回春状态：{lowHpHealStatus}。");
                        sb.AppendLine($"  - 镇岳阵（光环）：为全体友方提升防御力，数值相当于自身防御力的30%；并且，玄武会为其他友方分摊50%的伤害。");
                        sb.AppendLine($"  - 四象联动（被动）：任意神兽行动时，其余三只获得10%行动条填充。");
                        sb.AppendLine($"  - 阵亡惩罚（被动）：阵亡时，其余存活神兽速度提升15%。");
                        break;
                    default:
                        sb.AppendLine("  无");
                        break;
                }
                break;
        }

        sb.AppendLine("  法宝:");
        if (equippedArtifacts.Count == 0 || equippedArtifacts.All(a => a == null)) sb.AppendLine("  无");
        else
        {
            foreach (var art in equippedArtifacts)
            {
                if (art == null) continue;
                string effectDesc = GetArtifactEffectDescription(art.artifactEffect);
                string extra = "";
                switch (art.artifactEffect)
                {
                    case ArtifactEffect.JinGangSan: extra = $"(护盾:{jinGangSanShield})"; break;
                    case ArtifactEffect.LingFengPei: extra = $"(层数:{windWingStacks})"; break;
                    case ArtifactEffect.HuXinJing: extra = heartMirrorUsed ? "(已触发)" : "(未触发)"; break;
                    case ArtifactEffect.FengLeiYi: extra = fengLeiYiTriggeredThisTurn ? "(本回合已触发)" : "(未触发)"; break;
                }
                sb.AppendLine($"  - {art.itemName}：{effectDesc} {extra}");
            }
        }

        return sb.ToString();
    }

    private string GetArtifactEffectDescription(ArtifactEffect effect)
    {
        switch (effect)
        {
            case ArtifactEffect.FenTianZhu: return "攻击时20%概率触发爆炸，对目标及其周围所有敌人造成50%的溅射伤害（不可连续触发）";
            case ArtifactEffect.LeiShenChui: return "攻击时10%概率触发连锁闪电，对额外2个随机敌人造成50%伤害";
            case ArtifactEffect.PoJunFu: return "对生命值高于70%的敌人，造成的伤害提高30%";
            case ArtifactEffect.XuanBingJia: return "受到攻击时，有20%概率触发冰盾，使本次伤害降低50%，并对攻击者造成50%的反伤";
            case ArtifactEffect.JinGangSan: return "每回合开始时，获得一个可吸收20%最大生命值的护盾，持续1回合";
            case ArtifactEffect.HuXinJing: return "当生命值低于30%时，立即恢复30%生命值（每场战斗仅触发一次）";
            case ArtifactEffect.FengLeiYi: return "行动后有10%概率获得额外一次行动机会（不可连续触发）";
            case ArtifactEffect.LingFengPei: return "每次击杀敌人后，自身速度提高10%，持续至战斗结束，最多叠加5层";
            case ArtifactEffect.LunHuiJing: return "每次攻击命中后，恢复造成伤害20%的生命值（吸血）";
            case ArtifactEffect.ZhenHunFan: return "攻击时有15%概率使目标眩晕1回合（对精英/Boss效果减半，不可连续触发）";
            default: return "未知效果";
        }
    }

    private class TempBuff
    {
        public float multiplier;
        public float remainingTime;
        public TempBuff(float mult, float duration)
        {
            multiplier = mult;
            remainingTime = duration;
        }
    }

    private void ShowStunEffect(bool show) { if (stunEffectObject != null) stunEffectObject.SetActive(show); }
    private void ShowConfuseEffect(bool show) { if (confuseEffectObject != null) confuseEffectObject.SetActive(show); }
    private void ShowSleepEffect(bool show) { if (sleepEffectObject != null) sleepEffectObject.SetActive(show); }
    private Coroutine hitEffectCoroutine;

    private void ShowHitEffect(bool show)
    {
        if (hitEffectObject == null) return;
        if (show)
        {
            if (hitEffectCoroutine != null)
                StopCoroutine(hitEffectCoroutine);
            hitEffectCoroutine = StartCoroutine(PlayHitEffectCoroutine());
        }
        else
        {
            if (hitEffectCoroutine != null)
                StopCoroutine(hitEffectCoroutine);
            hitEffectObject.SetActive(false);
        }
    }
    private IEnumerator PlayHitEffectCoroutine()
    {
        hitEffectObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        hitEffectObject.SetActive(false);
    }

    public int Heal(int amount)
    {
        if (amount <= 0 || IsDead()) return 0;
        int newHP = currentHP + amount;
        if (newHP > MaxHP)
        {
            int overflow = newHP - MaxHP;
            currentHP = MaxHP;
            overHealShield = Mathf.Min(overHealShield + overflow, MaxHP);
            return amount - overflow;
        }
        else
        {
            currentHP = newHP;
            return amount;
        }
    }

    private void GetEffectObjects()
    {
        if (stunEffectObject == null) stunEffectObject = transform.Find("Effects/StunEffect")?.gameObject;
        if (confuseEffectObject == null) confuseEffectObject = transform.Find("Effects/ConfuseEffect")?.gameObject;
        if (sleepEffectObject == null) sleepEffectObject = transform.Find("Effects/SleepEffect")?.gameObject;
        if (hitEffectObject == null) hitEffectObject = transform.Find("Effects/HitEffect")?.gameObject;
    }
}