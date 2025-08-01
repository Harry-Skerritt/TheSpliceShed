using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(CanvasGroup))]
public class UIFadeTransition : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    [Header("Transition Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }
    
    public void FadeIn()
    {
        StopAllCoroutines();
        StartCoroutine(DoFade(canvasGroup.alpha, 1));
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    
    public void FadeOut()
    {
        StopAllCoroutines();
        StartCoroutine(DoFade(canvasGroup.alpha, 0));
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private IEnumerator DoFade(float startAlpha, float endAlpha)
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeDuration);
            canvasGroup.alpha = newAlpha;
            yield return null; 
        }
        canvasGroup.alpha = endAlpha;
    }
}
