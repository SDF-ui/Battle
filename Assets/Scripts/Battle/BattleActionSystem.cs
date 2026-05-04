using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 战斗行动系统 - 管理行动值、行动顺序、技能执行和伤害计算
/// 从 BattleManager 拆分出来的职责模块
/// </summary>
public class BattleActionSystem
{
    private BattleManager battleManager;
    private List<Character> allCharacters;
    private BattleUIManager uiManager;

    // 事件回调
    public System.Action<Character> onSetIconScale;
    public System.Func<bool> onCheckBattleEnd;

    public const float ACTION_THRESHOLD = 500f;
    public const float MAX_PREDICT_ACTION_THRESHOLD = 4000f;

    public BattleActionSystem(BattleManager manager, BattleUIManager ui, List<Character> characters)
    {
        battleManager = manager;
        uiManager = ui;
        allCharacters = characters;
    }

    /// <summary>
    /// 更新所有角色的行动值和 buff
    /// </summary>
    public void UpdateActions(float deltaTime)
    {
        foreach (var c in allCharacters)
            c.UpdateBuffs(deltaTime);

        foreach (var c in allCharacters)
            if (!c.IsDead())
                c.AddActionValue(deltaTime);
    }

    /// <summary>
    /// 获取行动值已满且可以行动的角色列表（按行动值降序）
    /// </summary>
    public List<Character> GetReadyCharacters()
    {
        var ready = allCharacters
            .Where(c => !c.IsDead() && c.currentActionValue >= ACTION_THRESHOLD)
            .ToList();
        ready.Sort((a, b) => b.currentActionValue.CompareTo(a.currentActionValue));
        return ready;
    }

    /// <summary>
    /// 执行一次攻击
    /// </summary>
    public IEnumerator PerformAttack(Character attacker, Character target, Skill skill, System.Action onComplete)
    {
        if (attacker == null || target == null || target.IsDead())
        {
            onComplete?.Invoke();
            yield break;
        }

        // 检查是否需要对攻击者减行动值（非免费技能）
        if (skill == null || !skill.isFreeAction)
        {
            attacker.currentActionValue = 0f;
            onSetIconScale?.Invoke(attacker);
        }

        // 动画播放
        if (attacker.animator != null)
            attacker.animator.SetTrigger("Attack");

        // 伤害计算逻辑（简化 - 实际逻辑在 BattleManager 中）
        float damageMultiplier = skill != null && skill.type == SkillType.Attack ? 1.0f : 1.0f;
        bool isCrit = Random.value < attacker.GetFinalCritRate();
        int baseDamage = attacker.GetFinalATK();
        int damage = Mathf.RoundToInt(baseDamage * damageMultiplier * (isCrit ? attacker.GetFinalCritDamage() : 1f));

        // 减伤和防御处理
        if (target.isDefending)
            damage = Mathf.RoundToInt(damage * target.defenseReduction);

        // 应用伤害
        target.TakeDamage(damage);
        uiManager?.AddTurnResultMessage($"{attacker.characterName} 对 {target.characterName} 造成 {damage} 点伤害{(isCrit ? "（暴击）" : "")}");

        // 检查目标死亡
        if (target.IsDead())
        {
            uiManager?.AddTurnResultMessage($"{target.characterName} 已被击败！");
            onCheckBattleEnd?.Invoke();
        }

        // 攻击者状态处理
        attacker.ReduceStatusDurations();

        // 盾剑光环检查
        if (ShadowAura.HasJueXianAura(attacker, battleManager))
        {
            target.ApplyBurn(1);
            uiManager?.AddTurnResultMessage($"{attacker.characterName} 灼烧效果已施加");
        }
        if (ShadowAura.HasLuXianAura(attacker, battleManager))
        {
            target.ApplyPoison(1);
            uiManager?.AddTurnResultMessage($"{attacker.characterName} 中毒效果已施加");
        }

        onComplete?.Invoke();
        yield return new WaitForSeconds(attacker.attackHitTime);
    }

    /// <summary>
    /// 敌人 AI 选择技能
    /// </summary>
    public Skill SelectEnemySkill(Character enemy)
    {
        if (enemy.skills == null || enemy.skills.Count == 0)
            return null;

        var available = enemy.skills
            .Where(s => s.currentCooldown <= 0 && enemy.currentMP >= s.mpCost)
            .ToList();

        if (available.Count == 0)
            return null;

        var nonNormal = available.Where(s => s.skillID != 0).ToList();
        if (nonNormal.Count > 0)
            return nonNormal[Random.Range(0, nonNormal.Count)];

        return available[0];
    }

    /// <summary>
    /// 敌人选择目标
    /// </summary>
    public Character SelectEnemyTarget(List<Character> playerParty)
    {
        var alive = playerParty.Where(p => !p.IsDead()).ToList();
        if (alive.Count == 0) return null;
        return alive[Random.Range(0, alive.Count)];
    }
}
