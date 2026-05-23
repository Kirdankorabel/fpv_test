# FPV Sim — MVP Design

**Date:** 2026-05-23
**Status:** Draft, awaiting user review
**Scope:** First implementable spec for a sim-grade FPV mobile simulator.

## Context

Project root: `D:\Unity\fpv_test`. Unity 6 (6000.4.7f1), URP (Mobile and PC RP assets already present), single `SampleScene`, no scripts yet. The product target is a Defense Tech FPV simulator for Android, with sim-grade flight feel comparable to Liftoff / Velocidrone. Long-term roadmap (out of this spec): Angle mode, gate-passage and lap timing, extended telemetry, FPV camera effects (chromatic, RPM vibration), audio, scenarios / missions, multiplayer.

This MVP covers a single playable scene with a sim-grade flight model, dual-stick touch input (plus keyboard/gamepad in editor), an urban placeholder polygon, crash and respawn, and a basic OSD.

## Goals

- A drone that feels like an FPV quad in Acro mode, not arcade.
- Strict layering: input is decoupled from flight logic; flight logic is decoupled from rendering and game state.
- Composition via Zenject DI. Classic OOP patterns (Strategy, State, Observer/Signals, Factory, Adapter, Chain of Responsibility) explicit in the design.
- 60 FPS on mid-tier Android (Snapdragon 7xx, 2022-2023).
- All non-MonoBehaviour logic unit-testable in Edit Mode.

## Non-goals (deferred to later specs)

- Angle / self-leveling mode.
- Gate-passage detection and lap timing.
- Telemetry overlay beyond minimal OSD.
- Audio.
- Multiplayer / networking.
- Mission/scenario system.
- Realistic city assets (MVP uses primitive building placeholders).
- Streaming / Addressables.
- Damage model, failsafes, prop wash.

## Architecture overview

Six logical layers, top to bottom, each crossed only via interfaces. Cross-layer events go upward only through `SignalBus`.

```
+-----------------------------------------------------------------+
| Presentation:  TouchSticks  OSD  SettingsMenu                   |
+-----------------------------------------------------------------+
| Input:         UnityInputSystemPilotProvider -> PilotCommand    |
+-----------------------------------------------------------------+
| Game logic:    DroneStateMachine  RespawnService                |
+-----------------------------------------------------------------+
| Flight ctrl:   BetaflightRateController -> QuadXMotorMixer      |
+-----------------------------------------------------------------+
| Physics:       SubsteppedFlightDynamics -> Rigidbody (PhysX)    |
+-----------------------------------------------------------------+
| Rendering:     URP Mobile, LOD, occlusion, baked GI             |
+-----------------------------------------------------------------+
   composition:  Zenject (ProjectContext / SceneContext / GOC)
   events up:    SignalBus (Arm/Disarm/Crash/Respawn)
```

### DI composition

Three Zenject contexts:

**ProjectContext** (cross-scene singletons):
- `ISettingsService` — read/write rates, expo, camera tilt, FOV (persisted to `PlayerPrefs`).
- `SignalBus` — Zenject's bus, global event channel.

**SceneContext** (per scene; MVP has one scene `Polygon`):
- `IPilotInputProvider` — facade over Unity Input System, returns `PilotCommand`.
- `IDroneFactory` — `IFactory<DroneSpawnPoint, DroneFacade>`, spawns a drone with its own sub-container.
- `IRespawnService` — listens to `DroneCrashedSignal`, picks safe spawn point, triggers `IDroneFactory`.
- `IGateRegistry` — empty stub in MVP, contract ready for future gate-passage feature.
- All `DroneSpawnPoint` instances in scene (bound by `FromMethod` query).

**GameObjectContext** on the drone prefab (per-entity sub-container):
- `IFlightDynamics` — sub-stepped force/torque integrator.
- `IRateController` — Strategy. `BetaflightRateController` is the single MVP implementation.
- `IMotorMixer` — Strategy. `QuadXMotorMixer` for X-frame.
- `IDroneStateMachine` — State pattern owner.
- `IDroneState` implementations: `DisarmedState`, `ArmingState`, `FlyingState`, `CrashedState`, `RespawningState`.
- `IFpvCamera` — tilt + FOV control.
- `IOsdView` — UI overlay for mode/throttle/time/FPS.
- `FlightConfig` (ScriptableObject) — physics + rate controller tuning.

### Patterns used (named explicitly)

| Pattern | Where |
|---|---|
| Strategy | `IRateController`, `IMotorMixer`, `IPilotInputProvider` |
| State | `IDroneState` + `DroneStateMachine` |
| Observer | `SignalBus` (Arm/Disarm/Crash/Respawn signals) |
| Factory | `IDroneFactory` via Zenject `IFactory<,>` |
| Adapter | `UnityInputSystemPilotProvider` wraps Input System into `IPilotInput` |
| Chain of Responsibility | Flight pipeline: PilotCommand -> RateController -> MotorMixer -> FlightDynamics -> Rigidbody |
| Composition root | Zenject installers; no `FindObjectOfType` calls in domain code |

## Flight dynamics

### Value objects (immutable structs in `FpvSim.Flight`)

```csharp
public readonly struct PilotCommand {
    public readonly float Throttle;
    public readonly float Roll;
    public readonly float Pitch;
    public readonly float Yaw;
    public readonly bool ArmRequested;
    public readonly bool RespawnRequested;
}

public readonly struct TargetBodyRates {
    public readonly float RollRate;
    public readonly float PitchRate;
    public readonly float YawRate;
}

public readonly struct TorqueDemand {
    public readonly float Roll;
    public readonly float Pitch;
    public readonly float Yaw;
}

public readonly struct MotorThrusts {
    public readonly float M1, M2, M3, M4;
}
```

### Rate controller (Strategy)

`BetaflightRateController` implements two operations:

```csharp
TargetBodyRates ComputeTargetRates(PilotCommand cmd, FlightConfig cfg);
TorqueDemand Update(TargetBodyRates target, Vector3 currentRatesRad, float dt);
```

`ComputeTargetRates` applies Betaflight 4.x rates formula (rcRate × superRate × expo) per axis. Stick value -1..1 maps to target rad/s.

`Update` runs three independent PID loops (roll, pitch, yaw):
```
error = target - measured
P = Kp * error
I = clamp(I_prev + Ki * error * dt, -I_max, I_max)
D = -Kd * (measured - measured_prev) / dt
output = clamp(P + I + D, -1, 1)
```
Anti-windup is the `I` clamp. D-term is computed on measurement (not on error) to avoid derivative kick on setpoint changes.

### Motor mixer (Strategy)

`QuadXMotorMixer` for X-frame. Motor order is our internal convention (M1 front-left CCW, M2 front-right CW, M3 rear-left CW, M4 rear-right CCW), not Betaflight's M1=RR/M2=FR/M3=RL/M4=FL. When we later integrate Betaflight rate calc compatibility tests or import real logs, a remap shim sits between them; the in-engine mixer formulas below are written against our convention:

```
M1 = throttle - roll + pitch - yaw
M2 = throttle + roll + pitch + yaw
M3 = throttle - roll - pitch + yaw
M4 = throttle + roll - pitch - yaw
```

Each motor clamped to `[0, 1]`. If any exceeds 1, all motors are scaled down by the same amount so that angular authority is preserved at full throttle (Betaflight "air mode" normalization).

### Flight dynamics integrator

`SubsteppedFlightDynamics` runs inside `FixedUpdate` with substepping 10× (effective 500 Hz at default 50 Hz fixed update).

Per substep, per motor `i`:

1. Motor response, first-order lag:
   `rpm_i += (demand_i - rpm_i) * (dt / motorTau)` with `motorTau = 0.030 s`.
2. Battery sag:
   `V_eff = V_nominal - I_total * R_internal`, where `I_total ∝ Σ rpm_i²`. Available thrust scales by `(V_eff / V_nominal)²`.
3. Thrust:
   `T_i = T_max * rpm_i² * (V_eff / V_nominal)² * groundEffectMultiplier(altitude)`.
4. Reactive torque (Y axis):
   `τ_i = spinSign_i * k_torque * rpm_i²`.
5. `Rigidbody.AddForceAtPosition(transform.up * T_i, motorWorldPos_i)` — produces thrust and natural roll/pitch torque.
6. `Rigidbody.AddRelativeTorque(0, Σ τ_i, 0)` — applies the residual reactive yaw.

Per substep, once:
- Linear drag: `F_drag = -k_lin * v * v.magnitude`.
- Angular drag: `M_drag = -k_ang * ω * ω.magnitude`.

Ground effect: `mult = 1 + groundEffectStrength * max(0, 1 - altitude / propDiameter)` with `groundEffectStrength = 0.15`, `propDiameter = 0.127 m` (5").

### FlightConfig (ScriptableObject)

Tunable fields: `mass`, diagonal `inertiaTensor`, `motorPositions[4]`, `motorSpinDirections[4]`, `T_max`, `motorTau`, `k_torque`, `k_lin_drag`, `k_ang_drag`, `propDiameter`, `groundEffectStrength`, `V_nominal`, `R_internal`, `Kp/Ki/Kd × 3 axes`, `rcRate/superRate/expo × 3 axes`, `cameraTiltDeg`, `fovDeg`. Defaults represent a typical 220 mm 5" race quad, ~600 g, 2200 KV motors, 6S Lipo.

### Ticker (only MonoBehaviour in the flight stack)

```csharp
public sealed class FlightDynamicsTickerMb : MonoBehaviour {
    [Inject] IPilotInputProvider input;
    [Inject] IDroneStateMachine fsm;
    void FixedUpdate() => fsm.Tick(input.Read(), Time.fixedDeltaTime);
}
```

The MonoBehaviour is the entry point only. All physics, control, and state logic live in plain C# classes resolved by Zenject — testable without a scene.

## Input layer

### Contract

```csharp
public interface IPilotInputProvider {
    PilotCommand Read();
}
```

Called exactly once per FixedUpdate. Substep loop keeps the same command for the whole tick — deterministic and free of aliasing between input and physics rates.

### Unity Input System actions

Single `ActionMap` "Pilot" in `Pilot.inputactions`:

```
Throttle    Value/Axis  1D
Yaw         Value/Axis  1D
Pitch       Value/Axis  1D
Roll        Value/Axis  1D
Arm         Button
Respawn     Button
ModeToggle  Button
```

Bindings:
- Touch: two `OnScreenStick` components write Throttle+Yaw and Pitch+Roll.
- Gamepad: `<Gamepad>/leftStick` and `<Gamepad>/rightStick` per axis; south/north buttons for Arm/Respawn.
- Keyboard (editor only): WASD throttle/yaw, arrows pitch/roll, Space arm, R respawn.

Input System merges active bindings — touch and gamepad can coexist.

### Adapter

`UnityInputSystemPilotProvider : IPilotInputProvider, IDisposable` wraps the generated `PilotInputActions` class. Button events are latched in `performed` callbacks and consumed in the next `Read()` to avoid losing edge events between Update (where events fire) and FixedUpdate (where we read).

### Scripted provider for tests

```csharp
public sealed class ScriptedPilotProvider : IPilotInputProvider {
    readonly Func<float, PilotCommand> sequence;
    float t;
    public ScriptedPilotProvider(Func<float, PilotCommand> sequence) => this.sequence = sequence;
    public PilotCommand Read() { var c = sequence(t); t += Time.fixedDeltaTime; return c; }
}
```

### Settings vs input

Rates and expo are flight controller config, not input concern. Input returns raw stick -1..1; `BetaflightRateController.ComputeTargetRates` applies expo. This allows multiple rate profiles with a single input scheme.

## Drone state machine

State pattern. FSM is the owner; each state is a stateless singleton with `Enter/Tick/Exit`.

| State | Behaviour | Transitions out |
|---|---|---|
| `DisarmedState` | Motors hard-zeroed, `Rigidbody.isKinematic = true`, OSD shows "DISARMED". | Arm → `ArmingState` |
| `ArmingState` | 300 ms timer, OSD blinks "ARMING", respawn blocked. | Timer → `FlyingState` |
| `FlyingState` | Full pipeline live. | Crash signal → `CrashedState`; Disarm → `DisarmedState` |
| `CrashedState` | Motors off, Rigidbody stays dynamic so it falls/rolls naturally, OSD "CRASHED — tap to respawn". | Respawn requested → `RespawningState` |
| `RespawningState` | Teleport to spawn point, reset velocities, 500 ms freeze. | Timer → `DisarmedState` |

```csharp
public interface IDroneState {
    void Enter(DroneContext ctx);
    void Tick(DroneContext ctx, PilotCommand cmd, float dt);
    void Exit(DroneContext ctx);
}

public sealed class DroneContext {
    public Rigidbody Rb;
    public Transform Root;
    public IFlightDynamics Dynamics;
    public IDroneStateMachine Fsm;
    public SignalBus Signals;
    public DroneSpawnPoint SpawnPoint;
    public float TimeInState;
}
```

`DroneContext` is a DTO passed per call. The FSM maintains `TimeInState`: zeroed on every `ChangeState<T>()`, incremented by `dt` before each `Tick`. Timed states (`ArmingState`, `RespawningState`) read `ctx.TimeInState` and transition when their threshold is reached. State classes themselves hold no mutable fields, so Zenject binds each as `AsSingle()`. FSM holds them in `Dictionary<Type, IDroneState>`.

## Crash detection

`DroneCrashDetectorMb` listens to Unity `OnCollisionEnter`. If `collision.relativeVelocity.magnitude > crashSpeedThreshold` (default 4 m/s), it fires `DroneCrashedSignal { ImpactPoint, ImpactSpeed }`. FSM subscribes and transitions from `FlyingState` → `CrashedState`. Detector knows nothing about FSM, FSM knows nothing about colliders — both only know the signal. Pure Observer pattern via `SignalBus`.

## Respawn

`IRespawnService` lives on SceneContext.

```csharp
public interface IRespawnService {
    DroneSpawnPoint PickSafeSpawn(Vector3 lastKnownPosition);
}
```

`NearestSafeSpawnService` picks the nearest spawn point to the crash location. (Later specs can swap for furthest-from-obstacles, free-aerial, etc., without touching the FSM.)

`RespawningState.Enter` teleports the rigidbody, zeroes `linearVelocity` and `angularVelocity`, calls `IFlightDynamics.ResetIntegrators()` (clears PID I-terms and motor RPM lag state), and fires `DroneRespawnedSignal`.

(Note: Unity 6 uses `linearVelocity` — `velocity` is deprecated.)

## Signals (finalized)

```csharp
public readonly struct DroneArmedSignal {}
public readonly struct DroneDisarmedSignal {}
public readonly struct DroneCrashedSignal {
    public readonly Vector3 ImpactPoint;
    public readonly float ImpactSpeed;
    public DroneCrashedSignal(Vector3 p, float s) { ImpactPoint = p; ImpactSpeed = s; }
}
public readonly struct DroneRespawnedSignal {}
```

MVP consumers:
- `OsdView` — Arm/Disarm/Crash/Respawn for mode text.
- `CrashFxController` (placeholder in MVP) — Crash for debris particles.
- `DroneStateMachine` — Crash for transition.

## Scene "Polygon"

### Hierarchy

```
Polygon
├── [DI]
│   └── SceneContext
├── [Environment]
│   ├── Lighting              Sun directional + Reflection Probe
│   ├── Sky                   Procedural skybox
│   ├── Terrain               Flat 1024×1024 m mesh, splat asphalt/grass/concrete
│   └── Buildings             root for GPU-instanced prefabs
│       └── BuildingCluster_{NE,NW,SE,SW}
├── [Gameplay]
│   ├── SpawnPoints           4-6 DroneSpawnPoint markers
│   ├── DroneRoot             empty; drone spawned at runtime via IDroneFactory
│   └── KillVolumes           BoxCollider triggers (below map, beyond 1 km)
├── [UI]
│   ├── HudCanvas             Screen Space Overlay (TouchSticksOverlay, OsdView)
│   └── SettingsCanvas        disabled by default
└── [Cameras]
    └── (FpvCamera spawned as child of drone)
```

### Building placeholders

`BuildingPaletteSo` ScriptableObject holds 5-7 prefabs: `Building_Small_5x5x10`, `Building_Mid_10x10x20`, `Building_Tall_15x15x35`, `Shed_3x3x4`, `Tower_5x5x50`. Each uses Cube mesh + URP/Lit (mobile variant) + a single shared material (windows + walls atlas) so GPU instancing batches them.

Layout is hand-placed in editor. An optional `BuildingPainter` `EditorWindow` allows click-to-place from the palette to speed up filling the map; results serialize as standard prefab instances.

### LOD

Each building prefab has `LODGroup` with three levels:
- LOD0 (0-50 m): full mesh + normal map. ~500 tris.
- LOD1 (50-150 m): simplified mesh, no normal map. ~80 tris.
- LOD2 (150-500 m): quad imposter with baked texture, billboarded. 2 tris.
- Culled beyond 500 m.

### Occlusion culling

`Window → Rendering → Occlusion Culling` baked. Buildings marked Occluder + Occludee Static. Significant savings expected in dense building clusters where the drone sits at low altitude in alleys.

### Lighting

- One `Directional Light` (sun), Mode = Mixed. Real-time shadows restricted to drone via URP Light Layers.
- Baked GI for terrain + buildings, lightmap resolution 8-10 texels/unit.
- One Reflection Probe blended on the drone.
- Zero real-time point/spot lights in MVP.

### URP Mobile RP asset

Reuse existing `Mobile_RPAsset` with these values:
- Rendering Path: Forward
- MSAA: Disabled (FXAA in post)
- HDR: Disabled
- Render Scale: 0.85 (later dynamic via `UniversalRenderPipeline.asset.renderScale`)
- Main Light Shadows: Soft, 1024×1024 atlas
- Cascade Count: 1
- Additional Lights: Disabled
- Post-processing: Enabled (FXAA, Bloom low quality)

### FPV camera

```csharp
public interface IFpvCamera {
    void SetTiltDegrees(float t);
    void SetFovDegrees(float fov);
}
```

`FpvCameraMb` is a child of the drone prefab, injected via `GameObjectContext`. Defaults: tilt 30°, FOV 110°.

### Drone visual placeholder

`DroneVisual` prefab: parent empty (with `Rigidbody`, `Collider`, `FlightDynamicsTickerMb`, `DroneCrashDetectorMb`, `FpvCameraMb`) + 5 children: 4 small cubes at corners (motors) + 1 box at center (frame). Single shared material, GPU-instanced. ~50 tris.

### DroneFactory

```csharp
public interface IDroneFactory {
    DroneFacade Create(DroneSpawnPoint spawn);
}

public sealed class DroneFactory : IFactory<DroneSpawnPoint, DroneFacade> {
    [Inject] DiContainer container;
    [Inject] GameObject dronePrefab;
    public DroneFacade Create(DroneSpawnPoint spawn) {
        var go = container.InstantiatePrefab(dronePrefab, spawn.transform.position, spawn.transform.rotation, null);
        return go.GetComponent<DroneFacade>();
    }
}
```

`container.InstantiatePrefab` ensures the prefab's `GameObjectContext` chains the scene's container as parent; injection propagates automatically.

`SceneStartupMb` spawns the drone once at scene start via `IDroneFactory.Create(spawnPoints[0])`.

### SceneContext bindings

```csharp
Container.Bind<IRespawnService>().To<NearestSafeSpawnService>().AsSingle();
Container.Bind<IDroneFactory>().To<DroneFactory>().AsSingle();
Container.Bind<IGateRegistry>().To<EmptyGateRegistry>().AsSingle();
Container.Bind<DroneSpawnPoint[]>()
    .FromMethod(_ => Object.FindObjectsByType<DroneSpawnPoint>(FindObjectsSortMode.None))
    .AsSingle();
Container.DeclareSignal<DroneArmedSignal>();
Container.DeclareSignal<DroneDisarmedSignal>();
Container.DeclareSignal<DroneCrashedSignal>();
Container.DeclareSignal<DroneRespawnedSignal>();
```

## Performance budget

Target Snapdragon 7xx, 60 FPS, frame budget 16.6 ms.

| Category | Budget |
|---|---|
| Tris on screen, avg | ≤ 150k |
| Tris on screen, peak | ≤ 250k |
| Draw calls | ≤ 80 (with GPU instancing) |
| Unique materials | ≤ 8 |
| Real-time lights | 1 (sun) |
| Shadow casters | 1 (drone only) |
| Texture memory | ≤ 256 MB |
| Physics FixedUpdate | ≤ 2 ms |
| Substep loop (10×) | ≤ 1 ms |

OSD shows fixed `Application.targetFrameRate` and live FPS so budget breaches are immediately visible during play.

## Testing

### Edit Mode (NUnit, no scene) — primary surface

```
Tests/EditMode/
  Flight/
    RateController/
      ExpoCurveTests
      PidProportionalTests
      PidIntegralWindupTests
      PidDerivativeOnMeasurementTests
    MotorMixer/
      HoverTests
      PureRollTests
      PurePitchTests
      PureYawTests
      AirModeNormalizationTests
    FlightDynamics/
      MotorResponseLagTests
      GroundEffectTests
      BatterySagTests
      DragQuadraticTests
    StateMachine/
      TransitionsTests
      CrashIgnoredOutsideFlyingTests
    Rates/
      BetaflightCompatibilityTests
  Architecture/
    LayeringTests
```

### Architecture / lint tests (NUnit + reflection)

- `FlightLayer_DoesNotReferenceInputSystem` — types in `FpvSim.Flight` do not reference `UnityEngine.InputSystem.*`.
- `FlightLayer_DoesNotReferenceUI` — same for `UnityEngine.UI`, `TMPro`.
- `FlightDynamics_OnlyDependsOnRigidbody_NotMonoBehaviour` — `SubsteppedFlightDynamics` has no fields of `MonoBehaviour` type.
- `States_AreStateless` — `IDroneState` implementations only have readonly fields (timer state lives in `DroneContext.TimeInState`, not in the state class).

### Play Mode (Unity Test Runner) — minimal, integration with PhysX

- `HoverScenarioTest` — drone at 10 m, throttle held empirically tuned to hover defaults for 3 s; assert `|velocity| < 0.2 m/s` and altitude in `[9.5, 10.5]`.
- `ForwardFlightScenarioTest` — throttle 0.55 + pitch 0.3 for 2 s; assert horizontal speed > 5 m/s and altitude loss < 2 m.
- `CrashTriggersStateTransitionTest` — drone at 5 m, throttle 0; assert FSM is in `CrashedState` within 1 s after ground impact.
- `RespawnResetsVelocityTest` — fly forward, trigger respawn; assert `linearVelocity == zero` and `position == spawnPoint.position`.

Play Mode fixtures use `TestSceneContextInstaller` that swaps `IPilotInputProvider` for `ScriptedPilotProvider`. This is the payoff of DI for testing — single binding swap, no real input plumbing.

### Manual QA checklist (`docs/qa/mvp-checklist.md`)

Ritual before merge:
1. Editor PC + keyboard: arm → hover → fly forward 50 m → bank turn → crash → respawn, three times.
2. Editor PC + Xbox gamepad: same.
3. Android build on mid-tier (e.g. Pixel 6a, Snapdragon 778G): touch sticks, stable 60 FPS in Stats overlay, stick response feels controlled.

### Out of MVP testing scope

- Performance regression on CI.
- Visual regression (screenshot diff).
- Network fuzz / multiplayer.
- Input fuzz.

## Error handling

- DI resolution failures surface at scene load — Zenject throws `ZenjectException` with full binding chain. No silent fallbacks.
- Crash detector ignores `OnCollisionEnter` while not in `FlyingState` (cheap guard before signal).
- `IRespawnService.PickSafeSpawn` falls back to the first spawn point if the spawn array is empty (logs error). Empty spawn array is a configuration bug, not a runtime path.
- `FlightConfig` validation: `OnValidate` asserts positive `mass`, non-zero `inertiaTensor`, four motor positions present.

## File layout

```
Assets/
  FpvSim/
    Flight/                pure C# domain (no UnityEngine where avoidable)
      PilotCommand.cs
      TargetBodyRates.cs
      TorqueDemand.cs
      MotorThrusts.cs
      IRateController.cs
      BetaflightRateController.cs
      IMotorMixer.cs
      QuadXMotorMixer.cs
      IFlightDynamics.cs
      SubsteppedFlightDynamics.cs
      FlightConfig.cs
    States/
      IDroneState.cs
      DroneContext.cs
      IDroneStateMachine.cs
      DroneStateMachine.cs
      DisarmedState.cs
      ArmingState.cs
      FlyingState.cs
      CrashedState.cs
      RespawningState.cs
    Input/
      IPilotInputProvider.cs
      UnityInputSystemPilotProvider.cs
      ScriptedPilotProvider.cs
      Pilot.inputactions
      PilotInputActions.cs (generated)
    Signals/
      DroneSignals.cs
    Scene/
      DroneSpawnPoint.cs
      IRespawnService.cs
      NearestSafeSpawnService.cs
      IDroneFactory.cs
      DroneFactory.cs
      DroneFacade.cs
      SceneStartupMb.cs
      IGateRegistry.cs
      EmptyGateRegistry.cs
      BuildingPaletteSo.cs
    Mb/
      FlightDynamicsTickerMb.cs
      DroneCrashDetectorMb.cs
      FpvCameraMb.cs
      OsdViewMb.cs
      TouchSticksOverlayMb.cs
    Installers/
      ProjectInstaller.cs
      SceneInstaller.cs
      DroneInstaller.cs
    Resources/
      Flight/
        DefaultQuad.asset            (FlightConfig)
  Scenes/
    Polygon.unity
Tests/
  EditMode/
    Flight/...
    Architecture/...
  PlayMode/
    Flight/...
```

## Risks

- Sim-grade Acro with dual-stick touch is hard for users. Default rates must be tamer than typical PC FPV defaults and expo aggressive. Tuning will need iteration; spec accepts that defaults out of MVP may need follow-up.
- 1 km urban + LOD on Snapdragon 7xx is at the upper edge of the budget. If the polygon is densely packed, MVP may need to reduce cluster density or LOD0 distance. The performance budget is the gate, not the scene size.
- Substepped physics × 500 Hz may exceed 1 ms budget on weaker mid-tier. Fallback plan: reduce substeps to 5 (250 Hz) and re-validate feel.

## Open questions

None blocking. Future specs will cover:
- Angle/self-leveling mode (adds outer attitude PID loop atop the existing rate loop).
- Gate-passage and lap timing (uses the already-bound `IGateRegistry` interface).
- FPV camera effects (RPM-driven shake, lens distortion).
- Audio.
- Network multiplayer (will require revisiting determinism of `SubsteppedFlightDynamics`).
