using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollViewHandler : MonoBehaviour, IScrollHandler
{
    [Tooltip("Reference to the ScrollRect component on this GameObject.")]
    public ScrollRect scrollRect;

    [Tooltip("Adjusts the sensitivity of mouse wheel scrolling.")]
    [Range(0.01f, 0.5f)]
    public float scrollSensitivity = 0.05f;
    
    [SerializeField] private CanvasGroup canvasGroup;

    void Awake()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                Debug.LogError("ScrollViewInputHandler: No ScrollRect component found on this GameObject.", this);
            }
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    void OnEnable()
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (scrollRect == null) return;
        float scrollAmount = eventData.scrollDelta.y * scrollSensitivity;
        scrollRect.verticalNormalizedPosition -= scrollAmount;
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
    }
}
