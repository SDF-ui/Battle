using UnityEngine;

public class EnemyWander : MonoBehaviour
{
    public float moveSpeed = 2f;               // 移动速度
    public float changeDirectionTime = 2f;      // 方向改变间隔
    public Animator animator;                   // 动画组件
    public Vector2 circleCenter;                 // 圆的中心点
    public float radius;                         // 圆的半径（直径的一半）

    private Rigidbody2D rb;
    private float directionY;                    // 当前垂直移动方向（1向上，-1向下）
    private float timer;

    void Start()
    {

        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();

        // 随机初始化移动方向
        directionY = Random.value > 0.5f ? 1f : -1f;
        timer = changeDirectionTime;

        // 确保初始位置在圆内，否则将位置修正到圆边界内
        Vector3 pos = transform.position;
        circleCenter.x = pos.x;
        circleCenter.y = pos.y;
        float distFromCenterY = Mathf.Abs(pos.y - circleCenter.y);
        if (distFromCenterY > radius)
        {
            pos.y = circleCenter.y + Mathf.Sign(pos.y - circleCenter.y) * radius;
            transform.position = pos;
        }
        // X坐标固定为圆心X（可根据需要调整）
        pos.x = circleCenter.x;
        transform.position = pos;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            // 随机反转方向
            directionY *= -1;
            timer = changeDirectionTime;
        }

        // 计算下一帧位置（仅Y轴变化）
        Vector2 newPos = rb.position;
        newPos.y += directionY * moveSpeed * Time.deltaTime;

        // 检查是否超出圆的垂直范围
        float lowerBound = circleCenter.y - radius;
        float upperBound = circleCenter.y + radius;

        if (newPos.y < lowerBound)
        {
            newPos.y = lowerBound;
            directionY = 1f; // 向上反弹
        }
        else if (newPos.y > upperBound)
        {
            newPos.y = upperBound;
            directionY = -1f; // 向下反弹
        }

        rb.MovePosition(newPos);

        // 更新动画参数（移动速度），仅在控制器存在时执行
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(directionY * moveSpeed));
        }
    }
}