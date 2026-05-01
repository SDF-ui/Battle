using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class FactionSelectionUI : MonoBehaviour
{
    [Header("门派按钮")]
    public Button tianWangDianButton;
    public Button wuZhuangGuanButton;
    public Button fangCunShanButton;

    [Header("描述文本")]
    public TMP_Text descriptionText;

    [Header("操作按钮")]
    public Button switchButton;
    public Button returnButton;

    [Header("提示文本")]
    public TMP_Text promptText;

    [Header("门派数据")]
    [SerializeField] private string currentSelectedFaction = "TianWangDian";

    private Dictionary<string, string> factionDescriptions = new Dictionary<string, string>();
    private TMP_Text tianWangDianText;
    private TMP_Text wuZhuangGuanText;
    private TMP_Text fangCunShanText;

    void Start()
    {
        tianWangDianText = tianWangDianButton.GetComponentInChildren<TMP_Text>();
        wuZhuangGuanText = wuZhuangGuanButton.GetComponentInChildren<TMP_Text>();
        fangCunShanText = fangCunShanButton.GetComponentInChildren<TMP_Text>();

        InitDescriptions();

        tianWangDianButton.onClick.AddListener(() => ShowFactionInfo("TianWangDian"));
        wuZhuangGuanButton.onClick.AddListener(() => ShowFactionInfo("WuZhuangGuan"));
        fangCunShanButton.onClick.AddListener(() => ShowFactionInfo("FangCunShan"));

        switchButton.onClick.AddListener(SwitchToSelectedFaction);
        returnButton.onClick.AddListener(ReturnToPreviousScene);

        string playerFaction = GameData.playerFaction;
        if (string.IsNullOrEmpty(playerFaction)) playerFaction = "TianWangDian";
        ShowFactionInfo(playerFaction);

        if (promptText != null) promptText.gameObject.SetActive(false);
    }

    private void InitDescriptions()
    {
        // 天王殿描述（与最新文档完全一致）
        factionDescriptions["TianWangDian"] =
            "天王殿·李天王\n\n" +
            "门派特色：擅长连击，通过连续攻击压制敌人，体现天兵神将的迅猛攻势；基础连击率提升20%。\n\n" +
            "【主动技能】\n" +
            "狮子搏兔 | 物理攻击 | 30 MP | 2回合 | 对目标连续攻击2次，每次造成80%攻击力的伤害；若两次攻击均命中，则自身下回合连击概率提升10%（不可叠加）。\n" +
            "威震山河 | 控制 | 20 MP | 2回合 | 有80%概率使目标陷入“错乱”状态，无法施展任何技能，持续3回合；若效果命中，则自身下回合连击概率提升10%；此次攻击回复自身30%内力值。\n\n" +
            "【辅助技能】\n" +
            "枕戈待旦 | 增益 | 50 MP | 2回合 | 本回合内，自身攻击附加30%连击率提升，此技能不占用回合行动值。\n" +
            "不动如山 | 防御 | 80 MP | 4回合 | 回复自身30%气血，并进入“列阵”状态：所受伤害降低40%，并对己方全体角色提升相当于自身10%的防御；在任意己方角色被敌方攻击时，自身有80%概率触发一次反击（造成相当于自身60%攻击力的固定伤害，不消耗行动值）；持续4回合。\n\n" +
            "【被动技能】\n" +
            "迅疾如风 | 被动 | - | - | 反击伤害增加30%（造成相当于90%攻击力的固定伤害）。\n" +
            "生生不息 | 被动 | - | - | 每次触发连击时，恢复自身15%最大生命值，并提升15%攻击力，持续2回合（不可叠加）。\n" +
            "蓄势待发 | 被动 | - | - | 连击可多次触发，每次触发连击时获得“敛锋”状态（提升自身10%伤害与5%防御忽视，并降低自身10%连击概率），可叠加（上限10层），持续到本回合结束；若回合结束时连击率大于等于50%且本回合内未触发连击，则额外发动一次攻击（造成100%攻击力的伤害）。";

        // 五庄观描述（与最新文档完全一致）
        factionDescriptions["WuZhuangGuan"] =
            "五庄观·镇元子\n\n" +
            "门派特色：擅长晕击，以乾坤之力震慑敌人，彰显地仙之祖的掌控之道；基础晕击率提升20%。\n\n" +
            "【主动技能】\n" +
            "雷霆一击 | 物理攻击 | 30 MP | 2回合 | 对目标造成150%攻击力的伤害；若目标已处于眩晕状态，则伤害提升30%（技能伤害系数提升至180%）并延长眩晕1回合。\n" +
            "袖里乾坤 | 控制/回复 | 20 MP | 2回合 | 有80%概率使目标其陷入“眩晕”状态，无法行动，持续3回合；若效果命中，则自身下回合晕击概率提升10%；此次攻击回复自身30%内力值。\n\n" +
            "【辅助技能】\n" +
            "引雷控电 | 增益 | 50 MP | 2回合 | 本回合内，自身攻击附加30%晕击率提升，此技能不占用回合行动值。\n" +
            "五雷轰顶 | 控制/推条 | 80 MP | 4回合 | 对敌方全体造成150%攻击力的伤害，并有80%概率使其行动条减少30%。同时进入“镇岳”状态：晕击概率提高10%，且每次成功触发晕击时，自身行动条增加30%；持续4回合。\n\n" +
            "【被动技能】\n" +
            "混元道体 | 被动 | - | - | 受到攻击时，有15%概率使攻击者眩晕1回合。\n" +
            "天地同寿 | 被动 | - | - | 每次成功眩晕目标时，恢复自身15%最大生命值，并施加“易伤”效果（受到的伤害提高10%），持续2回合（不可叠加）。\n" +
            "道玄缚祟 | 被动 | - | - | 攻击目标前，从未施加的负面效果中随机选择一个进行施加（除生命外的战斗属性降低10%，不可叠加），持续2回合，并对己方全体角色施加“迅捷”状态（提升15%的速度，持续2回合，不可叠加）；敌方每存在一个负面效果（战斗属性降低系列），提升15%伤害，最多可提升75%。";

        // 方寸山描述（与最新文档完全一致）
        factionDescriptions["FangCunShan"] =
            "方寸山·菩提子\n\n" +
            "门派特色：擅长暴击，以无上佛法一击制敌，体现菩提子的智慧与威能；基础暴击率提升10%，暴击伤害提升20%。\n\n" +
            "【主动技能】\n" +
            "无相慧剑 | 物理攻击 | 30 MP | 2回合 | 对敌方单体造成150%攻击力的伤害；若暴击率大于等于75%，则此次攻击必暴击，且对其他所有敌人造成40%的溅射伤害。\n" +
            "禅心入梦 | 控制 | 20 MP | 2回合 | 对敌方单体施放，有80%概率使其陷入“睡眠”状态，无法行动，持续3回合（受到伤害会醒来）；若效果命中，则自身下回合暴击概率提高10%；此次攻击回复自身30%内力值。\n\n" +
            "【辅助技能】\n" +
            "空明心境 | 增益 | 50 MP | 2回合 | 本回合内，自身攻击附加30%暴击率提升，此技能不占用回合行动值。\n" +
            "明心见性 | 驱散/增益 | 80 MP | 4回合 | 驱散自身所有负面状态，并进入“明心”状态：暴击率提高10%，暴伤系数提高20%，且每次暴击时自身行动条增加20%；持续4回合。\n\n" +
            "【被动技能】\n" +
            "破妄之眼 | 被动 | - | - | 受到攻击时，有80%概率使目标命中率降低10%，持续2回合（不可叠加）。\n" +
            "慧灯永续 | 被动 | - | - | 每次触发暴击时，自身回复15%最大生命值，并提升15%命中率，持续2回合（不可叠加）。\n" +
            "妙法承佑 | 被动 | - | - | 将溢出的暴击率按1:2比例转化为暴伤系数（每溢出10%暴击率转化为20%暴伤系数）；暴击时忽视敌方10%防御，并为己方全体角色施加相当于此次伤害20%的护盾；";
    }

    private void ShowFactionInfo(string factionKey)
    {
        currentSelectedFaction = factionKey;
        if (descriptionText != null && factionDescriptions.ContainsKey(factionKey))
            descriptionText.text = factionDescriptions[factionKey];

        if (tianWangDianText != null)
            tianWangDianText.fontStyle = (factionKey == "TianWangDian") ? FontStyles.Bold : FontStyles.Normal;
        if (wuZhuangGuanText != null)
            wuZhuangGuanText.fontStyle = (factionKey == "WuZhuangGuan") ? FontStyles.Bold : FontStyles.Normal;
        if (fangCunShanText != null)
            fangCunShanText.fontStyle = (factionKey == "FangCunShan") ? FontStyles.Bold : FontStyles.Normal;
    }

    private void SwitchToSelectedFaction()
    {
        GameData.playerFaction = currentSelectedFaction;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
            Debug.Log($"已切换到门派: {currentSelectedFaction} 并保存");
        }
        else
        {
            Debug.LogWarning("SaveManager.Instance 不存在，无法保存");
        }

        if (promptText != null)
        {
            string displayName = currentSelectedFaction switch
            {
                "TianWangDian" => "天王殿",
                "WuZhuangGuan" => "五庄观",
                "FangCunShan" => "方寸山",
                _ => currentSelectedFaction
            };
            promptText.text = $"已切换到{displayName}！";
            promptText.gameObject.SetActive(true);
            StopCoroutine(HidePromptAfterDelay());
            StartCoroutine(HidePromptAfterDelay());
        }
    }

    private IEnumerator HidePromptAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private void ReturnToPreviousScene()
    {
        SceneManager.LoadScene("Demon Tower");
    }
}