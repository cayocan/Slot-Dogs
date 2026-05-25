'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import { createSession, getSession, SessionState } from '@/lib/api';

interface Props {
  onSessionChange?: (sessionId: string) => void;
  /** Session ID enviado pelo Unity via postMessage — auto-carrega a sessão */
  liveSessionId?: string | null;
}

export default function SessionPanel({ onSessionChange, liveSessionId }: Props) {
  const [session, setSession] = useState<SessionState | null>(null);
  const [sessionIdInput, setSessionIdInput] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const startPolling = useCallback((id: string) => {
    if (intervalRef.current) clearInterval(intervalRef.current);
    intervalRef.current = setInterval(async () => {
      try {
        const s = await getSession(id);
        if (s) setSession(s);
      } catch {
        // silencia erros de polling
      }
    }, 3000);
  }, []);

  const loadSession = useCallback(async (id: string) => {
    setLoading(true);
    setError(null);
    try {
      const s = await getSession(id);
      if (!s) { setError('Sessão não encontrada.'); return; }
      setSession(s);
      onSessionChange?.(id);
      startPolling(id);
    } catch (e) {
      setError(String(e));
    } finally {
      setLoading(false);
    }
  }, [onSessionChange, startPolling]);

  const handleCreate = async () => {
    setLoading(true);
    setError(null);
    try {
      const s = await createSession();
      setSession(s);
      setSessionIdInput(s.sessionId);
      onSessionChange?.(s.sessionId);
      startPolling(s.sessionId);
    } catch (e) {
      setError(String(e));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    return () => { if (intervalRef.current) clearInterval(intervalRef.current); };
  }, []);

  // Auto-carrega a sessão quando o Unity envia o ID via postMessage
  useEffect(() => {
    if (liveSessionId && liveSessionId !== session?.sessionId) {
      loadSession(liveSessionId);
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [liveSessionId]);

  const refresh = () => { if (session) loadSession(session.sessionId); };

  return (
    <div className="bg-zinc-900 border border-zinc-700 rounded-xl p-4 space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-sm font-semibold uppercase tracking-widest text-amber-400">
          Sessão
        </h2>
        <div className="flex items-center gap-2">
          {liveSessionId && session?.sessionId === liveSessionId && (
            <span className="text-[10px] bg-green-900 text-green-400 px-1.5 py-0.5 rounded font-mono">
              🎮 jogo ativo
            </span>
          )}
          {session && (
            <button onClick={refresh} title="Atualizar" className="text-zinc-400 hover:text-white text-xs">
              ↻
            </button>
          )}
        </div>
      </div>

      {/* Criar ou buscar sessão */}
      {!session && (
        <div className="space-y-2">
          <button
            onClick={handleCreate}
            disabled={loading}
            className="w-full bg-amber-500 hover:bg-amber-400 disabled:opacity-50 text-black font-semibold text-sm py-2 rounded-lg transition-colors"
          >
            {loading ? 'Criando...' : 'Nova Sessão'}
          </button>

          <div className="flex gap-2">
            <input
              type="text"
              value={sessionIdInput}
              onChange={(e) => setSessionIdInput(e.target.value)}
              placeholder="Session ID existente"
              className="flex-1 bg-zinc-800 border border-zinc-600 rounded-lg px-3 py-1.5 text-xs text-white placeholder-zinc-500 focus:outline-none focus:border-amber-500"
            />
            <button
              onClick={() => loadSession(sessionIdInput.trim())}
              disabled={!sessionIdInput.trim() || loading}
              className="bg-zinc-700 hover:bg-zinc-600 disabled:opacity-40 text-white text-xs px-3 py-1.5 rounded-lg transition-colors"
            >
              Buscar
            </button>
          </div>
        </div>
      )}

      {error && <p className="text-red-400 text-xs">{error}</p>}

      {/* Status da sessão */}
      {session && (
        <div className="space-y-2 text-xs">
          <Field label="Session ID">
            <span className="font-mono text-[10px] break-all text-zinc-300">{session.sessionId}</span>
          </Field>
          <div className="grid grid-cols-2 gap-2">
            <Field label="Moedas">
              <span className="text-amber-300 font-bold text-sm">{session.coins}</span>
            </Field>
            <Field label="Nonce">
              <span className="text-white font-mono">{session.nonce}</span>
            </Field>
            <Field label="Aposta/linha">
              <span className="text-white font-mono">{session.betPerLine}</span>
            </Field>
            <Field label="Free Spins">
              <span className={session.freeSpinsRemaining > 0 ? 'text-green-400 font-bold' : 'text-zinc-500'}>
                {session.freeSpinsRemaining}
              </span>
            </Field>
          </div>
          <Field label="Server Seed Hash">
            <span className="font-mono text-[10px] break-all text-zinc-400">{session.serverSeedHash}</span>
          </Field>
          <Field label="Client Seed">
            <span className="font-mono text-[10px] break-all text-zinc-400">
              {session.clientSeed ?? <span className="text-zinc-600 italic">não definido</span>}
            </span>
          </Field>

          <button
            onClick={() => { setSession(null); setSessionIdInput(''); if (intervalRef.current) clearInterval(intervalRef.current); }}
            className="w-full text-zinc-500 hover:text-red-400 text-xs pt-1 transition-colors"
          >
            Trocar sessão
          </button>
        </div>
      )}
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="text-zinc-500 text-[10px] uppercase tracking-wider mb-0.5">{label}</p>
      <div>{children}</div>
    </div>
  );
}
