using System;
using System.Collections;
using UnityEngine;

public class BackendWakeUpPresenter
{
    private readonly BackendWakeUpView _view;
    public BackendHealthService HealthService { get; }

    private readonly string[] _messages = new string[]
    {
        "Waking up the dogs...",
        "The dogs are coming...",
        "Preparing the paws...",
        "The Husky is coming out of the kennel...",
        "Almost there, hang in there a little longer!",
        "The dogs are excited to play!",
        "Just a moment more, the dogs are stretching...",
        "The dogs are wagging their tails in anticipation...",
        "The dogs are sniffing around, getting ready...",
        "The dogs are doing their happy dance, almost ready...",
        "The dogs are putting on their party hats, just for you..."
    };

    private int _messageIndex = 0;
    private Coroutine _messageCoroutine;
    private Coroutine _dotsCoroutine;
    private Coroutine _pingCoroutine;
    private bool _isRunning = false;
    private Action _onComplete;
    private readonly MonoBehaviour _coroutineRunner;

    public BackendWakeUpPresenter(BackendWakeUpView view, BackendHealthService healthService, MonoBehaviour coroutineRunner)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        HealthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
        _coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));
    }

    public void Show() => _view?.Show();
    public void Hide() => _view?.Hide();
    public void SetMessage(string message) => _view?.SetMessage(message);
    public void PlayFadeOut(Action onComplete) => _view?.PlayFadeOut(onComplete);

    public void StartDotsAnimation()
    {
        if (_dotsCoroutine == null)
            _dotsCoroutine = _coroutineRunner.StartCoroutine(DotsAnimationCoroutine());
    }

    public void StopDotsAnimation()
    {
        if (_dotsCoroutine != null)
        {
            try { _coroutineRunner.StopCoroutine(_dotsCoroutine); } catch { }
            _dotsCoroutine = null;
            _view?.SetDots("");
        }
    }

    public void StartMessageRotation()
    {
        if (_messageCoroutine == null)
            _messageCoroutine = _coroutineRunner.StartCoroutine(MessageRotationCoroutine());
        StartDotsAnimation();
    }

    public void StopMessageRotation()
    {
        if (_messageCoroutine != null)
        {
            try { _coroutineRunner.StopCoroutine(_messageCoroutine); } catch { }
            _messageCoroutine = null;
        }
        StopDotsAnimation();
    }

    public void StartWakeUp(Action onComplete)
    {
        if (_isRunning) return;
        _isRunning = true;
        _onComplete = onComplete;

        Show();
        StartMessageRotation();

        _pingCoroutine = _coroutineRunner.StartCoroutine(HealthService.PingLoop(OnPingSuccessInternal, OnPingAttemptInternal));
    }

    public void StopWakeUp()
    {
        if (!_isRunning) return;
        _isRunning = false;

        if (_pingCoroutine != null)
        {
            try { _coroutineRunner.StopCoroutine(_pingCoroutine); } catch { }
            _pingCoroutine = null;
        }

        StopMessageRotation();
        Hide();
    }

    private void OnPingAttemptInternal(int attempt)
    {
        // Intencionalmente não exibir mensagens de tentativa de conexão.
        // Apenas mensagens relacionadas aos cachorros devem aparecer na UI.
    }

    private void OnPingSuccessInternal()
    {
        if (!_isRunning) return;
        _isRunning = false;
        _coroutineRunner.StartCoroutine(SuccessSequenceCoroutine());
    }

    private IEnumerator SuccessSequenceCoroutine()
    {
        StopMessageRotation();
        SetMessage("All Right! Let's play!");
        yield return new WaitForSeconds(0.6f);
        yield return new WaitForSeconds(0.4f);
        PlayFadeOut(_onComplete);
    }

    private IEnumerator MessageRotationCoroutine()
    {
        while (true)
        {
            string nextMessage = _messages[_messageIndex];
            _view?.FadeMessageTo(nextMessage);
            _messageIndex = (_messageIndex + 1) % _messages.Length;
            yield return new WaitForSeconds(4f);
        }
    }

    private IEnumerator DotsAnimationCoroutine()
    {
        string[] dotsCycle = { ".", "..", "..." };
        int idx = 0;
        while (true)
        {
            _view?.SetDots(dotsCycle[idx]);
            idx = (idx + 1) % dotsCycle.Length;
            yield return new WaitForSeconds(0.5f);
        }
    }
}
