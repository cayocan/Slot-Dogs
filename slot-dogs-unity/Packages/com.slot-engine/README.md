# Slot Engine

Generic slot machine engine for Unity.

## Features
- State machine (Idle → Spinning → ShowResult → FreeSpins)
- Configurable grid (columns × rows)
- Symbol library (symbolId → prefab)
- Generic `ISlotMachineView` with extension points
- Multi-step setup wizard (`Tools → Slot Engine → Game Setup Wizard`)
- Optional Spine support (`#if SPINE_EXISTS`)
- Optional DOTween support (`#if DOTWEEN_EXISTS`)

## Installation
Copy this folder to `Packages/com.slot-engine/` in your Unity project.

## Usage
1. Open `Tools → Slot Engine → Game Setup Wizard`
2. Fill in game name, symbols, buttons and background
3. Click "Gerar Jogo" to generate scenes and prefabs
4. Implement `ISpinProvider` for your game's spin logic
5. Subclass `GenericSlotMachineView` to add game-specific behavior

## Assemblies
- `SlotEngine.Runtime` — runtime engine (autoReferenced)
- `SlotEngine.Editor` — setup wizard (editor-only)

## Compile Defines
- `SPINE_EXISTS` — enable Spine skeleton support
- `DOTWEEN_EXISTS` — enable DOTween animations
