// ═══════════════════════════════════════════════════════════════════════════════
//  SlotBridge — Unity → Frontend (WebGL postMessage)
//
//  Em builds WebGL, envia o estado da sessão atual ao frame pai (Next.js)
//  via window.parent.postMessage(). Silencioso em Editor e builds standalone.
// ═══════════════════════════════════════════════════════════════════════════════

using System.Runtime.InteropServices;
using SlotEngine;

public static class SlotBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void JS_PostSessionData(string json);
#endif

    /// <summary>
    /// Envia o estado atual da sessão ao frontend via postMessage.
    /// Só tem efeito em builds WebGL (não faz nada em Editor / standalone).
    /// </summary>
    public static void BroadcastSession(SessionModel model)
    {
        if (model == null || !model.HasSession) return;

#if UNITY_WEBGL && !UNITY_EDITOR
        JS_PostSessionData(BuildJson(model));
#endif
    }

    // ── helpers privados ──────────────────────────────────────────────────────

    private static string BuildJson(SessionModel model)
    {
        return string.Concat(
            "{\"type\":\"slot-session\"",
            ",\"sessionId\":\"",     Esc(model.SessionId),         "\"",
            ",\"serverSeedHash\":\"", Esc(model.ServerSeedHash),    "\"",
            ",\"clientSeed\":\"",    Esc(model.ClientSeed ?? ""),   "\"",
            ",\"nonce\":",           model.Nonce,
            ",\"coins\":",           model.Coins,
            ",\"freeSpinsRemaining\":", model.FreeSpinsRemaining,
            "}"
        );
    }

    private static string Esc(string s)
        => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
}
