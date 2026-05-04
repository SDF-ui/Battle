using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 战斗 UI 管理器 - 管理战斗场景中的所有 UI 交互
/// 通过事件回调与 BattleManager 解耦
/// </summary>
public class BattleUIManager : MonoBehaviour
{
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
    public int maxQueueLength = 10;

    [Header("按钮")]
    public Button fleeButton;
    public Button defendButton;
    public Button cancelButton;

    [Header("信息面板")]
    public Button infoButton;
    public GameObject infoPanel;
    public Transform playerAvatarContainer;
    public Transform enemyAvatarContainer;
    public TMP_Text detailText;
    public Button exitButton;

    // 事件回调（由 BattleManager 设置）
    public System.Action onFleeClicked;
    public System.Action onDefendClicked;
    public System.Action onCancelClicked;
    public System.Action<Character> onQueueIconClicked;
    public System.Action<Skill> onSkillSelected;

    private List<Button> skillButtons = new List<Button>();
    private List<GameObject> queueIcons = new List<GameObject>();
    private Queue<string> turnResultQueue = new Queue<string>();
    private Coroutine hideDamageCoroutine = null;
    private Dictionary<Character, GameObject> characterIconMap = new Dictionary<Character, GameObject>();
    private int currentTurnDamage = 0;

    public void Initialize()
    {
        if (battleResultText != null)
            battleResultText.gameObject.SetActive(false);
        if (turnResultText != null)
            turnResultText.text = "";
        if (turnTotalDamageText != null)
        {
            turnTotalDamageText.gameObject.SetActive(false);
            turnTotalDamageText.color = Color.red;
        }
        if (skillTooltipText != null)
            skillTooltipText.gameObject.SetActive(false);
        if (infoPanel != null)
            infoPanel.SetActive(false);

        SetupButtons();
    }

    public void SetupButtons()
    {
        if (fleeButton != null)
            fleeButton.onClick.AddListener(() => onFleeClicked?.Invoke());
        if (defendButton != null)
            defendButton.onClick.AddListener(() => onDefendClicked?.Invoke());
        if (cancelButton != null)
            cancelButton.onClick.AddListener(() => onCancelClicked?.Invoke());
        if (infoButton != null)
            infoButton.onClick.AddListener(OnInfoButtonClick);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitInfoPanel);
    }

    public void UpdateAllCharacterUI(List<Character> playerParty, List<Character> enemyParty)
    {
        foreach (var c in playerParty)
            UpdateCharacterUI(c);
        foreach (var c in enemyParty)
            UpdateCharacterUI(c);
    }

    public void UpdateCharacterUI(Character c)
    {
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
            float displayValue = Mathf.Clamp01(c.currentActionValue / BattleActionSystem.ACTION_THRESHOLD);
            c.actionSlider.value = displayValue;
        }
        if (c.targetIcon != null)
            c.targetIcon.SetActive(!c.IsDead());
    }

    /// <summary>
    /// 创建固定数量的行动队列图标槽位（用于多轮预测）
    /// 初始全部隐藏，UpdateActionQueueOrder 中按需显示
    /// </summary>
    public void CreateActionQueueIcons(List<Character> allCharacters)
    {
        if (actionQueueContainer == null || characterIconPrefab == null) return;

        foreach (Transform child in actionQueueContainer)
            Destroy(child.gameObject);
        queueIcons.Clear();
        characterIconMap.Clear();

        // 创建 maxQueueLength 个固定图标槽位
        for (int i = 0; i < maxQueueLength; i++)
        {
            GameObject icon = Instantiate(characterIconPrefab, actionQueueContainer);
            var iconComponent = icon.AddComponent<CharacterQueueIcon>();
            // 初始无关联角色，后续由 UpdateActionQueueOrder 动态设置
            iconComponent.Initialize(null, onQueueIconClicked);

            // 删除按钮的过渡动画
            Button btn = icon.GetComponent<Button>();
            if (btn != null)
                btn.transition = Selectable.Transition.None;

            // 初始隐藏
            icon.SetActive(false);

            queueIcons.Add(icon);
        }
    }

    /// <summary>
    /// 更新行动队列：使用基于剩余行动值的预测算法。
    /// 显示值 = 剩余行动值（时间单位），越小越接近行动。
    /// 第一次出现: (ACTION_THRESHOLD - Cur) / SPD
    /// 第二次出现: (ACTION_THRESHOLD * 2 - Cur) / SPD
    /// 第N次出现:  (ACTION_THRESHOLD * N - Cur) / SPD
    /// 按剩余行动值非递减排序。
    /// </summary>
    public void UpdateActionQueueOrder(List<Character> allCharacters)
    {
        if (queueIcons.Count == 0) return;

        var living = allCharacters.Where(c => !c.IsDead()).ToList();
        if (living.Count == 0) return;

        // 预测并计算每个位置的剩余行动值
        var predicted = PredictNextActionsWithRemainingTime(living, maxQueueLength);

        for (int i = 0; i < queueIcons.Count; i++)
        {
            GameObject icon = queueIcons[i];

            if (i < predicted.Count)
            {
                var (character, remainingTime) = predicted[i];
                icon.SetActive(true);

                // 设置头像
                Transform iconTrans = icon.transform.Find("Icon");
                if (iconTrans != null)
                {
                    Image iconImage = iconTrans.GetComponent<Image>();
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

                // 显示剩余行动值 × 100
                TMP_Text tmp = icon.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                {
                    int displayVal = Mathf.RoundToInt(remainingTime * 100f);
                    tmp.text = displayVal.ToString();
                    tmp.gameObject.SetActive(true);
                }

                icon.transform.localScale = Vector3.one;
                characterIconMap[character] = icon;
            }
            else
            {
                icon.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 预测未来 count 次行动，计算每个位置的剩余行动值。
    /// 
    /// 算法：
    /// - 用 predTime 跟踪每个角色在预测时间轴上的"当前已过时间"
    /// - 每次选出 predTime 最小的角色（最先到达阈值）
    /// - 该角色的剩余行动值 = predTime[c]（即从当前到该角色行动经过的模拟时间）
    /// - 然后该角色的 predTime 增加 ACTION_THRESHOLD / SPD（进入下一轮）
    /// 
    /// 这样第一次出现时 remainingTime = (ACTION_THRESHOLD - Cur) / SPD
    /// 第二次出现时 remainingTime = (ACTION_THRESHOLD * 2 - Cur) / SPD
    /// 因为每轮增加 ACTION_THRESHOLD / SPD，所以第N次 = 初始时间 + (N-1)*周期
    /// </summary>
    private List<(Character character, float remainingTime)> PredictNextActionsWithRemainingTime(
        List<Character> living, int count)
    {
        var result = new List<(Character, float)>();

        // 初始化预测时间 = (ACTION_THRESHOLD - Cur) / SPD
        var predTime = new Dictionary<Character, float>();
        foreach (var c in living)
        {
            float remaining = BattleActionSystem.ACTION_THRESHOLD - c.currentActionValue;
            predTime[c] = remaining / c.CurrentSpeed;
        }

        while (result.Count < count)
        {
            // 找到剩余行动值最小的角色
            Character next = null;
            float minTime = float.MaxValue;
            foreach (var c in living)
            {
                float t = predTime[c];
                if (t < minTime)
                {
                    minTime = t;
                    next = c;
                }
            }
            if (next == null) break;

            // 记录该角色的剩余行动值
            result.Add((next, minTime));

            // 该角色行动后，增加一轮完整周期
            float nextTime = minTime + BattleActionSystem.ACTION_THRESHOLD / next.CurrentSpeed;
            predTime[next] = nextTime;
        }

        return result;
    }

    public void SetIconScale(Character character, float scale)
    {
        if (characterIconMap.TryGetValue(character, out GameObject icon))
            icon.transform.localScale = Vector3.one * scale;
    }

    public void GenerateSkillButtons(Character character)
    {
        ClearSkillButtons();

        if (character.skills == null || character.skills.Count == 0)
            return;

        foreach (var skill in character.skills)
        {
            if (skill.currentCooldown > 0)
                continue;
            if (character.currentMP < skill.mpCost)
                continue;

            GameObject btnObj = Instantiate(skillButtonPrefab, skillButtonContainer);
            Button btn = btnObj.GetComponent<Button>();

            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                btnText.text = skill.skillName;

            var capturedSkill = skill;
            btn.onClick.AddListener(() => onSkillSelected?.Invoke(capturedSkill));
            skillButtons.Add(btn);
        }
    }

    public void ClearSkillButtons()
    {
        foreach (var btn in skillButtons)
            if (btn != null) Destroy(btn.gameObject);
        skillButtons.Clear();
    }

    public void ShowSkillTooltip(Skill skill, Vector3 position)
    {
        if (skillTooltipText == null) return;
        skillTooltipText.text = $"{skill.skillName}\n{skill.description}\nMP消耗: {skill.mpCost}  冷却: {skill.cooldown}回合";
        skillTooltipText.gameObject.SetActive(true);
        skillTooltipText.transform.position = position;
    }

    public void HideSkillTooltip()
    {
        if (skillTooltipText != null)
            skillTooltipText.gameObject.SetActive(false);
    }

    public void AddTurnResultMessage(string message)
    {
        if (turnResultText == null) return;

        turnResultQueue.Enqueue(message);
        if (turnResultQueue.Count > 5)
            turnResultQueue.Dequeue();

        turnResultText.text = string.Join("\n", turnResultQueue.ToArray());
    }

    public void ShowBattleResult(string message)
    {
        if (battleResultText != null)
        {
            battleResultText.text = message;
            battleResultText.gameObject.SetActive(true);
        }
    }

    public void ShowTurnDamage(int damage)
    {
        if (turnTotalDamageText != null)
        {
            turnTotalDamageText.text = $"本回合总伤害: {damage}";
            turnTotalDamageText.gameObject.SetActive(true);

            if (hideDamageCoroutine != null)
                StopCoroutine(hideDamageCoroutine);
            hideDamageCoroutine = StartCoroutine(HideTurnDamageAfterDelay(2f));
        }
    }

    private IEnumerator HideTurnDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (turnTotalDamageText != null)
            turnTotalDamageText.gameObject.SetActive(false);
    }

    public void ResetTurnDamage()
    {
        currentTurnDamage = 0;
    }

    public void AddTurnDamage(int damage)
    {
        currentTurnDamage += damage;
    }

    public int GetTurnDamage() => currentTurnDamage;

    private void OnInfoButtonClick()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
            PopulateInfoPanel();
        }
    }

    private void OnExitInfoPanel()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    private void PopulateInfoPanel()
    {
        if (playerAvatarContainer != null)
        {
            foreach (Transform child in playerAvatarContainer)
                Destroy(child.gameObject);
        }
        if (enemyAvatarContainer != null)
        {
            foreach (Transform child in enemyAvatarContainer)
                Destroy(child.gameObject);
        }

        if (detailText != null)
            detailText.text = "点击角色头像查看详情";
    }
}

/// <summary>
/// 行动队列中角色图标组件（挂载在图标预制体上）
/// </summary>
public class CharacterQueueIcon : MonoBehaviour, IPointerClickHandler
{
    private Character character;
    private System.Action<Character> onClickCallback;

    public void Initialize(Character c, System.Action<Character> callback)
    {
        character = c;
        onClickCallback = callback;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (character != null)
            onClickCallback?.Invoke(character);
    }
}
