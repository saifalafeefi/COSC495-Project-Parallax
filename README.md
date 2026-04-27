# Carbon Conquest

A single-player AR/desktop sustainability strategy game built in Unity. Place a holographic Earth on a real surface, then manage carbon, economy, and stability across its regions — one policy card at a time.

---

## What It Is

You get 6 policy cards per round and a globe full of regions that are slowly falling apart. Play cards to reduce carbon, prop up economies, and keep things politically stable before the world hits a tipping point. Regions affect their neighbors, crises cascade, and random events keep you on your toes for 10 rounds.

Built as a university project (COSC495) for Android AR. Also fully playable on PC in 3D desktop mode.

---

## Features

- **Interactive Earth** — orbit and zoom with mouse/touch, click regions to focus and inspect
- **Region traits** — Tropical, Arid, Frozen, Industrial, Coastal, Temperate each play differently and modify card effects
- **18 policy cards** across three rarities with spillover to neighboring regions
- **Random events** every round (heat waves, recessions, breakthroughs, etc.)
- **Shop system** — spend economy income on new cards between rounds
- **Reward system** — net-positive rounds earn a free card pick
- **Focus system** — punishes over-targeting the same region
- **Three difficulty levels** — Easy / Normal / Hard
- **AR mode** — place the Earth on a real surface using AR Foundation
- **Tutorial** — scripted walkthrough with a shopkeeper mascot
- **Codex** — in-game reference for all policies and events

---

## Stack

- Unity 6000.3.9f1 LTS
- Universal Render Pipeline (URP)
- AR Foundation (Android)
- TextMeshPro
- Unity Input System (Enhanced Touch)

---

## How to Run

1. Open in Unity 6 LTS
2. Run **Carbon Conquest > Generate All Policies and Events** from the editor menu
3. Run **Generate Difficulty Presets** and **Generate Trait Color Config**
4. Hit Play in `MainMenuScene`

For Android: build with AR Foundation installed and target Android 7.0+.

---

## Scenes

| Scene | Purpose |
|---|---|
| `MainMenuScene` | Main menu, settings, codex, tutorial entry |
| `DesktopScene` | 3D desktop mode (also used for tutorial on mobile) |
| `ARGameScene` | AR mode — places Earth on detected surface |

---

## Project Structure

```
Assets/
  Scripts/       — all C# game logic
  Resources/
    Policies/    — ScriptableObjects (Common / Uncommon / Rare)
    Events/      — ScriptableObjects (Normal / Focus)
    Difficulty/  — Easy / Normal / Hard presets
    TraitColors/ — trait color config
```

---

*COSC495 Project — Spring 2026*
