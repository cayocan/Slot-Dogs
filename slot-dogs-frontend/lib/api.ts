const BASE_URL = (typeof process !== 'undefined' && process.env.NEXT_PUBLIC_API_URL) || 'http://localhost:3000';

export interface SessionState {
  sessionId: string;
  serverSeedHash: string;
  coins: number;
  betPerLine: number;
  nonce: number;
  freeSpinsRemaining: number;
  clientSeed: string | null;
}

export interface VerifyResult {
  valid: boolean;
  recomputedStops: number[];
}

export async function createSession(): Promise<SessionState> {
  const res = await fetch(`${BASE_URL}/session`, { method: 'POST' });
  if (!res.ok) throw new Error(`POST /session: ${res.status}`);
  return res.json();
}

export async function getSession(sessionId: string): Promise<SessionState | null> {
  const res = await fetch(`${BASE_URL}/session/${sessionId}`);
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`GET /session/${sessionId}: ${res.status}`);
  return res.json();
}

export async function verifySpin(params: {
  serverSeed: string;
  clientSeed: string;
  nonce: number;
  stops: number[];
}): Promise<VerifyResult> {
  const res = await fetch(`${BASE_URL}/verify`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(params),
  });
  if (!res.ok) throw new Error(`POST /verify: ${res.status}`);
  return res.json();
}
