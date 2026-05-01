using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public class CharacterAttribute : MonoBehaviour
{
    [Header("基本属性文本")]
    public TMP_Text levelValueText;
    public TMP_InputField conValueInput;      // 体质输入框
    public TMP_InputField intValueInput;      // 灵力输入框
    public TMP_InputField strValueInput;      // 力量输入框
    public TMP_InputField agiValueInput;      // 敏捷输入框
    public TMP_Text remainingPointsText;

    [Header("战斗属性文本")]
    public TMP_Text hpValueText;
    public TMP_Text mpValueText;
    public TMP_Text atkValueText;
    public TMP_Text defValueText;
    public TMP_Text spdValueText;
    public TMP_Text critValueText;
    public TMP_Text hitValueText;
    public TMP_Text evaValueText;

    [Header("加减按钮")]
    public Button CONPlusButton;
    public Button CONMinusButton;
    public Button INTPlusButton;
    public Button INTMinusButton;
    public Button STRPlusButton;
    public Button STRMinusButton;
    public Button AGIPlusButton;
    public Button AGIMinusButton;

    [Header("其他")]
    public Button ConfirmButton;
    public Button RetuenButton;
    public Button ResetButton;

    public Button additionalAttrButton;
    public Button DescriptionButton;

    public GameObject DescriptionPanel;
    public TMP_Text DescriptionText;

    // 当前分配值（仅基础分配，不含额外加成）
    private int allocatedSTR;
    private int allocatedINT;
    private int allocatedAGI;
    private int allocatedCON;

    // 剩余可分配点数（实时计算）
    private int remainingPoints;

    // 最终基础属性（包含装备和法宝的基础属性加成）
    private int finalSTR;
    private int finalINT;
    private int finalAGI;
    private int finalCON;

    // 固定基础属性（初始10 + 装备/法宝附加，不含分配点）
    private int fixedSTR;
    private int fixedINT;
    private int fixedAGI;
    private int fixedCON;

    private bool isUpdatingUI = false; // 防止递归更新

    void Start()
    {
        // 从 GameData 加载现有数据
        LoadFromGameData();

        // 绑定按钮事件（长按功能）
        SetupButtonLongPress(STRPlusButton, OnSTRPlus, OnSTRMinus, true);
        SetupButtonLongPress(STRMinusButton, OnSTRMinus, null, false);
        SetupButtonLongPress(INTPlusButton, OnINTPlus, null, true);
        SetupButtonLongPress(INTMinusButton, OnINTMinus, null, false);
        SetupButtonLongPress(AGIPlusButton, OnAGIPlus, null, true);
        SetupButtonLongPress(AGIMinusButton, OnAGIMinus, null, false);
        SetupButtonLongPress(CONPlusButton, OnCONPlus, null, true);
        SetupButtonLongPress(CONMinusButton, OnCONMinus, null, false);

        BindButton(ConfirmButton, OnConfirm);
        BindButton(RetuenButton, ClickReturn);
        BindButton(ResetButton, OnReset);
        BindButton(additionalAttrButton, OnAdditionalAttrClick);
        BindButton(DescriptionButton, OnDescriptionClick);

        // 为输入框添加监听
        SetupInputField(strValueInput, OnSTRInputChanged);
        SetupInputField(intValueInput, OnINTInputChanged);
        SetupInputField(agiValueInput, OnAGIInputChanged);
        SetupInputField(conValueInput, OnCONInputChanged);

        // DescriptionPanel.SetActive(false); // 确保初始关闭

        // 默认打开附加属性面板
        OnAdditionalAttrClick();
        // 刷新 UI
        RefreshUI();
    }

    private void SetupInputField(TMP_InputField inputField, UnityEngine.Events.UnityAction<string> onValueChanged)
    {
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveAllListeners();
            inputField.onValueChanged.AddListener(onValueChanged);
        }
    }

    // 力量输入框变更处理（使用最终值）
    private void OnSTRInputChanged(string value)
    {
        if (isUpdatingUI) return;
        int newFinalValue;
        if (!int.TryParse(value, out newFinalValue))
        {
            RefreshUI();
            return;
        }
        // 最小值 = 固定基础属性（初始10 + 装备/法宝附加）
        if (newFinalValue < fixedSTR) newFinalValue = fixedSTR;
        int delta = newFinalValue - finalSTR;
        if (delta == 0)
        {
            RefreshUI();
            return;
        }
        if (delta > 0 && remainingPoints >= delta)
        {
            allocatedSTR += delta;
            remainingPoints -= delta;
            RefreshUI();
            SaveToGameData();
        }
        else if (delta < 0)
        {
            allocatedSTR += delta;
            remainingPoints -= delta;
            RefreshUI();
            SaveToGameData();
        }
        else
        {
            RefreshUI();
        }
    }

    // 灵力输入框变更处理（使用最终值）
    private void OnINTInputChanged(string value)
    {
        if (isUpdatingUI) return;
        int newFinalValue;
        if (!int.TryParse(value, out newFinalValue))
        {
            RefreshUI();
            return;
        }
        if (newFinalValue < fixedINT) newFinalValue = fixedINT;
        int delta = newFinalValue - finalINT;
        if (delta == 0)
        {
            RefreshUI();
            return;
        }
        if (delta > 0 && remainingPoints >= delta)
        {
            allocatedINT += delta;
            remainingPoints -= delta;
            RefreshUI();
            SaveToGameData();
        }
        else if (delta < 0)
        {
            allocatedINT += delta;
            remainingPoints -= delta;
            RefreshUI();
            SaveToGameData();
        }
        else
        {
            RefreshUI();
        }
    }

    // 敏捷输入框变更处理（使用最终值）
    private void OnAGIInputChanged(string value)
    {
        if (isUpdatingUI) return;
        int newFinalValue;
        if (!int.TryParse(value, out newFinalValue))
        {
            RefreshUI();
            return;
        }
        if (newFinalValue < fixedAGI) newFinalValue = fixedAGI;
        int delta = newFinalValue - finalAGI;
        if (delta == 0)
        {
            RefreshUI();
            return;
        }
        if (delta > 0 && remainingPoints >= delta)
        {
            allocatedAGI += delta;
            remainingPoints -= delta;
            RefreshUI();
            SaveToGameData();
        }
        else if (delta < 0)
        {
            allocatedAGI += delta;
            remainingPoints -= delta;
            RefreshUI();
            SaveToGameData();
        }
        else
        {
            RefreshUI();
        }
    }

    // 体质输入框变更处理（使用最终值）
    private void OnCONInputChanged(string value)
    {
        if (isUpdatingUI) return;
        int newFinalValue;
        if (!int.TryParse(value, out newFinalValue))
        {
            RefreshUI();
            return;
        }
        if (newFinalValue < fixedCON) newFinalValue = fixedCON;
        int delta = newFinalValue - finalCON;
        if (delta == 0)
        {
            RefreshUI();
            return;
        }
        if (delta > 0 && remainingPoints >= delta)
        {
            allocatedCON += delta;
            remainingPoints -= delta;
            RefreshUI();
            SaveToGameData();
        }
        else if (delta < 0)
        {
            allocatedCON += delta;
            remainingPoints -= delta;
            RefreshUI();
            SaveToGameData();
        }
        else
        {
            RefreshUI();
        }
    }

    private void SetupButtonLongPress(Button btn, System.Action action, System.Action stopAction, bool isPlus)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => action?.Invoke());

        EventTrigger trigger = btn.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) =>
        {
            StartCoroutine(LongPressCoroutine(action, stopAction));
        });
        trigger.triggers.Add(pointerDown);

        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) =>
        {
            StopAllCoroutines();
            stopAction?.Invoke();
        });
        trigger.triggers.Add(pointerUp);
    }

    private IEnumerator LongPressCoroutine(System.Action action, System.Action stopAction)
    {
        yield return new WaitForSeconds(0.5f);
        while (true)
        {
            action?.Invoke();
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void BindButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }
        else
        {
            Debug.LogError($"按钮未赋值: {action.Method.Name}");
        }
    }

    private void OnSTRPlus() { if (remainingPoints > 0) { allocatedSTR++; remainingPoints--; RefreshUI(); SaveToGameData(); } }
    private void OnSTRMinus() { if (allocatedSTR > 0) { allocatedSTR--; remainingPoints++; RefreshUI(); SaveToGameData(); } }
    private void OnINTPlus() { if (remainingPoints > 0) { allocatedINT++; remainingPoints--; RefreshUI(); SaveToGameData(); } }
    private void OnINTMinus() { if (allocatedINT > 0) { allocatedINT--; remainingPoints++; RefreshUI(); SaveToGameData(); } }
    private void OnAGIPlus() { if (remainingPoints > 0) { allocatedAGI++; remainingPoints--; RefreshUI(); SaveToGameData(); } }
    private void OnAGIMinus() { if (allocatedAGI > 0) { allocatedAGI--; remainingPoints++; RefreshUI(); SaveToGameData(); } }
    private void OnCONPlus() { if (remainingPoints > 0) { allocatedCON++; remainingPoints--; RefreshUI(); SaveToGameData(); } }
    private void OnCONMinus() { if (allocatedCON > 0) { allocatedCON--; remainingPoints++; RefreshUI(); SaveToGameData(); } }

    private void OnConfirm()
    {
        Debug.Log($"属性已分配：力量{allocatedSTR}，灵力{allocatedINT}，敏捷{allocatedAGI}，体质{allocatedCON}，剩余点数{remainingPoints}");
        SaveToGameData();
    }

    public void ClickReturn()
    {
        SceneManager.LoadScene("Demon Tower");
    }

    private void OnAdditionalAttrClick()
    {
        if (DescriptionPanel == null) return;

        bool isActive = DescriptionPanel.activeSelf;

        DescriptionText.text = GetAdditionalAttributesText();

    }

    private void OnDescriptionClick()
    {
        if (DescriptionPanel == null) return;

        bool isActive = DescriptionPanel.activeSelf;

        DescriptionText.text = GetDescriptionText();

    }

    private string GetDescriptionText()
    {
        return "<color=#FFD700>【基础属性】</color>\n" +
                "<color=#FFD700>体质(CON)</color>：主要影响生命值与防御\n" +
               "<color=#FFD700>灵力(INT)</color>：主要影响内力值，并少量影响命中率和暴击率\n" +
               "<color=#FFD700>力量(STR)</color>：主要影响攻击力、暴击率和命中率\n" +
               "<color=#FFD700>敏捷(AGI)</color>：主要影响速度和闪避率，少量影响命中率\n" +
               "<color=#FFD700>【战斗属性】</color>\n" +
               "(1) HP   = CON x 20 + Level x 50 + 1000 + 附加属性\n" +
               "(2) MP   = INT x 5  + Level x 20 + 300  + 附加属性\n" +
               "(3) ATK  = STR x 6  + Level x 25 + 200  + 附加属性\n" +
               "(4) DEF  = CON x 4  + Level x 15 + 120  + 附加属性\n" +
               "(5) SPD  = AGI x 0.75 + Level x 0.5 + 500 + 附加属性\n" +
               "(6) CRIT = (INT+STR) x 0.05 + Level x 0.2 + 20 + 附加属性/10\n" +
               "(7) HIT  = (INT+STR+AGI) x 0.05 + Level x 0.2 + 20 + 附加属性/10\n" +
               "(8) EVA  = AGI x 0.05 + Level x 0.1 + 10 + 附加属性/10\n" +
               "<color=#FFD700>【门派特色加成】</color>\n" +
               "天王殿：基础连击率+20%\n" +
               "五庄观：基础晕击率+20%\n" +
               "方寸山：基础暴击率+10%，基础暴击伤害+20%\n" +
               "<color=#FFD700>【升级/配置规则】</color>\n" +
               "角色初始1级时所有属性均为10，每升一级获得4个属性点，等级上限70级；\n" +
               "从1级到70级共69次升级，总计可分配属性点：69x4 = 276点；\n" +
               "每使用1个人参果可额外获得1个属性点，最多可使用120个；\n" +
               "装备、法宝上的基础属性和战斗属性会直接加到对应属性上；\n" +
               "进行属性点分配时，长按 +/- 可持续分配/回收，也可输入目标值（不可低于角色初始值+附加值）。";
    }

    private string GetAdditionalAttributesText()
    {
        StringBuilder sb = new StringBuilder();

        // 定义基础属性和战斗属性的类型列表（不变）
        AttributeType[] basicTypes = new AttributeType[]
        {
            AttributeType.Constitution,
            AttributeType.Spirit,
            AttributeType.Strength,
            AttributeType.Agility
        };

        AttributeType[] combatTypes = new AttributeType[]
        {
            AttributeType.Health,
            AttributeType.Mana,
            AttributeType.Attack,
            AttributeType.Defense,
            AttributeType.Speed,
            AttributeType.CritRate,
            AttributeType.ComboRate,
            AttributeType.StunRate,
            AttributeType.HitRate,
            AttributeType.EvasionRate,
        };

        // 收集所有加成
        var sourceMap = new Dictionary<AttributeType, List<(string sourceName, int value)>>();

        // 处理装备
        for (int i = 0; i < GameData.equippedItems.Length; i++)
        {
            var item = GameData.equippedItems[i];
            if (item == null) continue;
            string sourceName = $"{item.itemName.Trim()}";
            AddAttributesToMap(item.basicAttributes, sourceMap, sourceName);
            AddAttributesToMap(item.extraAttributes, sourceMap, sourceName);
        }

        // 处理法宝
        for (int i = 0; i < GameData.artifactSlots.Length; i++)
        {
            var item = GameData.artifactSlots[i];
            if (item == null) continue;
            string sourceName = $"{item.itemName.Trim()}";
            AddAttributesToMap(item.basicAttributes, sourceMap, sourceName);
            AddAttributesToMap(item.extraAttributes, sourceMap, sourceName);
        }

        // 输出基础属性
        sb.AppendLine("<color=#FFD700>【基础属性来源】</color>");
        bool hasBasic = false;
        foreach (var type in basicTypes)
        {
            if (sourceMap.TryGetValue(type, out var sources))
            {
                hasBasic = true;
                // 合并同一来源
                var merged = sources.GroupBy(s => s.sourceName)
                                    .Select(g => (sourceName: g.Key, total: g.Sum(s => s.value)))
                                    .ToList();
                // 按数值降序排序
                merged = merged.OrderByDescending(m => m.total).ToList();
                int total = merged.Sum(m => m.total);
                sb.Append(GetAttributeName(type)).Append("（").Append(total).Append("）：");
                for (int i = 0; i < merged.Count; i++)
                {
                    if (i > 0) sb.Append("、");
                    sb.Append($"{merged[i].sourceName}({merged[i].total})");
                }
                sb.AppendLine();
            }
        }
        if (!hasBasic)
        {
            sb.AppendLine("暂无基础属性加成");
        }

        // 输出战斗属性
        sb.AppendLine();
        sb.AppendLine("<color=#FFD700>【战斗属性来源】</color>");
        bool hasCombat = false;
        foreach (var type in combatTypes)
        {
            if (sourceMap.TryGetValue(type, out var sources))
            {
                hasCombat = true;
                // 合并同一来源
                var merged = sources.GroupBy(s => s.sourceName)
                                    .Select(g => (sourceName: g.Key, total: g.Sum(s => s.value)))
                                    .ToList();
                // 按数值降序排序
                merged = merged.OrderByDescending(m => m.total).ToList();
                int total = merged.Sum(m => m.total);
                sb.Append(GetAttributeName(type)).Append("（").Append(total).Append("）：");
                for (int i = 0; i < merged.Count; i++)
                {
                    if (i > 0) sb.Append("、");
                    sb.Append($"{merged[i].sourceName}({merged[i].total})");
                }
                sb.AppendLine();
            }
        }
        if (!hasCombat)
        {
            sb.AppendLine("暂无战斗属性加成");
        }

        return sb.ToString();
    }

    private void AddAttributesToMap(List<ItemAttribute> attrs,
                                     Dictionary<AttributeType, List<(string sourceName, int value)>> map,
                                     string sourceName)
    {
        if (attrs == null) return;
        foreach (var attr in attrs)
        {
            if (!map.ContainsKey(attr.type))
                map[attr.type] = new List<(string, int)>();
            map[attr.type].Add((sourceName, attr.value));
        }
    }

    private string GetAttributeName(AttributeType type)
    {
        switch (type)
        {
            case AttributeType.Constitution: return "体质";
            case AttributeType.Spirit: return "灵力";
            case AttributeType.Strength: return "力量";
            case AttributeType.Agility: return "敏捷";
            case AttributeType.Health: return "生命";
            case AttributeType.Mana: return "内力";
            case AttributeType.Attack: return "攻击力";
            case AttributeType.Defense: return "防御力";
            case AttributeType.Speed: return "速度";
            case AttributeType.CritRate: return "暴击率";
            case AttributeType.HitRate: return "命中率";
            case AttributeType.EvasionRate: return "闪避率";
            case AttributeType.ComboRate: return "连击率";
            case AttributeType.StunRate: return "晕击率";
            default: return type.ToString();
        }
    }

    public void RefreshUI()
    {
        isUpdatingUI = true;

        // 先计算最终属性（刷新 finalXXX 和 fixedXXX）
        RefreshCombatStats();

        // 更新输入框文本（最终基础属性值）
        if (strValueInput != null) strValueInput.text = finalSTR.ToString();
        if (intValueInput != null) intValueInput.text = finalINT.ToString();
        if (agiValueInput != null) agiValueInput.text = finalAGI.ToString();
        if (conValueInput != null) conValueInput.text = finalCON.ToString();

        remainingPointsText.text = remainingPoints.ToString();
        levelValueText.text = GameData.playerLevel.ToString();

        // 更新按钮交互状态
        STRPlusButton.interactable = remainingPoints > 0;
        STRMinusButton.interactable = allocatedSTR > 0;
        INTPlusButton.interactable = remainingPoints > 0;
        INTMinusButton.interactable = allocatedINT > 0;
        AGIPlusButton.interactable = remainingPoints > 0;
        AGIMinusButton.interactable = allocatedAGI > 0;
        CONPlusButton.interactable = remainingPoints > 0;
        CONMinusButton.interactable = allocatedCON > 0;

        isUpdatingUI = false;
    }

    private void RefreshCombatStats()
    {
        int baseCON = 10 + allocatedCON;
        int baseINT = 10 + allocatedINT;
        int baseSTR = 10 + allocatedSTR;
        int baseAGI = 10 + allocatedAGI;

        var stats = CharacterStatsCalculator.CalculateFinalStats(
            baseSTR, baseINT, baseAGI, baseCON,
            GameData.playerLevel, GameData.playerFaction,
            GameData.equippedItems, GameData.artifactSlots);

        // 基础属性（最终值）
        finalCON = stats.FinalCON;
        finalINT = stats.FinalINT;
        finalSTR = stats.FinalSTR;
        finalAGI = stats.FinalAGI;
        fixedCON = finalCON - allocatedCON;
        fixedINT = finalINT - allocatedINT;
        fixedSTR = finalSTR - allocatedSTR;
        fixedAGI = finalAGI - allocatedAGI;

        // 战斗属性（先取计算器结果）
        int hp = stats.HP;
        int mp = stats.MP;
        int atk = stats.ATK;
        int def = stats.DEF;
        float spd = stats.SPD;
        float critRate = stats.CritRate;
        float critDamage = stats.CritDamage;
        float hitRate = stats.HitRate;
        float evaRate = stats.EvasionRate;
        float comboRate = stats.ComboRate;
        float stunRate = stats.StunRate;

        // 更新UI显示
        hpValueText.text = hp.ToString();
        mpValueText.text = mp.ToString();
        atkValueText.text = atk.ToString();
        defValueText.text = def.ToString();
        spdValueText.text = spd.ToString("F0");
        critValueText.text = (critRate * 100).ToString("F1") + "%";
        hitValueText.text = (hitRate * 100).ToString("F1") + "%";
        evaValueText.text = (evaRate * 100).ToString("F1") + "%";

        // 如果需要显示连击/晕击率，可以扩展UI，但原UI没有，此处仅作内部使用
        // 如果后续要显示，可以添加对应的 Text 组件
    }

    private void LoadFromGameData()
    {
        allocatedSTR = GameData.playerAllocatedSTR;
        allocatedINT = GameData.playerAllocatedINT;
        allocatedAGI = GameData.playerAllocatedAGI;
        allocatedCON = GameData.playerAllocatedCON;
        remainingPoints = GameData.unallocatedPoints;
    }

    private void SaveToGameData()
    {
        GameData.playerAllocatedSTR = allocatedSTR;
        GameData.playerAllocatedINT = allocatedINT;
        GameData.playerAllocatedAGI = allocatedAGI;
        GameData.playerAllocatedCON = allocatedCON;
        GameData.unallocatedPoints = remainingPoints;
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    private void OnReset()
    {
        int totalAllocated = allocatedSTR + allocatedINT + allocatedAGI + allocatedCON;
        remainingPoints += totalAllocated;
        allocatedSTR = 0;
        allocatedINT = 0;
        allocatedAGI = 0;
        allocatedCON = 0;
        RefreshUI();
        SaveToGameData();
        Debug.Log("属性点已重置");
    }
}