using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public Joystick joystick;
    public float moveSpeed = 5f;
    public Animator animator;

    private Rigidbody2D rb;
    private Vector2 lastInput;
    private Vector2 lastDirection;
    private bool isLoadingScene = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        lastInput = Vector2.zero;
        lastDirection = Vector2.down;
    }

    void FixedUpdate()
    {
        Vector2 moveInput = joystick.GetInput();
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        if (moveInput == Vector2.zero) rb.velocity = Vector2.zero;
    }

    void Update()
    {
        Vector2 moveInput = joystick.GetInput();
        float speed = moveInput.magnitude * 0.75f;
        animator.SetFloat("Speed", speed);

        if (speed > 0.1f)
        {
            animator.SetFloat("Horizontal", moveInput.x);
            animator.SetFloat("Vertical", moveInput.y);
            lastDirection = moveInput.normalized;
        }
        else
        {
            animator.SetFloat("IdleX", lastDirection.x);
            animator.SetFloat("IdleY", lastDirection.y);
        }

        if (lastInput != Vector2.zero && moveInput == Vector2.zero) rb.velocity = Vector2.zero;
        lastInput = moveInput;
    }

    /// <summary>
    /// 根据当前楼层生成敌人配置（心魔或普通怪物）
    /// </summary>
    private void GenerateEnemyConfigs()
    {
        int currentFloor = GameData.currentFloor;

        // ★ 第30层：最终Boss通天教主
        if (currentFloor % 30 == 0)
        {
            Debug.Log($"当前第 {currentFloor} 层，生成最终Boss：通天教主");
            CharacterConfig tongTian = new CharacterConfig
            {
                characterName = "通天教主",
                faction = "",
                level = 80,                     // 高等级确保属性足够
                allocatedCON = 400,             // 对应生命值
                allocatedINT = 100,
                allocatedSTR = 300,             // 对应攻击力
                allocatedAGI = 200,             // 对应速度
                extraCON = 0,
                extraINT = 0,
                extraSTR = 0,
                extraAGI = 0,
                equippedEquipments = new List<Item>(),
                equippedArtifacts = new List<Item>(),
                prefabPath = "LinBao",          // 使用通天教主模型
                isEliteOrBoss = true            // 标记为Boss（需在CharacterConfig中添加此字段）
            };
            BattleData.enemyConfigs = new List<CharacterConfig> { tongTian };
        }
        // 心魔层：10的整数倍层（但排除 30 倍数层，因为后者已经是Boss）
        else if (currentFloor % 10 == 0)
        {
            // 心魔属性增幅系数：层数越高越强
            float multiplier = 1.0f + currentFloor / 50f;

            // 深拷贝并缩放玩家装备
            List<Item> copiedEquipments = CloneAndScaleItems(GameData.equippedItems, multiplier);
            List<Item> copiedArtifacts = CloneAndScaleItems(GameData.artifactSlots, multiplier);

            CharacterConfig heartDemon = new CharacterConfig
            {
                characterName = "心魔",
                faction = GameData.playerFaction,
                level = GameData.playerLevel,
                allocatedCON = Mathf.RoundToInt(GameData.playerAllocatedCON * multiplier),
                allocatedINT = Mathf.RoundToInt(GameData.playerAllocatedINT * multiplier),
                allocatedSTR = Mathf.RoundToInt(GameData.playerAllocatedSTR * multiplier),
                allocatedAGI = Mathf.RoundToInt(GameData.playerAllocatedAGI * multiplier),
                extraCON = Mathf.RoundToInt(GameData.playerExtraCON * multiplier),
                extraINT = Mathf.RoundToInt(GameData.playerExtraINT * multiplier),
                extraSTR = Mathf.RoundToInt(GameData.playerExtraSTR * multiplier),
                extraAGI = Mathf.RoundToInt(GameData.playerExtraAGI * multiplier),
                equippedEquipments = copiedEquipments,
                equippedArtifacts = copiedArtifacts,
                prefabPath = "Player", // 使用玩家模型
                isEliteOrBoss = true
            };
            BattleData.enemyConfigs = new List<CharacterConfig> { heartDemon };
        }
        else if (currentFloor % 10 == 9)
        {
            // 第9层（或其他以9结尾的层）为精英层，4个敌人（示例：四神兽）
            Debug.Log($"当前第 {currentFloor} 层，生成精英层（4个敌人）");
            BattleData.enemyConfigs = new List<CharacterConfig>();
            int enemyLevel = Mathf.RoundToInt(5 + currentFloor * 2.0f);
            string[] names = { "青龙", "白虎", "朱雀", "玄武" };
            string[] paths = { "Dragon", "Tiger", "Bird", "Tortise" };
            for (int i = 0; i < 4; i++)
            {
                BattleData.enemyConfigs.Add(new CharacterConfig
                {
                    characterName = names[i],
                    faction = "",
                    level = enemyLevel,
                    prefabPath = paths[i],
                    equippedEquipments = new List<Item>(),
                    equippedArtifacts = new List<Item>(),
                    isEliteOrBoss = true
                });
            }
        }
        else if (currentFloor % 10 == 8)
        {
            // 第8层：四神兽（与第9层类似，但可区分）
            Debug.Log($"当前第 {currentFloor} 层，生成四神兽");
            BattleData.enemyConfigs = new List<CharacterConfig>();
            int enemyLevel = Mathf.RoundToInt(5 + currentFloor * 2.0f);
            string[] names = { "青龙", "白虎", "朱雀", "玄武" };
            string[] paths = { "Dragon", "Tiger", "Bird", "Tortise" };
            for (int i = 0; i < 4; i++)
            {
                BattleData.enemyConfigs.Add(new CharacterConfig
                {
                    characterName = names[i],
                    faction = "",
                    level = enemyLevel,
                    prefabPath = paths[i],
                    equippedEquipments = new List<Item>(),
                    equippedArtifacts = new List<Item>()
                });
            }
        }
        else
        {
            // 普通层：敌人数量随层数增加
            int baseMin = 1 + Mathf.FloorToInt(currentFloor / 10);
            int baseMax = 5;
            baseMin = Mathf.Min(baseMin, baseMax);
            int enemyCount = Random.Range(baseMin, baseMax + 1);

            Debug.Log($"普通层 {currentFloor}，生成 {enemyCount} 个敌人");

            BattleData.enemyConfigs = new List<CharacterConfig>();
            for (int i = 0; i < enemyCount; i++)
            {
                int enemyLevel = Mathf.RoundToInt(5 + currentFloor * 2.0f);

                string enemyName;
                string prefabPath;

                // 前7层按固定顺序，之后随机混合
                if (currentFloor <= 7)
                {
                    switch (currentFloor)
                    {
                        case 1:
                            enemyName = "剑魂";
                            prefabPath = "SwordSoul";
                            break;
                        case 2:
                            enemyName = "石妖";
                            prefabPath = "StoneDemon";
                            break;
                        case 3:
                            enemyName = "风刃豹";
                            prefabPath = "LeopardDemon";
                            break;
                        case 4:
                            enemyName = "青龙";
                            prefabPath = "Dragon";
                            break;
                        case 5:
                            enemyName = "白虎";
                            prefabPath = "Tiger";
                            break;
                        case 6:
                            enemyName = "朱雀";
                            prefabPath = "Bird";
                            break;
                        case 7:
                            enemyName = "玄武";
                            prefabPath = "Tortise";
                            break;
                        default:
                            enemyName = "剑魂";
                            prefabPath = "SwordSoul";
                            break;
                    }
                }
                else
                {
                    // 从多种怪物中随机选择
                    int randomIndex = Random.Range(0, 7);
                    switch (randomIndex)
                    {
                        case 0:
                            enemyName = "剑魂";
                            prefabPath = "SwordSoul";
                            break;
                        case 1:
                            enemyName = "石妖";
                            prefabPath = "StoneDemon";
                            break;
                        case 2:
                            enemyName = "风刃豹";
                            prefabPath = "LeopardDemon";
                            break;
                        case 3:
                            enemyName = "青龙";
                            prefabPath = "Dragon";
                            break;
                        case 4:
                            enemyName = "白虎";
                            prefabPath = "Tiger";
                            break;
                        case 5:
                            enemyName = "朱雀";
                            prefabPath = "Bird";
                            break;
                        case 6:
                            enemyName = "玄武";
                            prefabPath = "Tortise";
                            break;
                        default:
                            enemyName = "剑魂";
                            prefabPath = "SwordSoul";
                            break;
                    }
                }

                BattleData.enemyConfigs.Add(new CharacterConfig
                {
                    characterName = enemyName,
                    faction = "",
                    level = enemyLevel,
                    prefabPath = prefabPath,
                    equippedEquipments = new List<Item>(),
                    equippedArtifacts = new List<Item>()
                });
            }
        }

        Debug.Log($"最终生成敌人数量: {BattleData.enemyConfigs.Count}");
    }

    /// <summary>
    /// 深拷贝物品列表并缩放属性值（用于心魔复制玩家装备）
    /// </summary>
    private List<Item> CloneAndScaleItems(Item[] sourceItems, float scale)
    {
        List<Item> result = new List<Item>();
        for (int i = 0; i < sourceItems.Length; i++)
        {
            Item item = sourceItems[i];
            if (item != null)
            {
                string json = JsonUtility.ToJson(item);
                Item copy = JsonUtility.FromJson<Item>(json);
                copy.icon = item.icon;
                copy.iconPath = item.iconPath;

                if (copy.basicAttributes != null)
                {
                    foreach (var attr in copy.basicAttributes)
                    {
                        attr.value = Mathf.RoundToInt(attr.value * scale);
                    }
                }
                if (copy.extraAttributes != null)
                {
                    foreach (var attr in copy.extraAttributes)
                    {
                        attr.value = Mathf.RoundToInt(attr.value * scale);
                    }
                }
                result.Add(copy);
            }
            else
            {
                result.Add(null);
            }
        }
        return result;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") && !isLoadingScene)
        {
            GenerateEnemyConfigs();
            isLoadingScene = true;
            SceneManager.LoadScene("Battle");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && !isLoadingScene)
        {
            GenerateEnemyConfigs();
            Debug.Log("Enemy configs set via collision, count: " + BattleData.enemyConfigs.Count);
            isLoadingScene = true;
            SceneManager.LoadScene("Battle");
        }
    }
}