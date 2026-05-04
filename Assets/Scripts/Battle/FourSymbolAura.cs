using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 四象灵尊光环组件 - 管理青龙、白虎、朱雀、玄武的光环效果
/// 挂载在四神兽预制体上，由 BattleManager 在 SpawnEnemies 时添加
/// </summary>
public class FourSymbolAura : MonoBehaviour
{
    public enum SymbolType { None, Dragon, Tiger, Bird, Turtle }

    [SerializeField] private SymbolType symbolType = SymbolType.None;
    private Character owner;
    private BattleManager battleManager;

    // 所有已注册的四神兽光环实例
    private static List<FourSymbolAura> activeAuras = new List<FourSymbolAura>();

    public void Initialize(Character owner, BattleManager bm, SymbolType type)
    {
        this.owner = owner;
        this.battleManager = bm;
        this.symbolType = type;

        if (!activeAuras.Contains(this))
            activeAuras.Add(this);
    }

    public SymbolType GetSymbolType() => symbolType;
    public bool IsAlive() => owner != null && !owner.IsDead();

    private void OnDestroy()
    {
        if (activeAuras.Contains(this))
            activeAuras.Remove(this);
    }

    // === 静态查询方法 ===

    /// <summary>青龙御风阵光环是否生效（青龙存活）</summary>
    public static bool HasDragonAura(BattleManager bm)
    {
        return activeAuras.Any(a => a.symbolType == SymbolType.Dragon && a.IsAlive() && a.battleManager == bm);
    }

    /// <summary>白虎杀伐阵光环是否生效（白虎存活）</summary>
    public static bool HasTigerAura(BattleManager bm)
    {
        return activeAuras.Any(a => a.symbolType == SymbolType.Tiger && a.IsAlive() && a.battleManager == bm);
    }

    /// <summary>朱雀焚野阵光环是否生效（朱雀存活）</summary>
    public static bool HasBirdAura(BattleManager bm)
    {
        return activeAuras.Any(a => a.symbolType == SymbolType.Bird && a.IsAlive() && a.battleManager == bm);
    }

    /// <summary>玄武镇岳阵光环是否生效（玄武存活）</summary>
    public static bool HasTurtleAura(BattleManager bm)
    {
        return activeAuras.Any(a => a.symbolType == SymbolType.Turtle && a.IsAlive() && a.battleManager == bm);
    }

    /// <summary>获取玄武光环实例（用于分摊伤害）</summary>
    public static FourSymbolAura GetTurtleAura(BattleManager bm)
    {
        return activeAuras.FirstOrDefault(a => a.symbolType == SymbolType.Turtle && a.IsAlive() && a.battleManager == bm);
    }

    /// <summary>获取当前战斗中所有活跃的四神兽光环</summary>
    public static List<FourSymbolAura> GetActiveAuras(BattleManager bm)
    {
        return activeAuras.Where(a => a.IsAlive() && a.battleManager == bm).ToList();
    }

    /// <summary>清理指定战斗管理器中的所有光环引用</summary>
    public static void ClearAuras(BattleManager bm)
    {
        activeAuras.RemoveAll(a => a.battleManager == bm);
    }

    /// <summary>
    /// 获取杀伐阵的攻击力加成比例（已从暴击率加成改为攻击力加成）
    /// </summary>
    public static float GetAttackBonusPercent(BattleManager bm)
    {
        return HasTigerAura(bm) ? 0.15f : 0f;
    }

    /// <summary>
    /// 获取镇岳阵的防御力加成比例
    /// </summary>
    public static float GetDefenseBonusPercent(BattleManager bm)
    {
        return HasTurtleAura(bm) ? 0.30f : 0f;
    }
}
