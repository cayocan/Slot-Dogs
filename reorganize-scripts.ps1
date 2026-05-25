param()
$base = "c:\Users\cayoc\Desktop\Workspace\Projetos Unity\Slot Dogs\slot-dogs-unity\Assets\Scripts"

function Move-WithMeta($src, $dst) {
    $srcFile = "$base\$src"
    $dstFile = "$base\$dst"
    if (Test-Path $srcFile)        { Move-Item $srcFile $dstFile -Force }
    if (Test-Path "$srcFile.meta") { Move-Item "$srcFile.meta" "$dstFile.meta" -Force }
}

# ── Criar estrutura de destino ────────────────────────────────────────────────
@(
    "Engine\Core", "Engine\States", "Engine\Data", "Engine\Audio",
    "Games\SlotDogs\View", "Games\SlotDogs\Presenter",
    "Games\SlotDogs\Model", "Games\SlotDogs\Network",
    "Games\SlotDogs\Data", "Games\SlotDogs\Editor", "Games\SlotDogs\Utils"
) | ForEach-Object { New-Item -ItemType Directory -Path "$base\$_" -Force | Out-Null }

# ── Engine/Core ───────────────────────────────────────────────────────────────
Move-WithMeta "StateMachine\ISlotState.cs"       "Engine\Core\ISlotState.cs"
Move-WithMeta "StateMachine\SlotStateMachine.cs" "Engine\Core\SlotStateMachine.cs"
Move-WithMeta "StateMachine\SlotGameContext.cs"  "Engine\Core\SlotGameContext.cs"
Move-WithMeta "View\ISlotMachineView.cs"         "Engine\Core\ISlotMachineView.cs"
Move-WithMeta "Network\ISpinProvider.cs"         "Engine\Core\ISpinProvider.cs"

# ── Engine/States ─────────────────────────────────────────────────────────────
Move-WithMeta "StateMachine\States\SlotStateBase.cs"   "Engine\States\SlotStateBase.cs"
Move-WithMeta "StateMachine\States\IdleState.cs"       "Engine\States\IdleState.cs"
Move-WithMeta "StateMachine\States\SpinningState.cs"   "Engine\States\SpinningState.cs"
Move-WithMeta "StateMachine\States\ShowResultState.cs" "Engine\States\ShowResultState.cs"
Move-WithMeta "StateMachine\States\FreeSpinsState.cs"  "Engine\States\FreeSpinsState.cs"

# ── Engine/Data ───────────────────────────────────────────────────────────────
Move-WithMeta "Data\SlotMachineConfig.cs" "Engine\Data\SlotMachineConfig.cs"

# ── Engine/Audio ─────────────────────────────────────────────────────────────
Move-WithMeta "Audio\AudioManager.cs" "Engine\Audio\AudioManager.cs"

# ── Games/SlotDogs/View ───────────────────────────────────────────────────────
Move-WithMeta "View\SlotMachineView.cs" "Games\SlotDogs\View\SlotMachineView.cs"
Move-WithMeta "View\ReelStrip.cs"       "Games\SlotDogs\View\ReelStrip.cs"
Move-WithMeta "View\SessionView.cs"     "Games\SlotDogs\View\SessionView.cs"
Move-WithMeta "View\ISessionView.cs"    "Games\SlotDogs\View\ISessionView.cs"
Move-WithMeta "View\MenuView.cs"        "Games\SlotDogs\View\MenuView.cs"

# ── Games/SlotDogs/Presenter ─────────────────────────────────────────────────
Move-WithMeta "Presenter\SlotMachinePresenter.cs" "Games\SlotDogs\Presenter\SlotMachinePresenter.cs"
Move-WithMeta "Presenter\SessionPresenter.cs"     "Games\SlotDogs\Presenter\SessionPresenter.cs"

# ── Games/SlotDogs/Model ─────────────────────────────────────────────────────
Move-WithMeta "Model\SessionModel.cs" "Games\SlotDogs\Model\SessionModel.cs"

# ── Games/SlotDogs/Network ───────────────────────────────────────────────────
Move-WithMeta "Network\ApiClient.cs" "Games\SlotDogs\Network\ApiClient.cs"
Move-WithMeta "Network\Dtos.cs"      "Games\SlotDogs\Network\Dtos.cs"

# ── Games/SlotDogs/Data ──────────────────────────────────────────────────────
Move-WithMeta "Data\SymbolLibrary.cs" "Games\SlotDogs\Data\SymbolLibrary.cs"

# ── Games/SlotDogs/Editor ────────────────────────────────────────────────────
Move-WithMeta "Editor\SymbolLibraryEditor.cs" "Games\SlotDogs\Editor\SymbolLibraryEditor.cs"

# ── Games/SlotDogs/Utils ─────────────────────────────────────────────────────
Move-WithMeta "Utils\EnvConfig.cs" "Games\SlotDogs\Utils\EnvConfig.cs"

# ── Remover pastas antigas (vazias) ──────────────────────────────────────────
@(
    "StateMachine\States", "StateMachine",
    "View", "Presenter", "Model", "Network",
    "Audio", "Data", "Editor", "Utils"
) | ForEach-Object {
    $path = "$base\$_"
    if (Test-Path $path) { Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path "$path.meta") { Remove-Item "$path.meta" -Force -ErrorAction SilentlyContinue }
}

Write-Host "Reorganizacao concluida. Reabra o Unity." -ForegroundColor Green
