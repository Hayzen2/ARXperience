using UnityEngine;
using UnityEngine.EventSystems;

public class HorizontalJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] RectTransform outerCircle;
    [SerializeField] RectTransform innerCircle;
    [SerializeField] float rotationSpeed = 10.0f;
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

        float horizontalInput = direction.x;
        float normalizedInput = horizontalInput / maxDistance;

        // Clamp visual position
        float clampedX = Mathf.Clamp(horizontalInput, -maxDistance, maxDistance);
        innerCircle.localPosition = new Vector2(clampedX, 0);

        if (arSurface != null)
        {
            arSurface.RotateModel(normalizedInput * rotationSpeed * Time.deltaTime);
        }
    }
}