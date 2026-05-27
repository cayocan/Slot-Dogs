using System;
using UnityEngine;

/// <summary>
/// Controlador do menu principal. Coordena o Backend wake-up e habilita a UI
/// do menu apenas quando o backend estiver pronto.
/// Conecta: `BackendWakeUpView`, `BackendHealthService` e `MenuView`.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Backend Wake-up")]
    [SerializeField] private BackendWakeUpView backendWakeUpView;
    [SerializeField] private BackendHealthService backendHealthService;

    [Header("Menu")]
    [Tooltip("Root do menu (onde está a MenuView). Será ativado quando o backend estiver pronto")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private MenuView menuView;

    private BackendWakeUpPresenter _wakePresenter;

    private void Awake()
    {
        if (backendWakeUpView == null) Debug.LogWarning("[MainMenuController] backendWakeUpView não atribuído.");
        if (backendHealthService == null) Debug.LogWarning("[MainMenuController] backendHealthService não atribuído.");

        if (menuRoot == null && menuView != null) menuRoot = menuView.gameObject;
    }

    private void Start()
    {
        // criar presenter
        _wakePresenter = new BackendWakeUpPresenter(backendWakeUpView, backendHealthService, this);

        // Decide se deve executar o wake-up: não executa para o backend de dev (localhost)
        bool shouldWake = ShouldRunWakeUp();

        if (!shouldWake)
        {
            Debug.Log("[MainMenuController] Usando backend de desenvolvimento — pulando wake-up.");
            if (backendWakeUpView != null) backendWakeUpView.gameObject.SetActive(false);
            OnWakeUpComplete();
            return;
        }

        // inicia o fluxo de wake-up; o presenter cuidará do fade-out e chamará o callback
        _wakePresenter.StartWakeUp(OnWakeUpComplete);
    }

    private bool ShouldRunWakeUp()
    {
        try
        {
            string dev = EnvConfig.GetOrDefault("DEV_API_URL", "http://localhost:3000");
            string api = EnvConfig.ApiUrl;
            return !string.Equals(api, dev, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MainMenuController] Falha ao ler EnvConfig: {ex.Message}. Executando wake-up por segurança.");
            return true;
        }
    }

    private void OnWakeUpComplete()
    {
        Debug.Log("[MainMenuController] Backend pronto.");

        // garantir que a UI do menu esteja visível/ativa
        if (menuRoot != null) menuRoot.SetActive(true);
    }

    private void OnDestroy()
    {
        _wakePresenter?.StopWakeUp();
    }

    // Métodos públicos para debug/manual
    public void StartWakeUpNow()
    {
        if (_wakePresenter != null)
            _wakePresenter.StartWakeUp(OnWakeUpComplete);
    }

    public void SkipWakeUp()
    {
        _wakePresenter?.StopWakeUp();
        if (backendWakeUpView != null) backendWakeUpView.gameObject.SetActive(false);
        OnWakeUpComplete();
    }
}