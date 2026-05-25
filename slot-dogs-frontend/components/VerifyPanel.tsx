'use client';

import { useState } from 'react';
import { verifySpin, VerifyResult } from '@/lib/api';

export default function VerifyPanel() {
  const [serverSeed, setServerSeed] = useState('');
  const [clientSeed, setClientSeed] = useState('');
  const [nonce, setNonce] = useState('');
  const [stops, setStops] = useState(['', '', '', '', '']);
  const [result, setResult] = useState<VerifyResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const updateStop = (i: number, val: string) => {
    setStops((prev) => { const next = [...prev]; next[i] = val; return next; });
  };

  const canSubmit =
    serverSeed.trim().length > 0 &&
    clientSeed.trim().length > 0 &&
    nonce.trim().length > 0 &&
    stops.every((s) => s.trim().length > 0);

  const handleVerify = async () => {
    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const res = await verifySpin({
        serverSeed: serverSeed.trim(),
        clientSeed: clientSeed.trim(),
        nonce: parseInt(nonce, 10),
        stops: stops.map((s) => parseInt(s, 10)),
      });
      setResult(res);
    } catch (e) {
      setError(String(e));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="bg-zinc-900 border border-zinc-700 rounded-xl p-4 space-y-4">
      <h2 className="text-sm font-semibold uppercase tracking-widest text-amber-400">
        Provably Fair
      </h2>
      <p className="text-zinc-500 text-xs">
        Verifique se um spin não foi adulterado inserindo o server seed revelado após a rotação.
      </p>

      <div className="space-y-2">
        <Input
          label="Server Seed (revelado)"
          value={serverSeed}
          onChange={setServerSeed}
          placeholder="64 chars hex"
          mono
        />
        <Input
          label="Client Seed"
          value={clientSeed}
          onChange={setClientSeed}
          placeholder="seed usado na sessão"
          mono
        />
        <Input
          label="Nonce"
          value={nonce}
          onChange={setNonce}
          placeholder="0"
          type="number"
        />

        <div>
          <p className="text-zinc-500 text-[10px] uppercase tracking-wider mb-1">Stop Positions (0–29)</p>
          <div className="grid grid-cols-5 gap-1">
            {stops.map((val, i) => (
              <input
                key={i}
                type="number"
                min={0}
                max={29}
                value={val}
                onChange={(e) => updateStop(i, e.target.value)}
                placeholder={`R${i + 1}`}
                className="bg-zinc-800 border border-zinc-600 rounded-lg px-2 py-1.5 text-xs text-white text-center font-mono placeholder-zinc-600 focus:outline-none focus:border-amber-500"
              />
            ))}
          </div>
        </div>
      </div>

      <button
        onClick={handleVerify}
        disabled={!canSubmit || loading}
        className="w-full bg-amber-500 hover:bg-amber-400 disabled:opacity-40 text-black font-semibold text-sm py-2 rounded-lg transition-colors"
      >
        {loading ? 'Verificando...' : 'Verificar Spin'}
      </button>

      {error && <p className="text-red-400 text-xs">{error}</p>}

      {result && (
        <div className={`rounded-lg border p-3 text-sm space-y-2 ${result.valid ? 'border-green-600 bg-green-950' : 'border-red-600 bg-red-950'}`}>
          <p className={`font-bold text-base ${result.valid ? 'text-green-400' : 'text-red-400'}`}>
            {result.valid ? '✔ Spin verificado — íntegro' : '✘ Falha na verificação — adulterado'}
          </p>
          <div>
            <p className="text-zinc-400 text-xs uppercase tracking-wider mb-1">Stops recalculados</p>
            <div className="flex gap-2">
              {result.recomputedStops.map((s, i) => (
                <div key={i} className="flex-1 text-center">
                  <p className="text-[10px] text-zinc-500">R{i + 1}</p>
                  <p className={`font-mono text-sm ${s !== parseInt(stops[i], 10) ? 'text-red-400' : 'text-zinc-200'}`}>{s}</p>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function Input({
  label,
  value,
  onChange,
  placeholder,
  mono = false,
  type = 'text',
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  mono?: boolean;
  type?: string;
}) {
  return (
    <div>
      <p className="text-zinc-500 text-[10px] uppercase tracking-wider mb-1">{label}</p>
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className={`w-full bg-zinc-800 border border-zinc-600 rounded-lg px-3 py-1.5 text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-amber-500 ${mono ? 'font-mono' : ''}`}
      />
    </div>
  );
}
