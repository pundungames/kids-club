using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BeforeAfterSlider : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Görseller")]
    [SerializeField] private RawImage beforeImage;
    [SerializeField] private RawImage afterImage;

    [Header("Mask Componentleri")]
    [SerializeField] private RectMask2D beforeMask;

    [Header("Slider Handle")]
    [SerializeField] private RectTransform handle;
    [SerializeField] private RectTransform dividerLine;

    [Header("Ayarlar")]
    [SerializeField] private float handleWidth = 60f;
    [SerializeField] private Color dividerColor = Color.white;
    [SerializeField] private float dividerThickness = 4f;
    [SerializeField] private float maxPadding = 900f; // Handle'ýn maksimum hareket mesafesi

    private RectTransform containerRect;
    private RectTransform beforeMaskRect;
    private Canvas canvas;

    void Start()
    {
        containerRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        // Before mask'ý al
        if (beforeMask != null)
        {
            beforeMaskRect = beforeMask.GetComponent<RectTransform>();
        }

        // Baþlangýçta handle'ý ortaya yerleþtir
        SetSliderPosition(0.5f);

        // Divider line'ý ayarla
        if (dividerLine != null)
        {
            Image dividerImage = dividerLine.GetComponent<Image>();
            if (dividerImage != null)
            {
                dividerImage.color = dividerColor;
            }
            dividerLine.sizeDelta = new Vector2(dividerThickness, containerRect.rect.height);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        HandleDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        HandleDrag(eventData);
    }

    private void HandleDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            containerRect,
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );

        float containerWidth = containerRect.rect.width;
        float normalizedPosition = Mathf.Clamp01((localPoint.x + containerWidth / 2) / containerWidth);

        SetSliderPosition(normalizedPosition);
    }

    private void SetSliderPosition(float normalizedPosition)
    {
        float containerWidth = containerRect.rect.width;

        // MaxPadding deðerine göre gerçek geniþliði hesapla
        float effectiveWidth = containerWidth - (maxPadding * 2);
        float xPosition = (normalizedPosition * effectiveWidth) - (effectiveWidth / 2);

        // Handle pozisyonunu ayarla
        if (handle != null)
        {
            handle.anchoredPosition = new Vector2(-xPosition, 0);
        }

        // Divider line pozisyonunu ayarla
        if (dividerLine != null)
        {
            dividerLine.anchoredPosition = new Vector2(-xPosition, 0);
        }

        // Before mask'ýn padding'ini ayarla (sað padding ile kontrol)
        if (beforeMask != null && beforeMaskRect != null)
        {
            // Sað padding: normalizedPosition arttýkça azalýr (before image geniþler)
            float rightPadding = normalizedPosition * effectiveWidth + maxPadding;

            Vector4 padding = beforeMask.padding;
            //padding.x = maxPadding; // Sol padding sabit
            padding.z = rightPadding; // Sað padding deðiþken
            beforeMask.padding = padding;
        }
    }
}