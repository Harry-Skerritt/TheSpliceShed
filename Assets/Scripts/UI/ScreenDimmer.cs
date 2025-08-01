using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenDimmer : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    public float fadeDuration = 0.5f;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }

    public void FadeIn()
    {
        StartCoroutine(Fade(0.8f, fadeDuration));
    }

    public void FadeOut()
    {
        StartCoroutine(Fade(0, fadeDuration));
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        float start = canvasGroup.alpha;
        float time = 0;

        if (targetAlpha > 0)
        {
            canvasGroup.blocksRaycasts = true; //Todo: Check this is needed
        }

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, time / duration);
            yield return null;
        }
        
        canvasGroup.alpha = targetAlpha;

        if (targetAlpha == 0)
        {
            canvasGroup.blocksRaycasts = false;
        }
    }
}
