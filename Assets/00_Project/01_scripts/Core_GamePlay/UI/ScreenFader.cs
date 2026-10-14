using UnityEngine;
using DG.Tweening;
using System;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance; 
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    [SerializeField] bool autoFadeOutOnStart = false; 

    private void Awake()
    {
        SetSingleTon();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        // Ensure it starts transparent and blocks raycasts only when visible
        ;
        canvasGroup.blocksRaycasts = false;

        if (autoFadeOutOnStart)
        {
            GameFlowManager.OnSceneTransition += (string s)=>{ FadeOut(null); };
            canvasGroup.alpha = 1;
            FadeOut();
        }
    }

    public void FadeIn(Action onComplete = null)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.DOFade(1f, fadeDuration).OnComplete(() => onComplete?.Invoke()).SetUpdate(true); ;
    }

    public void FadeOut(Action onComplete = null)
    {
        canvasGroup.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            canvasGroup.blocksRaycasts = false;
            onComplete?.Invoke();
        }).SetUpdate(true) ;
    }


    private void SetSingleTon()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
        
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }
}