import { existsSync } from 'fs';
import { join } from 'path';
import { NextResponse } from 'next/server';

export async function GET() {
  const gamePath = join(process.cwd(), 'public', 'game', 'index.html');
  const ready = existsSync(gamePath);
  return NextResponse.json({ ready });
}
