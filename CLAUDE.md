# COSC495 Project Parallax

## Project Overview
Unity AR/3D application with an interactive Earth model. Developed in 3D desktop mode for fast iteration, then deployed to AR via image tracking.

## Dual-Mode Architecture
The project has **two parallel modes** sharing the same Earth prefab:

### Desktop Mode (for development & prototyping)
- `Assets/Scripts/DesktopPlacement.cs` — Spawns Earth at a fixed position on Start
- `Assets/Scripts/DesktopInteraction.cs` — Mouse controls: left-drag to rotate, scroll to scale
- Scene: `DesktopScene` — regular Camera, no AR components

### AR Mode (for device builds)
- `Assets/Scripts/EarthPlacement.cs` — Image-tracking placement via `ARTrackedImageManager`
- `Assets/Scripts/EarthInteraction.cs` — Touch controls: drag to rotate, pinch to scale
- Scene: `SampleScene` — XR Origin, AR Session, AR Tracked Image Manager

### Shared
- Same Earth prefab used in both modes
- Both placement scripts expose `SpawnedEarth` property for their respective interaction scripts

## Unity Setup — Desktop Scene
1. Create new scene `DesktopScene`
2. Add Camera, Directional Light
3. Create empty GameObject, add `DesktopPlacement` + `DesktopInteraction`
4. Assign Earth prefab and wire references
5. Hit Play — instant iteration

## Unity Setup — AR Scene (SampleScene)
1. XR Origin has `ARTrackedImageManager` with Reference Image Library assigned
2. `EarthPlacement` + `EarthInteraction` on XR Origin
3. Max Number of Moving Images = 1
4. Reference image physical size must match actual printed size

## Recent Changes
- 2026-03-04: Switched from plane detection to image tracking for drift-free AR placement
- 2026-03-04: Added desktop mode (DesktopPlacement + DesktopInteraction) for fast 3D prototyping
