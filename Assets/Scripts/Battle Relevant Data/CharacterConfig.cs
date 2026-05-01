using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterConfig
{
    public string characterName;           // 角色名称
    public string faction;                  // 门派
    public int level = 1;                   // 等级
    public int allocatedCON = 0;            // 分配体质
    public int allocatedINT = 0;            // 分配灵力
    public int allocatedSTR = 0;            // 分配力量
    public int allocatedAGI = 0;            // 分配敏捷
    public int extraCON = 0;                // 道具加成体质
    public int extraINT = 0;
    public int extraSTR = 0;
    public int extraAGI = 0;
    public bool isEliteOrBoss;

    // 装备列表（直接存储Item对象，或存储ID然后从数据库加载，这里直接存储Item以简化）
    public List<Item> equippedEquipments = new List<Item>();
    public List<Item> equippedArtifacts = new List<Item>();

    // 预制体路径（用于敌人，玩家使用固定预制体）
    public string prefabPath = "Characters/Enemy_Default";
}