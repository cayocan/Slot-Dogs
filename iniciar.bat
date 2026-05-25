@echo off
setlocal enabledelayedexpansion
title Slot Dogs

echo.
echo ========================================
echo   SLOT DOGS - Iniciando ambiente local
echo ========================================
echo.

:: -- Verificar Node.js --------------------------------------------------------
where node >nul 2>&1
if errorlevel 1 (
    echo [ERRO] Node.js nao encontrado. Instale em https://nodejs.org
    pause
    exit /b 1
)
for /f "tokens=*" %%v in ('node -v') do set NODE_VER=%%v
echo [OK] Node.js %NODE_VER%

:: -- Backend: instalar dependencias se necessario -----------------------------
echo.
echo [BACK] Verificando dependencias...
if not exist "%~dp0slot-dogs-backend\node_modules" (
    echo [BACK] node_modules ausente - instalando...
    pushd "%~dp0slot-dogs-backend"
    call npm install
    if errorlevel 1 (
        echo [ERRO] Falha ao instalar dependencias do backend.
        pause
        exit /b 1
    )
    popd
) else (
    echo [BACK] Dependencias ja instaladas.
)

:: -- Frontend: instalar dependencias se necessario ----------------------------
echo.
echo [FRONT] Verificando dependencias...
if not exist "%~dp0slot-dogs-frontend\node_modules" (
    echo [FRONT] node_modules ausente - instalando...
    pushd "%~dp0slot-dogs-frontend"
    call npm install
    if errorlevel 1 (
        echo [ERRO] Falha ao instalar dependencias do frontend.
        pause
        exit /b 1
    )
    popd
) else (
    echo [FRONT] Dependencias ja instaladas.
)

:: -- Iniciar Backend em janela separada ---------------------------------------
echo.
echo [BACK] Iniciando servidor backend na porta 3000...
start "Slot Dogs - Backend :3000" cmd /k "cd /d "%~dp0slot-dogs-backend" && npm run dev"

:: -- Aguardar backend inicializar ---------------------------------------------
echo [BACK] Aguardando backend inicializar (5s)...
timeout /t 5 /nobreak >nul
echo [BACK] Pronto.

:: -- Iniciar Frontend em janela separada --------------------------------------
echo.
echo [FRONT] Iniciando servidor frontend na porta 3001...
start "Slot Dogs - Frontend :3001" cmd /k "cd /d "%~dp0slot-dogs-frontend" && npm run dev -- --port 3001"

:: -- Aguardar frontend ficar disponivel e abrir navegador ---------------------
echo [FRONT] Aguardando frontend ficar disponivel...
set TRIES=0
:wait_frontend
timeout /t 2 /nobreak >nul
curl -s -o nul http://localhost:3001 2>nul
if errorlevel 1 (
    set /a TRIES+=1
    if !TRIES! lss 20 goto wait_frontend
    echo [AVISO] Timeout - abrindo navegador mesmo assim...
)

echo [FRONT] Pronto. Abrindo navegador...
start "" http://localhost:3001

echo.
echo ========================================
echo   Servidores rodando!
echo   Backend  -^> http://localhost:3000
echo   Frontend -^> http://localhost:3001
echo ========================================
echo   Feche esta janela quando quiser.
echo   Os servidores ficam em CMDs separados.
echo.
pause
