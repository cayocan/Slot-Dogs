#!/usr/bin/env bash
set -euo pipefail

# Cores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # sem cor

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo ""
echo "========================================"
echo "  SLOT DOGS - Iniciando ambiente local"
echo "========================================"
echo ""

# -- Verificar Node.js ---------------------------------------------------------
if ! command -v node &>/dev/null; then
    echo -e "${RED}[ERRO] Node.js nao encontrado.${NC}"
    echo "       Instale via Homebrew:  brew install node"
    echo "       Ou acesse:             https://nodejs.org"
    exit 1
fi
echo -e "${GREEN}[OK]${NC} Node.js $(node -v)"

# -- Backend: instalar dependencias se necessario ------------------------------
echo ""
echo "[BACK] Verificando dependencias..."
if [ ! -d "$SCRIPT_DIR/slot-dogs-backend/node_modules" ]; then
    echo "[BACK] node_modules ausente - instalando..."
    (cd "$SCRIPT_DIR/slot-dogs-backend" && npm install) || {
        echo -e "${RED}[ERRO] Falha ao instalar dependencias do backend.${NC}"
        exit 1
    }
else
    echo "[BACK] Dependencias ja instaladas."
fi

# -- Frontend: instalar dependencias se necessario -----------------------------
echo ""
echo "[FRONT] Verificando dependencias..."
if [ ! -d "$SCRIPT_DIR/slot-dogs-frontend/node_modules" ]; then
    echo "[FRONT] node_modules ausente - instalando..."
    (cd "$SCRIPT_DIR/slot-dogs-frontend" && npm install) || {
        echo -e "${RED}[ERRO] Falha ao instalar dependencias do frontend.${NC}"
        exit 1
    }
else
    echo "[FRONT] Dependencias ja instaladas."
fi

# -- Iniciar Backend em janela de Terminal separada ----------------------------
echo ""
echo "[BACK] Iniciando servidor backend na porta 3000..."
osascript <<EOF
tell application "Terminal"
    do script "cd '$SCRIPT_DIR/slot-dogs-backend' && npm run dev"
    set name of front window to "Slot Dogs - Backend :3000"
end tell
EOF

# -- Aguardar backend inicializar ----------------------------------------------
echo "[BACK] Aguardando backend inicializar (5s)..."
sleep 5
echo "[BACK] Pronto."

# -- Iniciar Frontend em janela de Terminal separada ---------------------------
echo ""
echo "[FRONT] Iniciando servidor frontend na porta 3001..."
osascript <<EOF
tell application "Terminal"
    do script "cd '$SCRIPT_DIR/slot-dogs-frontend' && npm run dev -- --port 3001"
    set name of front window to "Slot Dogs - Frontend :3001"
end tell
EOF

# -- Aguardar frontend ficar disponivel ----------------------------------------
echo "[FRONT] Aguardando frontend ficar disponivel..."
TRIES=0
until curl -s -o /dev/null http://localhost:3001 2>/dev/null; do
    sleep 2
    TRIES=$((TRIES + 1))
    if [ "$TRIES" -ge 20 ]; then
        echo -e "${YELLOW}[AVISO] Timeout - abrindo navegador mesmo assim...${NC}"
        break
    fi
done

# -- Abrir navegador -----------------------------------------------------------
echo "[FRONT] Pronto. Abrindo navegador..."
open http://localhost:3001

echo ""
echo "========================================"
echo "  Servidores rodando!"
echo "  Backend  -> http://localhost:3000"
echo "  Frontend -> http://localhost:3001"
echo "========================================"
echo "  Janelas do Terminal ficam abertas."
echo "  Feche-as quando quiser encerrar."
echo ""
