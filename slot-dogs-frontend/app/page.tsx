'use client';

import { useEffect, useState } from 'react';
import SessionPanel from '@/components/SessionPanel';
import VerifyPanel from '@/components/VerifyPanel';

const GAME_URL = (typeof process !== 'undefined' && process.env.NEXT_PUBLIC_GAME_URL) || '/game/index.html';

interface SlotSessionMessage {
  type: 'slot-session';
  sessionId: string;
  serverSeedHash: string;
  clientSeed: string;
  nonce: number;
  coins: number;
  freeSpinsRemaining: number;
}

export default function Home() {
  const [gameReady, setGameReady] = useState<boolean | null>(null); // null = checando
  const [liveSessionId, setLiveSessionId] = useState<string | null>(null);
  const [liveClientSeed, setLiveClientSeed] = useState<string | null>(null);

  useEffect(() => {
    fetch('/api/game-status')
      .then((r) => r.json())
      .then((d) => setGameReady(d.ready))
      .catch(() => setGameReady(false));
  }, []);

  // Escuta mensagens postMessage enviadas pelo Unity WebGL (SlotBridge.cs)
  useEffect(() => {
    const handler = (e: MessageEvent) => {
      const data = e.data as SlotSessionMessage;
      if (!data || data.type !== 'slot-session') return;
      if (data.sessionId) setLiveSessionId(data.sessionId);
      if (data.clientSeed) setLiveClientSeed(data.clientSeed);
    };
    window.addEventListener('message', handler);
    return () => window.removeEventListener('message', handler);
  }, []);

  return (
    <div className="flex h-screen bg-zinc-950 text-white overflow-hidden">
      {/* ── Área do jogo ── */}
      <main className="flex-1 flex flex-col min-w-0">
        <header className="flex items-center gap-3 px-5 py-3 border-b border-zinc-800 shrink-0">
          <span className="text-xl">🐕</span>
          <h1 className="text-sm font-bold tracking-widest uppercase text-amber-400">
            Slot Dogs
          </h1>
        </header>

        {/* contêiner que centraliza e força 16:9 */}
        <div
          className="flex-1 bg-black flex items-center justify-center overflow-hidden"
          style={{ containerType: 'size' }}
        >
          <div
            className="relative"
            style={{
              width: 'min(100cqw, calc(100cqh * 16 / 9))',
              height: 'min(100cqh, calc(100cqw * 9 / 16))',
            }}
          >
            {gameReady === null && (
              <div className="flex items-center justify-center h-full text-zinc-600 text-sm">
                Carregando...
              </div>
            )}

            {gameReady === false && (
              <div className="flex flex-col items-center justify-center h-full gap-3 text-zinc-600">
                <span className="text-5xl">🎰</span>
                <p className="text-sm font-mono text-zinc-500">Build WebGL não encontrado</p>
                <p className="text-xs text-zinc-700 text-center max-w-xs">
                  Exporte o Unity para WebGL e copie os arquivos para{' '}
                  <code className="text-amber-700">public/game/</code>
                </p>
              </div>
            )}

            {gameReady === true && (
              <iframe
                src={GAME_URL}
                className="absolute inset-0 w-full h-full border-0"
                allow="fullscreen"
                title="Slot Dogs WebGL"
              />
            )}
          </div>
        </div>
      </main>

      {/* ── Sidebar ── */}
      <aside className="w-80 shrink-0 border-l border-zinc-800 flex flex-col overflow-y-auto">
        <div className="p-4 space-y-4">
          <SessionPanel liveSessionId={liveSessionId} />
          <VerifyPanel prefillClientSeed={liveClientSeed} />
        </div>
      </aside>
    </div>
  );
}
