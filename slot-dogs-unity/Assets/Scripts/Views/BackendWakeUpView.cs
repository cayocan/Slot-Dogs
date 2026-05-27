using System;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class BackendWakeUpView : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvasGroup;
    public GameObject background;
    public RectTransform huskyContainer;
    public RectTransform huskyImage;
    public TMP_Text messageText;
    public TMP_Text dotsText;

    private Tween _huskyFloatTween;
    private Tween _huskyTiltTween;
    private Tween _fadeTween;
    private Tween _messageFadeTween;

    private void OnDestroy()
    {
        KillAllTweens();
    }

    public void Show()
    {
        canvasGroup.alpha = 1f;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetMessage(string message)
    {
        messageText.text = message;
        messageText.alpha = 1f;
    }

    public void FadeMessageTo(string nextMessage)
    {
        if (_messageFadeTween != null) _messageFadeTween.Kill();
        _messageFadeTween = messageText.DOFade(0f, 0.3f).OnComplete(() =>
        {
            messageText.text = nextMessage;
            messageText.DOFade(1f, 0.3f);
        });
    }

    public void SetDots(string dots)
    {
        if (dotsText != null)
            dotsText.text = dots;
    }

    public void PlayFadeOut(Action onComplete)
    {
        if (_fadeTween != null) _fadeTween.Kill();
        _fadeTween = canvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    private void KillHuskyTweens()
    {
        _huskyFloatTween?.Kill();
        _huskyTiltTween?.Kill();
        huskyImage.localScale = Vector3.one;
        huskyImage.localRotation = Quaternion.identity;
    }

    private void KillAllTweens()
    {
        KillHuskyTweens();
        _fadeTween?.Kill();
        _messageFadeTween?.Kill();
    }
}
