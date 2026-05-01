using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FantasyStatusPanel : MonoBehaviour
{
    [Header("角色属性")]
    public TMP_Text healthText;
    public TMP_Text manaText;
    public TMP_Text attackText;
    public TMP_Text defenseText;
    public TMP_Text speedText;
    public TMP_Text hitText;
    public TMP_Text evasionText;
    public TMP_Text critText;
    public TMP_Text comboText;
    public TMP_Text stunText;
    public TMP_Text fleeText;

    [Header("装备槽")]
    public ItemSlot[] equipSlots;
    [Header("法宝槽")]
    public ItemSlot[] artifactSlots;

    // 角色基础属性从 GameData 读取
    private int baseSTR => 10 + GameData.playerAllocatedSTR + GameData.playerExtraSTR;
    private int baseCON => 10 + GameData.playerAllocatedCON + GameData.playerExtraCON;
    private int baseAGI => 10 + GameData.playerAllocatedAGI + GameData.playerExtraAGI;
    private int baseINT => 10 + GameData.playerAllocatedINT + GameData.playerExtraINT;
    private int level => GameData.playerLevel;
    private string faction => GameData.playerFaction;

    // 门派基础概率（根据 faction 设置）
    private float baseComboChance => faction == "TianWangDian" ? 0.20f : 0f;
    private float baseStunChance => faction == "WuZhuangGuan" ? 0.20f : 0f;
    private float baseCritRate => faction == "FangCunShan" ? 0.10f : 0f;

    // 与 BattleManager 中保持一致
    private const float ACTION_THRESHOLD = 500f;

    public BackpackManager backpackManager;
    public ItemDetailPanel detailPanel;

    void Start()
    {
        if (backpackManager == null)
            backpackManager = FindObjectOfType<BackpackManager>();

        for (int i = 0; i < equipSlots.Length; i++)
            equipSlots[i].detailPanel = detailPanel;
        UpdateEquipmentUI();

        for (int i = 0; i < artifactSlots.Length; i++)
        {
            artifactSlots[i].detailPanel = detailPanel;
            artifactSlots[i].isArtifactSlot = true;
        }
        RefreshArtifacts();

        RefreshStats();
    }

    public void RefreshEquipment() => UpdateEquipmentUI();
    public void RefreshArtifacts()
    {
        if (backpackManager == null) return;
        for (int i = 0; i < artifactSlots.Length; i++)
        {
            if (artifactSlots[i] == null) continue;
            Item item = backpackManager.GetArtifactSlot(i);
            artifactSlots[i].SetItem(item);
        }
    }

    void UpdateEquipmentUI()
    {
        if (backpackManager == null) return;
        for (int i = 0; i < equipSlots.Length; i++)
        {
            if (equipSlots[i] == null) continue;
            Item item = backpackManager.GetEquippedItem((EquipSlot)i);
            equipSlots[i].SetItem(item);
        }
    }

    public void RefreshStats()
    {
        var stats = CharacterStatsCalculator.CalculateFinalStats(
            baseSTR, baseINT, baseAGI, baseCON,
            level, faction,
            GameData.equippedItems, GameData.artifactSlots);

        // 更新 UI 文本
        healthText.text = stats.HP.ToString();
        manaText.text = stats.MP.ToString();
        attackText.text = stats.ATK.ToString();
        defenseText.text = stats.DEF.ToString();
        speedText.text = stats.SPD.ToString("F0");
        hitText.text = (stats.HitRate * 100).ToString("F1") + "%";
        evasionText.text = (stats.EvasionRate * 100).ToString("F1") + "%";
        critText.text = (stats.CritRate * 100).ToString("F1") + "%";
        comboText.text = (stats.ComboRate * 100).ToString("F1") + "%";
        stunText.text = (stats.StunRate * 100).ToString("F1") + "%";
        fleeText.text = ((0.3f + stats.SPD / ACTION_THRESHOLD) * 100).ToString("F1") + "%";
    }
    // 处理装备属性（包含基础属性和战斗属性）
    private void AddAttribute(ref int hp, ref int mp, ref int atk, ref int def, ref int spd,
                               ref int crit, ref int hit, ref int eva, ref int combo, ref int stun,
                               ref int con, ref int intel, ref int str, ref int agi,
                               ItemAttribute attr)
    {
        switch (attr.type)
        {
            case AttributeType.Constitution: con += attr.value; break;
            case AttributeType.Spirit: intel += attr.value; break;
            case AttributeType.Strength: str += attr.value; break;
            case AttributeType.Agility: agi += attr.value; break;
            case AttributeType.Health: hp += attr.value; break;
            case AttributeType.Mana: mp += attr.value; break;
            case AttributeType.Attack: atk += attr.value; break;
            case AttributeType.Defense: def += attr.value; break;
            case AttributeType.Speed: spd += attr.value; break;
            case AttributeType.CritRate: crit += attr.value; break;
            case AttributeType.HitRate: hit += attr.value; break;
            case AttributeType.EvasionRate: eva += attr.value; break;
            case AttributeType.ComboRate: combo += attr.value; break;
            case AttributeType.StunRate: stun += attr.value; break;
        }
    }

    // 处理法宝属性（包含基础属性和战斗属性）
    private void AddArtifactAttribute(ItemAttribute attr,
                                       ref int con, ref int intel, ref int str, ref int agi,
                                       ref int hp, ref int mp, ref int atk, ref int def, ref int spd,
                                       ref int crit, ref int hit, ref int eva, ref int combo, ref int stun)
    {
        switch (attr.type)
        {
            case AttributeType.Constitution: con += attr.value; break;
            case AttributeType.Spirit: intel += attr.value; break;
            case AttributeType.Strength: str += attr.value; break;
            case AttributeType.Agility: agi += attr.value; break;
            case AttributeType.Health: hp += attr.value; break;
            case AttributeType.Mana: mp += attr.value; break;
            case AttributeType.Attack: atk += attr.value; break;
            case AttributeType.Defense: def += attr.value; break;
            case AttributeType.Speed: spd += attr.value; break;
            case AttributeType.CritRate: crit += attr.value; break;
            case AttributeType.HitRate: hit += attr.value; break;
            case AttributeType.EvasionRate: eva += attr.value; break;
            case AttributeType.ComboRate: combo += attr.value; break;
            case AttributeType.StunRate: stun += attr.value; break;
        }
    }

    public static FantasyStatusPanel Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
}