using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
public class BackpackManager : MonoBehaviour
{
    public static BackpackManager Instance;

    [Header("UI References")]
    public Transform contentParent;
    public GameObject itemSlotPrefab;
    public TMP_InputField searchInputField;
    public ScrollRect scrollRect;

    [Header("Data")]
    private const int CAPACITY = 120;
    private Item[] slots = new Item[CAPACITY];
    private List<GameObject> slotObjects = new List<GameObject>();
    private string lastSearchText = "";

    [Header("装备槽")]
    public Item[] equippedItems = new Item[7];

    [Header("法宝槽")]
    public Item[] artifactSlots = new Item[3];

    [Header("按钮")]
    public Button patchButton;  // 在 Inspector 中拖拽赋值

    //public Button  Patch;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        CreateSlots();
        Debug.Log($"创建格子数量：{slotObjects.Count}");

        LoadFromGameData(); // 从 GameData 加载现有数据
        Debug.Log($"从GameData加载后背包物品数：{slots.Count(s => s != null)}");

        // 首次启动时，GameData 为空，生成测试物品并保存
        if (IsGameDataEmpty())
        {
            InitializeItems(); // 生成测试物品（内部调用 AddItem，会触发保存）
        }

        if (patchButton != null)
            patchButton.onClick.AddListener(OnPatchClicked);
        UpdatePatchButtonText(); // 初始化按钮文本

        if (searchInputField != null)
            searchInputField.onValueChanged.AddListener(OnSearchTextChanged);

        RectTransform contentRect = contentParent as RectTransform;
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(0, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.anchoredPosition = Vector2.zero;
        }

        RefreshUI(); // 确保界面显示最新数据
    }

    // 辅助方法供 SaveManager 使用
    public int GetSlotsLength() => slots.Length;
    public Item GetSlot(int index) => slots[index];
    public void SetSlot(int index, Item item) => slots[index] = item;

    // 从 GameData 加载数据到本地
    private void LoadFromGameData()
    {
        GameData.CopyToBackpack(slots);
        // 确保所有装备都有原始备份
        foreach (var item in slots)
        {
            if (item != null && item.type == ItemType.Equipment)
                item.BackupOriginalBasicAttributes();
        }
        GameData.CopyToEquipped(equippedItems);
        GameData.CopyToArtifact(artifactSlots);
    }

    // 将本地数据保存到 GameData
    public void SaveToGameData()
    {
        GameData.CopyFromBackpack(slots);
        GameData.CopyFromEquipped(equippedItems);
        GameData.CopyFromArtifact(artifactSlots);
    }

    // 检查 GameData 是否为空（所有槽位均为 null）
    private bool IsGameDataEmpty()
    {
        for (int i = 0; i < GameData.backpackSlots.Length; i++)
            if (GameData.backpackSlots[i] != null) return false;
        for (int i = 0; i < GameData.equippedItems.Length; i++)
            if (GameData.equippedItems[i] != null) return false;
        for (int i = 0; i < GameData.artifactSlots.Length; i++)
            if (GameData.artifactSlots[i] != null) return false;
        return true;
    }

    // 创建格子 UI
    void CreateSlots()
    {
        foreach (var slot in slotObjects)
            Destroy(slot);
        slotObjects.Clear();

        for (int i = 0; i < CAPACITY; i++)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, contentParent);
            ItemSlot slotComponent = slotObj.GetComponent<ItemSlot>();
            if (slotComponent == null)
            {
                Debug.LogError("预制体缺少 ItemSlot 组件！");
                continue;
            }
            slotComponent.SetItem(null);
            slotObjects.Add(slotObj);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
    }

    // 初始化测试物品（仅在首次运行时调用）
    void InitializeItems()
    {
        Debug.Log("InitializeItems 被调用");

        int nextId = 1;

        // ---------- 装备 ----------
        Item xuanyuanSword = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Weapon,
            itemName = "  轩辕剑",
            description = "众神采首山之铜为黄帝所铸，后传与夏禹，是一把圣道之剑",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Sword",
            icon = Resources.Load<Sprite>("EquipmentIcon/Sword"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "连击", valueText = "+200  ", type = AttributeType.ComboRate, value = 200 },
                new ItemAttribute { attributeName = "暴击", valueText = "+200  ", type = AttributeType.CritRate, value = 200 },
                new ItemAttribute { attributeName = "晕击", valueText = "+200  ", type = AttributeType.StunRate, value = 200 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "攻击", valueText = "+5800", type = AttributeType.Attack, value = 5800 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 10,
            forgingLevelText = "10"
        };
        xuanyuanSword.BackupOriginalBasicAttributes();
        AddItem(xuanyuanSword);

        Item shenSword = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Weapon,
            itemName = "  神之剑",
            description = "神界守护神的兵器，具有极强的灵性，妖魔闻之丧胆",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Sword",
            icon = Resources.Load<Sprite>("EquipmentIcon/Sword"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "攻击", valueText = "+800  ", type = AttributeType.Attack, value = 800 },
                new ItemAttribute { attributeName = "攻击", valueText = "+800  ", type = AttributeType.Attack, value = 800 },
                new ItemAttribute { attributeName = "攻击", valueText = "+800  ", type = AttributeType.Attack, value = 800 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "攻击", valueText = "+4896", type = AttributeType.Attack, value = 4896 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 10,
            forgingLevelText = "10"
        };
        shenSword.BackupOriginalBasicAttributes();
        AddItem(shenSword);

        Item shenArmor = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Armor,
            itemName = "  神之甲",
            description = "神界守护神的铠甲，固若金汤，堪称生命永恒的象征",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Armor",
            icon = Resources.Load<Sprite>("EquipmentIcon/Armor"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "防御", valueText = "+500  ", type = AttributeType.Defense, value = 500 },
                new ItemAttribute { attributeName = "防御", valueText = "+500  ", type = AttributeType.Defense, value = 500 },
                new ItemAttribute { attributeName = "防御", valueText = "+500  ", type = AttributeType.Defense, value = 500 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "防御", valueText = "+960", type = AttributeType.Defense, value = 960 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 10,
            forgingLevelText = "10"
        };
        shenArmor.BackupOriginalBasicAttributes();
        AddItem(shenArmor);

        Item shenNecklace = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Necklace,
            itemName = "  神之项链",
            description = "呈七彩，能发出耀眼光芒，为神界吉祥之物",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Necklace",
            icon = Resources.Load<Sprite>("EquipmentIcon/Necklace"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "暴击", valueText = "+100  ", type = AttributeType.CritRate, value = 100 },
                new ItemAttribute { attributeName = "暴击", valueText = "+100  ", type = AttributeType.CritRate, value = 100 },
                new ItemAttribute { attributeName = "暴击", valueText = "+100  ", type = AttributeType.CritRate, value = 100 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "暴击", valueText = "+168", type = AttributeType.CritRate, value = 168 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 10,
            forgingLevelText = "10"
        };
        shenNecklace.BackupOriginalBasicAttributes();
        AddItem(shenNecklace);

        Item shenHelmet = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Helmet,
            itemName = "  神之盔",
            description = "神界守护神的头盔，可令人迅速集中斗气，发出致命一击",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Helmet",
            icon = Resources.Load<Sprite>("EquipmentIcon/Helmet"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "生命", valueText = "+800  ", type = AttributeType.Health, value = 800 },
                new ItemAttribute { attributeName = "生命", valueText = "+800  ", type = AttributeType.Health, value = 800 },
                new ItemAttribute { attributeName = "生命", valueText = "+800  ", type = AttributeType.Health, value = 800 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "内力", valueText = "+480", type = AttributeType.Mana, value = 480 },
                new ItemAttribute { attributeName = "防御", valueText = "+144", type = AttributeType.Defense, value = 144 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 10,
            forgingLevelText = "10"
        };
        shenHelmet.BackupOriginalBasicAttributes();
        AddItem(shenHelmet);

        Item shenBracelet = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Bracelet,
            itemName = "  神之手镯",
            description = "神界守护神的手镯，可发出雷鸣般巨响，威慑群雄，再好不过",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Bracelet",
            icon = Resources.Load<Sprite>("EquipmentIcon/Bracelet"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "命中", valueText = "+100  ", type = AttributeType.HitRate, value = 100 },
                new ItemAttribute { attributeName = "命中", valueText = "+100  ", type = AttributeType.HitRate, value = 100 },
                new ItemAttribute { attributeName = "命中", valueText = "+100  ", type = AttributeType.HitRate, value = 100 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "命中", valueText = "+360", type = AttributeType.HitRate, value = 360 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 10,
            forgingLevelText = "10"
        };
        shenBracelet.BackupOriginalBasicAttributes();
        AddItem(shenBracelet);

        Item shenBelt = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Belt,
            itemName = "  神之腰带",
            description = "神界七大神器之一，相传为上古神龙幻化而成，佩戴之人可得神龙相助",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Belt",
            icon = Resources.Load<Sprite>("EquipmentIcon/Belt"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "体质", valueText = "+100  ", type = AttributeType.Constitution, value = 100 },
                new ItemAttribute { attributeName = "体质", valueText = "+100  ", type = AttributeType.Constitution, value = 100 },
                new ItemAttribute { attributeName = "体质", valueText = "+100  ", type = AttributeType.Constitution, value = 100 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "生命", valueText = "+1680", type = AttributeType.Health, value = 1680 },
                new ItemAttribute { attributeName = "防御", valueText = "+108", type = AttributeType.Defense, value = 108 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 10,
            forgingLevelText = "10"
        };
        shenBelt.BackupOriginalBasicAttributes();
        AddItem(shenBelt);

        Item shenBoots = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Boots,
            itemName = "  神之靴",
            description = "用上古神兽的毛皮制作而成，穿上此靴，可力挽狂澜，独当一面",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Boots",
            icon = Resources.Load<Sprite>("EquipmentIcon/Boots"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "速度", valueText = "+100  ", type = AttributeType.Speed, value = 100 },
                new ItemAttribute { attributeName = "速度", valueText = "+100  ", type = AttributeType.Speed, value = 100 },
                new ItemAttribute { attributeName = "速度", valueText = "+100  ", type = AttributeType.Speed, value = 100 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "闪避", valueText = "+120", type = AttributeType.EvasionRate, value = 120 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 10,
            forgingLevelText = "10"
        };
        shenBoots.BackupOriginalBasicAttributes();
        AddItem(shenBoots);

        // ---------- 法宝 ----------
        Item fenTianZhu = new Item
        {
            id = nextId++,
            itemName = "焚天珠",
            description = "攻击时有20%概率触发爆炸，对目标及其周围所有敌人造成50%的溅射伤害（不可连续触发）",
            type = ItemType.Artifact,
            count = 1,
            iconPath = "ArtifactIcon/FenTianZhu",
            icon = Resources.Load<Sprite>("ArtifactIcon/FenTianZhu"),
            artifactEffect = ArtifactEffect.FenTianZhu,
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "攻击", valueText = "+500", type = AttributeType.Attack, value = 500 },
            },
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "力量", valueText = "+50", type = AttributeType.Strength, value = 50 },
                new ItemAttribute { attributeName = "攻击", valueText = "+400", type = AttributeType.Attack, value = 400 },
                new ItemAttribute { attributeName = "暴击", valueText = "+40", type = AttributeType.CritRate, value = 40 },
            },
            requireLevel = 1,
            requireLevelText = "1",
            forgingLevel = 0,
            forgingLevelText = "0"
        };
        fenTianZhu.BackupOriginalBasicAttributes();
        AddItem(fenTianZhu);

        Item leiShenChui = new Item
        {
            id = nextId++,
            itemName = "雷神锤",
            description = "攻击时有10%概率触发连锁闪电，对额外2个随机敌人造成50%伤害",
            type = ItemType.Artifact,
            count = 1,
            iconPath = "ArtifactIcon/LeiShenChui",
            icon = Resources.Load<Sprite>("ArtifactIcon/LeiShenChui"),
            artifactEffect = ArtifactEffect.LeiShenChui,
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "攻击", valueText = "+500", type = AttributeType.Attack, value = 500 },
            },
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "力量", valueText = "+50", type = AttributeType.Strength, value = 50 },
                new ItemAttribute { attributeName = "命中", valueText = "+40", type = AttributeType.HitRate, value = 40 },
                new ItemAttribute { attributeName = "暴击", valueText = "+40", type = AttributeType.CritRate, value = 40 },
            },
            requireLevel = 1,
            requireLevelText = "1",
            forgingLevel = 0,
            forgingLevelText = "0"
        };
        leiShenChui.BackupOriginalBasicAttributes();
        AddItem(leiShenChui);

        Item poJunFu = new Item
        {
            id = nextId++,
            itemName = "破军斧",
            description = "对生命值高于70%的敌人，造成的伤害提高30%",
            type = ItemType.Artifact,
            count = 1,
            iconPath = "ArtifactIcon/PoJunFu",
            icon = Resources.Load<Sprite>("ArtifactIcon/PoJunFu"),
            artifactEffect = ArtifactEffect.PoJunFu,
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "攻击", valueText = "+500", type = AttributeType.Attack, value = 500 },
            },
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "力量", valueText = "+50", type = AttributeType.Strength, value = 50 },
                new ItemAttribute { attributeName = "攻击", valueText = "+400", type = AttributeType.Attack, value = 400 },
                new ItemAttribute { attributeName = "暴击", valueText = "+40", type = AttributeType.CritRate, value = 40 },
            },
            requireLevel = 1,
            requireLevelText = "1",
            forgingLevel = 0,
            forgingLevelText = "0"
        };
        poJunFu.BackupOriginalBasicAttributes();
        AddItem(poJunFu);

        Item xuanBingJia = new Item
        {
            id = nextId++,
            itemName = "玄冰甲",
            description = "受到攻击时，有20%概率触发冰盾，使本次伤害降低50%，并对攻击者造成50%的反伤",
            type = ItemType.Artifact,
            count = 1,
            iconPath = "ArtifactIcon/XuanBingJia",
            icon = Resources.Load<Sprite>("ArtifactIcon/XuanBingJia"),
            artifactEffect = ArtifactEffect.XuanBingJia,
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "防御", valueText = "+400", type = AttributeType.Defense, value = 400 },
            },
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "灵力", valueText = "+50", type = AttributeType.Spirit, value = 50 },
                new ItemAttribute { attributeName = "内力", valueText = "+50", type = AttributeType.Mana, value = 50 },
                new ItemAttribute { attributeName = "体质", valueText = "+50", type = AttributeType.Constitution, value = 50 },
            },
            requireLevel = 1,
            requireLevelText = "1",
            forgingLevel = 0,
            forgingLevelText = "0"
        };
        xuanBingJia.BackupOriginalBasicAttributes();
        AddItem(xuanBingJia);

        Item jinGangSan = new Item
        {
            id = nextId++,
            itemName = "金刚伞",
            description = "每回合开始时，获得一个可吸收20%最大生命值的护盾，持续1回合",
            type = ItemType.Artifact,
            count = 1,
            iconPath = "ArtifactIcon/JinGangSan",
            icon = Resources.Load<Sprite>("ArtifactIcon/JinGangSan"),
            artifactEffect = ArtifactEffect.JinGangSan,
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "防御", valueText = "+400", type = AttributeType.Defense, value = 400 },
            },
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "体质", valueText = "+50", type = AttributeType.Constitution, value = 50 },
                new ItemAttribute { attributeName = "生命", valueText = "+400", type = AttributeType.Health, value = 400 },
                new ItemAttribute { attributeName = "闪避", valueText = "+40", type = AttributeType.EvasionRate, value = 40 },
            },
            requireLevel = 1,
            requireLevelText = "1",
            forgingLevel = 0,
            forgingLevelText = "0"
        };
        jinGangSan.BackupOriginalBasicAttributes();
        AddItem(jinGangSan);

        Item huXinJing = new Item
        {
            id = nextId++,
            itemName = "护心镜",
            description = "当生命值低于30%时，立即恢复30%生命值（每场战斗仅触发一次）",
            type = ItemType.Artifact,
            count = 1,
            iconPath = "ArtifactIcon/HuXinJing",
            icon = Resources.Load<Sprite>("ArtifactIcon/HuXinJing"),
            artifactEffect = ArtifactEffect.HuXinJing,
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "生命", valueText = "+500", type = AttributeType.Health, value = 500 },
            },
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "体质", valueText = "+50", type = AttributeType.Constitution, value = 50 },
                new ItemAttribute { attributeName = "生命", valueText = "+400", type = AttributeType.Health, value = 400 },
                new ItemAttribute { attributeName = "防御", valueText = "+300", type = AttributeType.Defense, value = 300 },
            },
            requireLevel = 1,
            requireLevelText = "1",
            forgingLevel = 0,
            forgingLevelText = "0"
        };
        huXinJing.BackupOriginalBasicAttributes();
        AddItem(huXinJing);

        Item fengLeiYi = new Item
        {
            id = nextId++,
            itemName = "风雷翼",
            description = "行动后有10%概率获得额外一次行动机会（不可连续触发）",
            type = ItemType.Artifact,
            count = 1,
            iconPath = "ArtifactIcon/FengLeiYi",
            icon = Resources.Load<Sprite>("ArtifactIcon/FengLeiYi"),
            artifactEffect = ArtifactEffect.FengLeiYi,
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "速度", valueText = "+60", type = AttributeType.Speed, value = 60 },
            },
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "敏捷", valueText = "+50", type = AttributeType.Agility, value = 50 },
                new ItemAttribute { attributeName = "速度", valueText = "+40", type = AttributeType.Speed, value = 40 },
                new ItemAttribute { attributeName = "闪避", valueText = "+40", type = AttributeType.EvasionRate, value = 40 },
            },
            requireLevel = 1,
            requireLevelText = "1",
            forgingLevel = 0,
            forgingLevelText = "0"
        };
        fengLeiYi.BackupOriginalBasicAttributes();
        AddItem(fengLeiYi);

        Item lingFengPei = new Item
        {
            id = nextId++,
            itemName = "灵风佩",
            description = "每次击杀敌人后，自身速度提高10%，持续至战斗结束，最多叠加5层",
            type = ItemType.Artifact,
            count = 1,
            iconPath = "ArtifactIcon/LingFengPei",
            icon = Resources.Load<Sprite>("ArtifactIcon/LingFengPei"),
            artifactEffect = ArtifactEffect.LingFengPei,
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "速度", valueText = "+60", type = AttributeType.Speed, value = 60 },
            },
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "敏捷", valueText = "+50", type = AttributeType.Agility, value = 50 },
                new ItemAttribute { attributeName = "速度", valueText = "+40", type = AttributeType.Speed, value = 40 },
                new ItemAttribute { attributeName = "闪避", valueText = "+40", type = AttributeType.EvasionRate, value = 40 },
            },
            requireLevel = 1,
            requireLevelText = "1",
            forgingLevel = 0,
            forgingLevelText = "0"
        };
        lingFengPei.BackupOriginalBasicAttributes();
        AddItem(lingFengPei);

        Item lunHuiJing = new Item
        {
            id = nextId++,
            itemName = "轮回镜",
            description = "每次攻击命中后，恢复造成伤害20%的生命值（吸血）",
            type = ItemType.Artifact,
            count = 1,
            iconPath = "ArtifactIcon/LunHuiJing",
            icon = Resources.Load<Sprite>("ArtifactIcon/LunHuiJing"),
            artifactEffect = ArtifactEffect.LunHuiJing,
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "生命", valueText = "+500", type = AttributeType.Health, value = 500 },
            },
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "灵力", valueText = "+50", type = AttributeType.Spirit, value = 50 },
                new ItemAttribute { attributeName = "内力", valueText = "+50", type = AttributeType.Mana, value = 50 },
                new ItemAttribute { attributeName = "体质", valueText = "+50", type = AttributeType.Constitution, value = 50 },
            },
            requireLevel = 1,
            requireLevelText = "1",
            forgingLevel = 0,
            forgingLevelText = "0"
        };
        lunHuiJing.BackupOriginalBasicAttributes();
        AddItem(lunHuiJing);

        Item zhenHunFan = new Item
        {
            id = nextId++,
            itemName = "镇魂幡",
            description = "攻击时有15%概率使目标眩晕1回合（对精英/Boss效果减半，不可连续触发）",
            type = ItemType.Artifact,
            count = 1,
            iconPath = "ArtifactIcon/ZhenHunFan",
            icon = Resources.Load<Sprite>("ArtifactIcon/ZhenHunFan"),
            artifactEffect = ArtifactEffect.ZhenHunFan,
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "命中", valueText = "+60", type = AttributeType.HitRate, value = 60 },
            },
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "力量", valueText = "+50", type = AttributeType.Strength, value = 50 },
                new ItemAttribute { attributeName = "攻击", valueText = "+400", type = AttributeType.Attack, value = 400 },
                new ItemAttribute { attributeName = "暴击", valueText = "+40", type = AttributeType.CritRate, value = 40 },
            },
            requireLevel = 1,
            requireLevelText = "1",
            forgingLevel = 0,
            forgingLevelText = "0"
        };
        zhenHunFan.BackupOriginalBasicAttributes();
        AddItem(zhenHunFan);

        Item renShenGuo = new Item
        {
            id = nextId++,
            itemName = "人参果",
            description = "西牛贺洲五庄观镇元子仙根所结灵果，历万年方得成熟，食之益寿增功（使用后额外获得 1 点属性点，最多可使用 120 个）",
            type = ItemType.Consumable,
            count = 150,
            iconPath = "Basic/Attribute Fruit",
            icon = Resources.Load<Sprite>("Basic/Attribute Fruit"),
            basicAttributes = new List<ItemAttribute>(),
            extraAttributes = new List<ItemAttribute>(),
            requireLevel = 1,
            requireLevelText = "1",
            forgingLevel = 0,
            forgingLevelText = "0"
        };
        AddItem(renShenGuo);

        // 强化石
        Item enhanceStone = new Item
        {
            id = nextId++,
            itemName = "强化石",
            description = "天地灵气凝结的晶石，蕴含精纯能量，可用于锻造装备，提升基础属性",
            type = ItemType.Material,
            count = 1000,
            iconPath = "Material/EnhanceStone",
            requireLevel = 1,
            forgingLevel = 0
        };
        AddItem(enhanceStone);

        // 幸运符
        Item luckStone = new Item
        {
            id = nextId++,
            itemName = "幸运符",
            description = "绘有祥云瑞兽的符咒，能带来好运，提升锻造成功率",
            type = ItemType.Material,
            count = 300,
            iconPath = "Material/LuckStone",
            requireLevel = 1
        };
        AddItem(luckStone);

        // 高级幸运符
        Item advancedLuckStone = new Item
        {
            id = nextId++,
            itemName = "高级幸运符",
            description = "以朱砂灵血书写的天师符箓，灵光流转，大幅提升锻造成功率",
            type = ItemType.Material,
            count = 100,
            iconPath = "Material/AdvancedLuckStone",
            requireLevel = 1
        };
        AddItem(advancedLuckStone);

        // 防碎石
        Item protectStone = new Item
        {
            id = nextId++,
            itemName = "防碎石",
            description = "上古神石碎片，坚硬无比，可抵御锻造失败时的破碎之力，保护装备不毁",
            type = ItemType.Material,
            count = 500,
            iconPath = "Material/ProtectStone",
            requireLevel = 1
        };
        AddItem(protectStone);

        Item shenSword_1 = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Weapon,
            itemName = "  神之剑",
            description = "神界守护神的兵器，具有极强的灵性，妖魔闻之丧胆",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Sword",
            icon = Resources.Load<Sprite>("EquipmentIcon/Sword"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "攻击", valueText = "+800  ", type = AttributeType.Attack, value = 800 },
                new ItemAttribute { attributeName = "攻击", valueText = "+800  ", type = AttributeType.Attack, value = 800 },
                new ItemAttribute { attributeName = "攻击", valueText = "+800  ", type = AttributeType.Attack, value = 800 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "攻击", valueText = "+2040", type = AttributeType.Attack, value = 2040 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 1,
            forgingLevelText = "1"
        };
        AddItem(shenSword_1);

        Item shenArmor_1 = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Armor,
            itemName = "  神之甲",
            description = "神界守护神的铠甲，固若金汤，堪称生命永恒的象征",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Armor",
            icon = Resources.Load<Sprite>("EquipmentIcon/Armor"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "防御", valueText = "+500  ", type = AttributeType.Defense, value = 500 },
                new ItemAttribute { attributeName = "防御", valueText = "+500  ", type = AttributeType.Defense, value = 500 },
                new ItemAttribute { attributeName = "防御", valueText = "+500  ", type = AttributeType.Defense, value = 500 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "防御", valueText = "+400", type = AttributeType.Defense, value = 400 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 1,
            forgingLevelText = "1"
        };
        shenArmor_1.BackupOriginalBasicAttributes();
        AddItem(shenArmor_1);

        Item shenNecklace_1 = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Necklace,
            itemName = "  神之项链",
            description = "呈七彩，能发出耀眼光芒，为神界吉祥之物",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Necklace",
            icon = Resources.Load<Sprite>("EquipmentIcon/Necklace"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "暴击", valueText = "+100  ", type = AttributeType.CritRate, value = 100 },
                new ItemAttribute { attributeName = "暴击", valueText = "+100  ", type = AttributeType.CritRate, value = 100 },
                new ItemAttribute { attributeName = "暴击", valueText = "+100  ", type = AttributeType.CritRate, value = 100 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "暴击", valueText = "+70", type = AttributeType.CritRate, value = 70 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 1,
            forgingLevelText = "1"
        };
        shenNecklace_1.BackupOriginalBasicAttributes();
        AddItem(shenNecklace_1);

        Item shenHelmet_1 = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Helmet,
            itemName = "  神之盔",
            description = "神界守护神的头盔，可令人迅速集中斗气，发出致命一击",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Helmet",
            icon = Resources.Load<Sprite>("EquipmentIcon/Helmet"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "生命", valueText = "+800  ", type = AttributeType.Health, value = 800 },
                new ItemAttribute { attributeName = "生命", valueText = "+800  ", type = AttributeType.Health, value = 800 },
                new ItemAttribute { attributeName = "生命", valueText = "+800  ", type = AttributeType.Health, value = 800 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "内力", valueText = "+200", type = AttributeType.Mana, value = 200 },
                new ItemAttribute { attributeName = "防御", valueText = "+60", type = AttributeType.Defense, value = 60 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 1,
            forgingLevelText = "1"
        };
        shenHelmet_1.BackupOriginalBasicAttributes();
        AddItem(shenHelmet_1);

        Item shenBracelet_1 = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Bracelet,
            itemName = "  神之手镯",
            description = "神界守护神的手镯，可发出雷鸣般巨响，威慑群雄，再好不过",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Bracelet",
            icon = Resources.Load<Sprite>("EquipmentIcon/Bracelet"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "命中", valueText = "+100  ", type = AttributeType.HitRate, value = 100 },
                new ItemAttribute { attributeName = "命中", valueText = "+100  ", type = AttributeType.HitRate, value = 100 },
                new ItemAttribute { attributeName = "命中", valueText = "+100  ", type = AttributeType.HitRate, value = 100 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "命中", valueText = "+150", type = AttributeType.HitRate, value = 150 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 1,
            forgingLevelText = "1"
        };
        shenBracelet_1.BackupOriginalBasicAttributes();
        AddItem(shenBracelet_1);

        Item shenBelt_1 = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Belt,
            itemName = "  神之腰带",
            description = "神界七大神器之一，相传为上古神龙幻化而成，佩戴之人可得神龙相助",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Belt",
            icon = Resources.Load<Sprite>("EquipmentIcon/Belt"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "体质", valueText = "+100  ", type = AttributeType.Constitution, value = 100 },
                new ItemAttribute { attributeName = "体质", valueText = "+100  ", type = AttributeType.Constitution, value = 100 },
                new ItemAttribute { attributeName = "体质", valueText = "+100  ", type = AttributeType.Constitution, value = 100 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "生命", valueText = "+700", type = AttributeType.Health, value = 700 },
                new ItemAttribute { attributeName = "防御", valueText = "+45", type = AttributeType.Defense, value = 45 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 1,
            forgingLevelText = "1"
        };
        shenBelt_1.BackupOriginalBasicAttributes();
        AddItem(shenBelt_1);

        Item shenBoots_1 = new Item
        {
            id = nextId++,
            equipSlot = EquipSlot.Boots,
            itemName = "  神之靴",
            description = "用上古神兽的毛皮制作而成，穿上此靴，可力挽狂澜，独当一面",
            type = ItemType.Equipment,
            count = 1,
            iconPath = "EquipmentIcon/Boots",
            icon = Resources.Load<Sprite>("EquipmentIcon/Boots"),
            extraAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "速度", valueText = "+100  ", type = AttributeType.Speed, value = 100 },
                new ItemAttribute { attributeName = "速度", valueText = "+100  ", type = AttributeType.Speed, value = 100 },
                new ItemAttribute { attributeName = "速度", valueText = "+100  ", type = AttributeType.Speed, value = 100 },
            },
            basicAttributes = new List<ItemAttribute>
            {
                new ItemAttribute { attributeName = "闪避", valueText = "+50", type = AttributeType.EvasionRate, value = 50 },
            },
            requireLevel = 30,
            requireLevelText = "30",
            forgingLevel = 1,
            forgingLevelText = "1"
        };
        shenBoots_1.BackupOriginalBasicAttributes();
        AddItem(shenBoots_1);


    }

    // 搜索文本变更
    void OnSearchTextChanged(string searchText)
    {
        lastSearchText = searchText;
        RefreshUI();
    }

    // 刷新 UI（根据搜索条件过滤）
    public void RefreshUI()
    {
        if (string.IsNullOrEmpty(lastSearchText))
        {
            for (int i = 0; i < CAPACITY; i++)
                slotObjects[i].GetComponent<ItemSlot>().SetItem(slots[i]);
        }
        else
        {
            var matchingItems = slots.Where(item => item != null && (item.itemName.Contains(lastSearchText) || item.description.Contains(lastSearchText)))
                                     .OrderBy(item => item.itemName).ToList();
            int index = 0;
            for (; index < matchingItems.Count && index < CAPACITY; index++)
                slotObjects[index].GetComponent<ItemSlot>().SetItem(matchingItems[index]);
            for (; index < CAPACITY; index++)
                slotObjects[index].GetComponent<ItemSlot>().SetItem(null);
        }
    }

    // 添加物品（返回是否成功）
    public bool AddItem(Item newItem)
    {
        if (newItem == null || string.IsNullOrEmpty(newItem.itemName))
        {
            Debug.LogWarning("尝试添加无效物品，已拒绝");
            return false;
        }

        for (int i = 0; i < CAPACITY; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = newItem;
                RefreshUI();
                SaveToGameData();
                if (SaveManager.Instance != null)
                    SaveManager.Instance.SaveGame();
                return true;
            }
        }
        Debug.LogWarning("背包已满，无法添加物品");
        return false;
    }

    // 移除指定槽位的物品
    public void RemoveItemAt(int index)
    {
        if (index >= 0 && index < CAPACITY && slots[index] != null)
        {
            slots[index] = null;
            RefreshUI();
            SaveToGameData();
            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();
        }
    }

    // 获取指定槽位的物品
    public Item GetItemAt(int index)
    {
        if (index >= 0 && index < CAPACITY)
            return slots[index];
        return null;
    }

    // 获取装备槽物品
    public Item GetEquippedItem(EquipSlot slot)
    {
        return equippedItems[(int)slot];
    }

    // 穿戴装备
    public bool EquipItem(Item item, EquipSlot slot)
    {
        if (item == null) return false;
        // 如果目标槽已有装备，先卸下
        if (equippedItems[(int)slot] != null)
            UnequipItem(slot);
        int index = System.Array.IndexOf(slots, item);
        if (index >= 0)
        {
            slots[index] = null;
            equippedItems[(int)slot] = item;
            RefreshUI();
            SaveToGameData();
            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();

            FantasyStatusPanel panel = FindObjectOfType<FantasyStatusPanel>();
            if (panel != null)
            {
                panel.RefreshStats();
                panel.RefreshEquipment(); // 刷新装备槽 UI
            }
            UpdatePatchButtonText(); // 新增
            return true;
        }

        return false;
    }

    // 卸下装备
    public bool UnequipItem(EquipSlot slot)
    {
        Item item = equippedItems[(int)slot];
        if (item == null) return false;

        // 打印所有非空格子的索引和名称
        for (int i = 0; i < CAPACITY; i++)
        {
            if (slots[i] != null)
                Debug.Log($"背包索引 {i}: {slots[i].itemName}");
        }

        for (int i = 0; i < CAPACITY; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                equippedItems[(int)slot] = null;
                RefreshUI();
                SaveToGameData();
                if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();

                FantasyStatusPanel panel = FindObjectOfType<FantasyStatusPanel>();
                if (panel != null)
                {
                    panel.RefreshStats();
                    panel.RefreshEquipment();
                }
                UpdatePatchButtonText(); // 新增
                return true;
            }
        }
        Debug.LogError("背包已满，无法卸下装备");
        return false;
    }

    // 获取法宝槽物品
    public Item GetArtifactSlot(int index)
    {
        if (index >= 0 && index < artifactSlots.Length)
            return artifactSlots[index];
        return null;
    }

    // 装备法宝
    public bool EquipArtifact(Item newItem)
    {
        if (newItem == null || newItem.type != ItemType.Artifact) return false;
        for (int i = 0; i < artifactSlots.Length; i++)
        {
            if (artifactSlots[i] == null)
            {
                int index = System.Array.IndexOf(slots, newItem);
                if (index >= 0)
                {
                    slots[index] = null;
                    artifactSlots[i] = newItem;
                    RefreshUI();
                    SaveToGameData();
                    if (SaveManager.Instance != null)
                        SaveManager.Instance.SaveGame();

                    FantasyStatusPanel panel = FindObjectOfType<FantasyStatusPanel>();
                    if (panel != null)
                    {
                        panel.RefreshStats();
                        panel.RefreshArtifacts(); // 刷新法宝槽
                    }
                    UpdatePatchButtonText(); // 新增
                    return true;
                }
                return false;
            }
        }
        Debug.LogWarning("法宝槽已满，无法装备");
        return false;
    }

    // 卸下法宝
    public void UnequipArtifact(int index)
    {
        if (index < 0 || index >= artifactSlots.Length) return;
        Item item = artifactSlots[index];
        if (item == null) return;
        for (int i = 0; i < CAPACITY; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                artifactSlots[index] = null;
                RefreshUI();
                SaveToGameData();
                if (SaveManager.Instance != null)
                    SaveManager.Instance.SaveGame();
                FantasyStatusPanel panel = FindObjectOfType<FantasyStatusPanel>();
                if (panel != null)
                {
                    panel.RefreshStats();
                    panel.RefreshArtifacts();
                }
                UpdatePatchButtonText(); // 新增
                return;
            }
        }
        Debug.LogWarning("背包已满，无法卸下法宝");
    }

    /// <summary>
    /// 减少指定物品的数量（1个），如果数量变为0则从背包移除
    /// </summary>
    /// <returns>是否成功减少</returns>
    public bool ReduceItemCount(Item item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        int index = System.Array.IndexOf(slots, item);
        if (index >= 0 && slots[index] != null && slots[index].count >= amount)
        {
            slots[index].count -= amount;
            if (slots[index].count <= 0)
            {
                slots[index] = null;
            }
            RefreshUI();
            SaveToGameData();
            if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
            return true;
        }
        return false;
    }

    // 根据物品查找法宝槽索引
    public int FindArtifactSlot(Item item)
    {
        for (int i = 0; i < artifactSlots.Length; i++)
            if (artifactSlots[i] == item) return i;
        return -1;
    }

    public void RefreshUIFromGameData()
    {
        LoadFromGameData();
        RefreshUI();
    }

    // 一键装备/卸装按钮点击事件
    public void OnPatchClicked()
    {
        // 检查当前是否有任何装备或法宝被穿戴
        bool hasEquipped = false;
        for (int i = 0; i < equippedItems.Length; i++)
            if (equippedItems[i] != null) { hasEquipped = true; break; }
        if (!hasEquipped)
        {
            for (int i = 0; i < artifactSlots.Length; i++)
                if (artifactSlots[i] != null) { hasEquipped = true; break; }
        }

        if (hasEquipped)
            UnequipAll();
        else
            AutoEquip();
    }

    // 自动装备背包中所有符合条件的装备和法宝
    void AutoEquip()
    {
        // 装备
        for (EquipSlot slot = EquipSlot.Weapon; slot <= EquipSlot.Boots; slot++)
        {
            // 如果该槽位已有装备，跳过（避免重复）
            if (equippedItems[(int)slot] != null)
                continue;

            // 从背包中查找该部位的装备
            Item bestItem = null;
            foreach (var item in slots)
            {
                if (item == null) continue;
                if (item.type == ItemType.Equipment && item.equipSlot == slot && item.requireLevel <= GameData.playerLevel)
                {
                    // 简单取第一个符合条件的，可以扩展为最佳品质等
                    bestItem = item;
                    break;
                }
            }

            if (bestItem != null)
                EquipItem(bestItem, slot);
        }

        // 法宝：依次填充空槽位
        for (int i = 0; i < artifactSlots.Length; i++)
        {
            if (artifactSlots[i] != null)
                continue;

            Item bestArtifact = null;
            foreach (var item in slots)
            {
                if (item == null) continue;
                if (item.type == ItemType.Artifact && item.requireLevel <= GameData.playerLevel)
                {
                    bestArtifact = item;
                    break;
                }
            }

            if (bestArtifact != null)
                EquipArtifact(bestArtifact);
        }
    }

    // 一键卸下所有装备和法宝（放入背包）
    void UnequipAll()
    {
        // 卸下所有装备（从最后一个开始避免索引问题，但直接遍历即可）
        for (EquipSlot slot = EquipSlot.Weapon; slot <= EquipSlot.Boots; slot++)
        {
            if (equippedItems[(int)slot] != null)
                UnequipItem(slot);
        }

        // 卸下所有法宝
        for (int i = artifactSlots.Length - 1; i >= 0; i--)
        {
            if (artifactSlots[i] != null)
                UnequipArtifact(i);
        }
    }

    // 更新一键按钮的文本
    void UpdatePatchButtonText()
    {
        if (patchButton == null) return;

        bool hasEquipped = false;
        for (int i = 0; i < equippedItems.Length; i++)
            if (equippedItems[i] != null) { hasEquipped = true; break; }
        if (!hasEquipped)
        {
            for (int i = 0; i < artifactSlots.Length; i++)
                if (artifactSlots[i] != null) { hasEquipped = true; break; }
        }

        patchButton.GetComponentInChildren<TMP_Text>().text = hasEquipped ? "一键卸装" : "一键装备";
    }

    // 获取背包中所有物品（非空）
    public List<Item> GetAllItems()
    {
        return slots.Where(s => s != null).ToList();
    }

    // 按类型获取物品
    public List<Item> GetItemsByType(ItemType type)
    {
        return slots.Where(s => s != null && s.type == type).ToList();
    }

    // 移除指定物品实例（从背包中删除）
    public void RemoveItem(Item item)
    {
        int index = System.Array.IndexOf(slots, item);
        if (index >= 0)
        {
            slots[index] = null;
            RefreshUI();
            SaveToGameData();
            if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
        }
    }

    public void CloseSlotObjects()
    {
        foreach (var slot in slotObjects)
        {
            // if (slot != null)
            slot.SetActive(false);
        }
    }

    public void CloseAndReturn()
    {
        SceneManager.LoadScene("Demon Tower");
    }

}