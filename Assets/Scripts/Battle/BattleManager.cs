using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[System.Serializable]
public class NormalAttack
{
    public string attackName = "普通攻击";
    public float damageMultiplier = 1.0f;
    public int mpCost = 0;
    public string description = "对敌人造成100%攻击力的伤害；本次攻击回复 10% 内力";
}

public class BattleManager : MonoBehaviour
{
    [Header("队伍设置")]
    public List<Character> playerParty = new List<Character>();
    public List<Character> enemyParty = new List<Character>();

    [Header("出生点")]
    public Transform[] playerSpawnPoints;
    public Transform[] enemySpawnPoints;

    [Header("出生点设置")]
    public Transform enemySpawnCenter;
    public float enemySpawnRadius = 3f;
    public bool useCircularSpawning = true;

    [Header("按钮")]
    public Button fleeButton;
    public Button defendButton;
    public Button cancelButton;

    [Header("战斗讯息")]
    public TMP_Text battleResultText;
    public TMP_Text turnResultText;
    public TMP_Text turnTotalDamageText;

    [Header("技能UI")]
    public Transform skillButtonContainer;
    public GameObject skillButtonPrefab;
    public TMP_Text skillTooltipText;

    [Header("行动队列UI")]
    public Transform actionQueueContainer;
    public GameObject characterIconPrefab;
    private int maxQueueLength = 10;

    public const float ACTION_THRESHOLD = 500f;
    public const float MAX_PREDICT_ACTION_THRESHOLD = 4000f;

    private List<GameObject> playerTargetIcons = new List<GameObject>();
    private List<GameObject> enemyTargetIcons = new List<GameObject>();

    [Header("战斗参数")]
    public float fleeBaseChance = 0.3f;

    [Header("信息面板")]
    public Button infoButton;
    public GameObject infoPanel;
    public Transform playerAvatarContainer;
    public Transform enemyAvatarContainer;
    public TMP_Text detailText;
    public Button exitButton;

    [Header("UI管理器（可选）")]
    public BattleUIManager uiManager;

    private enum BattleState { Idle, PlayerTurn, EnemyTurn, BattleEnd, SelectingTarget }
    private BattleState currentState = BattleState.Idle;
    private bool battleActive = false;

    private Character currentActor;
    private Skill selectedSkill;
    private bool hasUsedMainActionThisTurn = false;
    private bool hasTriggeredGuaranteedComboThisTurn = false;

    private List<Character> allCharacters = new List<Character>();
    private List<GameObject> queueIcons = new List<GameObject>();
    private List<Button> skillButtons = new List<Button>();

    private Queue<string> turnResultQueue = new Queue<string>();
    private bool isLoadingScene = false;

    private bool isExecutingSkill = false;
    private Dictionary<Character, GameObject> characterIconMap = new Dictionary<Character, GameObject>();

    private int currentTurnDamage = 0;
    private Coroutine hideDamageCoroutine = null;
    private BattleActionSystem actionSystem;

    void Start()
    {
        playerTargetIcons.Clear();
        enemyTargetIcons.Clear();
        Debug.Log("BattleManager Start, enemyConfigs count: " + (BattleData.enemyConfigs?.Count ?? 0));

        SpawnPlayer();
        SpawnEnemies();

        allCharacters.AddRange(playerParty);
        allCharacters.AddRange(enemyParty);

        foreach (var c in allCharacters)
            c.Initialize();

        // 初始化 BattleActionSystem（如果 uiManager 存在）
        if (uiManager != null)
        {
            actionSystem = new BattleActionSystem(this, uiManager, allCharacters);
            actionSystem.onSetIconScale = (character) => SetIconScale(character, 1f);
            actionSystem.onCheckBattleEnd = () => CheckBattleEnd();
        }

        // 绑定 BattleUIManager 事件回调（如果存在）
        if (uiManager != null)
        {
            uiManager.onFleeClicked = () => OnFlee();
            uiManager.onDefendClicked = () => OnDefend();
            uiManager.onCancelClicked = () => OnCancelSkill();
            uiManager.onSkillSelected = (skill) => OnSkillClicked(skill);
            uiManager.onQueueIconClicked = (character) => OnQueueIconClicked(character);
            uiManager.Initialize();

            // ★ 创建并初始化行动队列图标（UIManager 模式下必须调用，否则不会显示）
            uiManager.CreateActionQueueIcons(allCharacters);
            uiManager.UpdateActionQueueOrder(allCharacters);
        }
        else
        {
            // 兼容模式：使用原有的直接绑定方式
            CreateActionQueueIcons();

            if (fleeButton != null)
                fleeButton.onClick.AddListener(OnFlee);
            if (defendButton != null)
                defendButton.onClick.AddListener(OnDefend);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelSkill);
        }

        if (battleResultText != null)
            battleResultText.gameObject.SetActive(false);
        if (turnResultText != null)
            turnResultText.text = "";
        if (turnTotalDamageText != null)
        {
            turnTotalDamageText.gameObject.SetActive(false);
            turnTotalDamageText.color = Color.red;
        }

        battleActive = true;
        currentState = BattleState.Idle;

        if (infoButton != null)
            infoButton.onClick.AddListener(OnInfoButtonClick);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitInfoPanel);
        if (infoPanel != null)
            infoPanel.SetActive(false);

        if (skillTooltipText != null)
            skillTooltipText.gameObject.SetActive(false);

        UpdateUI();
    }

    void SpawnPlayer()
    {
        GameObject prefab = CharacterPrefabDB.GetPlayerPrefab();
        if (prefab == null)
        {
            Debug.LogError("玩家预制体未找到！请检查路径：Characters/Player");
            return;
        }

        Vector3 spawnPos = playerSpawnPoints != null && playerSpawnPoints.Length > 0 ? playerSpawnPoints[0].position : Vector3.zero;
        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        Character character = obj.GetComponent<Character>();
        if (character == null)
        {
            Debug.LogError("玩家预制体上缺少 Character 组件！");
            return;
        }

        character.characterName = "Player";
        character.faction = GameData.playerFaction;
        character.level = GameData.playerLevel;
        character.allocatedCON = GameData.playerAllocatedCON;
        character.allocatedINT = GameData.playerAllocatedINT;
        character.allocatedSTR = GameData.playerAllocatedSTR;
        character.allocatedAGI = GameData.playerAllocatedAGI;
        character.extraCON = GameData.playerExtraCON;
        character.extraINT = GameData.playerExtraINT;
        character.extraSTR = GameData.playerExtraSTR;
        character.extraAGI = GameData.playerExtraAGI;

        character.equippedEquipments = new List<Item>(GameData.equippedItems);
        character.equippedArtifacts = new List<Item>(GameData.artifactSlots);

        if (character.targetClickButton != null)
            playerTargetIcons.Add(character.targetClickButton.gameObject);
        else
            Debug.LogWarning("玩家缺少 targetClickButton 组件！");

        playerParty.Add(character);
    }

    void SpawnEnemies()
    {
        if (BattleData.enemyConfigs == null || BattleData.enemyConfigs.Count == 0)
            return;

        float centerY = 600.0f;
        float spacing = 160.0f;
        float fixedX = 600.0f;

        for (int i = 0; i < BattleData.enemyConfigs.Count; i++)
        {
            float posX = i == 0 ? fixedX : fixedX + 150;
            float posY;
            switch (i % 2)
            {
                case 0:
                    posY = centerY - i / 2 * spacing;
                    break;
                case 1:
                    posY = centerY + (i + 1) / 2 * spacing;
                    break;
                default:
                    posY = centerY - i / 2 * spacing;
                    break;
            }
            var config = BattleData.enemyConfigs[i];
            GameObject prefab = CharacterPrefabDB.GetEnemyPrefab(config.prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"敌人预制体 {config.prefabPath} 未找到，使用默认预制体。");
                prefab = CharacterPrefabDB.GetEnemyPrefab("Default");
                if (prefab == null)
                {
                    Debug.LogError("默认敌人预制体也未找到！");
                    continue;
                }
            }

            Vector3 spawnPos = new Vector3(posX, posY, 0);
            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
            Character character = obj.GetComponent<Character>();
            if (character == null)
            {
                Debug.LogError($"敌人预制体 {config.prefabPath} 上缺少 Character 组件！");
                continue;
            }

            character.characterName = config.characterName;
            character.faction = config.faction;
            character.level = config.level;
            character.allocatedCON = config.allocatedCON;
            character.allocatedINT = config.allocatedINT;
            character.allocatedSTR = config.allocatedSTR;
            character.allocatedAGI = config.allocatedAGI;
            character.extraCON = config.extraCON;
            character.extraINT = config.extraINT;
            character.extraSTR = config.extraSTR;
            character.extraAGI = config.extraAGI;

            character.equippedEquipments = config.equippedEquipments ?? new List<Item>();
            character.equippedArtifacts = config.equippedArtifacts ?? new List<Item>();

            // ★ 为通天教主添加专属技能（编号601-603）
            if (character.characterName == "通天教主")
            {
                Skill normalAttack = new Skill
                {
                    skillName = "普通攻击",
                    type = SkillType.Attack,
                    mpCost = 0,
                    cooldown = 0,
                    currentCooldown = 0,
                    isFreeAction = false,
                    skillID = 0,
                    description = "对敌人造成100%攻击力的伤害；本次攻击回复10%内力"
                };
                character.skills.Add(normalAttack);

                // 无极圣裁 (601)
                Skill wuJiShengCai = new Skill
                {
                    skillName = "无极圣裁",
                    type = SkillType.Attack,
                    mpCost = 60,
                    cooldown = 2,
                    currentCooldown = 0,
                    isFreeAction = false,
                    skillID = 601,
                    description = "单体物理攻击，伤害系数200%，主角行动条推迟30%，并附加10%减速，持续2回合。"
                };
                character.skills.Add(wuJiShengCai);

                // 万仙来朝 (602)
                Skill wanXianLaiChao = new Skill
                {
                    skillName = "万仙来朝",
                    type = SkillType.Control,
                    mpCost = 90,
                    cooldown = 10,
                    currentCooldown = 0,
                    isFreeAction = false,
                    skillID = 602,
                    description = "蓄力1回合，召唤4把剑阵虚影（诛仙、陷仙、绝仙、戮仙），属性为通天教主的60%。"
                };
                character.skills.Add(wanXianLaiChao);

                // 混元一气 (603)
                Skill hunYuanYiQi = new Skill
                {
                    skillName = "混元一气",
                    type = SkillType.Attack,
                    mpCost = 100,
                    cooldown = 10,
                    currentCooldown = 0,
                    isFreeAction = false,
                    skillID = 603,
                    description = "蓄力2回合，造成最大生命值99%的伤害。"
                };
                character.skills.Add(hunYuanYiQi);

                // 强制覆盖基础属性（忽略计算）
                character.currentHP = 60000;
                character.cachedMaxHP = 60000;
                character.currentMP = 5000;
                character.cachedMaxMP = 5000;
                character.cachedATK = 7000;
                character.cachedDEF = 4500;
                character.cachedHitRate = 1.5f;
                character.cachedCritRate = 0.8f;
                character.cachedSPD = 500;
                // 设置阶段相关字段
                character.currentPhase = 1;
                character.maxPhase = 3;
                character.phaseMaxHPs = new float[] { 60000, 90000, 120000 };
                character.phaseMaxMPs = new float[] { 5000, 8000, 10000 };
                character.phaseAttack = new float[] { 7000, 8500, 10000 };
                character.phaseDefense = new float[] { 4500, 5400, 6300 };
                character.phaseSpeed = new float[] { 500, 750, 1000 };
                character.isEliteOrBoss = true;

                // ★ 天生圣体被动
                character.damageReductionPercent = 0.5f;   // 免伤50%
                character.reflectDamagePercent = 0.1f;     // 反弹10%
                character.immuneToControl = true;
                character.controlToPushMultiplier = 0.25f; // 每回合推迟25%行动条

                character.isCustomStats = true;
            }

            // ★ 四象灵尊（青龙、白虎、朱雀、玄武）

            if (character.characterName == "青龙" ||
                character.characterName == "白虎" ||
                character.characterName == "朱雀" ||
                character.characterName == "玄武")
            {
                // 设为基础自定义属性，由 SpawnEnemies 覆盖
                character.isCustomStats = true;
                character.isEliteOrBoss = true;

                switch (character.characterName)
                {
                    case "青龙":
                        character.cachedMaxHP = 36000;
                        character.cachedMaxMP = 3000;
                        character.cachedATK = 7000;
                        character.cachedDEF = 4000;
                        character.cachedSPD = 600f;
                        character.cachedHitRate = 1.35f;
                        character.cachedCritRate = 0.15f;
                        character.cachedCritDamage = 1.5f;
                        character.damageReductionPercent = 0.25f;
                        break;
                    case "白虎":
                        character.cachedMaxHP = 36000;
                        character.cachedMaxMP = 3000;
                        character.cachedATK = 8000;
                        character.cachedDEF = 4000;
                        character.cachedSPD = 550f;
                        character.cachedHitRate = 1.5f;
                        character.cachedCritRate = 0.8f;
                        character.cachedCritDamage = 2.0f;
                        character.damageReductionPercent = 0.25f;
                        break;
                    case "朱雀":
                        character.cachedMaxHP = 36000;
                        character.cachedMaxMP = 3000;
                        character.cachedATK = 6000;
                        character.cachedDEF = 4000;
                        character.cachedSPD = 600f;
                        character.cachedHitRate = 1.2f;
                        character.cachedCritRate = 0.10f;
                        character.damageReductionPercent = 0.25f;
                        break;
                    case "玄武":
                        character.cachedMaxHP = 60000;
                        character.cachedMaxMP = 3000;
                        character.cachedATK = 5000;
                        character.cachedDEF = 6000;
                        character.cachedSPD = 600f;
                        character.cachedHitRate = 1.2f;
                        character.cachedCritRate = 0.05f;
                        character.damageReductionPercent = 0.25f;
                        break;
                }
                character.currentHP = character.cachedMaxHP;
                character.currentMP = character.cachedMaxMP;

                // 添加四神兽光环组件
                FourSymbolAura aura = character.gameObject.AddComponent<FourSymbolAura>();
                FourSymbolAura.SymbolType auraType = FourSymbolAura.SymbolType.None;
                switch (character.characterName)
                {
                    case "青龙": auraType = FourSymbolAura.SymbolType.Dragon; break;
                    case "白虎": auraType = FourSymbolAura.SymbolType.Tiger; break;
                    case "朱雀": auraType = FourSymbolAura.SymbolType.Bird; break;
                    case "玄武": auraType = FourSymbolAura.SymbolType.Turtle; break;
                }
                aura.Initialize(character, this, auraType);

                // 添加普通攻击
                character.skills.Clear();
                character.skills.Add(new Skill
                {
                    skillName = "普通攻击",
                    type = SkillType.Attack,
                    mpCost = 0,
                    cooldown = 0,
                    currentCooldown = 0,
                    isFreeAction = false,
                    skillID = 0,
                    description = "对敌人造成100%攻击力的伤害"
                });

                // 添加专属主动技能
                switch (character.characterName)
                {
                    case "青龙":
                        character.skills.Add(new Skill
                        {
                            skillName = "龙爪裂甲",
                            type = SkillType.Attack,
                            mpCost = 20,
                            cooldown = 3,
                            currentCooldown = 0,
                            isFreeAction = false,
                            skillID = 700,
                            description = "单体物理伤害，系数120%，并减少目标20%防御力，持续3回合"
                        });
                        break;
                    case "白虎":
                        character.skills.Add(new Skill
                        {
                            skillName = "虎啸震林",
                            type = SkillType.Attack,
                            mpCost = 20,
                            cooldown = 3,
                            currentCooldown = 0,
                            isFreeAction = false,
                            skillID = 701,
                            description = "单体物理伤害，系数150%"
                        });
                        break;
                    case "朱雀":
                        character.skills.Add(new Skill
                        {
                            skillName = "雀羽焚天",
                            type = SkillType.Attack,
                            mpCost = 25,
                            cooldown = 3,
                            currentCooldown = 0,
                            isFreeAction = false,
                            skillID = 702,
                            description = "单体法术伤害，系数120%，附带灼烧，持续4回合"
                        });
                        break;
                    case "玄武":
                        character.skills.Add(new Skill
                        {
                            skillName = "玄甲护体",
                            type = SkillType.Defense,
                            mpCost = 30,
                            cooldown = 3,
                            currentCooldown = 0,
                            isFreeAction = false,
                            skillID = 703,
                            description = "为自身施加一个护盾，吸收相当于自身最大生命值50%的伤害，持续4回合（不可叠加）"
                        });
                        break;
                }
            }



            if (character.targetClickButton != null)
            {
                enemyTargetIcons.Add(character.targetClickButton.gameObject);
                Debug.Log($"Added target click button for {character.characterName}, button: {character.targetClickButton.name}");
            }
            else
            {
                Debug.LogWarning($"targetClickButton is null for {character.characterName}");
            }

            enemyParty.Add(character);
        }
    }

    void Update()
    {
        if (!battleActive) return;

        foreach (var c in allCharacters)
            c.UpdateBuffs(Time.deltaTime);

        if (currentState == BattleState.Idle && !isExecutingSkill)
        {
            foreach (var c in allCharacters)
                if (!c.IsDead())
                    c.AddActionValue(Time.deltaTime);

            var readyCharacters = allCharacters.Where(c => !c.IsDead() && c.currentActionValue >= ACTION_THRESHOLD).ToList();
            if (readyCharacters.Count > 0)
            {
                readyCharacters.Sort((a, b) => b.currentActionValue.CompareTo(a.currentActionValue));

                foreach (var actor in readyCharacters)
                {
                    if (actor.isStunned)
                    {
                        actor.currentActionValue = 0f;
                        SetIconScale(actor, 1f);
                        actor.ReduceStatusDurations();
                        if (actor.stunRemaining <= 0)
                            actor.ClearStun();
                        Debug.Log($"{actor.characterName} 因眩晕无法行动，剩余 {actor.stunRemaining + 1} 回合");
                        AddTurnResultMessage($"{actor.characterName} 因眩晕无法行动");
                        continue;
                    }

                    currentActor = actor;

                    if (currentActor.defenseRemaining > 0)
                    {
                        currentActor.defenseRemaining--;
                        if (currentActor.defenseRemaining <= 0)
                        {
                            currentActor.isDefending = false;
                            currentActor.counterChance = 0f;
                            currentActor.defenseReduction = 0.5f;
                        }
                    }

                    currentActor.ApplyJinGangSanShield(this);

                    currentActor.OnTurnStart();

                    if (actor.isSleep)
                    {
                        actor.currentActionValue = 0f;
                        SetIconScale(actor, 1f);
                        actor.ReduceStatusDurations();
                        AddTurnResultMessage($"{actor.characterName} 正在睡眠，无法行动");
                        continue;
                    }
                    else if (actor.isConfused)
                    {
                        SetIconScale(actor, 1f);
                        currentActor = actor;
                        AddTurnResultMessage($"{actor.characterName} 陷入错乱，随机攻击");
                        StartCoroutine(ConfusedPlayerTurn());
                        break;
                    }

                    if (playerParty.Contains(currentActor))
                    {
                        currentActor.allowMultipleComboThisTurn = currentActor.faction == "TianWangDian";
                        currentState = BattleState.PlayerTurn;
                        hasUsedMainActionThisTurn = false;
                        GenerateSkillButtons(currentActor);
                        Debug.Log($"{currentActor.characterName} 的回合");
                        AddTurnResultMessage($"轮到 {currentActor.characterName} 的回合");
                        break;
                    }
                    else
                    {
                        currentActor.allowMultipleComboThisTurn = currentActor.faction == "TianWangDian";
                        currentState = BattleState.EnemyTurn;
                        AddTurnResultMessage($"敌人 {currentActor.characterName} 开始行动");
                        StartCoroutine(EnemyTurn());
                        break;
                    }
                }
            }
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        for (int i = 0; i < playerParty.Count; i++)
        {
            Character c = playerParty[i];
            if (c.hpSlider != null)
                c.hpSlider.value = (float)c.currentHP / c.MaxHP;
            if (c.mpSlider != null)
                c.mpSlider.value = (float)c.currentMP / c.MaxMP;
            if (c.shieldSlider != null)
            {
                float maxShield = c.MaxHP;
                float shieldValue = (c.overHealShield + c.jinGangSanShield) / maxShield;
                c.shieldSlider.value = Mathf.Clamp01(shieldValue);
            }
            if (c.actionSlider != null)
            {
                float displayValue = Mathf.Clamp01(c.currentActionValue / ACTION_THRESHOLD);
                c.actionSlider.value = displayValue;
            }

            if (c.targetIcon != null)
                c.targetIcon.SetActive(!c.IsDead());
        }

        for (int i = 0; i < enemyParty.Count; i++)
        {
            Character c = enemyParty[i];
            if (c.hpSlider != null)
                c.hpSlider.value = (float)c.currentHP / c.MaxHP;
            if (c.mpSlider != null)
                c.mpSlider.value = (float)c.currentMP / c.MaxMP;
            if (c.shieldSlider != null)
            {
                float maxShield = c.MaxHP;
                float shieldValue = (c.overHealShield + c.jinGangSanShield) / maxShield;
                c.shieldSlider.value = Mathf.Clamp01(shieldValue);
            }
            if (c.actionSlider != null)
            {
                float displayValue = Mathf.Clamp01(c.currentActionValue / ACTION_THRESHOLD);
                c.actionSlider.value = displayValue;
            }

            if (c.targetIcon != null)
                c.targetIcon.SetActive(!c.IsDead());
        }

        foreach (var c in allCharacters)
            if (c.iconImage != null)
                c.iconImage.gameObject.SetActive(!c.IsDead());

        if (uiManager != null)
            uiManager.UpdateActionQueueOrder(allCharacters);
        else
            UpdateQueueOrder();

        bool playerTurn = (currentState == BattleState.PlayerTurn) && !CheckBattleEnd();
        if (fleeButton != null) fleeButton.interactable = playerTurn;
        if (defendButton != null) defendButton.interactable = playerTurn;

        bool canCancel = (currentState == BattleState.SelectingTarget);
        if (cancelButton != null)
            cancelButton.interactable = canCancel;
    }

    void CreateActionQueueIcons()
    {
        for (int i = 0; i < maxQueueLength; i++)
        {
            GameObject icon = Instantiate(characterIconPrefab, actionQueueContainer);
            icon.SetActive(false);
            queueIcons.Add(icon);
        }
    }

    void UpdateQueueOrder()
    {
        characterIconMap.Clear();

        var living = allCharacters.Where(c => !c.IsDead()).ToList();
        if (living.Count == 0) return;

        // 用初始 predTime = (ACTION_THRESHOLD - Cur) / SPD 排序预测
        var predTime = new Dictionary<Character, float>();
        foreach (var c in living)
        {
            float remaining = ACTION_THRESHOLD - c.currentActionValue;
            predTime[c] = remaining / c.CurrentSpeed;
        }

        // 预测 maxQueueLength 次行动，记录每个位置的剩余行动值
        var predictedWithTime = new List<(Character character, float remainingTime)>();
        var workingTime = new Dictionary<Character, float>(predTime);

        while (predictedWithTime.Count < maxQueueLength)
        {
            Character next = null;
            float minTime = float.MaxValue;
            foreach (var c in living)
            {
                float t = workingTime[c];
                if (t < minTime)
                {
                    minTime = t;
                    next = c;
                }
            }
            if (next == null) break;

            predictedWithTime.Add((next, minTime));
            workingTime[next] = minTime + ACTION_THRESHOLD / next.CurrentSpeed;
        }

        for (int i = 0; i < queueIcons.Count; i++)
        {
            GameObject icon = queueIcons[i];
            if (i < predictedWithTime.Count)
            {
                var (character, remainingTime) = predictedWithTime[i];
                icon.SetActive(true);

                Transform iconTransform = icon.transform.Find("Icon");
                if (iconTransform != null)
                {
                    Image iconImage = iconTransform.GetComponent<Image>();
                    if (iconImage != null && character.queueIconSprite != null)
                    {
                        iconImage.sprite = character.queueIconSprite;
                        iconImage.gameObject.SetActive(true);
                    }
                    else if (iconImage != null)
                    {
                        iconImage.gameObject.SetActive(false);
                    }
                }

                TMP_Text tmp = icon.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                {
                    int displayVal = Mathf.RoundToInt(remainingTime * 100f);
                    tmp.text = displayVal.ToString();
                    tmp.gameObject.SetActive(true);
                }

                characterIconMap[character] = icon;
            }
            else
            {
                icon.SetActive(false);
            }
        }
    }

    void GenerateSkillButtons(Character character)
    {
        if (character.isConfused) return;

        if (skillTooltipText != null)
            skillTooltipText.gameObject.SetActive(false);

        foreach (var btn in skillButtons)
            Destroy(btn.gameObject);
        skillButtons.Clear();

        foreach (var skill in character.skills)
        {
            if (skill.currentCooldown > 0) continue;
            if (skill.mpCost > character.currentMP) continue;

            GameObject btnObj = Instantiate(skillButtonPrefab, skillButtonContainer);
            Button btn = btnObj.GetComponent<Button>();
            TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
            txt.text = $"{skill.skillName}\n{skill.mpCost}MP";

            if (btn == null)
            {
                Debug.LogError("Skill button prefab missing Button component!");
                continue;
            }

            Skill localSkill = skill;
            btn.onClick.AddListener(() => OnSkillClicked(localSkill));

            EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btnObj.AddComponent<EventTrigger>();

            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { OnSkillPointerEnter(localSkill); });
            trigger.triggers.Add(enterEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => { OnSkillPointerExit(); });
            trigger.triggers.Add(exitEntry);

            skillButtons.Add(btn);
        }
    }

    void OnSkillClicked(Skill skill)
    {
        if (skillTooltipText != null)
            skillTooltipText.gameObject.SetActive(false);

        if (currentState != BattleState.PlayerTurn) return;
        if (skill.mpCost > currentActor.currentMP) return;

        if (skill.type == SkillType.Buff && skill.isFreeAction)
        {
            AddTurnResultMessage($"{currentActor.characterName} 使用 {skill.skillName}");
            StartCoroutine(ExecuteSkill(currentActor, currentActor, skill));
            GenerateSkillButtons(currentActor);
            UpdateUI();
            return;
        }

        if (skill.skillID == 104 || skill.skillID == 204 || skill.skillID == 304)
        {
            AddTurnResultMessage($"{currentActor.characterName} 使用 {skill.skillName}");
            StartCoroutine(ExecuteSkillAndEndTurn(skill));
        }
        else
        {
            selectedSkill = skill;
            currentState = BattleState.SelectingTarget;
            Debug.Log($"Entered SelectingTarget state, skill: {skill.skillName}");
            AddTurnResultMessage($"选择 {skill.skillName} 的目标");
            EnableTargetSelectionForSkill(skill);
        }

        if (hasUsedMainActionThisTurn)
        {
            Debug.Log("本回合已使用主行动，无法再次使用");
            return;
        }
        UpdateUI();
    }

    IEnumerator ExecuteSkillAndEndTurn(Skill skill)
    {
        isExecutingSkill = true;
        yield return StartCoroutine(ExecuteSkill(currentActor, currentActor, skill));
        isExecutingSkill = false;
        hasUsedMainActionThisTurn = true;
        EndPlayerTurn();
    }

    void EnableTargetSelectionForSkill(Skill skill)
    {
        Debug.Log($"EnableTargetSelectionForSkill called, skill: {skill?.skillName}, enemy icons count: {enemyTargetIcons.Count}, player icons count: {playerTargetIcons.Count}");

        foreach (var icon in playerTargetIcons)
            if (icon != null)
            {
                Button btn = icon.GetComponent<Button>();
                btn.interactable = false;
                btn.onClick.RemoveAllListeners();
            }
        foreach (var icon in enemyTargetIcons)
            if (icon != null)
            {
                Button btn = icon.GetComponent<Button>();
                btn.interactable = false;
                btn.onClick.RemoveAllListeners();
            }

        if (skill == null) return;

        if (skill.type == SkillType.Attack || skill.type == SkillType.Control)
        {
            foreach (var icon in enemyTargetIcons)
            {
                if (icon != null)
                {
                    Button btn = icon.GetComponent<Button>();
                    btn.interactable = true;
                    Debug.Log($"Enemy icon {icon.name} set interactable = {btn.interactable}");

                    btn.onClick.RemoveAllListeners();
                    Character localTarget = icon.GetComponentInParent<Character>();
                    Debug.Log($"localTarget for {icon.name}: {(localTarget != null ? localTarget.characterName : "null")}");

                    Skill localSkill = skill;
                    btn.onClick.AddListener(() =>
                    {
                        Debug.Log($"Enemy icon {icon.name} clicked, target = {localTarget?.characterName}");
                        OnTargetSelectedForSkill(localTarget, localSkill);
                    });
                }
            }
        }
        else if (skill.type == SkillType.Buff || skill.type == SkillType.Heal || skill.type == SkillType.Defense)
        {
            foreach (var icon in playerTargetIcons)
            {
                if (icon != null)
                {
                    Button btn = icon.GetComponent<Button>();
                    btn.interactable = true;
                    Debug.Log($"Player icon {icon.name} set interactable = {btn.interactable}");

                    btn.onClick.RemoveAllListeners();
                    Character localTarget = icon.GetComponentInParent<Character>();
                    Debug.Log($"localTarget for {icon.name}: {(localTarget != null ? localTarget.characterName : "null")}");

                    btn.onClick.AddListener(() =>
                    {
                        Debug.Log($"Player icon {icon.name} clicked, target = {localTarget?.characterName}");
                        OnTargetSelectedForSkill(localTarget, skill);
                    });
                }
            }
        }
    }

    void OnTargetSelectedForSkill(Character target, Skill skill)
    {
        Debug.Log($"OnTargetSelectedForSkill called, target = {(target != null ? target.characterName : "null")}, skill = {skill.skillName}");
        if (currentState != BattleState.SelectingTarget) return;
        AddTurnResultMessage($"{currentActor.characterName} 对 {target.characterName} 使用 {skill.skillName}");
        StartCoroutine(OnTargetSelectedCoroutine(target, skill));
    }

    IEnumerator OnTargetSelectedCoroutine(Character target, Skill skill)
    {
        isExecutingSkill = true;
        yield return StartCoroutine(ExecuteSkill(currentActor, target, skill));
        isExecutingSkill = false;

        if (skill.isFreeAction)
        {
            currentState = BattleState.PlayerTurn;
            EnableTargetSelectionForSkill(null);
            GenerateSkillButtons(currentActor);
        }
        else
        {
            hasUsedMainActionThisTurn = true;

            // ★ 先执行回合结束清理（冷却、状态、临时buff、技能按钮）
            EndPlayerTurnCleanup();

            // ★ 先扣行动值（正常结束本回合的消耗）
            currentActor.SpendAction();

            // ★ 再检查风雷翼：如果有额外行动，重新填充行动条
            TryTriggerFengLeiYi(currentActor);
            if (currentActor.fengLeiYiTriggeredThisTurn)
            {
                // 已由 TryTriggerFengLeiYi 设为 ACTION_THRESHOLD
                // 不设 currentState = Idle，让 Update 循环在下一帧检测到满行动条角色
                currentState = BattleState.Idle;
            }
            else
            {
                currentState = BattleState.Idle;
            }
        }
        UpdateUI();
    }

    public void OnCancelSkill()
    {
        if (currentState != BattleState.SelectingTarget) return;

        currentState = BattleState.PlayerTurn;
        selectedSkill = null;
        EnableTargetSelectionForSkill(null);
        GenerateSkillButtons(currentActor);
        AddTurnResultMessage($"{currentActor.characterName} 取消了技能选择");
        UpdateUI();
    }

    /// <summary>
    /// 点击行动队列中的角色图标时的处理
    /// </summary>
    public void OnQueueIconClicked(Character character)
    {
        if (currentState == BattleState.SelectingTarget && character != null && !character.IsDead())
        {
            // 通过行动队列图标选择目标
            OnTargetSelectedForSkill(character, selectedSkill);
        }
    }

    IEnumerator ExecuteSkill(Character caster, Character target, Skill skill)
    {
        ResetTurnDamage();
        bool dealtDamage = false;

        caster.currentMP -= skill.mpCost;
        skill.currentCooldown = skill.cooldown;

        if (skill.skillID != 0 && skill.skillID != 101 && skill.skillID != 201 && skill.skillID != 301 && skill.skillID != 601)
        {
            caster.PlaySkillAnimation();
            yield return new WaitForSeconds(caster.skillAnimationTime);
        }

        switch (skill.skillID)
        {
            case 0:
                yield return StartCoroutine(PerformAttack(caster, target, 1.0f, false));
                caster.currentMP = Mathf.Min(caster.currentMP + Mathf.RoundToInt(caster.MaxMP * 0.1f), caster.MaxMP);
                AddTurnResultMessage($"{caster.characterName} 普通攻击回复10%内力");
                dealtDamage = true;
                break;
            case 700:   // 青龙：龙爪裂甲（系数120%，减目标20%防御力，持续3回合，近战攻击）
                yield return StartCoroutine(PerformAttack(caster, target, 1.2f, false));
                dealtDamage = true;
                target.AddOrRefreshAttributeDebuff("防御", 0.2f, 3);
                AddTurnResultMessage($"龙爪裂甲使 {target.characterName} 防御力降低20%，持续3回合");
                break;
            case 701:   // 白虎：虎啸震林（系数150%，远程攻击）
                yield return StartCoroutine(ApplyAttackToTarget(caster, target, 1.5f, false, skill.skillID));
                dealtDamage = true;
                break;
            case 702:   // 朱雀：雀羽焚天（系数120%，灼烧4回合，远程攻击）
                yield return StartCoroutine(ApplyAttackToTarget(caster, target, 1.2f, false, skill.skillID));
                dealtDamage = true;
                target.ApplyBurn(4);
                AddTurnResultMessage($"雀羽焚天使 {target.characterName} 灼烧4回合");
                break;
            case 703:   // 玄武：玄甲护体（自身护盾50%最大生命，持续4回合，不可叠加）
                {
                    dealtDamage = false;
                    int shieldAmount = Mathf.RoundToInt(caster.MaxHP * 0.5f);
                    caster.overHealShield = shieldAmount; // 覆盖而非叠加，实现"不可叠加"
                    AddTurnResultMessage($"{caster.characterName} 获得玄甲护体，吸收 {shieldAmount} 点伤害，持续4回合");
                    caster.xuanWuShieldRemainingTurns = 4;
                    break;
                }
            case 101:
                yield return StartCoroutine(TwoStrikeAttack(caster, target));
                dealtDamage = true;
                break;
            case 102:
                float confuseChance = 0.8f;
                if (caster.baGuaZhenYueActive) confuseChance += caster.controlChanceBonus;
                if (Random.value < confuseChance)
                {
                    target.ApplyConfuse(3);
                    AddTurnResultMessage($"{target.characterName} 陷入错乱");
                    caster.weiZhenBuffActive = true;
                    AddTurnResultMessage($"{caster.characterName} 威震山河成功，下次攻击时连击率+10%");
                }
                caster.currentMP = Mathf.Min(caster.currentMP + Mathf.RoundToInt(caster.MaxMP * 0.3f), caster.MaxMP);
                AddTurnResultMessage($"{caster.characterName} 回复30%内力");
                break;
            case 103:
                caster.tempComboBonus = 0.3f;
                AddTurnResultMessage($"{caster.characterName} 连击率+30%");
                break;
            case 104:
                caster.isInArrayFormation = true;
                caster.arrayFormationReduction = 0.2f;
                caster.arrayFormationCounterChance = 0.8f;
                caster.arrayFormationRemaining = 4;

                int healAmount = Mathf.RoundToInt(caster.MaxHP * 0.3f);
                caster.Heal(healAmount);
                AddTurnResultMessage($"{caster.characterName} 回复 {healAmount} 生命");

                int defBonus = Mathf.RoundToInt(caster.DEF * 0.1f);
                foreach (var ally in playerParty)
                {
                    if (ally != null && !ally.IsDead())
                    {
                        ally.extraDEF = defBonus;
                        ally.extraDEFRemaining = 4;
                    }
                }
                AddTurnResultMessage($"{caster.characterName} 为全体队友增加 {defBonus} 点防御，持续4回合");
                AddTurnResultMessage($"{caster.characterName} 进入列阵状态 (4回合, 免伤+20%, 75%反击, 团队防御+{defBonus})");
                break;
            case 201:
                float dmgMult = 1.8f;
                if (target.isStunned)
                {
                    dmgMult = 2.1f;
                    target.stunRemaining++;
                    AddTurnResultMessage($"{target.characterName} 眩晕被延长1回合");
                }
                yield return StartCoroutine(PerformAttack(caster, target, dmgMult, false));
                dealtDamage = true;
                break;
            case 202:
                float stunChance202 = 0.8f + caster.GetTotalStunChance();
                caster.ApplyDaoXuanFuSuiEffects(target, this);

                if (Random.value < stunChance202)
                {
                    target.ApplyStun(3);
                    AddTurnResultMessage($"{target.characterName} 被眩晕3回合");
                    caster.xiuLiStunBuffActive = true;
                    caster.xiuLiStunBuffRemaining = 2;
                    AddTurnResultMessage($"{caster.characterName} 袖里乾坤成功，下次攻击时晕击率+10%");
                    caster.OnStunSuccess(target, this);
                }
                caster.currentMP = Mathf.Min(caster.currentMP + Mathf.RoundToInt(caster.MaxMP * 0.3f), caster.MaxMP);
                AddTurnResultMessage($"{caster.characterName} 回复30%内力");
                break;
            case 203:
                caster.tempStunBonus = 0.3f;
                AddTurnResultMessage($"{caster.characterName} 晕击率+30%");
                break;
            case 204:
                foreach (var enemy in enemyParty.Where(e => !e.IsDead()))
                {
                    yield return StartCoroutine(ApplyAttackToTarget(caster, enemy, 1.2f, true, skill.skillID));
                }
                float pushChance = 0.8f;
                foreach (var enemy in enemyParty.Where(e => !e.IsDead()))
                {
                    float finalChance = enemy.isEliteOrBoss ? pushChance * 0.5f : pushChance;
                    if (Random.value < finalChance)
                    {
                        float pushAmount = ACTION_THRESHOLD * 0.3f;
                        enemy.currentActionValue -= pushAmount;
                        AddTurnResultMessage($"{enemy.characterName} 行动条减少30%");
                    }
                }
                caster.baGuaZhenYueActive = true;
                caster.baGuaZhenYueRemaining = 4;
                caster.controlChanceBonus = 0.1f;
                AddTurnResultMessage($"{caster.characterName} 进入镇岳状态(4回合, 晕击概率+10%)");
                dealtDamage = true;
                break;
            case 301:
                yield return StartCoroutine(PerformWisdomSword(caster, target));
                dealtDamage = true;
                break;
            case 302:
                float sleepChance = 0.8f + caster.tempStunBonus;
                if (caster.baGuaZhenYueActive) sleepChance += caster.controlChanceBonus;
                if (Random.value < sleepChance)
                {
                    target.ApplySleep(3);
                    AddTurnResultMessage($"{target.characterName} 陷入睡眠");
                    caster.chanXinCritBonus = 0.1f;
                    AddTurnResultMessage($"{caster.characterName} 禅心入梦成功，下次攻击时暴击率+10%");
                }
                caster.currentMP = Mathf.Min(caster.currentMP + Mathf.RoundToInt(caster.MaxMP * 0.3f), caster.MaxMP);
                AddTurnResultMessage($"{caster.characterName} 回复30%内力");
                break;
            case 303:
                caster.tempCritRateBonus = 0.3f;
                AddTurnResultMessage($"{caster.characterName} 暴击率+30%");
                break;
            case 304:
                caster.ClearConfuse();
                caster.ClearStun();
                caster.ClearSleep();
                caster.attributeDebuffs.Clear();
                caster.damageTakenIncrease = 0f;
                caster.damageTakenIncreaseRemaining = 0;
                caster.hitRateDecrease = 0f;
                caster.hitRateDecreaseRemaining = 0;
                caster.mingXinActive = true;
                caster.mingXinRemaining = 4;
                caster.mingXinCritRateBonus = 0.1f;
                caster.mingXinCritDamageBonus = 0.2f;
                AddTurnResultMessage($"{caster.characterName} 进入明心状态(4回合, 暴击+10%, 爆伤+20%, 暴击拉条20%)");
                break;
            case 601:   // 无极圣裁
                {
                    // 单体高伤，伤害系数200%
                    yield return StartCoroutine(PerformAttack(caster, target, 2.0f, false));
                    dealtDamage = true;

                    // 如果目标是玩家，附加推条30%和减速10%持续2回合
                    if (playerParty.Contains(target))
                    {
                        // 行动条推迟30%
                        target.currentActionValue -= ACTION_THRESHOLD * 0.3f;
                        AddTurnResultMessage($"{target.characterName} 行动条推迟30%");

                        // 减速10%：添加速度属性减益（持续2回合）
                        target.AddOrRefreshAttributeDebuff("速度", 0.1f, 2);
                        AddTurnResultMessage($"{target.characterName} 速度降低10%，持续2回合");
                    }
                    break;
                }
            case 602:   // 万仙来朝（召唤四把剑阵虚影）
                {
                    dealtDamage = false;

                    // 检查是否为通天教主
                    if (caster.characterName != "通天教主")
                    {
                        Debug.LogWarning("非通天教主尝试使用万仙来朝");
                        break;
                    }

                    // ★ 阶段检查：仅在阶段 2 或 3 可召唤
                    if (caster.currentPhase < 2)
                    {
                        AddTurnResultMessage($"{caster.characterName} 当前阶段无法使用万仙来朝");
                        break;
                    }

                    // 清除之前召唤的剑阵
                    ClearSummonedUnits(caster);

                    // 定义四把虚影的名称和预制体路径
                    string[] shadowNames = { "诛仙剑", "陷仙剑", "绝仙剑", "戮仙剑" };
                    string[] prefabPaths = { "ZhuXian", "XianXian", "JueXian", "LuXian" };

                    // 计算虚影属性：通天教主当前阶段的60%
                    float shadowHealth = caster.cachedMaxHP * 0.6f;
                    float shadowAttack = caster.cachedATK * 0.6f;
                    float shadowDefense = caster.cachedDEF * 0.6f;

                    // ★ 收集所有虚影，暂不初始化光环
                    List<Character> createdShadows = new List<Character>();

                    // 出生点参数（与 SpawnEnemies 一致）
                    float centerY = 600.0f;
                    float spacing = 160.0f;
                    float fixedX = 600.0f;

                    for (int i = 0; i < 4; i++)
                    {
                        // 加载预制体
                        GameObject prefab = CharacterPrefabDB.GetEnemyPrefab(prefabPaths[i]);
                        if (prefab == null)
                        {
                            Debug.LogError($"虚影预制体 {prefabPaths[i]} 未找到");
                            continue;
                        }

                        // 计算位置（参考 SpawnEnemies）
                        float posX = fixedX + 150;
                        float posY;
                        int displayIndex = i + 1;
                        switch (displayIndex % 2)
                        {
                            case 0: posY = centerY - displayIndex / 2 * spacing; break;
                            case 1: posY = centerY + (displayIndex + 1) / 2 * spacing; break;
                            default: posY = centerY - displayIndex / 2 * spacing; break;
                        }
                        Vector3 spawnPos = new Vector3(posX, posY, caster.visualTransform.position.z);

                        // 实例化虚影
                        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
                        Character shadow = obj.GetComponent<Character>();
                        if (shadow == null)
                        {
                            Debug.LogError($"虚影预制体缺少 Character 组件");
                            Destroy(obj);
                            continue;
                        }

                        // 设置虚影基础属性
                        shadow.characterName = shadowNames[i];
                        shadow.faction = "";
                        shadow.isEliteOrBoss = false;
                        shadow.summoner = caster;
                        shadow.level = caster.level;

                        // 强制覆盖属性（忽略计算）
                        shadow.cachedMaxHP = Mathf.RoundToInt(shadowHealth);
                        shadow.currentHP = shadow.cachedMaxHP;
                        shadow.cachedATK = Mathf.RoundToInt(shadowAttack);
                        shadow.cachedDEF = Mathf.RoundToInt(shadowDefense);
                        shadow.cachedSPD = 450f;
                        shadow.cachedHitRate = 1.0f;
                        shadow.cachedCritRate = 0.05f;
                        shadow.cachedCritDamage = 1.5f;

                        // 设置虚影被动（不灭剑意）
                        shadow.damageReductionPercent = 0.25f;
                        shadow.reflectDamagePercent = 0.05f;
                        shadow.immuneToControl = true;
                        shadow.controlToPushMultiplier = 0.5f;

                        // 添加普通攻击技能
                        shadow.skills.Clear();
                        shadow.skills.Add(new Skill
                        {
                            skillName = "普通攻击",
                            type = SkillType.Attack,
                            mpCost = 0,
                            cooldown = 0,
                            currentCooldown = 0,
                            isFreeAction = false,
                            skillID = 0,
                            description = "对敌人造成100%攻击力的伤害"
                        });

                        // 标记为自定义属性，跳过 RecalcStats
                        shadow.isCustomStats = true;

                        // 添加到战斗列表（暂不初始化光环）
                        enemyParty.Add(shadow);
                        allCharacters.Add(shadow);
                        if (shadow.targetClickButton != null)
                            enemyTargetIcons.Add(shadow.targetClickButton.gameObject);

                        // 收集到列表
                        createdShadows.Add(shadow);

                        AddTurnResultMessage($"{caster.characterName} 召唤了 {shadowNames[i]}");
                    }

                    // ★ 所有虚影添加完毕后，统一初始化光环（此时 enemyParty 已包含所有虚影）
                    foreach (var shadow in createdShadows)
                    {
                        // 初始化角色（执行 Initialize 中的动画速度等设置，但不会覆盖属性因为 isCustomStats=true）
                        shadow.Initialize();
                        // 添加光环管理脚本并初始化
                        ShadowAura aura = shadow.gameObject.AddComponent<ShadowAura>();
                        aura.Initialize(shadow, this);
                    }

                    break;
                }
            case 603:   // 混元一气
                {
                    // 造成目标最大生命值99%的伤害（可被护盾抵挡）
                    int damage = Mathf.RoundToInt(target.MaxHP * 0.99f);
                    // 由于TakeDamage会先扣除护盾，直接调用即可
                    target.TakeDamage(damage, false);
                    AddTurnResultMessage($"{caster.characterName} 释放混元一气，对 {target.characterName} 造成 {damage} 点伤害");
                    dealtDamage = true;
                    break;
                }
            default:
                Debug.LogWarning("未知技能ID");
                break;
        }

        if (dealtDamage && currentTurnDamage > 0)
        {
            ShowTurnDamage(caster, currentTurnDamage);
        }
    }

    IEnumerator PerformWisdomSword(Character caster, Character target)
    {
        // 重置暴击总伤害缓存
        caster.currentCritTotalDamage = 0;

        float finalCritRate = caster.GetFinalCritRate();
        bool guaranteedCrit = finalCritRate >= 0.75f;
        float damageMult = 1.5f;

        yield return StartCoroutine(PerformAttack(caster, target, damageMult, true, 0f, 0f, false, guaranteedCrit));

        if (guaranteedCrit && caster.lastAttackHit)
        {
            int mainDamage = caster.lastMainDamageDealt;
            if (mainDamage > 0)
            {
                int splashDamage = Mathf.RoundToInt(mainDamage * 0.6f);
                int totalSplashDamage = 0;
                foreach (var enemy in enemyParty)
                {
                    if (enemy != target && !enemy.IsDead())
                    {
                        int finalSplash = splashDamage;
                        if (enemy.isDefending)
                            finalSplash = Mathf.RoundToInt(finalSplash * (1 - enemy.defenseReduction));
                        enemy.TakeDamage(finalSplash, false);
                        AddDamageToCurrentTurn(finalSplash);
                        PlayIconHitAnimation(enemy);
                        AddTurnResultMessage($"无相慧剑溅射对 {enemy.characterName} 造成 {finalSplash} 伤害");
                        totalSplashDamage += finalSplash;
                    }
                }

                // ★ 若溅射触发暴击伤害累计，将溅射伤害也计入妙法承佑护盾
                if (caster.faction == "FangCunShan" && totalSplashDamage > 0)
                {
                    // 将溅射伤害累计到暴击总伤害，并补发护盾
                    caster.currentCritTotalDamage += totalSplashDamage;
                    int splashShield = Mathf.RoundToInt(totalSplashDamage * 0.3f);
                    foreach (var ally in playerParty)
                    {
                        if (ally != null && !ally.IsDead())
                        {
                            int newShield = Mathf.Min(ally.overHealShield + splashShield, ally.MaxHP);
                            ally.overHealShield = newShield;
                        }
                    }
                    AddTurnResultMessage($"妙法承佑溅射护盾 +{splashShield}");
                }
            }
        }
    }

    IEnumerator PerformAttack(Character attacker, Character defender, float damageMult, bool isMagic, float extraCritRate = 0f, float extraCritDamage = 0f, bool isComboAttack = false, bool forceCrit = false)
    {
        bool isPlayer = playerParty.Contains(attacker);
        Vector3 originalPos = attacker.visualTransform.position;
        Vector3 direction = (defender.visualTransform.position - attacker.visualTransform.position).normalized;
        float finalDistance = -20.0f;
        float nearDistance = 258.0f;
        if (!isPlayer)
        {
            finalDistance = 48.0f;
            nearDistance = 288.0f;
        }

        Vector3 nearPoint = defender.visualTransform.position - direction * nearDistance;
        Vector3 finalPoint = defender.visualTransform.position - direction * finalDistance;

        attacker.visualTransform.position = nearPoint;

        if (isPlayer)
        {
            attacker.PlayAttackAnimation();
        }

        float moveDuration = 0.4f;
        float elapsed = 0f;
        if (!isPlayer)
        {
            moveDuration = 0.2f;
        }
        while (elapsed < moveDuration)
        {
            attacker.visualTransform.position = Vector3.Lerp(nearPoint, finalPoint, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        attacker.visualTransform.position = finalPoint;

        if (!isPlayer)
        {
            attacker.PlayAttackAnimation();
            yield return new WaitForSeconds(attacker.attackHitTime);
        }
        else
        {
            float remainingTime = attacker.attackHitTime - moveDuration;
            if (remainingTime > 0)
                yield return new WaitForSeconds(remainingTime);
        }

        yield return StartCoroutine(ApplyAttackToTarget(attacker, defender, damageMult, isMagic, 0, extraCritRate, extraCritDamage, forceCrit));

        // 记录本次主攻击伤害（非连击）
        if (!isComboAttack)
        {
            attacker.lastMainDamageDealt = attacker.lastDamageDealt;
        }

        if (CheckBattleEnd()) yield break;

        defender.CheckHeartMirror(this);

        bool wasComboTriggered = false;

        if (!isComboAttack)
        {
            if (attacker.allowMultipleComboThisTurn)
            {
                int maxCombo = 10;
                for (int i = 0; i < maxCombo; i++)
                {
                    float currentComboChance = attacker.GetTotalComboChance();
                    AddTurnResultMessage($"连击概率：{currentComboChance * 100.0f}% ");
                    if (currentComboChance <= 0f) break;
                    if (Random.value >= currentComboChance) break;

                    AddTurnResultMessage($"{attacker.characterName} 触发连击！");
                    wasComboTriggered = true;
                    attacker.OnComboTriggered(this);
                    attacker.OnComboTriggeredForLianFeng(this);

                    Character comboDefender = defender;
                    if (comboDefender.IsDead())
                    {
                        var aliveEnemies = enemyParty.Where(e => !e.IsDead()).ToList();
                        if (aliveEnemies.Count == 0) break;
                        comboDefender = aliveEnemies[Random.Range(0, aliveEnemies.Count)];
                    }

                    attacker.visualTransform.position = originalPos;
                    yield return StartCoroutine(PerformAttack(attacker, comboDefender, 1.0f, isMagic, 0f, 0f, true));
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                float currentComboChance = attacker.GetTotalComboChance();
                if (Random.value < currentComboChance)
                {
                    AddTurnResultMessage($"{attacker.characterName} 触发连击！");
                    attacker.OnComboTriggered(this);

                    Character comboDefender = defender;
                    if (comboDefender.IsDead())
                    {
                        var aliveEnemies = enemyParty.Where(e => !e.IsDead()).ToList();
                        if (aliveEnemies.Count > 0)
                            comboDefender = aliveEnemies[Random.Range(0, aliveEnemies.Count)];
                        else
                            yield break;
                    }

                    attacker.visualTransform.position = originalPos;
                    yield return new WaitForSeconds(0.1f);
                    yield return StartCoroutine(PerformAttack(attacker, comboDefender, 1.0f, isMagic, 0f, 0f, true));
                }
            }

            // 保底连击
            if (attacker.allowMultipleComboThisTurn && !isComboAttack && !hasTriggeredGuaranteedComboThisTurn && !wasComboTriggered)
            {
                float totalComboChance = attacker.GetTotalComboChance();
                if (totalComboChance >= 0.5f)
                {
                    hasTriggeredGuaranteedComboThisTurn = true;
                    AddTurnResultMessage($"{attacker.characterName} 蓄势待发：触发保底连击！");
                    attacker.OnComboTriggered(this);
                    attacker.OnComboTriggeredForLianFeng(this);

                    Character comboDefender = defender;
                    if (comboDefender.IsDead())
                    {
                        var aliveEnemies = enemyParty.Where(e => !e.IsDead()).ToList();
                        if (aliveEnemies.Count == 0) yield break;
                        comboDefender = aliveEnemies[Random.Range(0, aliveEnemies.Count)];
                    }

                    attacker.visualTransform.position = originalPos;
                    yield return new WaitForSeconds(0.1f);
                    yield return StartCoroutine(PerformAttack(attacker, comboDefender, 1.0f, isMagic, 0f, 0f, true));
                }
            }
        }

        if (defender.isSleep)
        {
            defender.ClearSleep();
            AddTurnResultMessage($"{defender.characterName} 从睡眠中醒来");
        }

        attacker.visualTransform.position = originalPos;

        // 原有反击（例如敌人自带反击）
        float counterProbability = 0f;
        if (defender.isInArrayFormation)
            counterProbability = defender.arrayFormationCounterChance;
        else
            counterProbability = defender.counterChance;
        if (!attacker.IsDead() && counterProbability > 0 && Random.value < counterProbability)
        {
            AddTurnResultMessage($"{defender.characterName} 触发反击！");
            yield return StartCoroutine(PerformCounter(defender, attacker));
        }

        // 列阵反击：若攻击者是敌人，且被攻击的目标是我方角色，则我方列阵角色进行反击
        if (playerParty.Contains(attacker) == false && playerParty.Contains(defender))
        {
            foreach (var ally in playerParty)
            {
                // 关键修复：跳过防御者自身，避免与第一段反击重复
                if (ally == defender) continue;

                if (ally.isInArrayFormation && ally.arrayFormationCounterChance > 0)
                {
                    if (Random.value < ally.arrayFormationCounterChance)
                    {
                        AddTurnResultMessage($"{ally.characterName} 触发列阵反击！");
                        yield return StartCoroutine(PerformCounter(ally, attacker));
                        break;
                    }
                }
            }
        }
    }

    IEnumerator ApplyAttackToTarget(Character attacker, Character defender, float damageMult, bool isMagic, int skillID, float extraCritRate = 0f, float extraCritDamage = 0f, bool forceCrit = false)
    {
        bool isPlayer = playerParty.Contains(attacker);
        if (attacker.faction == "WuZhuangGuan" && !attacker.IsDead())
        {
            attacker.ApplyDaoXuanFuSuiEffects(defender, this);
        }

        float hitChance = attacker.HitRate - defender.EvasionRate;
        bool isHit = Random.value < hitChance;
        if (!isHit)
        {
            yield return defender.PlayEvasionAnimationCoroutine();
            attacker.lastAttackHit = false;
            AddTurnResultMessage($"{attacker.characterName} 攻击 {defender.characterName} 未命中");
            yield break;
        }

        attacker.lastAttackHit = true;

        bool isDefendingAnim = defender.isDefending || defender.jinGangSanShield > 0;
        yield return defender.PlayHitAnimationCoroutine(isDefendingAnim);

        float ignoreDefPercent = 0f;
        ignoreDefPercent += attacker.lianFengArmorPen;

        float finalDamageMult = damageMult;
        if (attacker.HasArtifactEffect(ArtifactEffect.PoJunFu) && (float)defender.currentHP / defender.MaxHP > 0.7f)
        {
            finalDamageMult *= 1.3f;
            AddTurnResultMessage("破军斧增伤30%");
        }

        float effectiveDef = defender.DEF * (1f - Mathf.Clamp01(ignoreDefPercent));

        int baseDamage = Mathf.Max(1, attacker.GetFinalATK() - Mathf.RoundToInt(effectiveDef));
        int finalDamage = Mathf.RoundToInt(baseDamage * finalDamageMult);

        float stunDamageMult = attacker.GetDamageMultiplierAgainstStunned(defender);
        finalDamage = Mathf.RoundToInt(finalDamage * stunDamageMult);

        finalDamage = Mathf.RoundToInt(finalDamage * (1f + attacker.lianFengDamageBonus));

        // ★ 妙法承佑：最终伤害加成（每层+20%，上限60%，独立乘算，不参与攻击力计算）
        float miaoFaDamageBonus = attacker.faction == "FangCunShan" ? attacker.nextAttackDamageBonusStacks * 0.20f : 0f;
        if (miaoFaDamageBonus > 0f)
        {
            finalDamage = Mathf.RoundToInt(finalDamage * (1f + miaoFaDamageBonus));
        }

        if (attacker.faction == "WuZhuangGuan")
        {
            int debuffCount = defender.attributeDebuffs.Count;
            float daoXuanBonus = Mathf.Min(debuffCount * 0.15f, 0.75f);
            if (daoXuanBonus > 0)
            {
                finalDamage = Mathf.RoundToInt(finalDamage * (1f + daoXuanBonus));
                AddTurnResultMessage($"道玄缚祟：目标有{debuffCount}个负面效果，伤害提高{daoXuanBonus * 100:F0}%");
            }
        }

        float finalCritRate = attacker.GetFinalCritRate() + extraCritRate;
        float finalCritDamage = attacker.GetFinalCritDamage() + extraCritDamage;

        // ★ 暴击前消耗妙法承佑层数（让本次攻击带着旧层数加成打完伤害，然后消耗掉）
        if (attacker.faction == "FangCunShan")
        {
            attacker.ConsumeMiaoFaBonus();
        }

        bool isCrit = forceCrit || (Random.value < finalCritRate);

        if (isCrit)
        {
            if (attacker.faction == "FangCunShan")
            {
                float newIgnore = ignoreDefPercent + 0.1f;
                float newEffectiveDef = defender.DEF * (1f - Mathf.Clamp01(newIgnore));
                int newBaseDamage = Mathf.Max(1, attacker.GetFinalATK() - Mathf.RoundToInt(newEffectiveDef));
                finalDamage = Mathf.RoundToInt(newBaseDamage * finalDamageMult);
            }

            finalDamage = Mathf.RoundToInt(finalDamage * finalCritDamage);
            AddTurnResultMessage($"暴击！");
            attacker.OnCrit(defender, this, skillID);
        }

        bool xuanBingJiaTriggered = false;
        float originalDamage = finalDamage;
        if (defender.HasArtifactEffect(ArtifactEffect.XuanBingJia) && Random.value < 0.2f)
        {
            finalDamage = Mathf.RoundToInt(finalDamage * 0.5f);
            xuanBingJiaTriggered = true;
            AddTurnResultMessage("玄冰甲触发，伤害减半");
        }

        // ★ 应用通用免伤（damageReductionPercent）
        float finalReduction = 0f;
        if (defender.isInArrayFormation)
            finalReduction += defender.arrayFormationReduction;
        else if (defender.isDefending)
            finalReduction += defender.defenseReduction;
        finalReduction += defender.damageReductionPercent;
        if (finalReduction > 0f)
            finalDamage = Mathf.RoundToInt(finalDamage * (1 - finalReduction));

        // ★ 玄武镇岳阵分摊：四神兽受到伤害时，50%由玄武承担（玄武自身不受分摊）
        bool isPlayerAttacking = playerParty.Contains(attacker);

        // ★ 妙法承佑护盾需要基于分摊前的总伤害，所以在分摊前缓存
        int preSharingDamage = finalDamage;

        if (isPlayerAttacking && FourSymbolAura.HasTurtleAura(this) &&
            defender.characterName != "玄武" &&
            (defender.characterName == "青龙" || defender.characterName == "白虎" || defender.characterName == "朱雀"))
        {
            Character xuanwu = null;
            foreach (var e in enemyParty)
            {
                if (e.characterName == "玄武" && !e.IsDead())
                {
                    xuanwu = e;
                    break;
                }
            }
            if (xuanwu != null)
            {
                int sharedDamage = Mathf.RoundToInt(finalDamage * 0.5f);
                int actualDamage = finalDamage - sharedDamage;
                // 玄武承担部分
                xuanwu.TakeDamage(sharedDamage, isCrit);
                AddTurnResultMessage($"玄武镇岳阵分摊 {sharedDamage} 点伤害");
                // 目标实际只受一半伤害
                finalDamage = actualDamage;
            }
        }

        defender.TakeDamage(finalDamage, isCrit);
        AddDamageToCurrentTurn(finalDamage);
        attacker.lastDamageDealt = finalDamage;

        // ★ 攻击命中后消耗"下次攻击加成"buff（威震山河/袖里乾坤/禅心入梦/狮子搏兔）
        if (isPlayer && attacker.lastAttackHit)
        {
            attacker.ConsumeNextAttackBonus();
        }
        if (attacker == currentActor && !isPlayer && attacker.lastAttackHit)
        {
            attacker.ConsumeNextAttackBonus();
        }

        PlayIconHitAnimation(defender);
        AddTurnResultMessage($"{attacker.characterName} 造成 {finalDamage} 伤害");

        // ★ 反弹伤害（reflectDamagePercent）
        if (defender.reflectDamagePercent > 0 && !attacker.IsDead())
        {
            int reflectDamage = Mathf.RoundToInt(finalDamage * defender.reflectDamagePercent);
            attacker.TakeDamage(reflectDamage, false);
            AddTurnResultMessage($"{defender.characterName} 反弹 {reflectDamage} 伤害给 {attacker.characterName}");
        }

        if (isCrit && attacker.faction == "FangCunShan")
        {
            int shieldAmount = Mathf.RoundToInt(preSharingDamage * 0.3f);
            attacker.currentCritTotalDamage += preSharingDamage;
            foreach (var ally in playerParty)
            {
                if (ally != null && !ally.IsDead())
                {
                    int newShield = Mathf.Min(ally.overHealShield + shieldAmount, ally.MaxHP);
                    ally.overHealShield = newShield;
                }
            }
            AddTurnResultMessage($"妙法承佑为全体提供 {shieldAmount} 点护盾");
        }

        defender.OnAttacked(attacker, this);

        if (xuanBingJiaTriggered)
        {
            int reflectDamage = Mathf.RoundToInt(originalDamage * 0.5f);
            if (attacker.isDefending)
                reflectDamage = Mathf.RoundToInt(reflectDamage * (1 - attacker.defenseReduction));
            attacker.TakeDamage(reflectDamage, isCrit);
            PlayIconHitAnimation(attacker);
            AddTurnResultMessage($"玄冰甲反伤 {reflectDamage} 给 {attacker.characterName}");
        }

        if (attacker.HasArtifactEffect(ArtifactEffect.LunHuiJing))
        {
            int heal = Mathf.RoundToInt(finalDamage * 0.2f);
            int actualHeal = attacker.Heal(heal);
            AddTurnResultMessage($"轮回镜吸血 {heal}{(heal > actualHeal ? $"，溢出{heal - actualHeal}转化为护盾" : "")}");
        }

        if (attacker.HasArtifactEffect(ArtifactEffect.ZhenHunFan))
        {
            float stunChance = 0.15f;
            if (defender.isEliteOrBoss) stunChance *= 0.5f;
            if (Random.value < stunChance)
            {
                defender.ApplyStun(1);
                AddTurnResultMessage($"镇魂幡触发，{defender.characterName} 眩晕");
                attacker.OnStunSuccess(defender, this);
            }
        }

        if (defender.IsDead())
        {
            // ★ 四象灵尊阵亡惩罚：其余存活神兽速度提升25%（可叠加最多3层，加算）
            if (defender.characterName == "青龙" || defender.characterName == "白虎" ||
                defender.characterName == "朱雀" || defender.characterName == "玄武")
            {
                int aliveSymbolCount = 0;
                foreach (var e in enemyParty)
                {
                    if (e != defender && !e.IsDead() &&
                        (e.characterName == "青龙" || e.characterName == "白虎" ||
                         e.characterName == "朱雀" || e.characterName == "玄武"))
                    {
                        aliveSymbolCount++;
                        // 加算：每层+25%，上限75%（3层）
                        e.deathPenaltySpeedBonus = Mathf.Min(e.deathPenaltySpeedBonus + 0.25f, 0.75f);
                    }
                }
                if (aliveSymbolCount > 0)
                {
                    AddTurnResultMessage($"{defender.characterName} 阵亡，其余神兽速度提升25%（当前层数：{enemyParty.FirstOrDefault(e => !e.IsDead() && (e.characterName == "青龙" || e.characterName == "白虎" || e.characterName == "朱雀" || e.characterName == "玄武"))?.deathPenaltySpeedBonus * 100 ?? 0:F0}%）");
                    // 移除四神兽光环
                    FourSymbolAura aura = defender.GetComponent<FourSymbolAura>();
                    if (aura != null)
                        Destroy(aura);
                }
            }

            foreach (var art in attacker.equippedArtifacts)
            {
                if (art != null && art.artifactEffect == ArtifactEffect.LingFengPei)
                {
                    attacker.windWingStacks = Mathf.Min(attacker.windWingStacks + 1, 5);
                    AddTurnResultMessage($"灵风佩层数+1，当前 {attacker.windWingStacks} 层");
                    break;
                }
            }

            // ★ 虚影死亡时：通天教主扣血加攻
            if (defender.summoner != null && defender.summoner.characterName == "通天教主")
            {
                Character tongTian = defender.summoner;
                // 扣除最大生命值15%（不会致死）
                int hpLoss = Mathf.RoundToInt(tongTian.MaxHP * 0.10f);
                tongTian.currentHP -= hpLoss;
                if (tongTian.currentHP < 1) tongTian.currentHP = 1;
                AddTurnResultMessage($"{defender.characterName} 被消灭，{tongTian.characterName} 损失 {hpLoss} 生命");

                // 增加攻击力层数（最多4层）
                tongTian.shadowDeathAttackBonusStacks = Mathf.Min(tongTian.shadowDeathAttackBonusStacks + 1, 4);
                tongTian.shadowDeathAttackBonusValue = tongTian.shadowDeathAttackBonusStacks * 0.05f;
                AddTurnResultMessage($"{tongTian.characterName} 攻击力提升5%（当前 {tongTian.shadowDeathAttackBonusStacks} 层）");
            }
        }

        if (attacker.HasArtifactEffect(ArtifactEffect.FenTianZhu) && Random.value < 0.2f)
        {
            int splashDamage = Mathf.RoundToInt(finalDamage * 0.5f);
            foreach (var enemy in enemyParty)
            {
                if (enemy != defender && !enemy.IsDead())
                {
                    int targetDamage = splashDamage;
                    if (enemy.isDefending)
                        targetDamage = Mathf.RoundToInt(targetDamage * (1 - enemy.defenseReduction));
                    enemy.TakeDamage(targetDamage, isCrit);
                    AddDamageToCurrentTurn(targetDamage);
                    PlayIconHitAnimation(enemy);
                    AddTurnResultMessage($"溅射对 {enemy.characterName} 造成 {targetDamage} 伤害");
                }
            }
        }

        if (attacker.HasArtifactEffect(ArtifactEffect.LeiShenChui) && Random.value < 0.1f)
        {
            int chainDamage = Mathf.RoundToInt(finalDamage * 0.5f);
            var otherEnemies = enemyParty.Where(e => e != defender && !e.IsDead()).ToList();
            int count = Mathf.Min(2, otherEnemies.Count);
            for (int i = 0; i < count; i++)
            {
                if (otherEnemies.Count == 0) break;
                int idx = Random.Range(0, otherEnemies.Count);
                var target = otherEnemies[idx];
                int targetDamage = chainDamage;
                if (target.isDefending)
                    targetDamage = Mathf.RoundToInt(targetDamage * (1 - target.defenseReduction));
                target.TakeDamage(targetDamage, isCrit);
                AddDamageToCurrentTurn(targetDamage);
                PlayIconHitAnimation(target);
                AddTurnResultMessage($"连锁闪电对 {target.characterName} 造成 {targetDamage} 伤害");
                otherEnemies.RemoveAt(idx);
            }
        }

        if (defender.isSleep)
        {
            defender.ClearSleep();
            AddTurnResultMessage($"{defender.characterName} 从睡眠中醒来");
        }

        // 目前仅限敌人有结界
        if (!isPlayer)
        {
            if (ShadowAura.HasJueXianAura(attacker, this))
            {
                defender.ApplyBurn(2);
                AddTurnResultMessage($"{defender.characterName} 受到灼烧效果");
            }
            if (ShadowAura.HasLuXianAura(attacker, this))
            {
                defender.ApplyPoison(2);
                AddTurnResultMessage($"{defender.characterName} 受到中毒效果");
            }
        }

        // ★ 四象灵尊光环与被动效果
        // 1. 朱雀焚野阵：全体友方攻击时附带灼烧（朱雀存活时）
        if (!isPlayer && FourSymbolAura.HasBirdAura(this))
        {
            defender.ApplyBurn(2);
            AddTurnResultMessage($"焚野阵触发，{defender.characterName} 受到灼烧效果");
        }

        // 2. 青龙御风阵：全体友方攻击时减少目标10%行动条（青龙存活时）
        if (!isPlayer && FourSymbolAura.HasDragonAura(this))
        {
            float pushAmount = ACTION_THRESHOLD * 0.1f;
            defender.currentActionValue -= pushAmount;
            AddTurnResultMessage($"御风阵触发，{defender.characterName} 行动条减少10%");
        }

        // 3. 青龙被动（龙之逆鳞）：攻击时减少目标10%行动条
        if (!isPlayer && attacker.characterName == "青龙")
        {
            float pushAmount = ACTION_THRESHOLD * 0.1f;
            defender.currentActionValue -= pushAmount;
            AddTurnResultMessage($"龙之逆鳞触发，{defender.characterName} 行动条减少10%");
        }

        // 4. 白虎被动（虎煞噬魂）：暴击时额外造成目标最大生命值15%的真实伤害
        if (!isPlayer && attacker.characterName == "白虎" && isCrit)
        {
            int trueDamage = Mathf.RoundToInt(defender.MaxHP * 0.15f);
            defender.TakeDamage(trueDamage, false);
            AddDamageToCurrentTurn(trueDamage);
            AddTurnResultMessage($"虎煞噬魂触发，造成 {trueDamage} 点真实伤害");
        }

        // 6. 青龙被动：龙之逆鳞 — 每3次主动攻击后追加一次普通攻击，并填充30%行动条
        // （计数在 EnemyTurn 的 qingLongConsecutiveAttacks 中处理，这里只标记触发）
        if (!isPlayer && attacker.characterName == "青龙")
        {
            attacker.qingLongConsecutiveAttacks++;
        }

        // ★ 晕击判定（所有门派，仅对非控制类技能生效，避免与控制技能重复）
        bool isControlSkill = (skillID == 102 || skillID == 202 || skillID == 302); // 威震山河、袖里乾坤、禅心入梦
        if (!isControlSkill)
        {
            float stunChance = attacker.GetTotalStunChance();
            if (Random.value < stunChance)
            {
                if (defender.immuneToControl)
                {
                    float pushAmount = ACTION_THRESHOLD * defender.controlToPushMultiplier * 1; // 眩晕持续1回合
                    defender.currentActionValue -= pushAmount;
                    AddTurnResultMessage($"{attacker.characterName} 晕击触发，{defender.characterName} 免疫眩晕，行动条推迟{pushAmount}");
                }
                else
                {
                    defender.ApplyStun(1);
                    AddTurnResultMessage($"{attacker.characterName} 晕击触发，{defender.characterName} 眩晕1回合");
                    // 只有五庄观触发门派被动（天地同寿、镇岳等）
                    if (attacker.faction == "WuZhuangGuan")
                    {
                        attacker.OnStunSuccess(defender, this);
                    }
                }
            }
        }

    }

    IEnumerator PerformCounter(Character counter, Character target)
    {
        if (target == null || target.IsDead()) yield break;
        bool isPlayer = playerParty.Contains(counter);
        Vector3 originalPos = counter.visualTransform.position;
        Vector3 direction = (target.visualTransform.position - counter.visualTransform.position).normalized;
        float finalDistance = -20.0f;
        float nearDistance = 258.0f;
        Vector3 nearPoint = target.visualTransform.position - direction * nearDistance;
        Vector3 finalPoint = target.visualTransform.position - direction * finalDistance;

        counter.visualTransform.position = nearPoint;

        if (!isPlayer)
        {
            finalDistance = 48.0f;
            nearDistance = 288.0f;
        }

        if (isPlayer)
        {
            counter.PlayAttackAnimation();
        }

        float moveDuration = 0.4f;
        float elapsed = 0f;
        if (!isPlayer)
        {
            moveDuration = 0.2f;
        }
        while (elapsed < moveDuration)
        {
            counter.visualTransform.position = Vector3.Lerp(nearPoint, finalPoint, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        counter.visualTransform.position = finalPoint;

        if (!isPlayer)
        {
            counter.PlayAttackAnimation();
            yield return new WaitForSeconds(counter.attackHitTime);
        }
        else
        {
            float remainingTime = counter.attackHitTime - moveDuration;
            if (remainingTime > 0)
                yield return new WaitForSeconds(remainingTime);
        }

        if (counter.faction == "WuZhuangGuan" && !counter.IsDead())
        {
            counter.ApplyDaoXuanFuSuiEffects(target, this);
        }

        float counterDamageMult = 0.75f;

        int counterDamage = Mathf.RoundToInt(counter.GetFinalATK() * counterDamageMult);
        // 固定伤害，不再额外计算防御减免（但目标减伤效果仍会生效）

        // ★ 玄武镇岳阵分摊：反击时，四神兽受到伤害50%由玄武承担（玄武自身不受分摊）
        if (target.characterName != "玄武" &&
            (target.characterName == "青龙" || target.characterName == "白虎" || target.characterName == "朱雀"))
        {
            Character xuanwu = null;
            foreach (var e in enemyParty)
            {
                if (e.characterName == "玄武" && !e.IsDead())
                {
                    xuanwu = e;
                    break;
                }
            }
            if (xuanwu != null && FourSymbolAura.HasTurtleAura(this))
            {
                int sharedDamage = Mathf.RoundToInt(counterDamage * 0.5f);
                int actualDamage = counterDamage - sharedDamage;
                xuanwu.TakeDamage(sharedDamage, false);
                AddTurnResultMessage($"玄武镇岳阵分摊 {sharedDamage} 点反击伤害");
                counterDamage = actualDamage;
            }
        }

        target.TakeDamage(counterDamage, false);
        PlayIconHitAnimation(target);
        AddTurnResultMessage($"{counter.characterName} 反击造成 {counterDamage} 伤害");

        // ★ 迅疾如风：反击后使自身行动提前10%（天王殿专属）
        if (counter.faction == "TianWangDian")
        {
            float pushAmount = ACTION_THRESHOLD * 0.1f;
            counter.currentActionValue += pushAmount;
            AddTurnResultMessage($"{counter.characterName} 迅疾如风触发，反击后行动提前10%");
        }

        // ★ 反击击杀灵风佩叠层 ★
        if (target.IsDead())
        {
            foreach (var art in counter.equippedArtifacts)
            {
                if (art != null && art.artifactEffect == ArtifactEffect.LingFengPei)
                {
                    counter.windWingStacks = Mathf.Min(counter.windWingStacks + 1, 5);
                    AddTurnResultMessage($"灵风佩层数+1，当前 {counter.windWingStacks} 层");
                    break;
                }
            }
        }

        ShowTurnDamage(counter, counterDamage);

        if (target.isDefending || target.jinGangSanShield > 0)
            target.PlayDefenseAnimation();
        else
            target.PlayHitAnimation();

        yield return new WaitForSeconds(0.3f);

        counter.visualTransform.position = originalPos;
    }

    public IEnumerator PerformAdditionalAttack(Character attacker, Character defender, float damageMult, bool isMagic)
    {
        yield return StartCoroutine(ApplyAttackToTarget(attacker, defender, damageMult, isMagic, 0));
    }

    IEnumerator TwoStrikeAttack(Character attacker, Character defender)
    {
        yield return StartCoroutine(PerformAttack(attacker, defender, 1.0f, false));
        bool firstHit = attacker.lastAttackHit;

        yield return new WaitForSeconds(0.1f);

        Character secondTarget = defender;
        if (defender.IsDead())
        {
            var aliveEnemies = enemyParty.Where(e => !e.IsDead()).ToList();
            if (aliveEnemies.Count > 0)
                secondTarget = aliveEnemies[Random.Range(0, aliveEnemies.Count)];
            else
                yield break;
        }

        yield return StartCoroutine(PerformAttack(attacker, secondTarget, 1.0f, false));
        bool secondHit = attacker.lastAttackHit;

        if (firstHit && secondHit)
        {
            attacker.lionComboBuffActive = true;
            AddTurnResultMessage($"{attacker.characterName} 狮子搏兔两次命中，下次攻击时连击率+10%！");
        }
    }

    public void OnDefend()
    {
        currentActor.defenseReduction = 0.5f;
        currentActor.counterChance = 0f;
        currentActor.defenseRemaining = 1;
        currentActor.isImmobilizeDefense = false;
        AddTurnResultMessage($"{currentActor.characterName} 进入防御状态");
        hasUsedMainActionThisTurn = true;

        // ★ 先执行回合结束清理
        EndPlayerTurnCleanup();
        // ★ 先扣行动值
        currentActor.SpendAction();
        // ★ 再检查风雷翼
        TryTriggerFengLeiYi(currentActor);
        currentState = BattleState.Idle;
        UpdateUI();
    }

    public void OnFlee()
    {
        if (currentState != BattleState.PlayerTurn) return;
        if (hasUsedMainActionThisTurn)
        {
            Debug.Log("本回合已使用主行动，无法逃跑");
            return;
        }
        float chance = fleeBaseChance + (currentActor.CurrentSpeed / ACTION_THRESHOLD);
        Debug.Log($"逃跑概率是：{chance}");
        if (Random.value < chance)
        {
            battleActive = false;
            battleResultText.text = "逃跑成功！";
            battleResultText.gameObject.SetActive(true);
            currentState = BattleState.BattleEnd;
            DisableAllButtons();
            AddTurnResultMessage($"{currentActor.characterName} 逃跑成功");
            if (!isLoadingScene) StartCoroutine(ReturnToDemonTowerAfterDelay(2f));
        }
        else
        {
            AddTurnResultMessage($"{currentActor.characterName} 逃跑失败");
            hasUsedMainActionThisTurn = true;

            // ★ 先执行回合结束清理
            EndPlayerTurnCleanup();
            // ★ 先扣行动值
            currentActor.SpendAction();
            // ★ 再检查风雷翼
            TryTriggerFengLeiYi(currentActor);
            currentState = BattleState.Idle;
        }
        UpdateUI();
    }

    void TryTriggerFengLeiYi(Character character)
    {
        if (character.fengLeiYiTriggeredThisTurn) return;
        if (character.HasArtifactEffect(ArtifactEffect.FengLeiYi) && Random.value < 0.1f)
        {
            character.fengLeiYiTriggeredThisTurn = true;
            // 风雷翼：额外获得一次行动，直接填充行动条（不经过 SpendAction 扣除）
            character.currentActionValue = ACTION_THRESHOLD;
            AddTurnResultMessage($"{character.characterName} 触发风雷翼，获得额外行动！");
        }
    }

    void EndPlayerTurn()
    {
        EndPlayerTurnCleanup();
        currentActor.SpendAction();
        hasTriggeredGuaranteedComboThisTurn = false;
        currentState = BattleState.Idle;
    }

    /// <summary>
    /// 玩家回合结束的清理工作（不包含 SpendAction 和状态切换），
    /// 风雷翼触发时也需要调用。
    /// </summary>
    void EndPlayerTurnCleanup()
    {
        if (skillTooltipText != null)
            skillTooltipText.gameObject.SetActive(false);

        currentActor.ReduceCooldowns();
        currentActor.ReduceStatusDurations();
        currentActor.ClearTempBuffs();

        foreach (var btn in skillButtons)
            Destroy(btn.gameObject);
        skillButtons.Clear();

        SetIconScale(currentActor, 1f);
        hasTriggeredGuaranteedComboThisTurn = false;
    }

    IEnumerator EnemyTurn()
    {
        if (currentActor.defenseRemaining > 0)
        {
            currentActor.defenseRemaining--;
            if (currentActor.defenseRemaining <= 0)
            {
                currentActor.isDefending = false;
                currentActor.counterChance = 0f;
                currentActor.defenseReduction = 0.5f;
            }
        }

        yield return new WaitForSeconds(0.5f);

        ResetTurnDamage();

        // 处理蓄力状态
        if (currentActor.isCharging)
        {
            currentActor.currentChargeTurns--;
            if (currentActor.currentChargeTurns <= 0)
            {
                currentActor.isCharging = false;
                AddTurnResultMessage($"{currentActor.characterName} 蓄力完成！");
                Character target = playerParty.FirstOrDefault(p => !p.IsDead());
                if (target != null && currentActor.chargedSkill != null)
                {
                    yield return StartCoroutine(ExecuteSkill(currentActor, target, currentActor.chargedSkill));
                }
                currentActor.chargedSkill = null;
            }
            else
            {
                AddTurnResultMessage($"{currentActor.characterName} 蓄力中... ({currentActor.currentChargeTurns}回合)");
            }

            if (CheckBattleEnd()) yield break;
            currentActor.ReduceCooldowns();
            currentActor.ReduceStatusDurations();
            SetIconScale(currentActor, 1f);
            currentActor.SpendAction();
            currentState = BattleState.Idle;
            UpdateUI();
            yield break;
        }

        // 非蓄力状态：选择目标
        List<Character> possibleTargets;
        if (currentActor.isConfused)
            possibleTargets = allCharacters.Where(c => !c.IsDead() && c != currentActor).ToList();
        else
            possibleTargets = playerParty.Where(p => !p.IsDead()).ToList();

        if (possibleTargets.Count == 0)
        {
            currentActor.SpendAction();
            currentState = BattleState.Idle;
            UpdateUI();
            yield break;
        }

        Character finalTarget = possibleTargets[Random.Range(0, possibleTargets.Count)];
        Skill selectedSkill = null;

        // 通天教主特殊AI（编号601-603）
        if (currentActor.characterName == "通天教主")
        {
            int phase = GetTongTianPhase(currentActor);
            // ★ 修改：只有阶段 2 或 3 才考虑使用万仙来朝（602）
            Skill wanXianSkill = null;
            if (phase >= 2)
            {
                wanXianSkill = currentActor.skills.FirstOrDefault(s => s.skillID == 602 && s.currentCooldown == 0);
            }

            if (wanXianSkill != null)
            {
                selectedSkill = wanXianSkill;
            }
            else if (phase == 3)
            {
                // 三阶段：其次使用混元一气
                var hunYuanSkill = currentActor.skills.FirstOrDefault(s => s.skillID == 603 && s.currentCooldown == 0);
                if (hunYuanSkill != null)
                    selectedSkill = hunYuanSkill;
                else
                    selectedSkill = currentActor.skills.FirstOrDefault(s => s.skillID == 601 && s.currentCooldown == 0);
            }
            else if (phase == 2)
            {
                // 二阶段：万仙来朝已优先，其次无极圣裁
                selectedSkill = currentActor.skills.FirstOrDefault(s => s.skillID == 601 && s.currentCooldown == 0);
            }
            else // phase == 1
            {
                selectedSkill = currentActor.skills.FirstOrDefault(s => s.skillID == 601 && s.currentCooldown == 0);
            }
        }

        // ★ 四象灵尊AI
        if (currentActor.characterName == "青龙" || currentActor.characterName == "白虎" ||
            currentActor.characterName == "朱雀" || currentActor.characterName == "玄武")
        {
            // 优先使用专属技能（skillID 700-703），其次普通攻击
            var specialSkill = currentActor.skills.FirstOrDefault(
                s => s.skillID >= 700 && s.skillID <= 703 && s.currentCooldown == 0 && currentActor.currentMP >= s.mpCost);
            if (specialSkill != null)
                selectedSkill = specialSkill;
        }

        // ★ 错乱状态：无法使用主动技能，强制普攻
        if (currentActor.isConfused)
        {
            selectedSkill = null;
        }

        if (selectedSkill != null)
        {
            // 处理蓄力技能（万仙来朝和混元一气需要蓄力）
            if (selectedSkill.skillID == 602) // 万仙来朝
            {
                currentActor.isCharging = true;
                currentActor.currentChargeTurns = 1;
                currentActor.chargedSkill = selectedSkill;
                AddTurnResultMessage($"{currentActor.characterName} 开始蓄力：{selectedSkill.skillName} (1回合)");
            }
            else if (selectedSkill.skillID == 603) // 混元一气
            {
                currentActor.isCharging = true;
                currentActor.currentChargeTurns = 2;
                currentActor.chargedSkill = selectedSkill;
                AddTurnResultMessage($"{currentActor.characterName} 开始蓄力：{selectedSkill.skillName} (2回合)");
            }
            else
            {
                AddTurnResultMessage($"{currentActor.characterName} 使用 {selectedSkill.skillName}");
                yield return StartCoroutine(ExecuteSkill(currentActor, finalTarget, selectedSkill));
            }
        }
        else
        {
            // 默认普通攻击
            AddTurnResultMessage($"{currentActor.characterName} 攻击 {finalTarget.characterName}");
            yield return StartCoroutine(PerformAttack(currentActor, finalTarget, 1.0f, false));
        }

        // ★ 虚影行动后：为通天教主增加行动条10%
        if (currentActor.summoner != null && currentActor.summoner.characterName == "通天教主")
        {
            Character tongTian = currentActor.summoner;
            tongTian.currentActionValue += ACTION_THRESHOLD * 0.1f;
            if (tongTian.currentActionValue > ACTION_THRESHOLD)
                tongTian.currentActionValue = ACTION_THRESHOLD;
            AddTurnResultMessage($"{currentActor.characterName} 行动，{tongTian.characterName} 行动条增加10%");
        }

        // ★ 诛仙剑行动后施加护盾
        if (currentActor.characterName == "诛仙剑")
        {
            ShadowAura aura = currentActor.GetComponent<ShadowAura>();
            if (aura != null) aura.OnActionPerformed();
        }

        // ★ 二/三阶段追加攻击逻辑（通天教主）
        if (currentActor.characterName == "通天教主")
        {
            int phase = GetTongTianPhase(currentActor);
            if (phase >= 2)
            {
                int threshold = (phase == 2) ? 3 : 2;
                currentActor.consecutiveAttackCount++;
                if (currentActor.consecutiveAttackCount >= threshold)
                {
                    currentActor.consecutiveAttackCount = 0;
                    // 追加一次普通攻击
                    AddTurnResultMessage($"{currentActor.characterName} 追加攻击！");
                    yield return StartCoroutine(PerformAttack(currentActor, finalTarget, 1.0f, false));
                    // 三阶段额外填充50%行动条
                    if (phase == 3)
                    {
                        currentActor.currentActionValue += ACTION_THRESHOLD * 0.5f;
                        AddTurnResultMessage($"{currentActor.characterName} 行动条增加50%");
                    }
                }
            }
        }

        // ★ 四象联动：任意神兽行动时，其余三只获得10%行动条
        if (currentActor.characterName == "青龙" || currentActor.characterName == "白虎" ||
            currentActor.characterName == "朱雀" || currentActor.characterName == "玄武")
        {
            float pushAmount = ACTION_THRESHOLD * 0.1f;
            foreach (var other in enemyParty)
            {
                if (other != currentActor && !other.IsDead() &&
                    (other.characterName == "青龙" || other.characterName == "白虎" ||
                     other.characterName == "朱雀" || other.characterName == "玄武"))
                {
                    other.currentActionValue = Mathf.Min(other.currentActionValue + pushAmount, MAX_PREDICT_ACTION_THRESHOLD);
                }
            }
        }

        // ★ 青龙被动（龙之逆鳞）：攻击后自身行动条额外增加10%
        if (currentActor.characterName == "青龙")
        {
            float pushAmount = ACTION_THRESHOLD * 0.1f;
            currentActor.currentActionValue = Mathf.Min(currentActor.currentActionValue + pushAmount, MAX_PREDICT_ACTION_THRESHOLD);
            AddTurnResultMessage($"青龙之逆鳞触发，自身行动条增加10%");
        }

        // ★ 青龙被动：每3次主动攻击追加一次普通攻击，填充30%行动条
        if (currentActor.characterName == "青龙" && currentActor.qingLongConsecutiveAttacks >= 3)
        {
            currentActor.qingLongConsecutiveAttacks = 0;
            AddTurnResultMessage($"青龙触发追加攻击！");
            yield return StartCoroutine(PerformAttack(currentActor, finalTarget, 1.0f, false));
            float extraPush = ACTION_THRESHOLD * 0.3f;
            currentActor.currentActionValue = Mathf.Min(currentActor.currentActionValue + extraPush, MAX_PREDICT_ACTION_THRESHOLD);
            AddTurnResultMessage($"青龙行动条额外填充30%");
        }

        // ★ 朱雀被动（朱雀涅槃）：每回合结束时检查——已通过重生标记实现

        if (currentTurnDamage > 0)
            ShowTurnDamage(currentActor, currentTurnDamage);

        if (CheckBattleEnd()) yield break;

        currentActor.ReduceCooldowns();
        currentActor.ReduceStatusDurations();

        SetIconScale(currentActor, 1f);
        currentActor.SpendAction();

        currentState = BattleState.Idle;
        UpdateUI();
    }

    // 辅助方法：获取通天教主当前阶段（基于HP，但阶段转换时会更新currentPhase，优先使用currentPhase）
    private int GetTongTianPhase(Character boss)
    {
        // 优先使用存储的阶段值
        if (boss.currentPhase > 0)
            return boss.currentPhase;
        // 降级方案：基于生命值判断
        if (boss.currentHP > 90000) return 1;
        if (boss.currentHP > 60000) return 2;
        return 3;
    }

    public bool CheckBattleEnd()
    {
        if (playerParty.All(p => p.IsDead()))
        {
            battleActive = false;
            battleResultText.text = "战斗失败...";
            battleResultText.gameObject.SetActive(true);
            currentState = BattleState.BattleEnd;
            DisableAllButtons();
            AddTurnResultMessage("战斗失败...");
            foreach (var c in allCharacters) SetIconScale(c, 1f);
            FourSymbolAura.ClearAuras(this);
            if (!isLoadingScene) StartCoroutine(ReturnToDemonTowerAfterDelay(2f));
            return true;
        }
        else if (enemyParty.All(e => e.IsDead()))
        {
            // 清理四神兽光环
            FourSymbolAura.ClearAuras(this);

            battleActive = false;
            battleResultText.text = "战斗胜利！";
            battleResultText.gameObject.SetActive(true);
            currentState = BattleState.BattleEnd;
            DisableAllButtons();
            AddTurnResultMessage("战斗胜利！");
            GameData.currentFloor++;

            // ★ 战胜通天教主特殊奖励
            if (enemyParty.Any(e => e.characterName == "通天教主"))
            {
                GrantTongTianReward();
            }

            foreach (var c in allCharacters) SetIconScale(c, 1f);
            if (!isLoadingScene) StartCoroutine(ReturnToDemonTowerAfterDelay(2f));
            return true;
        }
        return false;
    }

    void DisableAllButtons()
    {
        if (fleeButton != null) fleeButton.interactable = false;
        if (defendButton != null) defendButton.interactable = false;
        if (cancelButton != null) cancelButton.interactable = false;
        foreach (var btn in skillButtons)
            if (btn != null) btn.interactable = false;
    }

    void OnInfoButtonClick()
    {
        if (currentState == BattleState.BattleEnd) return;
        battleActive = false;
        RefreshAvatarList();

        Character defaultTarget = enemyParty.FirstOrDefault(e => !e.IsDead());
        if (defaultTarget == null)
            defaultTarget = playerParty.FirstOrDefault(p => !p.IsDead());
        if (defaultTarget != null)
            OnAvatarClicked(defaultTarget);

        infoPanel.SetActive(true);
    }

    void RefreshAvatarList()
    {
        foreach (Transform child in playerAvatarContainer) Destroy(child.gameObject);
        foreach (Transform child in enemyAvatarContainer) Destroy(child.gameObject);

        foreach (var c in playerParty)
        {
            GameObject btnObj = Instantiate(characterIconPrefab, playerAvatarContainer);
            Button btn = btnObj.GetComponent<Button>();
            if (btn == null) btn = btnObj.AddComponent<Button>();

            Transform iconTrans = btnObj.transform.Find("Icon");
            if (iconTrans != null)
            {
                Image iconImg = iconTrans.GetComponent<Image>();
                if (iconImg != null)
                {
                    iconImg.sprite = c.queueIconSprite;
                    iconImg.gameObject.SetActive(c.queueIconSprite != null);
                }
            }

            Transform nameTrans = btnObj.transform.Find("Name");
            if (nameTrans != null)
            {
                TMP_Text nameTxt = nameTrans.GetComponent<TMP_Text>();
                if (nameTxt != null) nameTxt.text = c.characterName;
            }

            btn.onClick.AddListener(() => OnAvatarClicked(c));
            if (c.IsDead()) btn.interactable = false;
        }

        foreach (var c in enemyParty)
        {
            GameObject btnObj = Instantiate(characterIconPrefab, enemyAvatarContainer);
            Button btn = btnObj.GetComponent<Button>();
            if (btn == null) btn = btnObj.AddComponent<Button>();

            Transform iconTrans = btnObj.transform.Find("Icon");
            if (iconTrans != null)
            {
                Image iconImg = iconTrans.GetComponent<Image>();
                if (iconImg != null)
                {
                    iconImg.sprite = c.queueIconSprite;
                    iconImg.gameObject.SetActive(c.queueIconSprite != null);
                }
            }

            Transform nameTrans = btnObj.transform.Find("Name");
            if (nameTrans != null)
            {
                TMP_Text nameTxt = nameTrans.GetComponent<TMP_Text>();
                if (nameTxt != null) nameTxt.text = c.characterName;
            }

            btn.onClick.AddListener(() => OnAvatarClicked(c));
            if (c.IsDead()) btn.interactable = false;
        }
    }

    void OnAvatarClicked(Character character)
    {
        detailText.text = character.GetDetailedInfo();
    }

    void OnExitInfoPanel()
    {
        infoPanel.SetActive(false);
        battleActive = true;
    }

    void OnSkillPointerEnter(Skill skill)
    {
        if (skillTooltipText != null)
        {
            skillTooltipText.text = skill.description;
            skillTooltipText.gameObject.SetActive(true);
        }
    }

    void OnSkillPointerExit()
    {
        if (skillTooltipText != null)
            skillTooltipText.gameObject.SetActive(false);
    }

    public void AddTurnResultMessage(string message)
    {
        turnResultQueue.Enqueue(message);
        if (turnResultQueue.Count > 25)
            turnResultQueue.Dequeue();
        if (turnResultText != null)
            turnResultText.text = string.Join("\n", turnResultQueue);
    }

    IEnumerator ConfusedPlayerTurn()
    {
        isExecutingSkill = true;

        var allAlive = allCharacters.Where(c => !c.IsDead() && c != currentActor).ToList();
        if (allAlive.Count == 0)
        {
            AddTurnResultMessage($"{currentActor.characterName} 处于错乱状态，但没有其他可攻击的目标，无法行动");
            isExecutingSkill = false;
            hasUsedMainActionThisTurn = true;
            EndPlayerTurn();
            yield break;
        }

        Character target = allAlive[Random.Range(0, allAlive.Count)];
        AddTurnResultMessage($"{currentActor.characterName} 错乱攻击 {target.characterName}");

        yield return StartCoroutine(ExecuteSkill(currentActor, target, currentActor.skills.First(s => s.skillID == 0)));

        isExecutingSkill = false;
        hasUsedMainActionThisTurn = true;
        EndPlayerTurn();
    }

    public void SetIconScale(Character character, float scale)
    {
        if (character == null) return;

        // UIManager 模式下委托给 uiManager
        if (uiManager != null)
        {
            uiManager.SetIconScale(character, scale);
            return;
        }

        if (characterIconMap.TryGetValue(character, out GameObject icon))
        {
            icon.transform.localScale = Vector3.one * scale;
        }
    }

    private void PlayIconHitAnimation(Character character)
    {
        if (character == null || character.IsDead()) return;

        // UIManager 模式下也委托给 uiManager（uiManager 的 icon 有 CharacterQueueIcon 组件）
        if (uiManager != null)
            return;

        if (characterIconMap.TryGetValue(character, out GameObject icon))
        {
            StartCoroutine(IconShakeCoroutine(icon));
        }
    }

    private IEnumerator IconShakeCoroutine(GameObject icon)
    {
        RectTransform rect = icon.GetComponent<RectTransform>();
        if (rect == null) yield break;

        Vector3 originalPos = rect.anchoredPosition;
        float shakeAmount = 5f;
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = originalPos.x + Random.Range(-shakeAmount, shakeAmount);
            rect.anchoredPosition = new Vector2(x, originalPos.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rect.anchoredPosition = originalPos;
    }

    IEnumerator ReturnToDemonTowerAfterDelay(float delay)
    {
        isLoadingScene = true;
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("Demon Tower");
    }

    private void ResetTurnDamage()
    {
        currentTurnDamage = 0;
        if (turnTotalDamageText != null)
            turnTotalDamageText.gameObject.SetActive(false);
    }

    private void AddDamageToCurrentTurn(int damage)
    {
        if (damage > 0)
            currentTurnDamage += damage;
    }

    public void ShowTurnDamage(Character actor, int totalDamage)
    {
        if (turnTotalDamageText == null) return;

        if (totalDamage <= 0)
        {
            if (turnTotalDamageText.gameObject.activeSelf)
                turnTotalDamageText.gameObject.SetActive(false);
            return;
        }

        turnTotalDamageText.text = $"Total:{totalDamage}";
        turnTotalDamageText.color = Color.red;
        turnTotalDamageText.gameObject.SetActive(true);

        if (hideDamageCoroutine != null)
            StopCoroutine(hideDamageCoroutine);
        hideDamageCoroutine = StartCoroutine(HideDamageAfterDelay(2f));
    }

    private IEnumerator HideDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (turnTotalDamageText != null)
            turnTotalDamageText.gameObject.SetActive(false);
    }

    public void ClearSummonedUnits(Character summoner)
    {
        var toRemove = enemyParty.Where(e => e.summoner == summoner).ToList();
        foreach (var unit in toRemove)
        {
            // ★ 立即移除光环效果（避免延迟）
            ShadowAura aura = unit.GetComponent<ShadowAura>();
            if (aura != null)
            {
                aura.RemoveAuraImmediately();
            }
            enemyParty.Remove(unit);
            allCharacters.Remove(unit);
            Destroy(unit.gameObject);
        }
        AddTurnResultMessage($"{summoner.characterName} 清除了所有召唤物");
    }

    /// <summary>
    /// 四象灵尊阵亡惩罚：移除光环，其余存活神兽速度提升25%（可叠加最多3层）
    /// 由 Character.cs 在白虎无敌到期死亡时调用
    /// </summary>
    public void OnFourSymbolDeath(Character deadSymbol)
    {
        if (deadSymbol.characterName != "青龙" && deadSymbol.characterName != "白虎" &&
            deadSymbol.characterName != "朱雀" && deadSymbol.characterName != "玄武")
            return;

        int aliveSymbolCount = 0;
        foreach (var e in enemyParty)
        {
            if (e != deadSymbol && !e.IsDead() &&
                (e.characterName == "青龙" || e.characterName == "白虎" ||
                 e.characterName == "朱雀" || e.characterName == "玄武"))
            {
                aliveSymbolCount++;
                // 加算：每层+25%，上限75%（3层）
                e.deathPenaltySpeedBonus = Mathf.Min(e.deathPenaltySpeedBonus + 0.25f, 0.75f);
            }
        }
        if (aliveSymbolCount > 0)
        {
            AddTurnResultMessage($"{deadSymbol.characterName} 阵亡，其余神兽速度提升25%（当前层数：{enemyParty.FirstOrDefault(e => !e.IsDead() && (e.characterName == "青龙" || e.characterName == "白虎" || e.characterName == "朱雀" || e.characterName == "玄武"))?.deathPenaltySpeedBonus * 100 ?? 0:F0}%）");
            // 移除四神兽光环
            FourSymbolAura aura = deadSymbol.GetComponent<FourSymbolAura>();
            if (aura != null)
                Destroy(aura);
        }
    }

    private void GrantTongTianReward()
    {
        // 创建通天认可令
        Item tongTianReward = new Item
        {
            id = Item.TONG_TIAN_REWARD_ID,
            itemName = "通天认可令",
            description = "通天教主对玩家实力的肯定，象征着封神之战的无上荣耀。",
            type = ItemType.Material,      // 可堆叠材料
            count = 1,
            iconPath = "Basic/TongTianReward",  // 请确保该图标资源存在，否则使用默认图标
            requireLevel = 1
        };

        // 尝试添加到背包（通过 GameData）
        if (GameData.AddRewardItem(tongTianReward))
        {
            AddTurnResultMessage("获得特殊道具：通天认可令！");
            // 保存游戏
            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();
        }
        else
        {
            AddTurnResultMessage("背包已满，无法获得通天认可令！");
        }
    }
}
