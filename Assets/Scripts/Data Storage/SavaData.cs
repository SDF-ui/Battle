using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SaveData
{
    // 玩家基础属性
    public int playerLevel = 70;
    public int unallocatedPoints = 0;        // 新增
    public int playerAllocatedCON = 0;
    public int playerAllocatedINT = 0;
    public int playerAllocatedSTR = 0;
    public int playerAllocatedAGI = 0;
    public int playerExtraCON = 0;
    public int playerExtraINT = 0;
    public int playerExtraSTR = 0;
    public int playerExtraAGI = 0;

    public int ginsengFruitUsedCount = 0;
    public string playerFaction = "WuZhuangGuan";

    public int currentFloor = 1;

    // 背包数据
    public List<Item> backpackSlots = new List<Item>();
    public List<Item> equippedItems = new List<Item>();
    public List<Item> artifactSlots = new List<Item>();
}