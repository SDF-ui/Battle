using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 长按处理器 - 为 UI 按钮添加可复用的长按支持
/// 替代在各个 UI 脚本中重复实现的长按协程逻辑
/// </summary>
public class LongPressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("长按参数")]
    [SerializeField] private float longPressDelay = 0.5f;
    [SerializeField] private float repeatInterval = 0.05f;
    [SerializeField] private bool triggerOnClickFirst = true;

    [Header("事件")]
    public UnityEvent onLongPressStart;
    public UnityEvent onLongPressRepeat;
    public UnityEvent onLongPressEnd;

    private Coroutine longPressCoroutine;
    private bool isLongPressing = false;

    /// <summary>
    /// 为按钮添加长按支持（静态方法，方便调用）
    /// </summary>
    public static LongPressHandler AddLongPressSupport(
        Component buttonComponent,
        System.Action onPressAction,
        System.Action onRepeatAction = null,
        System.Action onReleaseAction = null,
        float delay = 0.5f,
        float interval = 0.05f,
        bool triggerFirst = true)
    {
        if (buttonComponent == null) return null;

        var handler = buttonComponent.gameObject.GetComponent<LongPressHandler>();
        if (handler == null)
            handler = buttonComponent.gameObject.AddComponent<LongPressHandler>();

        handler.longPressDelay = delay;
        handler.repeatInterval = interval;
        handler.triggerOnClickFirst = triggerFirst;

        handler.onLongPressStart.RemoveAllListeners();
        handler.onLongPressRepeat.RemoveAllListeners();
        handler.onLongPressEnd.RemoveAllListeners();

        if (onPressAction != null)
            handler.onLongPressStart.AddListener(new UnityEngine.Events.UnityAction(onPressAction));

        if (onRepeatAction != null)
        {
            if (onPressAction == null && triggerFirst)
                handler.onLongPressStart.AddListener(new UnityEngine.Events.UnityAction(onRepeatAction));
            handler.onLongPressRepeat.AddListener(new UnityEngine.Events.UnityAction(onRepeatAction));
        }

        if (onReleaseAction != null)
            handler.onLongPressEnd.AddListener(new UnityEngine.Events.UnityAction(onReleaseAction));

        return handler;
    }

    /// <summary>
    /// 简化版本：直接为按钮添加长按，重复执行同一个动作
    /// </summary>
    public static LongPressHandler SetupButtonLongPress(Component button, System.Action action, float delay = 0.5f, float interval = 0.05f)
    {
        return AddLongPressSupport(button, action, action, null, delay, interval, true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (triggerOnClickFirst)
        {
            onLongPressStart?.Invoke();
        }

        StopLongPress();
        longPressCoroutine = StartCoroutine(LongPressRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopLongPress();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopLongPress();
    }

    private IEnumerator LongPressRoutine()
    {
        yield return new WaitForSeconds(longPressDelay);

        isLongPressing = true;
        while (true)
        {
            onLongPressRepeat?.Invoke();
            yield return new WaitForSeconds(repeatInterval);
        }
    }

    private void StopLongPress()
    {
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }

        if (isLongPressing)
        {
            isLongPressing = false;
            onLongPressEnd?.Invoke();
        }
    }

    private void OnDestroy()
    {
        StopLongPress();
    }
}
