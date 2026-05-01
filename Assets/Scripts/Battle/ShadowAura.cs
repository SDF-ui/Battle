using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 四象剑阵虚影的光环效果管理脚本
/// 挂载在诛仙剑、陷仙剑、绝仙剑、戮仙剑虚影上
/// </summary>
public class ShadowAura : MonoBehaviour
{
    private Character shadow;
    private BattleManager battleManager;
    private string auraType;  // 诛仙剑、陷仙剑、绝仙剑、戮仙剑

    // 光环生效标记（用于移除时恢复）
    private bool isAuraApplied = false;

    public void Initialize(Character character, BattleManager manager)
    {
        shadow = character;
        battleManager = manager;
        auraType = character.characterName;  // 根据名称确定光环类型
        ApplyAura();
    }

    private void ApplyAura()
    {
        if (isAuraApplied) return;

        switch (auraType)
        {
            case "诛仙剑":
                // 诛仙结界：每次行动后，为全体友方施加5%最大生命值的护盾（持续2回合，不可叠加）
                // 通过订阅行动事件实现
                if (shadow != null)
                {
                    // 使用协程或事件，这里简单在Update中检测行动结束不准确，改用角色行动后回调
                    // 由于Character没有内置行动结束事件，可在BattleManager中触发。为了简化，使用全局每帧检查行动值变化？
                    // 更可靠的方法：在BattleManager的PerformAttack最后触发一个事件。这里采用简化方案：虚影每次行动后调用。
                    // 实际使用时，应在BattleManager中虚影行动结束后调用此方法。为了完整，先提供空实现，需要配合修改BattleManager。
                    // 为了演示，先不做事件，但留下接口。可在BattleManager中每次敌人行动后调用。
                }
                break;
            case "陷仙剑":
                // 陷仙结界：全体友方暴击率提升15%（不可叠加）
                ApplyCritRateBonus();
                break;
            case "绝仙剑":
                // 绝仙结界：全体友方攻击附带灼烧效果（1回合）
                // 灼烧效果在攻击时触发，需要在攻击方法中检查光环存在
                // 这里只做标记，实际攻击逻辑需在ApplyAttackToTarget中判断
                break;
            case "戮仙剑":
                // 戮仙结界：全体友方攻击附带中毒效果（1回合）
                break;
            default:
                Debug.LogWarning($"未知虚影类型: {auraType}");
                break;
        }

        isAuraApplied = true;
    }

    /// <summary>
    /// 施加陷仙剑的暴击光环（全体敌人）
    /// </summary>
    private void ApplyCritRateBonus()
    {
        if (battleManager == null) return;
        foreach (var enemy in battleManager.enemyParty)
        {
            if (enemy != null && !enemy.IsDead())
            {
                // 使用独立字段，避免与玩家临时暴击加成冲突
                enemy.xianXianCritBonus += 0.15f;
            }
        }
        battleManager.AddTurnResultMessage("陷仙剑光环生效：全体敌人暴击率+15%");
    }

    /// <summary>
    /// 移除陷仙剑的暴击光环（虚影死亡时调用）
    /// </summary>
    private void RemoveCritRateBonus()
    {
        if (battleManager == null) return;
        foreach (var enemy in battleManager.enemyParty)
        {
            if (enemy != null)
            {
                enemy.xianXianCritBonus -= 0.15f;
            }
        }
        battleManager.AddTurnResultMessage("陷仙剑光环消失，暴击率恢复");
    }

    /// <summary>
    /// 诛仙剑结界：为全体友方施加护盾
    /// 需在虚影行动结束后调用此方法
    /// </summary>
    public void ApplyZhuXianShield()
    {
        if (battleManager == null) return;
        foreach (var enemy in battleManager.enemyParty)
        {
            if (enemy != null && !enemy.IsDead())
            {
                int shieldAmount = Mathf.RoundToInt(enemy.MaxHP * 0.05f);
                // 护盾不可叠加，直接覆盖（保留原值或取最大）
                enemy.overHealShield = Mathf.Max(enemy.overHealShield, shieldAmount);
            }
        }
        battleManager.AddTurnResultMessage("诛仙剑结界：全体敌人获得护盾");
    }

    /// <summary>
    /// 检查绝仙剑光环（攻击时附带灼烧）
    /// </summary>
    public static bool HasJueXianAura(Character attacker, BattleManager bm)
    {
        // 检查攻击者阵营中是否有存活的绝仙剑虚影
        var jueXian = bm.enemyParty.FirstOrDefault(e => e.characterName == "绝仙剑" && !e.IsDead());
        return jueXian != null;
    }

    /// <summary>
    /// 检查戮仙剑光环（攻击时附带中毒）
    /// </summary>
    public static bool HasLuXianAura(Character attacker, BattleManager bm)
    {
        var luXian = bm.enemyParty.FirstOrDefault(e => e.characterName == "戮仙剑" && !e.IsDead());
        return luXian != null;
    }

    /// <summary>
    /// 诛仙剑行动后调用护盾
    /// </summary>
    public void OnActionPerformed()
    {
        if (auraType == "诛仙剑")
        {
            ApplyZhuXianShield();
        }
    }

    /// <summary>
    /// 立即移除光环效果（不等待 OnDestroy）
    /// </summary>
    public void RemoveAuraImmediately()
    {
        if (!isAuraApplied) return;

        switch (auraType)
        {
            case "陷仙剑":
                RemoveCritRateBonus();
                break;
            default:
                break;
        }
        isAuraApplied = false;
    }

    private void OnDestroy()
    {
        // 如果还没有被立即移除，则在销毁时移除
        if (isAuraApplied)
        {
            RemoveAuraImmediately();
        }
    }
}