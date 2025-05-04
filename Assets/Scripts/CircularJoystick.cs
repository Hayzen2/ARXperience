using UnityEngine;
using UnityEngine.EventSystems;

public class CircularJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] RectTransform outerCircle;
    [SerializeField] RectTransform innerCircle;
    ARSurface arSurface;
    Vector2 originalInnerCirclePosition;

    private bool isDragging = false;
    private int pointerId = -1;

    void Start()
    {
        arSurface = FindObjectOfType<ARSurface>();
        originalInnerCirclePosition = innerCircle.localPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isDragging)
        {
            isDragging = true;
            pointerId = eventData.pointerId;
            UpdateJoystick(eventData.position);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId)
        {
            isDragging = false;
            pointerId = -1;
            innerCircle.localPosition = originalInnerCirclePosition;
        }
    }

    void Update()
    {
        if (isDragging)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.fingerId == pointerId)
                {
                    UpdateJoystick(touch.position);
                    break;
                }
            }
        }
    }

    void UpdateJoystick(Vector2 touchPosition)
    {
        Vector2 direction = touchPosition - (Vector2)outerCircle.position;
        float maxDistance = outerCircle.rect.width / 2;

        Vector2 normalizedInput = direction / maxDistance;

        // Clamp visual position
        float distance = Mathf.Clamp(direction.magnitude, 0, maxDistance);
        Vector2 clampedPosition = direction.normalized * distance;
        innerCircle.localPosition = clampedPosition;

        if (arSurface != null)
        {
            arSurface.MoveModelOnPlane(normalizedInput);
        }
    }
}