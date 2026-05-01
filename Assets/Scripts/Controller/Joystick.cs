using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform background;
    public RectTransform handle;
    public float handleRange = 1f;

    private Vector2 input = Vector2.zero;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out position))
        {
            position.x = (position.x / background.sizeDelta.x) * 2f;
            position.y = (position.y / background.sizeDelta.y) * 2f;

            input = (position.magnitude > 1) ? position.normalized : position;

            handle.anchoredPosition = new Vector2(input.x * (background.sizeDelta.x / 2f) * handleRange, input.y * (background.sizeDelta.y / 2f) * handleRange);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }

    public Vector2 GetInput()
    {
        return input;
    }
}