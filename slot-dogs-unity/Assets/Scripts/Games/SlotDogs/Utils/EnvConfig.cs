using System;
using System.Collections.Generic;
using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════════
//  EnvConfig — carrega e expõe as variáveis de Assets/Resources/app.env
//
//  Formato do arquivo (chave=valor, # para comentários):
//    DEV_API_URL=http://localhost:3000
//    PROD_API_URL=https://api.slotdogs.com
//
//  Acesso:
//    string url = EnvConfig.Get("DEV_API_URL");
//    string apiUrl = EnvConfig.ApiUrl;   // seleciona automaticamente dev/prod
// ═══════════════════════════════════════════════════════════════════════════════

public static class EnvConfig
{
    private const string ResourcePath = "app";          // Resources/app.env → "app"
    private const string DevUrlKey    = "DEV_API_URL";
    private const string ProdUrlKey   = "PROD_API_URL";
    private const string UseProdInEditorKey = "USE_PROD_API_IN_EDITOR";

    private static Dictionary<string, string> _vars;

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>
    /// URL da API selecionada automaticamente:
    /// Development Build → DEV_API_URL | Release → PROD_API_URL.
    /// Se USE_PROD_API_IN_EDITOR=true, sempre usa PROD_API_URL no Editor.
    /// </summary>
    public static string ApiUrl
    {
        get
        {
#if UNITY_EDITOR
            if (GetOrDefault(UseProdInEditorKey, "false").ToLowerInvariant() == "true")
                return Get(ProdUrlKey);
#endif
            return Debug.isDebugBuild ? Get(DevUrlKey) : Get(ProdUrlKey);
        }
    }

    /// <summary>
    /// Retorna o valor da chave. Lança <see cref="KeyNotFoundException"/> se ausente.
    /// </summary>
    public static string Get(string key)
    {
        EnsureLoaded();

        if (_vars.TryGetValue(key, out string value))
            return value;

        throw new KeyNotFoundException($"[EnvConfig] Chave '{key}' não encontrada em Resources/{ResourcePath}.env");
    }

    /// <summary>
    /// Retorna o valor da chave ou <paramref name="fallback"/> se ausente.
    /// </summary>
    public static string GetOrDefault(string key, string fallback = "")
    {
        EnsureLoaded();
        return _vars.TryGetValue(key, out string value) ? value : fallback;
    }

    // ── Carregamento ─────────────────────────────────────────────────────────

    private static void EnsureLoaded()
    {
        if (_vars != null) return;

        var asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
            throw new InvalidOperationException(
                $"[EnvConfig] Arquivo 'Resources/{ResourcePath}.env' não encontrado. " +
                "Certifique-se de que ele existe e a extensão está registrada no Unity como TextAsset.");

        _vars = Parse(asset.text);
    }

    private static Dictionary<string, string> Parse(string raw)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (string rawLine in raw.Split('\n'))
        {
            string line = rawLine.Trim();

            // ignora linhas vazias e comentários
            if (string.IsNullOrEmpty(line) || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            int sep = line.IndexOf('=');
            if (sep <= 0) continue;

            string key   = line.Substring(0, sep).Trim();
            string value = line.Substring(sep + 1).Trim();

            // remove aspas opcionais: "valor" ou 'valor'
            if (value.Length >= 2 &&
                ((value[0] == '"'  && value[value.Length - 1] == '"') ||
                 (value[0] == '\'' && value[value.Length - 1] == '\'')))
            {
                value = value.Substring(1, value.Length - 2);
            }

            result[key] = value;
        }

        return result;
    }
}
