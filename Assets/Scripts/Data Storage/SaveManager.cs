using UnityEngine;
using System.IO;
using System.Collections.Generic;

using UnityEngine.UI;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    private string savePath;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        savePath = Application.persistentDataPath + "/save.dat";
    }

    void Start()
    {
        if (File.Exists(savePath)) LoadGame();
        else InitializeDefaultData();
    }

    void InitializeDefaultData()
    {
        GameData.playerLevel = 70;  // 可根据需要改为 1
        GameData.unallocatedPoints = (GameData.playerLevel - 1) * 4; // 默认未分配点数（若等级70则276点）
        GameData.playerAllocatedCON = 0;
        GameData.playerAllocatedINT = 0;
        GameData.playerAllocatedSTR = 0;
        GameData.playerAllocatedAGI = 0;
        GameData.playerExtraCON = 0;
        GameData.playerExtraINT = 0;
        GameData.playerExtraSTR = 0;
        GameData.playerExtraAGI = 0;
        GameData.playerFaction = "WuZhuangGuan";
        GameData.currentFloor = 1;
    }

    public void SaveGame()
    {
        GameData.CleanupInvalidItems();

        SaveData data = new SaveData();
        data.playerLevel = GameData.playerLevel;
        data.unallocatedPoints = GameData.unallocatedPoints;   // 新增
        data.playerAllocatedCON = GameData.playerAllocatedCON;
        data.playerAllocatedINT = GameData.playerAllocatedINT;
        data.playerAllocatedSTR = GameData.playerAllocatedSTR;
        data.playerAllocatedAGI = GameData.playerAllocatedAGI;
        data.playerExtraCON = GameData.playerExtraCON;
        data.playerExtraINT = GameData.playerExtraINT;
        data.playerExtraSTR = GameData.playerExtraSTR;
        data.playerExtraAGI = GameData.playerExtraAGI;
        data.playerFaction = GameData.playerFaction;
        data.currentFloor = GameData.currentFloor;
        data.ginsengFruitUsedCount = GameData.ginsengFruitUsedCount;

        for (int i = 0; i < GameData.backpackSlots.Length; i++) data.backpackSlots.Add(GameData.backpackSlots[i]);
        for (int i = 0; i < GameData.equippedItems.Length; i++) data.equippedItems.Add(GameData.equippedItems[i]);
        for (int i = 0; i < GameData.artifactSlots.Length; i++) data.artifactSlots.Add(GameData.artifactSlots[i]);

        File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
        Debug.Log("游戏已保存");
    }

    public void LoadGame()
    {
        if (!File.Exists(savePath)) return;
        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(savePath));

        GameData.playerLevel = data.playerLevel;
        GameData.unallocatedPoints = data.unallocatedPoints;   // 新增
        GameData.playerAllocatedCON = data.playerAllocatedCON;
        GameData.playerAllocatedINT = data.playerAllocatedINT;
        GameData.playerAllocatedSTR = data.playerAllocatedSTR;
        GameData.playerAllocatedAGI = data.playerAllocatedAGI;
        GameData.playerExtraCON = data.playerExtraCON;
        GameData.playerExtraINT = data.playerExtraINT;
        GameData.playerExtraSTR = data.playerExtraSTR;
        GameData.playerExtraAGI = data.playerExtraAGI;
        GameData.playerFaction = data.playerFaction;
        GameData.currentFloor = data.currentFloor;
        GameData.ginsengFruitUsedCount = data.ginsengFruitUsedCount;

        for (int i = 0; i < data.backpackSlots.Count && i < GameData.backpackSlots.Length; i++) GameData.backpackSlots[i] = data.backpackSlots[i];
        for (int i = 0; i < data.equippedItems.Count && i < GameData.equippedItems.Length; i++) GameData.equippedItems[i] = data.equippedItems[i];
        for (int i = 0; i < data.artifactSlots.Count && i < GameData.artifactSlots.Length; i++) GameData.artifactSlots[i] = data.artifactSlots[i];
        Debug.Log("存档加载成功");

        GameData.CleanupInvalidItems();
        GameData.ValidateAllocatedPoints(); // 加载后校验并修正超限
        if (BackpackManager.Instance != null)
            BackpackManager.Instance.RefreshUIFromGameData();
    }

    public void ManualSave() => SaveGame();
    public void ManualLoad() => LoadGame();
}