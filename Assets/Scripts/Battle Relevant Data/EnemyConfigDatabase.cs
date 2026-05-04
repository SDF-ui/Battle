using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人配置数据库 - 使用 ScriptableObject 将硬编码的敌人配置数据化
/// 在 Project 窗口右键 -> Create -> Battle -> Enemy Config Database 创建实例
/// </summary>
[CreateAssetMenu(fileName = "EnemyConfigDatabase", menuName = "Battle/Enemy Config Database")]
public class EnemyConfigDatabase : ScriptableObject
{
    [Header("普通怪物模板")]
    public List<EnemyTemplate> normalEnemyTemplates = new List<EnemyTemplate>();

    [Header("精英怪物模板")]
    public List<EnemyTemplate> eliteEnemyTemplates = new List<EnemyTemplate>();

    [Header("Boss配置")]
    public BossConfig bossConfig;

    [Header("心魔配置")]
    public HeartDemonConfig heartDemonConfig;

    [Header("特殊楼层配置")]
    public List<SpecialFloorConfig> specialFloorConfigs = new List<SpecialFloorConfig>();

    public SpecialFloorConfig GetSpecialFloorConfig(int floor)
    {
        foreach (var config in specialFloorConfigs)
        {
            if (floor % config.floorMod == config.modValue)
                return config;
        }
        return null;
    }

    public EnemyTemplate GetNormalEnemyTemplate(int floor, int seed = -1)
    {
        if (normalEnemyTemplates.Count == 0)
        {
            Debug.LogError("普通怪物模板列表为空！");
            return null;
        }

        if (floor <= normalEnemyTemplates.Count && floor > 0)
            return normalEnemyTemplates[floor - 1];

        int index;
        if (seed >= 0)
            Random.InitState(seed);
        index = Random.Range(0, normalEnemyTemplates.Count);

        return normalEnemyTemplates[index];
    }
}

[System.Serializable]
public class EnemyTemplate
{
    public string enemyName = "剑魂";
    public string prefabPath = "SwordSoul";
    public string faction = "";
    public int baseLevelOffset = 5;
    public float levelPerFloorMultiplier = 2.0f;
    [Range(1, 100)]
    public int spawnWeight = 10;
}

[System.Serializable]
public class BossConfig
{
    public string bossName = "通天教主";
    public int bossLevel = 80;
    public int allocatedCON = 400;
    public int allocatedINT = 100;
    public int allocatedSTR = 300;
    public int allocatedAGI = 200;
    public string prefabPath = "LinBao";
    public int bossFloorInterval = 30;
}

[System.Serializable]
public class HeartDemonConfig
{
    public string demonName = "心魔";
    public float baseMultiplier = 1.0f;
    public float multiplierPerFloor = 0.02f;
    public string prefabPath = "Player";
    public int heartDemonFloorInterval = 10;
}

[System.Serializable]
public class SpecialFloorConfig
{
    public string groupName = "四神兽";
    public int floorMod = 10;
    public int modValue = 9;
    public List<SpecialEnemyEntry> enemies = new List<SpecialEnemyEntry>();
    public bool isEliteOrBoss = true;
}

[System.Serializable]
public class SpecialEnemyEntry
{
    public string enemyName;
    public string prefabPath;
}
