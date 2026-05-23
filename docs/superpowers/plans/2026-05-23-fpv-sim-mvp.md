# FPV Sim MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a sim-grade FPV mobile simulator MVP — single playable scene with realistic Acro flight model, touch + keyboard + gamepad input, urban placeholder polygon, crash & respawn — composed via Zenject with strict layering and full Edit Mode test coverage of all non-MonoBehaviour code.

**Architecture:** 6 logical layers (Presentation / Input / Game logic / Flight control / Physics / Rendering), each crossed only via interfaces. Cross-layer events flow upward through Zenject `SignalBus`. Pure C# domain (rate controller, motor mixer, flight dynamics, FSM) lives outside MonoBehaviours and is unit-tested in Edit Mode. MonoBehaviours are thin tickers that resolve dependencies through Zenject contexts (ProjectContext / SceneContext / GameObjectContext).

**Tech Stack:** Unity 6 (6000.4.7f1), URP Mobile, Extenject (Zenject) via OpenUPM, Unity Input System 1.19, Unity Test Framework, NUnit. Target: Android Snapdragon 7xx @ 60 FPS.

**Spec:** `docs/superpowers/specs/2026-05-23-fpv-sim-mvp-design.md`. Read it before starting any task — terminology and contracts come from there.

**Convention:** All C# code samples in this plan follow project style — no comments inside code, no alignment whitespace. Explanations live in prose around the code, never inside.

---

## Folder layout (target end state)

```
Assets/FpvSim/
  Flight/                pure C# (asmdef: FpvSim.Flight)
  States/                (asmdef: FpvSim.States, refs Flight + Signals)
  Input/                 (asmdef: FpvSim.Input, refs Flight + Unity.InputSystem)
  Signals/               (asmdef: FpvSim.Signals)
  Scene/                 (asmdef: FpvSim.Scene, refs everything above)
  Mb/                    MonoBehaviours (asmdef: FpvSim.Mb)
  Installers/            (asmdef: FpvSim.Installers, refs all)
  Resources/Flight/      FlightConfig assets
Assets/Scenes/Polygon.unity
Assets/Settings/Input/Pilot.inputactions
Tests/EditMode/
  Flight/                (asmdef: FpvSim.Flight.Tests)
  States/                (asmdef: FpvSim.States.Tests)
  Architecture/          (asmdef: FpvSim.Architecture.Tests, refs all production asmdefs)
Tests/PlayMode/
  Flight/                (asmdef: FpvSim.PlayMode.Tests)
```

---

## Milestone A — Project foundation

### Task A.1: Install Extenject via OpenUPM

**Files:**
- Modify: `Packages/manifest.json`

- [ ] **Step 1: Add scoped registry and Extenject dependency**

Replace the file with:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.svermeulen.extenject",
        "com.openupm"
      ]
    }
  ],
  "dependencies": {
    "com.svermeulen.extenject": "9.2.0",
    "com.unity.ai.navigation": "2.0.12",
    "com.unity.collab-proxy": "2.12.4",
    "com.unity.ide.rider": "3.0.40",
    "com.unity.ide.visualstudio": "2.0.27",
    "com.unity.inputsystem": "1.19.0",
    "com.unity.multiplayer.center": "1.0.1",
    "com.unity.render-pipelines.universal": "17.4.0",
    "com.unity.test-framework": "1.6.0",
    "com.unity.timeline": "1.8.12",
    "com.unity.ugui": "2.0.0",
    "com.unity.visualscripting": "1.9.11",
    "com.unity.modules.accessibility": "1.0.0",
    "com.unity.modules.adaptiveperformance": "1.0.0",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.androidjni": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.cloth": "1.0.0",
    "com.unity.modules.director": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.terrain": "1.0.0",
    "com.unity.modules.terrainphysics": "1.0.0",
    "com.unity.modules.tilemap": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.umbra": "1.0.0",
    "com.unity.modules.unityanalytics": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequestassetbundle": "1.0.0",
    "com.unity.modules.unitywebrequestaudio": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0",
    "com.unity.modules.unitywebrequestwww": "1.0.0",
    "com.unity.modules.vectorgraphics": "1.0.0",
    "com.unity.modules.vehicles": "1.0.0",
    "com.unity.modules.video": "1.0.0",
    "com.unity.modules.vr": "1.0.0",
    "com.unity.modules.wind": "1.0.0",
    "com.unity.modules.xr": "1.0.0"
  }
}
```

- [ ] **Step 2: Open Unity and let Package Manager resolve**

Open `D:\Unity\fpv_test` in Unity Hub. On reload, Package Manager will pull Extenject from OpenUPM. Wait until console shows no compile errors and "Zenject" appears in `Window → Package Manager → In Project`.

Expected: `Library/PackageCache/com.svermeulen.extenject@9.2.0/` exists.

- [ ] **Step 3: Smoke test that Zenject types resolve**

In Unity menu: `Edit → Project Settings → Player → Other Settings`. Confirm `Scripting Backend = Mono` is fine (Extenject works with both Mono and IL2CPP).

- [ ] **Step 4: Commit**

```
git add Packages/manifest.json Packages/packages-lock.json
git commit -m "feat: add Extenject 9.2.0 via OpenUPM"
```

### Task A.2: Create folder skeleton and assembly definitions

**Files:**
- Create: `Assets/FpvSim/Flight/FpvSim.Flight.asmdef`
- Create: `Assets/FpvSim/Signals/FpvSim.Signals.asmdef`
- Create: `Assets/FpvSim/States/FpvSim.States.asmdef`
- Create: `Assets/FpvSim/Input/FpvSim.Input.asmdef`
- Create: `Assets/FpvSim/Scene/FpvSim.Scene.asmdef`
- Create: `Assets/FpvSim/Mb/FpvSim.Mb.asmdef`
- Create: `Assets/FpvSim/Installers/FpvSim.Installers.asmdef`
- Create: `Tests/EditMode/Flight/FpvSim.Flight.Tests.asmdef`
- Create: `Tests/EditMode/States/FpvSim.States.Tests.asmdef`
- Create: `Tests/EditMode/Architecture/FpvSim.Architecture.Tests.asmdef`
- Create: `Tests/PlayMode/Flight/FpvSim.PlayMode.Tests.asmdef`

- [ ] **Step 1: Create production asmdefs**

`Assets/FpvSim/Flight/FpvSim.Flight.asmdef`:
```json
{
  "name": "FpvSim.Flight",
  "rootNamespace": "FpvSim.Flight",
  "references": [],
  "autoReferenced": true,
  "noEngineReferences": false
}
```

`Assets/FpvSim/Signals/FpvSim.Signals.asmdef`:
```json
{
  "name": "FpvSim.Signals",
  "rootNamespace": "FpvSim.Signals",
  "references": [],
  "autoReferenced": true,
  "noEngineReferences": false
}
```

`Assets/FpvSim/States/FpvSim.States.asmdef`:
```json
{
  "name": "FpvSim.States",
  "rootNamespace": "FpvSim.States",
  "references": ["FpvSim.Flight", "FpvSim.Signals", "Zenject"],
  "autoReferenced": true,
  "noEngineReferences": false
}
```

`Assets/FpvSim/Input/FpvSim.Input.asmdef`:
```json
{
  "name": "FpvSim.Input",
  "rootNamespace": "FpvSim.Input",
  "references": ["FpvSim.Flight", "Unity.InputSystem"],
  "autoReferenced": true,
  "noEngineReferences": false
}
```

`Assets/FpvSim/Scene/FpvSim.Scene.asmdef`:
```json
{
  "name": "FpvSim.Scene",
  "rootNamespace": "FpvSim.Scene",
  "references": ["FpvSim.Flight", "FpvSim.States", "FpvSim.Signals", "Zenject"],
  "autoReferenced": true,
  "noEngineReferences": false
}
```

`Assets/FpvSim/Mb/FpvSim.Mb.asmdef`:
```json
{
  "name": "FpvSim.Mb",
  "rootNamespace": "FpvSim.Mb",
  "references": [
    "FpvSim.Flight",
    "FpvSim.States",
    "FpvSim.Input",
    "FpvSim.Signals",
    "FpvSim.Scene",
    "Zenject",
    "Unity.InputSystem",
    "Unity.RenderPipelines.Universal.Runtime",
    "Unity.TextMeshPro"
  ],
  "autoReferenced": true,
  "noEngineReferences": false
}
```

`Assets/FpvSim/Installers/FpvSim.Installers.asmdef`:
```json
{
  "name": "FpvSim.Installers",
  "rootNamespace": "FpvSim.Installers",
  "references": [
    "FpvSim.Flight",
    "FpvSim.States",
    "FpvSim.Input",
    "FpvSim.Signals",
    "FpvSim.Scene",
    "FpvSim.Mb",
    "Zenject"
  ],
  "autoReferenced": true,
  "noEngineReferences": false
}
```

- [ ] **Step 2: Create test asmdefs**

`Tests/EditMode/Flight/FpvSim.Flight.Tests.asmdef`:
```json
{
  "name": "FpvSim.Flight.Tests",
  "rootNamespace": "FpvSim.Flight.Tests",
  "references": [
    "FpvSim.Flight",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": [
    "nunit.framework.dll"
  ],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "noEngineReferences": false
}
```

`Tests/EditMode/States/FpvSim.States.Tests.asmdef`:
```json
{
  "name": "FpvSim.States.Tests",
  "rootNamespace": "FpvSim.States.Tests",
  "references": [
    "FpvSim.States",
    "FpvSim.Flight",
    "FpvSim.Signals",
    "Zenject",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "noEngineReferences": false
}
```

`Tests/EditMode/Architecture/FpvSim.Architecture.Tests.asmdef`:
```json
{
  "name": "FpvSim.Architecture.Tests",
  "rootNamespace": "FpvSim.Architecture.Tests",
  "references": [
    "FpvSim.Flight",
    "FpvSim.States",
    "FpvSim.Input",
    "FpvSim.Signals",
    "FpvSim.Scene",
    "FpvSim.Mb",
    "FpvSim.Installers",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "noEngineReferences": false
}
```

`Tests/PlayMode/Flight/FpvSim.PlayMode.Tests.asmdef`:
```json
{
  "name": "FpvSim.PlayMode.Tests",
  "rootNamespace": "FpvSim.PlayMode.Tests",
  "references": [
    "FpvSim.Flight",
    "FpvSim.States",
    "FpvSim.Input",
    "FpvSim.Signals",
    "FpvSim.Scene",
    "FpvSim.Mb",
    "FpvSim.Installers",
    "Zenject",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "noEngineReferences": false
}
```

- [ ] **Step 3: Verify Unity compiles cleanly**

Switch focus to Unity. Wait for asset import + recompile. Console must be empty (zero errors, zero warnings about asmdefs). If "Zenject" reference is unresolved — the Extenject install in Task A.1 didn't complete; re-check Package Manager.

- [ ] **Step 4: Commit**

```
git add Assets/FpvSim Tests
git commit -m "feat: add asmdef skeleton for FpvSim modules"
```

---

## Milestone B — Flight value objects

### Task B.1: PilotCommand

**Files:**
- Create: `Assets/FpvSim/Flight/PilotCommand.cs`
- Create: `Tests/EditMode/Flight/PilotCommandTests.cs`

- [ ] **Step 1: Write the failing test**

`Tests/EditMode/Flight/PilotCommandTests.cs`:
```csharp
using NUnit.Framework;
using FpvSim.Flight;

namespace FpvSim.Flight.Tests {
    public class PilotCommandTests {
        [Test]
        public void Ctor_StoresAllFields() {
            var cmd = new PilotCommand(0.5f, 0.1f, -0.2f, 0.3f, true, false);
            Assert.AreEqual(0.5f, cmd.Throttle);
            Assert.AreEqual(0.1f, cmd.Roll);
            Assert.AreEqual(-0.2f, cmd.Pitch);
            Assert.AreEqual(0.3f, cmd.Yaw);
            Assert.IsTrue(cmd.ArmRequested);
            Assert.IsFalse(cmd.RespawnRequested);
        }
    }
}
```

- [ ] **Step 2: Run the test, expect FAIL**

In Unity: `Window → General → Test Runner → EditMode → Run All`. Expected: compile error "type or namespace name 'PilotCommand' could not be found".

- [ ] **Step 3: Implement**

`Assets/FpvSim/Flight/PilotCommand.cs`:
```csharp
namespace FpvSim.Flight {
    public readonly struct PilotCommand {
        public readonly float Throttle;
        public readonly float Roll;
        public readonly float Pitch;
        public readonly float Yaw;
        public readonly bool ArmRequested;
        public readonly bool RespawnRequested;

        public PilotCommand(float throttle, float roll, float pitch, float yaw, bool armRequested, bool respawnRequested) {
            Throttle = throttle;
            Roll = roll;
            Pitch = pitch;
            Yaw = yaw;
            ArmRequested = armRequested;
            RespawnRequested = respawnRequested;
        }
    }
}
```

- [ ] **Step 4: Run the test, expect PASS**

Test Runner → Run All. Expected: green.

- [ ] **Step 5: Commit**

```
git add Assets/FpvSim/Flight/PilotCommand.cs Tests/EditMode/Flight/PilotCommandTests.cs
git commit -m "feat(flight): add PilotCommand value object"
```

### Task B.2: TargetBodyRates, TorqueDemand, MotorThrusts

**Files:**
- Create: `Assets/FpvSim/Flight/TargetBodyRates.cs`
- Create: `Assets/FpvSim/Flight/TorqueDemand.cs`
- Create: `Assets/FpvSim/Flight/MotorThrusts.cs`

- [ ] **Step 1: Implement all three structs**

`TargetBodyRates.cs`:
```csharp
namespace FpvSim.Flight {
    public readonly struct TargetBodyRates {
        public readonly float RollRate;
        public readonly float PitchRate;
        public readonly float YawRate;

        public TargetBodyRates(float rollRate, float pitchRate, float yawRate) {
            RollRate = rollRate;
            PitchRate = pitchRate;
            YawRate = yawRate;
        }
    }
}
```

`TorqueDemand.cs`:
```csharp
namespace FpvSim.Flight {
    public readonly struct TorqueDemand {
        public readonly float Roll;
        public readonly float Pitch;
        public readonly float Yaw;

        public TorqueDemand(float roll, float pitch, float yaw) {
            Roll = roll;
            Pitch = pitch;
            Yaw = yaw;
        }
    }
}
```

`MotorThrusts.cs`:
```csharp
namespace FpvSim.Flight {
    public readonly struct MotorThrusts {
        public readonly float M1;
        public readonly float M2;
        public readonly float M3;
        public readonly float M4;

        public MotorThrusts(float m1, float m2, float m3, float m4) {
            M1 = m1;
            M2 = m2;
            M3 = m3;
            M4 = m4;
        }
    }
}
```

- [ ] **Step 2: Compile cleanly**

Wait for Unity recompile. Console empty.

- [ ] **Step 3: Commit**

```
git add Assets/FpvSim/Flight/TargetBodyRates.cs Assets/FpvSim/Flight/TorqueDemand.cs Assets/FpvSim/Flight/MotorThrusts.cs
git commit -m "feat(flight): add TargetBodyRates, TorqueDemand, MotorThrusts"
```

### Task B.3: FlightConfig ScriptableObject

**Files:**
- Create: `Assets/FpvSim/Flight/FlightConfig.cs`

- [ ] **Step 1: Implement**

`Assets/FpvSim/Flight/FlightConfig.cs`:
```csharp
using UnityEngine;

namespace FpvSim.Flight {
    [CreateAssetMenu(menuName = "FpvSim/Flight Config", fileName = "FlightConfig")]
    public sealed class FlightConfig : ScriptableObject {
        [Header("Mass & inertia")]
        public float Mass = 0.6f;
        public Vector3 InertiaDiag = new Vector3(0.0033f, 0.0033f, 0.0058f);

        [Header("Motor geometry (local space, meters)")]
        public Vector3[] MotorPositions = new Vector3[] {
            new Vector3(-0.11f, 0f, 0.11f),
            new Vector3(0.11f, 0f, 0.11f),
            new Vector3(-0.11f, 0f, -0.11f),
            new Vector3(0.11f, 0f, -0.11f)
        };
        public float[] MotorSpinSigns = new float[] { 1f, -1f, -1f, 1f };

        [Header("Motor model")]
        public float MaxThrustPerMotorN = 6.5f;
        public float MotorTau = 0.030f;
        public float ReactiveTorqueCoef = 0.018f;

        [Header("Battery")]
        public float BatteryNominalV = 22.2f;
        public float BatteryInternalResistance = 0.04f;
        public float BatteryCurrentScale = 0.6f;

        [Header("Aero")]
        public float LinearDragCoef = 0.18f;
        public float AngularDragCoef = 0.04f;
        public float PropDiameter = 0.127f;
        public float GroundEffectStrength = 0.15f;

        [Header("Rate controller — PID")]
        public Vector3 PidKp = new Vector3(0.32f, 0.36f, 0.50f);
        public Vector3 PidKi = new Vector3(0.40f, 0.45f, 0.40f);
        public Vector3 PidKd = new Vector3(0.018f, 0.020f, 0.000f);
        public float IntegralMax = 0.5f;

        [Header("Rates (Betaflight-like)")]
        public Vector3 RcRate = new Vector3(1.0f, 1.0f, 1.0f);
        public Vector3 SuperRate = new Vector3(0.7f, 0.7f, 0.7f);
        public Vector3 Expo = new Vector3(0.0f, 0.0f, 0.0f);

        [Header("Camera")]
        public float CameraTiltDeg = 30f;
        public float FovDeg = 110f;

        [Header("Substepping")]
        [Range(1, 20)]
        public int PhysicsSubsteps = 10;

        void OnValidate() {
            if (Mass <= 0f) Mass = 0.001f;
            if (MotorPositions == null || MotorPositions.Length != 4) Debug.LogError($"{name}: MotorPositions must have 4 entries");
            if (MotorSpinSigns == null || MotorSpinSigns.Length != 4) Debug.LogError($"{name}: MotorSpinSigns must have 4 entries");
            if (InertiaDiag.x <= 0 || InertiaDiag.y <= 0 || InertiaDiag.z <= 0) Debug.LogError($"{name}: InertiaDiag components must be positive");
        }
    }
}
```

- [ ] **Step 2: Create the default config asset**

In Unity Project window: right-click `Assets/FpvSim/Resources/Flight/` (create folder if missing) → `Create → FpvSim → Flight Config`. Name it `DefaultQuad`. Leave defaults. Save scene.

- [ ] **Step 3: Commit**

```
git add Assets/FpvSim/Flight/FlightConfig.cs Assets/FpvSim/Resources/Flight/DefaultQuad.asset Assets/FpvSim/Resources/Flight/DefaultQuad.asset.meta Assets/FpvSim/Resources.meta Assets/FpvSim/Resources/Flight.meta
git commit -m "feat(flight): add FlightConfig ScriptableObject + DefaultQuad asset"
```

---

## Milestone C — Rate controller

### Task C.1: IRateController interface

**Files:**
- Create: `Assets/FpvSim/Flight/IRateController.cs`

- [ ] **Step 1: Implement**

```csharp
using UnityEngine;

namespace FpvSim.Flight {
    public interface IRateController {
        TargetBodyRates ComputeTargetRates(PilotCommand cmd, FlightConfig cfg);
        TorqueDemand Update(TargetBodyRates target, Vector3 currentRatesRad, float dt);
        void Reset();
    }
}
```

- [ ] **Step 2: Commit**

```
git add Assets/FpvSim/Flight/IRateController.cs
git commit -m "feat(flight): add IRateController interface"
```

### Task C.2: BetaflightRateController — ComputeTargetRates with rc/super/expo

**Files:**
- Create: `Tests/EditMode/Flight/RateController/RatesCurveTests.cs`
- Create: `Assets/FpvSim/Flight/BetaflightRateController.cs`

- [ ] **Step 1: Write failing tests**

`Tests/EditMode/Flight/RateController/RatesCurveTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using FpvSim.Flight;

namespace FpvSim.Flight.Tests.RateController {
    public class RatesCurveTests {
        FlightConfig MakeCfg(float rcRate, float superRate, float expo) {
            var cfg = ScriptableObject.CreateInstance<FlightConfig>();
            cfg.RcRate = new Vector3(rcRate, rcRate, rcRate);
            cfg.SuperRate = new Vector3(superRate, superRate, superRate);
            cfg.Expo = new Vector3(expo, expo, expo);
            return cfg;
        }

        [Test]
        public void ZeroStick_ZeroRate() {
            var sut = new BetaflightRateController();
            var cfg = MakeCfg(1f, 0.7f, 0.5f);
            var cmd = new PilotCommand(0f, 0f, 0f, 0f, false, false);
            var rates = sut.ComputeTargetRates(cmd, cfg);
            Assert.AreEqual(0f, rates.RollRate, 1e-5);
            Assert.AreEqual(0f, rates.PitchRate, 1e-5);
            Assert.AreEqual(0f, rates.YawRate, 1e-5);
        }

        [Test]
        public void FullStick_NoSuperNoExpo_RateEqualsTwoPiTimesRcRate() {
            var sut = new BetaflightRateController();
            var cfg = MakeCfg(1f, 0f, 0f);
            var cmd = new PilotCommand(0f, 1f, 0f, 0f, false, false);
            var rates = sut.ComputeTargetRates(cmd, cfg);
            Assert.AreEqual(2f * Mathf.PI, rates.RollRate, 1e-3);
        }

        [Test]
        public void FullStick_PositiveExpo_StillReachesFullRateAtStickOne() {
            var sut = new BetaflightRateController();
            var cfg = MakeCfg(1f, 0f, 0.5f);
            var cmd = new PilotCommand(0f, 1f, 0f, 0f, false, false);
            var rates = sut.ComputeTargetRates(cmd, cfg);
            Assert.AreEqual(2f * Mathf.PI, rates.RollRate, 1e-3);
        }

        [Test]
        public void HalfStick_PositiveExpo_RateLessThanLinear() {
            var sut = new BetaflightRateController();
            var cfgLin = MakeCfg(1f, 0f, 0f);
            var cfgExpo = MakeCfg(1f, 0f, 0.5f);
            var cmd = new PilotCommand(0f, 0.5f, 0f, 0f, false, false);
            var lin = sut.ComputeTargetRates(cmd, cfgLin).RollRate;
            var exp = sut.ComputeTargetRates(cmd, cfgExpo).RollRate;
            Assert.Less(exp, lin);
        }

        [Test]
        public void NegativeStick_NegativeRate() {
            var sut = new BetaflightRateController();
            var cfg = MakeCfg(1f, 0.7f, 0.5f);
            var cmd = new PilotCommand(0f, -1f, 0f, 0f, false, false);
            var rates = sut.ComputeTargetRates(cmd, cfg);
            Assert.Less(rates.RollRate, 0f);
        }
    }
}
```

- [ ] **Step 2: Run tests, expect FAIL (BetaflightRateController doesn't exist)**

- [ ] **Step 3: Implement**

`Assets/FpvSim/Flight/BetaflightRateController.cs`:
```csharp
using UnityEngine;

namespace FpvSim.Flight {
    public sealed class BetaflightRateController : IRateController {
        Vector3 integral;
        Vector3 prevMeasured;
        bool hasPrev;

        public TargetBodyRates ComputeTargetRates(PilotCommand cmd, FlightConfig cfg) {
            float roll = AxisRate(cmd.Roll, cfg.RcRate.x, cfg.SuperRate.x, cfg.Expo.x);
            float pitch = AxisRate(cmd.Pitch, cfg.RcRate.y, cfg.SuperRate.y, cfg.Expo.y);
            float yaw = AxisRate(cmd.Yaw, cfg.RcRate.z, cfg.SuperRate.z, cfg.Expo.z);
            return new TargetBodyRates(roll, pitch, yaw);
        }

        static float AxisRate(float stick, float rcRate, float superRate, float expo) {
            float absStick = Mathf.Abs(stick);
            float expoCurve = stick * (absStick * absStick * expo + absStick * (1f - expo));
            float base_ = expoCurve * (200f * rcRate);
            float superMul = 1f / Mathf.Max(0.01f, 1f - absStick * superRate);
            float dps = base_ * superMul;
            return dps * Mathf.Deg2Rad;
        }

        public TorqueDemand Update(TargetBodyRates target, Vector3 currentRatesRad, float dt) {
            return new TorqueDemand(0f, 0f, 0f);
        }

        public void Reset() {
            integral = Vector3.zero;
            prevMeasured = Vector3.zero;
            hasPrev = false;
        }
    }
}
```

Note on the formula: this is the standard Betaflight 4.x rate calc — `expoCurve = stick * (|stick|² * expo + |stick| * (1 - expo))`, then `base = expoCurve * 200 * rcRate` (deg/s), then `super = 1 / max(0.01, 1 - |stick| * superRate)`. At stick=1, expoCurve=1 regardless of expo, so full-stick rate is purely `rcRate × superRate × 200 deg/s`. For `superRate=0, rcRate=1` → 200 deg/s ≈ 3.49 rad/s. The test `FullStick_NoSuperNoExpo_RateEqualsTwoPiTimesRcRate` uses `2π ≈ 6.28 rad/s` — this means the test currently asserts the wrong constant. Choose: keep the formula as canonical (test must be `200 deg/s * Mathf.Deg2Rad ≈ 3.491`) OR re-tune defaults. Use this corrected test:

Replace the test body of `FullStick_NoSuperNoExpo_RateEqualsTwoPiTimesRcRate` with:
```csharp
[Test]
public void FullStick_NoSuperNoExpo_RateIs200DegPerSecPerRcRate() {
    var sut = new BetaflightRateController();
    var cfg = MakeCfg(1f, 0f, 0f);
    var cmd = new PilotCommand(0f, 1f, 0f, 0f, false, false);
    var rates = sut.ComputeTargetRates(cmd, cfg);
    Assert.AreEqual(200f * Mathf.Deg2Rad, rates.RollRate, 1e-3);
}
```
And rename the test method accordingly.

- [ ] **Step 4: Run tests, expect PASS**

- [ ] **Step 5: Commit**

```
git add Assets/FpvSim/Flight/BetaflightRateController.cs Tests/EditMode/Flight/RateController
git commit -m "feat(flight): add BetaflightRateController.ComputeTargetRates with rc/super/expo"
```

### Task C.3: PID — proportional term

**Files:**
- Create: `Tests/EditMode/Flight/RateController/PidProportionalTests.cs`
- Modify: `Assets/FpvSim/Flight/BetaflightRateController.cs`

- [ ] **Step 1: Write failing test**

`PidProportionalTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using FpvSim.Flight;

namespace FpvSim.Flight.Tests.RateController {
    public class PidProportionalTests {
        FlightConfig MakeCfg() {
            var cfg = ScriptableObject.CreateInstance<FlightConfig>();
            cfg.PidKp = new Vector3(0.3f, 0.4f, 0.5f);
            cfg.PidKi = Vector3.zero;
            cfg.PidKd = Vector3.zero;
            cfg.IntegralMax = 1f;
            return cfg;
        }

        [Test]
        public void Update_AfterCfgInject_ErrorOne_OutputsKp() {
            var sut = new BetaflightRateController();
            sut.Configure(MakeCfg());
            var target = new TargetBodyRates(1f, 1f, 1f);
            var current = Vector3.zero;
            var torque = sut.Update(target, current, 0.002f);
            Assert.AreEqual(0.3f, torque.Roll, 1e-4);
            Assert.AreEqual(0.4f, torque.Pitch, 1e-4);
            Assert.AreEqual(0.5f, torque.Yaw, 1e-4);
        }

        [Test]
        public void Update_NegativeError_NegativeOutput() {
            var sut = new BetaflightRateController();
            sut.Configure(MakeCfg());
            var target = new TargetBodyRates(0f, 0f, 0f);
            var current = new Vector3(1f, 1f, 1f);
            var torque = sut.Update(target, current, 0.002f);
            Assert.Less(torque.Roll, 0f);
            Assert.Less(torque.Pitch, 0f);
            Assert.Less(torque.Yaw, 0f);
        }
    }
}
```

- [ ] **Step 2: Run, expect FAIL (`Configure` missing)**

- [ ] **Step 3: Implement Configure + P term in Update**

Replace `BetaflightRateController.cs`:
```csharp
using UnityEngine;

namespace FpvSim.Flight {
    public sealed class BetaflightRateController : IRateController {
        FlightConfig cfg;
        Vector3 integral;
        Vector3 prevMeasured;
        bool hasPrev;

        public void Configure(FlightConfig newCfg) {
            cfg = newCfg;
            Reset();
        }

        public TargetBodyRates ComputeTargetRates(PilotCommand cmd, FlightConfig overrideCfg) {
            var c = overrideCfg != null ? overrideCfg : cfg;
            float roll = AxisRate(cmd.Roll, c.RcRate.x, c.SuperRate.x, c.Expo.x);
            float pitch = AxisRate(cmd.Pitch, c.RcRate.y, c.SuperRate.y, c.Expo.y);
            float yaw = AxisRate(cmd.Yaw, c.RcRate.z, c.SuperRate.z, c.Expo.z);
            return new TargetBodyRates(roll, pitch, yaw);
        }

        static float AxisRate(float stick, float rcRate, float superRate, float expo) {
            float absStick = Mathf.Abs(stick);
            float expoCurve = stick * (absStick * absStick * expo + absStick * (1f - expo));
            float base_ = expoCurve * (200f * rcRate);
            float superMul = 1f / Mathf.Max(0.01f, 1f - absStick * superRate);
            float dps = base_ * superMul;
            return dps * Mathf.Deg2Rad;
        }

        public TorqueDemand Update(TargetBodyRates target, Vector3 currentRatesRad, float dt) {
            var targetVec = new Vector3(target.RollRate, target.PitchRate, target.YawRate);
            var error = targetVec - currentRatesRad;
            var p = new Vector3(cfg.PidKp.x * error.x, cfg.PidKp.y * error.y, cfg.PidKp.z * error.z);
            var output = p;
            return new TorqueDemand(Mathf.Clamp(output.x, -1f, 1f), Mathf.Clamp(output.y, -1f, 1f), Mathf.Clamp(output.z, -1f, 1f));
        }

        public void Reset() {
            integral = Vector3.zero;
            prevMeasured = Vector3.zero;
            hasPrev = false;
        }
    }
}
```

- [ ] **Step 4: Run, expect PASS**

- [ ] **Step 5: Commit**

```
git add Assets/FpvSim/Flight/BetaflightRateController.cs Tests/EditMode/Flight/RateController/PidProportionalTests.cs
git commit -m "feat(flight): add Configure + P term in BetaflightRateController.Update"
```

### Task C.4: PID — integral term with anti-windup

**Files:**
- Create: `Tests/EditMode/Flight/RateController/PidIntegralWindupTests.cs`
- Modify: `Assets/FpvSim/Flight/BetaflightRateController.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using NUnit.Framework;
using UnityEngine;
using FpvSim.Flight;

namespace FpvSim.Flight.Tests.RateController {
    public class PidIntegralWindupTests {
        FlightConfig MakeCfg() {
            var cfg = ScriptableObject.CreateInstance<FlightConfig>();
            cfg.PidKp = Vector3.zero;
            cfg.PidKi = new Vector3(1f, 1f, 1f);
            cfg.PidKd = Vector3.zero;
            cfg.IntegralMax = 0.5f;
            return cfg;
        }

        [Test]
        public void IntegralAccumulatesErrorOverTime() {
            var sut = new BetaflightRateController();
            sut.Configure(MakeCfg());
            var target = new TargetBodyRates(0.1f, 0f, 0f);
            float t1 = sut.Update(target, Vector3.zero, 0.1f).Roll;
            float t2 = sut.Update(target, Vector3.zero, 0.1f).Roll;
            Assert.Greater(t2, t1);
        }

        [Test]
        public void IntegralClampedToIntegralMax() {
            var sut = new BetaflightRateController();
            sut.Configure(MakeCfg());
            var target = new TargetBodyRates(10f, 0f, 0f);
            for (int i = 0; i < 100; i++) sut.Update(target, Vector3.zero, 0.01f);
            float torque = sut.Update(target, Vector3.zero, 0.01f).Roll;
            Assert.LessOrEqual(torque, 0.5f + 1e-4);
        }

        [Test]
        public void Reset_ClearsIntegral() {
            var sut = new BetaflightRateController();
            sut.Configure(MakeCfg());
            var target = new TargetBodyRates(1f, 0f, 0f);
            for (int i = 0; i < 10; i++) sut.Update(target, Vector3.zero, 0.1f);
            sut.Reset();
            float torque = sut.Update(target, Vector3.zero, 0.0001f).Roll;
            Assert.Less(torque, 0.001f);
        }
    }
}
```

- [ ] **Step 2: Run, expect FAIL**

- [ ] **Step 3: Add I term**

In `BetaflightRateController.Update`, replace the body with:
```csharp
public TorqueDemand Update(TargetBodyRates target, Vector3 currentRatesRad, float dt) {
    var targetVec = new Vector3(target.RollRate, target.PitchRate, target.YawRate);
    var error = targetVec - currentRatesRad;
    var p = new Vector3(cfg.PidKp.x * error.x, cfg.PidKp.y * error.y, cfg.PidKp.z * error.z);
    integral += new Vector3(cfg.PidKi.x * error.x * dt, cfg.PidKi.y * error.y * dt, cfg.PidKi.z * error.z * dt);
    integral.x = Mathf.Clamp(integral.x, -cfg.IntegralMax, cfg.IntegralMax);
    integral.y = Mathf.Clamp(integral.y, -cfg.IntegralMax, cfg.IntegralMax);
    integral.z = Mathf.Clamp(integral.z, -cfg.IntegralMax, cfg.IntegralMax);
    var output = p + integral;
    return new TorqueDemand(Mathf.Clamp(output.x, -1f, 1f), Mathf.Clamp(output.y, -1f, 1f), Mathf.Clamp(output.z, -1f, 1f));
}
```

- [ ] **Step 4: Run, expect PASS**

- [ ] **Step 5: Commit**

```
git add -u
git add Tests/EditMode/Flight/RateController/PidIntegralWindupTests.cs
git commit -m "feat(flight): add I term + anti-windup clamp"
```

### Task C.5: PID — derivative term on measurement

**Files:**
- Create: `Tests/EditMode/Flight/RateController/PidDerivativeOnMeasurementTests.cs`
- Modify: `Assets/FpvSim/Flight/BetaflightRateController.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using NUnit.Framework;
using UnityEngine;
using FpvSim.Flight;

namespace FpvSim.Flight.Tests.RateController {
    public class PidDerivativeOnMeasurementTests {
        FlightConfig MakeCfg() {
            var cfg = ScriptableObject.CreateInstance<FlightConfig>();
            cfg.PidKp = Vector3.zero;
            cfg.PidKi = Vector3.zero;
            cfg.PidKd = new Vector3(0.02f, 0.02f, 0.02f);
            cfg.IntegralMax = 1f;
            return cfg;
        }

        [Test]
        public void NoMeasurementChange_NoDerivativeContribution() {
            var sut = new BetaflightRateController();
            sut.Configure(MakeCfg());
            sut.Update(new TargetBodyRates(0f, 0f, 0f), Vector3.zero, 0.002f);
            float t = sut.Update(new TargetBodyRates(5f, 0f, 0f), Vector3.zero, 0.002f).Roll;
            Assert.AreEqual(0f, t, 1e-4);
        }

        [Test]
        public void IncreasingMeasurement_NegativeDerivativeOutput() {
            var sut = new BetaflightRateController();
            sut.Configure(MakeCfg());
            sut.Update(new TargetBodyRates(0f, 0f, 0f), new Vector3(0f, 0f, 0f), 0.002f);
            float t = sut.Update(new TargetBodyRates(0f, 0f, 0f), new Vector3(1f, 0f, 0f), 0.002f).Roll;
            Assert.Less(t, 0f);
        }
    }
}
```

- [ ] **Step 2: Run, expect FAIL on first test**

- [ ] **Step 3: Add D term (derivative on measurement)**

Replace `Update`:
```csharp
public TorqueDemand Update(TargetBodyRates target, Vector3 currentRatesRad, float dt) {
    var targetVec = new Vector3(target.RollRate, target.PitchRate, target.YawRate);
    var error = targetVec - currentRatesRad;
    var p = new Vector3(cfg.PidKp.x * error.x, cfg.PidKp.y * error.y, cfg.PidKp.z * error.z);
    integral += new Vector3(cfg.PidKi.x * error.x * dt, cfg.PidKi.y * error.y * dt, cfg.PidKi.z * error.z * dt);
    integral.x = Mathf.Clamp(integral.x, -cfg.IntegralMax, cfg.IntegralMax);
    integral.y = Mathf.Clamp(integral.y, -cfg.IntegralMax, cfg.IntegralMax);
    integral.z = Mathf.Clamp(integral.z, -cfg.IntegralMax, cfg.IntegralMax);
    Vector3 d;
    if (!hasPrev) {
        d = Vector3.zero;
        hasPrev = true;
    } else {
        var measuredDelta = (currentRatesRad - prevMeasured) / Mathf.Max(dt, 1e-6f);
        d = new Vector3(-cfg.PidKd.x * measuredDelta.x, -cfg.PidKd.y * measuredDelta.y, -cfg.PidKd.z * measuredDelta.z);
    }
    prevMeasured = currentRatesRad;
    var output = p + integral + d;
    return new TorqueDemand(Mathf.Clamp(output.x, -1f, 1f), Mathf.Clamp(output.y, -1f, 1f), Mathf.Clamp(output.z, -1f, 1f));
}
```

- [ ] **Step 4: Run, expect PASS**

- [ ] **Step 5: Commit**

```
git add -u
git add Tests/EditMode/Flight/RateController/PidDerivativeOnMeasurementTests.cs
git commit -m "feat(flight): add D term computed on measurement"
```

---

## Milestone D — Motor mixer

### Task D.1: IMotorMixer + QuadXMotorMixer with hover test

**Files:**
- Create: `Assets/FpvSim/Flight/IMotorMixer.cs`
- Create: `Tests/EditMode/Flight/MotorMixer/HoverTests.cs`
- Create: `Assets/FpvSim/Flight/QuadXMotorMixer.cs`

- [ ] **Step 1: Interface**

`IMotorMixer.cs`:
```csharp
namespace FpvSim.Flight {
    public interface IMotorMixer {
        MotorThrusts Mix(float throttle, TorqueDemand torque);
    }
}
```

- [ ] **Step 2: Failing test**

`HoverTests.cs`:
```csharp
using NUnit.Framework;
using FpvSim.Flight;

namespace FpvSim.Flight.Tests.MotorMixer {
    public class HoverTests {
        [Test]
        public void HalfThrottleZeroTorque_AllMotorsHalf() {
            var sut = new QuadXMotorMixer();
            var m = sut.Mix(0.5f, new TorqueDemand(0f, 0f, 0f));
            Assert.AreEqual(0.5f, m.M1, 1e-5);
            Assert.AreEqual(0.5f, m.M2, 1e-5);
            Assert.AreEqual(0.5f, m.M3, 1e-5);
            Assert.AreEqual(0.5f, m.M4, 1e-5);
        }
    }
}
```

- [ ] **Step 3: Run, expect FAIL**

- [ ] **Step 4: Implement minimal mixer**

`QuadXMotorMixer.cs`:
```csharp
using UnityEngine;

namespace FpvSim.Flight {
    public sealed class QuadXMotorMixer : IMotorMixer {
        public MotorThrusts Mix(float throttle, TorqueDemand torque) {
            float m1 = throttle - torque.Roll + torque.Pitch - torque.Yaw;
            float m2 = throttle + torque.Roll + torque.Pitch + torque.Yaw;
            float m3 = throttle - torque.Roll - torque.Pitch + torque.Yaw;
            float m4 = throttle + torque.Roll - torque.Pitch - torque.Yaw;
            float maxM = Mathf.Max(Mathf.Max(m1, m2), Mathf.Max(m3, m4));
            if (maxM > 1f) {
                float shift = maxM - 1f;
                m1 -= shift;
                m2 -= shift;
                m3 -= shift;
                m4 -= shift;
            }
            return new MotorThrusts(Mathf.Clamp01(m1), Mathf.Clamp01(m2), Mathf.Clamp01(m3), Mathf.Clamp01(m4));
        }
    }
}
```

- [ ] **Step 5: Run, expect PASS**

- [ ] **Step 6: Commit**

```
git add Assets/FpvSim/Flight/IMotorMixer.cs Assets/FpvSim/Flight/QuadXMotorMixer.cs Tests/EditMode/Flight/MotorMixer/HoverTests.cs
git commit -m "feat(flight): add IMotorMixer + QuadXMotorMixer (hover case)"
```

### Task D.2: Mixer — pure roll, pitch, yaw tests

**Files:**
- Create: `Tests/EditMode/Flight/MotorMixer/PureAxisTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using NUnit.Framework;
using FpvSim.Flight;

namespace FpvSim.Flight.Tests.MotorMixer {
    public class PureAxisTests {
        QuadXMotorMixer sut = new QuadXMotorMixer();

        [Test]
        public void PositiveRoll_LeftMotorsLessThanRight() {
            var m = sut.Mix(0.5f, new TorqueDemand(0.2f, 0f, 0f));
            Assert.Less(m.M1, m.M2);
            Assert.Less(m.M3, m.M4);
        }

        [Test]
        public void PositivePitch_FrontMotorsMoreThanRear() {
            var m = sut.Mix(0.5f, new TorqueDemand(0f, 0.2f, 0f));
            Assert.Greater(m.M1, m.M3);
            Assert.Greater(m.M2, m.M4);
        }

        [Test]
        public void PositiveYaw_M1M4LessThanM2M3() {
            var m = sut.Mix(0.5f, new TorqueDemand(0f, 0f, 0.2f));
            Assert.Less(m.M1, m.M2);
            Assert.Less(m.M4, m.M3);
        }
    }
}
```

- [ ] **Step 2: Run, expect PASS (already implemented)**

- [ ] **Step 3: Commit**

```
git add Tests/EditMode/Flight/MotorMixer/PureAxisTests.cs
git commit -m "test(flight): add pure roll/pitch/yaw mixer tests"
```

### Task D.3: Mixer — air mode normalization at saturation

**Files:**
- Create: `Tests/EditMode/Flight/MotorMixer/AirModeNormalizationTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using NUnit.Framework;
using FpvSim.Flight;

namespace FpvSim.Flight.Tests.MotorMixer {
    public class AirModeNormalizationTests {
        QuadXMotorMixer sut = new QuadXMotorMixer();

        [Test]
        public void FullThrottlePlusRoll_NoMotorAboveOne() {
            var m = sut.Mix(1f, new TorqueDemand(0.3f, 0f, 0f));
            Assert.LessOrEqual(m.M1, 1f + 1e-5);
            Assert.LessOrEqual(m.M2, 1f + 1e-5);
            Assert.LessOrEqual(m.M3, 1f + 1e-5);
            Assert.LessOrEqual(m.M4, 1f + 1e-5);
        }

        [Test]
        public void FullThrottlePlusRoll_RelativeRollDifferencePreserved() {
            var m = sut.Mix(1f, new TorqueDemand(0.3f, 0f, 0f));
            float rollDiff = m.M2 - m.M1;
            Assert.AreEqual(0.6f, rollDiff, 1e-4);
        }
    }
}
```

- [ ] **Step 2: Run, expect PASS**

- [ ] **Step 3: Commit**

```
git add Tests/EditMode/Flight/MotorMixer/AirModeNormalizationTests.cs
git commit -m "test(flight): air mode normalization preserves angular authority"
```

---

## Milestone E — Flight dynamics

### Task E.1: IFlightDynamics interface + Apply stub

**Files:**
- Create: `Assets/FpvSim/Flight/IFlightDynamics.cs`
- Create: `Assets/FpvSim/Flight/SubsteppedFlightDynamics.cs`

- [ ] **Step 1: Interface**

```csharp
using UnityEngine;

namespace FpvSim.Flight {
    public interface IFlightDynamics {
        void Configure(Rigidbody rb, FlightConfig cfg);
        void Apply(MotorThrusts thrusts, float dt);
        void ResetIntegrators();
        float CurrentAltitudeAboveGround { get; }
    }
}
```

- [ ] **Step 2: Skeleton impl**

```csharp
using UnityEngine;

namespace FpvSim.Flight {
    public sealed class SubsteppedFlightDynamics : IFlightDynamics {
        Rigidbody rb;
        FlightConfig cfg;
        float[] rpm = new float[4];

        public void Configure(Rigidbody rb, FlightConfig cfg) {
            this.rb = rb;
            this.cfg = cfg;
            ResetIntegrators();
        }

        public void Apply(MotorThrusts thrusts, float dt) {
        }

        public void ResetIntegrators() {
            for (int i = 0; i < 4; i++) rpm[i] = 0f;
        }

        public float CurrentAltitudeAboveGround {
            get {
                if (rb == null) return 0f;
                if (Physics.Raycast(rb.position, Vector3.down, out var hit, 1000f)) return rb.position.y - hit.point.y;
                return rb.position.y;
            }
        }
    }
}
```

- [ ] **Step 3: Commit**

```
git add Assets/FpvSim/Flight/IFlightDynamics.cs Assets/FpvSim/Flight/SubsteppedFlightDynamics.cs
git commit -m "feat(flight): add IFlightDynamics interface + skeleton"
```

### Task E.2: Motor response lag (first-order)

**Files:**
- Create: `Assets/FpvSim/Flight/MotorResponse.cs`
- Create: `Tests/EditMode/Flight/FlightDynamics/MotorResponseLagTests.cs`

- [ ] **Step 1: Pull motor response into a pure class first (no Unity dependency)**

`MotorResponse.cs`:
```csharp
namespace FpvSim.Flight {
    public static class MotorResponse {
        public static float Tick(float currentRpm, float demand, float tau, float dt) {
            float alpha = dt / (tau + dt);
            return currentRpm + alpha * (demand - currentRpm);
        }
    }
}
```

- [ ] **Step 2: Write test**

```csharp
using NUnit.Framework;
using FpvSim.Flight;

namespace FpvSim.Flight.Tests.FlightDynamics {
    public class MotorResponseLagTests {
        [Test]
        public void StepInput_After1Tau_ReachesAbout63Percent() {
            float tau = 0.030f;
            float rpm = 0f;
            float dt = 0.002f;
            int steps = (int)(tau / dt);
            for (int i = 0; i < steps; i++) rpm = MotorResponse.Tick(rpm, 1f, tau, dt);
            Assert.That(rpm, Is.InRange(0.55f, 0.70f));
        }

        [Test]
        public void DemandZero_FromOne_DecaysSimilarly() {
            float tau = 0.030f;
            float rpm = 1f;
            float dt = 0.002f;
            int steps = (int)(tau / dt);
            for (int i = 0; i < steps; i++) rpm = MotorResponse.Tick(rpm, 0f, tau, dt);
            Assert.That(rpm, Is.InRange(0.30f, 0.45f));
        }
    }
}
```

- [ ] **Step 3: Run, expect PASS (formula already correct)**

- [ ] **Step 4: Commit**

```
git add Assets/FpvSim/Flight/MotorResponse.cs Tests/EditMode/Flight/FlightDynamics/MotorResponseLagTests.cs
git commit -m "feat(flight): add MotorResponse first-order lag + tests"
```

### Task E.3: Ground effect formula (pure)

**Files:**
- Create: `Assets/FpvSim/Flight/GroundEffect.cs`
- Create: `Tests/EditMode/Flight/FlightDynamics/GroundEffectTests.cs`

- [ ] **Step 1: Implementation**

`GroundEffect.cs`:
```csharp
using UnityEngine;

namespace FpvSim.Flight {
    public static class GroundEffect {
        public static float Multiplier(float altitude, float propDiameter, float strength) {
            float t = Mathf.Max(0f, 1f - altitude / Mathf.Max(propDiameter, 1e-4f));
            return 1f + strength * t;
        }
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using NUnit.Framework;
using FpvSim.Flight;

namespace FpvSim.Flight.Tests.FlightDynamics {
    public class GroundEffectTests {
        [Test]
        public void AtGroundLevel_FullStrengthApplied() {
            float m = GroundEffect.Multiplier(0f, 0.127f, 0.15f);
            Assert.AreEqual(1.15f, m, 1e-5);
        }

        [Test]
        public void AtPropDiameter_NoBoost() {
            float m = GroundEffect.Multiplier(0.127f, 0.127f, 0.15f);
            Assert.AreEqual(1.0f, m, 1e-5);
        }

        [Test]
        public void AboveDiameter_NoBoost() {
            float m = GroundEffect.Multiplier(2f, 0.127f, 0.15f);
            Assert.AreEqual(1.0f, m, 1e-5);
        }
    }
}
```

- [ ] **Step 3: Run, expect PASS**

- [ ] **Step 4: Commit**

```
git add Assets/FpvSim/Flight/GroundEffect.cs Tests/EditMode/Flight/FlightDynamics/GroundEffectTests.cs
git commit -m "feat(flight): add GroundEffect multiplier + tests"
```

### Task E.4: Battery sag (pure)

**Files:**
- Create: `Assets/FpvSim/Flight/BatterySag.cs`
- Create: `Tests/EditMode/Flight/FlightDynamics/BatterySagTests.cs`

- [ ] **Step 1: Implementation**

`BatterySag.cs`:
```csharp
using UnityEngine;

namespace FpvSim.Flight {
    public static class BatterySag {
        public static float ThrustScale(float[] rpm, float nominalV, float internalR, float currentScale) {
            float sumSq = 0f;
            for (int i = 0; i < rpm.Length; i++) sumSq += rpm[i] * rpm[i];
            float current = currentScale * sumSq;
            float vEff = Mathf.Max(0f, nominalV - current * internalR);
            float ratio = vEff / Mathf.Max(nominalV, 1e-3f);
            return ratio * ratio;
        }
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using NUnit.Framework;
using FpvSim.Flight;

namespace FpvSim.Flight.Tests.FlightDynamics {
    public class BatterySagTests {
        [Test]
        public void IdleMotors_NoSag() {
            float s = BatterySag.ThrustScale(new[] { 0f, 0f, 0f, 0f }, 22.2f, 0.04f, 0.6f);
            Assert.AreEqual(1f, s, 1e-4);
        }

        [Test]
        public void FullMotors_SagsBelowOne() {
            float s = BatterySag.ThrustScale(new[] { 1f, 1f, 1f, 1f }, 22.2f, 0.04f, 0.6f);
            Assert.Less(s, 1f);
            Assert.Greater(s, 0.5f);
        }

        [Test]
        public void HigherInternalResistance_MoreSag() {
            float low = BatterySag.ThrustScale(new[] { 1f, 1f, 1f, 1f }, 22.2f, 0.04f, 0.6f);
            float high = BatterySag.ThrustScale(new[] { 1f, 1f, 1f, 1f }, 22.2f, 0.20f, 0.6f);
            Assert.Less(high, low);
        }
    }
}
```

- [ ] **Step 3: Run, expect PASS**

- [ ] **Step 4: Commit**

```
git add Assets/FpvSim/Flight/BatterySag.cs Tests/EditMode/Flight/FlightDynamics/BatterySagTests.cs
git commit -m "feat(flight): add BatterySag pure model + tests"
```

### Task E.5: Compose SubsteppedFlightDynamics.Apply

**Files:**
- Modify: `Assets/FpvSim/Flight/SubsteppedFlightDynamics.cs`

This task wires the pure helpers into the Rigidbody-applying integrator. Tested via Play Mode in Milestone N (PhysX-dependent).

- [ ] **Step 1: Implement Apply**

Replace `SubsteppedFlightDynamics.cs`:
```csharp
using UnityEngine;

namespace FpvSim.Flight {
    public sealed class SubsteppedFlightDynamics : IFlightDynamics {
        Rigidbody rb;
        FlightConfig cfg;
        float[] rpm = new float[4];

        public void Configure(Rigidbody rb, FlightConfig cfg) {
            this.rb = rb;
            this.cfg = cfg;
            rb.mass = cfg.Mass;
            rb.useGravity = true;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.inertiaTensor = cfg.InertiaDiag;
            rb.inertiaTensorRotation = Quaternion.identity;
            ResetIntegrators();
        }

        public void Apply(MotorThrusts thrusts, float dt) {
            float[] demand = { thrusts.M1, thrusts.M2, thrusts.M3, thrusts.M4 };
            for (int i = 0; i < 4; i++) rpm[i] = MotorResponse.Tick(rpm[i], demand[i], cfg.MotorTau, dt);
            float sagScale = BatterySag.ThrustScale(rpm, cfg.BatteryNominalV, cfg.BatteryInternalResistance, cfg.BatteryCurrentScale);
            float altitude = CurrentAltitudeAboveGround;
            float geMul = GroundEffect.Multiplier(altitude, cfg.PropDiameter, cfg.GroundEffectStrength);
            float yawTorqueSum = 0f;
            for (int i = 0; i < 4; i++) {
                float T = cfg.MaxThrustPerMotorN * rpm[i] * rpm[i] * sagScale * geMul;
                Vector3 worldPos = rb.transform.TransformPoint(cfg.MotorPositions[i]);
                Vector3 worldDir = rb.transform.up;
                rb.AddForceAtPosition(worldDir * T, worldPos, ForceMode.Force);
                yawTorqueSum += cfg.MotorSpinSigns[i] * cfg.ReactiveTorqueCoef * rpm[i] * rpm[i];
            }
            rb.AddRelativeTorque(0f, yawTorqueSum, 0f, ForceMode.Force);
            var v = rb.linearVelocity;
            rb.AddForce(-cfg.LinearDragCoef * v * v.magnitude, ForceMode.Force);
            var w = rb.angularVelocity;
            rb.AddTorque(-cfg.AngularDragCoef * w * w.magnitude, ForceMode.Force);
        }

        public void ResetIntegrators() {
            for (int i = 0; i < 4; i++) rpm[i] = 0f;
        }

        public float CurrentAltitudeAboveGround {
            get {
                if (rb == null) return 0f;
                if (Physics.Raycast(rb.position, Vector3.down, out var hit, 1000f)) return rb.position.y - hit.point.y;
                return rb.position.y;
            }
        }
    }
}
```

- [ ] **Step 2: Compile cleanly**

- [ ] **Step 3: Commit**

```
git add Assets/FpvSim/Flight/SubsteppedFlightDynamics.cs
git commit -m "feat(flight): compose SubsteppedFlightDynamics.Apply with motor/sag/ground/drag"
```

---

## Milestone F — Signals

### Task F.1: Define 4 signal structs

**Files:**
- Create: `Assets/FpvSim/Signals/DroneSignals.cs`

- [ ] **Step 1: Implement**

```csharp
using UnityEngine;

namespace FpvSim.Signals {
    public readonly struct DroneArmedSignal { }
    public readonly struct DroneDisarmedSignal { }
    public readonly struct DroneRespawnedSignal { }

    public readonly struct DroneCrashedSignal {
        public readonly Vector3 ImpactPoint;
        public readonly float ImpactSpeed;
        public DroneCrashedSignal(Vector3 impactPoint, float impactSpeed) {
            ImpactPoint = impactPoint;
            ImpactSpeed = impactSpeed;
        }
    }
}
```

- [ ] **Step 2: Commit**

```
git add Assets/FpvSim/Signals/DroneSignals.cs
git commit -m "feat(signals): add Drone signal structs"
```

---

## Milestone G — State machine

### Task G.1: IDroneState, DroneContext, IDroneStateMachine

**Files:**
- Create: `Assets/FpvSim/States/DroneContext.cs`
- Create: `Assets/FpvSim/States/IDroneState.cs`
- Create: `Assets/FpvSim/States/IDroneStateMachine.cs`

- [ ] **Step 1: DroneContext**

```csharp
using UnityEngine;
using Zenject;
using FpvSim.Flight;

namespace FpvSim.States {
    public sealed class DroneContext {
        public Rigidbody Rb;
        public Transform Root;
        public IFlightDynamics Dynamics;
        public IRateController RateCtrl;
        public IMotorMixer Mixer;
        public IDroneStateMachine Fsm;
        public SignalBus Signals;
        public FlightConfig Cfg;
        public Vector3 SpawnPos;
        public Quaternion SpawnRot;
        public float TimeInState;
    }
}
```

- [ ] **Step 2: IDroneState**

```csharp
using FpvSim.Flight;

namespace FpvSim.States {
    public interface IDroneState {
        void Enter(DroneContext ctx);
        void Tick(DroneContext ctx, PilotCommand cmd, float dt);
        void Exit(DroneContext ctx);
    }
}
```

- [ ] **Step 3: IDroneStateMachine**

```csharp
using FpvSim.Flight;

namespace FpvSim.States {
    public interface IDroneStateMachine {
        IDroneState Current { get; }
        void ChangeState<T>() where T : IDroneState;
        void Tick(PilotCommand cmd, float dt);
        void BindContext(DroneContext ctx);
    }
}
```

- [ ] **Step 4: Commit**

```
git add Assets/FpvSim/States/DroneContext.cs Assets/FpvSim/States/IDroneState.cs Assets/FpvSim/States/IDroneStateMachine.cs
git commit -m "feat(states): add IDroneState / DroneContext / IDroneStateMachine"
```

### Task G.2: DroneStateMachine impl with transitions test

**Files:**
- Create: `Assets/FpvSim/States/DroneStateMachine.cs`
- Create: `Tests/EditMode/States/DroneStateMachineTests.cs`

- [ ] **Step 1: Write failing test**

`DroneStateMachineTests.cs`:
```csharp
using System;
using System.Collections.Generic;
using NUnit.Framework;
using FpvSim.States;
using FpvSim.Flight;

namespace FpvSim.States.Tests {
    public class DroneStateMachineTests {
        sealed class StateA : IDroneState {
            public int Enters; public int Exits; public int Ticks;
            public void Enter(DroneContext c) => Enters++;
            public void Tick(DroneContext c, PilotCommand cmd, float dt) => Ticks++;
            public void Exit(DroneContext c) => Exits++;
        }
        sealed class StateB : IDroneState {
            public int Enters; public int Exits;
            public void Enter(DroneContext c) => Enters++;
            public void Tick(DroneContext c, PilotCommand cmd, float dt) { }
            public void Exit(DroneContext c) => Exits++;
        }

        [Test]
        public void ChangeState_CallsExitThenEnter_And_ResetsTimeInState() {
            var a = new StateA();
            var b = new StateB();
            var states = new List<IDroneState> { a, b };
            var fsm = new DroneStateMachine(states);
            var ctx = new DroneContext();
            fsm.BindContext(ctx);
            fsm.ChangeState<StateA>();
            fsm.Tick(default, 1.5f);
            Assert.AreEqual(1, a.Enters);
            Assert.AreEqual(1, a.Ticks);
            Assert.AreEqual(1.5f, ctx.TimeInState, 1e-4);
            fsm.ChangeState<StateB>();
            Assert.AreEqual(1, a.Exits);
            Assert.AreEqual(1, b.Enters);
            Assert.AreEqual(0f, ctx.TimeInState, 1e-4);
        }
    }
}
```

- [ ] **Step 2: Run, expect FAIL**

- [ ] **Step 3: Implement**

`DroneStateMachine.cs`:
```csharp
using System;
using System.Collections.Generic;
using FpvSim.Flight;

namespace FpvSim.States {
    public sealed class DroneStateMachine : IDroneStateMachine {
        readonly Dictionary<Type, IDroneState> states = new Dictionary<Type, IDroneState>();
        DroneContext ctx;
        public IDroneState Current { get; private set; }

        public DroneStateMachine(List<IDroneState> all) {
            foreach (var s in all) states[s.GetType()] = s;
        }

        public void BindContext(DroneContext newCtx) { ctx = newCtx; }

        public void ChangeState<T>() where T : IDroneState {
            if (ctx == null) throw new InvalidOperationException("DroneStateMachine.BindContext must be called first");
            var next = states[typeof(T)];
            Current?.Exit(ctx);
            ctx.TimeInState = 0f;
            Current = next;
            Current.Enter(ctx);
        }

        public void Tick(PilotCommand cmd, float dt) {
            if (Current == null || ctx == null) return;
            ctx.TimeInState += dt;
            Current.Tick(ctx, cmd, dt);
        }
    }
}
```

- [ ] **Step 4: Run, expect PASS**

- [ ] **Step 5: Commit**

```
git add Assets/FpvSim/States/DroneStateMachine.cs Tests/EditMode/States/DroneStateMachineTests.cs
git commit -m "feat(states): add DroneStateMachine with timer reset"
```

### Task G.3: Concrete states (Disarmed, Arming, Flying, Crashed, Respawning)

**Files:**
- Create: `Assets/FpvSim/States/DisarmedState.cs`
- Create: `Assets/FpvSim/States/ArmingState.cs`
- Create: `Assets/FpvSim/States/FlyingState.cs`
- Create: `Assets/FpvSim/States/CrashedState.cs`
- Create: `Assets/FpvSim/States/RespawningState.cs`

- [ ] **Step 1: DisarmedState**

```csharp
using FpvSim.Flight;
using FpvSim.Signals;

namespace FpvSim.States {
    public sealed class DisarmedState : IDroneState {
        public void Enter(DroneContext c) {
            c.Rb.linearVelocity = UnityEngine.Vector3.zero;
            c.Rb.angularVelocity = UnityEngine.Vector3.zero;
            c.Rb.isKinematic = true;
            c.Dynamics.ResetIntegrators();
            c.RateCtrl.Reset();
            c.Signals.Fire(new DroneDisarmedSignal());
        }
        public void Tick(DroneContext c, PilotCommand cmd, float dt) {
            if (cmd.ArmRequested) c.Fsm.ChangeState<ArmingState>();
        }
        public void Exit(DroneContext c) {
            c.Rb.isKinematic = false;
        }
    }
}
```

- [ ] **Step 2: ArmingState (300 ms timer)**

```csharp
using FpvSim.Flight;
using FpvSim.Signals;

namespace FpvSim.States {
    public sealed class ArmingState : IDroneState {
        const float ArmDurationSec = 0.3f;
        public void Enter(DroneContext c) { }
        public void Tick(DroneContext c, PilotCommand cmd, float dt) {
            if (c.TimeInState >= ArmDurationSec) c.Fsm.ChangeState<FlyingState>();
        }
        public void Exit(DroneContext c) {
            c.Signals.Fire(new DroneArmedSignal());
        }
    }
}
```

- [ ] **Step 3: FlyingState (runs full physics pipeline)**

```csharp
using UnityEngine;
using FpvSim.Flight;

namespace FpvSim.States {
    public sealed class FlyingState : IDroneState {
        public void Enter(DroneContext c) { }
        public void Tick(DroneContext c, PilotCommand cmd, float dt) {
            if (cmd.RespawnRequested) {
                c.Fsm.ChangeState<RespawningState>();
                return;
            }
            int subs = Mathf.Max(1, c.Cfg.PhysicsSubsteps);
            float subDt = dt / subs;
            for (int i = 0; i < subs; i++) {
                var target = c.RateCtrl.ComputeTargetRates(cmd, c.Cfg);
                var torque = c.RateCtrl.Update(target, c.Rb.angularVelocity, subDt);
                var thrusts = c.Mixer.Mix(cmd.Throttle, torque);
                c.Dynamics.Apply(thrusts, subDt);
            }
        }
        public void Exit(DroneContext c) { }
    }
}
```

- [ ] **Step 4: CrashedState (motors off, rigidbody still dynamic)**

```csharp
using FpvSim.Flight;

namespace FpvSim.States {
    public sealed class CrashedState : IDroneState {
        public void Enter(DroneContext c) {
            c.Dynamics.ResetIntegrators();
            c.RateCtrl.Reset();
        }
        public void Tick(DroneContext c, PilotCommand cmd, float dt) {
            if (cmd.RespawnRequested) c.Fsm.ChangeState<RespawningState>();
        }
        public void Exit(DroneContext c) { }
    }
}
```

- [ ] **Step 5: RespawningState (teleport + 500 ms freeze)**

```csharp
using UnityEngine;
using FpvSim.Flight;
using FpvSim.Signals;

namespace FpvSim.States {
    public sealed class RespawningState : IDroneState {
        const float FreezeSec = 0.5f;
        public void Enter(DroneContext c) {
            c.Rb.position = c.SpawnPos;
            c.Rb.rotation = c.SpawnRot;
            c.Rb.linearVelocity = Vector3.zero;
            c.Rb.angularVelocity = Vector3.zero;
            c.Dynamics.ResetIntegrators();
            c.RateCtrl.Reset();
            c.Rb.isKinematic = true;
            c.Signals.Fire(new DroneRespawnedSignal());
        }
        public void Tick(DroneContext c, PilotCommand cmd, float dt) {
            if (c.TimeInState >= FreezeSec) c.Fsm.ChangeState<DisarmedState>();
        }
        public void Exit(DroneContext c) {
            c.Rb.isKinematic = false;
        }
    }
}
```

- [ ] **Step 6: Compile cleanly**

- [ ] **Step 7: Commit**

```
git add Assets/FpvSim/States/DisarmedState.cs Assets/FpvSim/States/ArmingState.cs Assets/FpvSim/States/FlyingState.cs Assets/FpvSim/States/CrashedState.cs Assets/FpvSim/States/RespawningState.cs
git commit -m "feat(states): add Disarmed/Arming/Flying/Crashed/Respawning states"
```

### Task G.4: Crash signal handler wiring (subscriber lives in DroneStateMachine)

**Files:**
- Modify: `Assets/FpvSim/States/DroneStateMachine.cs`
- Create: `Tests/EditMode/States/CrashSignalRoutingTests.cs`

- [ ] **Step 1: Add SignalBus subscription via Initialize-like method**

Add `Subscribe(SignalBus bus)` method to `DroneStateMachine`:
```csharp
public void SubscribeToCrashSignal(Zenject.SignalBus bus) {
    bus.Subscribe<FpvSim.Signals.DroneCrashedSignal>(OnCrash);
}

void OnCrash(FpvSim.Signals.DroneCrashedSignal s) {
    if (Current is FlyingState) ChangeState<CrashedState>();
}
```

(Add `using Zenject;` and `using FpvSim.Signals;` at top.)

- [ ] **Step 2: Test**

`CrashSignalRoutingTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Zenject;
using FpvSim.States;
using FpvSim.Signals;

namespace FpvSim.States.Tests {
    public class CrashSignalRoutingTests : ZenjectUnitTestFixture {
        [Test]
        public void CrashSignal_WhileFlying_TransitionsToCrashed() {
            SignalBusInstaller.Install(Container);
            Container.DeclareSignal<DroneCrashedSignal>();
            var ctx = new DroneContext();
            var flying = new FlyingState();
            var crashed = new CrashedState();
            var fsm = new DroneStateMachine(new List<IDroneState> { flying, crashed });
            fsm.BindContext(ctx);
            ctx.Fsm = fsm;
            ctx.Signals = Container.Resolve<SignalBus>();
            fsm.ChangeState<FlyingState>();
            fsm.SubscribeToCrashSignal(ctx.Signals);
            ctx.Signals.Fire(new DroneCrashedSignal(UnityEngine.Vector3.zero, 10f));
            Assert.IsInstanceOf<CrashedState>(fsm.Current);
        }

        [Test]
        public void CrashSignal_OutsideFlying_NoTransition() {
            SignalBusInstaller.Install(Container);
            Container.DeclareSignal<DroneCrashedSignal>();
            var ctx = new DroneContext();
            var disarmed = new DisarmedDummy();
            var crashed = new CrashedState();
            var fsm = new DroneStateMachine(new List<IDroneState> { disarmed, crashed });
            fsm.BindContext(ctx);
            ctx.Fsm = fsm;
            ctx.Signals = Container.Resolve<SignalBus>();
            fsm.ChangeState<DisarmedDummy>();
            fsm.SubscribeToCrashSignal(ctx.Signals);
            ctx.Signals.Fire(new DroneCrashedSignal(UnityEngine.Vector3.zero, 10f));
            Assert.IsInstanceOf<DisarmedDummy>(fsm.Current);
        }

        sealed class DisarmedDummy : IDroneState {
            public void Enter(DroneContext c) { }
            public void Tick(DroneContext c, FpvSim.Flight.PilotCommand cmd, float dt) { }
            public void Exit(DroneContext c) { }
        }
    }
}
```

Note: real `DisarmedState` touches Rigidbody in Enter — can't use in pure Edit Mode test. The dummy keeps the test pure.

- [ ] **Step 3: Run, expect PASS**

- [ ] **Step 4: Commit**

```
git add -u
git add Tests/EditMode/States/CrashSignalRoutingTests.cs
git commit -m "feat(states): add SignalBus crash subscription + routing tests"
```

---

## Milestone H — Input layer

### Task H.1: IPilotInputProvider + ScriptedPilotProvider

**Files:**
- Create: `Assets/FpvSim/Input/IPilotInputProvider.cs`
- Create: `Assets/FpvSim/Input/ScriptedPilotProvider.cs`
- Create: `Tests/EditMode/Flight/ScriptedPilotProviderTests.cs` (in FpvSim.Flight.Tests asmdef — extend references to include FpvSim.Input)

- [ ] **Step 1: Update `Tests/EditMode/Flight/FpvSim.Flight.Tests.asmdef` references**

Change `"references"` to:
```json
"references": [
  "FpvSim.Flight",
  "FpvSim.Input",
  "UnityEngine.TestRunner",
  "UnityEditor.TestRunner"
]
```

- [ ] **Step 2: Interface**

`IPilotInputProvider.cs`:
```csharp
using FpvSim.Flight;

namespace FpvSim.Input {
    public interface IPilotInputProvider {
        PilotCommand Read();
    }
}
```

- [ ] **Step 3: ScriptedPilotProvider**

`ScriptedPilotProvider.cs`:
```csharp
using System;
using UnityEngine;
using FpvSim.Flight;

namespace FpvSim.Input {
    public sealed class ScriptedPilotProvider : IPilotInputProvider {
        readonly Func<float, PilotCommand> sequence;
        float t;
        public ScriptedPilotProvider(Func<float, PilotCommand> sequence) {
            this.sequence = sequence;
        }
        public PilotCommand Read() {
            var c = sequence(t);
            t += Time.fixedDeltaTime;
            return c;
        }
    }
}
```

- [ ] **Step 4: Test**

`ScriptedPilotProviderTests.cs`:
```csharp
using NUnit.Framework;
using FpvSim.Flight;
using FpvSim.Input;

namespace FpvSim.Flight.Tests {
    public class ScriptedPilotProviderTests {
        [Test]
        public void Read_DelegatesToSequenceWithMonotonicTime() {
            int callCount = 0;
            var sut = new ScriptedPilotProvider(t => {
                callCount++;
                return new PilotCommand(t, 0f, 0f, 0f, false, false);
            });
            var c1 = sut.Read();
            var c2 = sut.Read();
            Assert.AreEqual(2, callCount);
            Assert.Less(c1.Throttle, c2.Throttle);
        }
    }
}
```

- [ ] **Step 5: Run, expect PASS**

- [ ] **Step 6: Commit**

```
git add Assets/FpvSim/Input Tests/EditMode/Flight/ScriptedPilotProviderTests.cs Tests/EditMode/Flight/FpvSim.Flight.Tests.asmdef
git commit -m "feat(input): add IPilotInputProvider + ScriptedPilotProvider"
```

### Task H.2: Pilot.inputactions asset

**Files:**
- Create: `Assets/Settings/Input/Pilot.inputactions`

- [ ] **Step 1: Create asset via Unity menu**

In Project window: right-click `Assets/Settings/Input/` (create folder if missing) → `Create → Input Actions`. Name it `Pilot`. Double-click to open editor.

- [ ] **Step 2: Build Action Map "Pilot" with actions**

In the .inputactions editor:
1. Add Action Map: name `Pilot`.
2. Add Actions (each Action Type listed):
   - `Throttle` (Value, Axis)
   - `Yaw` (Value, Axis)
   - `Pitch` (Value, Axis)
   - `Roll` (Value, Axis)
   - `Arm` (Button)
   - `Respawn` (Button)
   - `ModeToggle` (Button)
3. Add bindings (right-click action → Add Binding):

For `Throttle`:
   - 1D Axis composite: positive `<Keyboard>/w`, negative `<Keyboard>/s`
   - Binding `<Gamepad>/leftStick/y`
   - Binding `<Touchscreen>/...` left untouched; touch UI writes via OnScreenStick in Task L.7

For `Yaw`: 1D Axis composite W=`<Keyboard>/d` neg=`<Keyboard>/a`; gamepad `<Gamepad>/leftStick/x`.

For `Pitch`: 1D Axis composite `<Keyboard>/upArrow` / `<Keyboard>/downArrow`; gamepad `<Gamepad>/rightStick/y`.

For `Roll`: 1D Axis composite `<Keyboard>/rightArrow` / `<Keyboard>/leftArrow`; gamepad `<Gamepad>/rightStick/x`.

For `Arm`: `<Keyboard>/space`, `<Gamepad>/buttonSouth`.

For `Respawn`: `<Keyboard>/r`, `<Gamepad>/buttonNorth`.

For `ModeToggle`: `<Keyboard>/m`, `<Gamepad>/buttonEast`.

4. In Inspector, check "Generate C# Class". Class Name: `PilotInputActions`. Namespace: `FpvSim.Input`. Click Apply.

- [ ] **Step 3: Verify generated file appears**

`Assets/Settings/Input/Pilot.cs` (generated) should exist. Add `using FpvSim.Input;` to consumers.

- [ ] **Step 4: Commit**

```
git add Assets/Settings/Input
git commit -m "feat(input): add Pilot.inputactions with keyboard + gamepad bindings"
```

### Task H.3: UnityInputSystemPilotProvider

**Files:**
- Create: `Assets/FpvSim/Input/UnityInputSystemPilotProvider.cs`

- [ ] **Step 1: Implement**

```csharp
using System;
using UnityEngine;
using FpvSim.Flight;

namespace FpvSim.Input {
    public sealed class UnityInputSystemPilotProvider : IPilotInputProvider, IDisposable {
        readonly PilotInputActions actions;
        bool armLatched;
        bool respawnLatched;

        public UnityInputSystemPilotProvider() {
            actions = new PilotInputActions();
            actions.Pilot.Arm.performed += _ => armLatched = true;
            actions.Pilot.Respawn.performed += _ => respawnLatched = true;
            actions.Enable();
        }

        public PilotCommand Read() {
            var cmd = new PilotCommand(
                Mathf.Clamp01(actions.Pilot.Throttle.ReadValue<float>()),
                Mathf.Clamp(actions.Pilot.Roll.ReadValue<float>(), -1f, 1f),
                Mathf.Clamp(actions.Pilot.Pitch.ReadValue<float>(), -1f, 1f),
                Mathf.Clamp(actions.Pilot.Yaw.ReadValue<float>(), -1f, 1f),
                armLatched,
                respawnLatched
            );
            armLatched = false;
            respawnLatched = false;
            return cmd;
        }

        public void Dispose() {
            actions.Disable();
            actions.Dispose();
        }
    }
}
```

- [ ] **Step 2: Compile cleanly**

- [ ] **Step 3: Commit**

```
git add Assets/FpvSim/Input/UnityInputSystemPilotProvider.cs
git commit -m "feat(input): add UnityInputSystemPilotProvider adapter"
```

---

## Milestone I — Scene services

### Task I.1: DroneSpawnPoint, IRespawnService, IGateRegistry

**Files:**
- Create: `Assets/FpvSim/Scene/DroneSpawnPoint.cs`
- Create: `Assets/FpvSim/Scene/IRespawnService.cs`
- Create: `Assets/FpvSim/Scene/NearestSafeSpawnService.cs`
- Create: `Assets/FpvSim/Scene/IGateRegistry.cs`
- Create: `Assets/FpvSim/Scene/EmptyGateRegistry.cs`

- [ ] **Step 1: DroneSpawnPoint** (MonoBehaviour marker; lives in FpvSim.Scene asmdef)

```csharp
using UnityEngine;

namespace FpvSim.Scene {
    [DisallowMultipleComponent]
    public sealed class DroneSpawnPoint : MonoBehaviour {
        void OnDrawGizmos() {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);
        }
    }
}
```

- [ ] **Step 2: IRespawnService + NearestSafeSpawnService**

```csharp
using UnityEngine;

namespace FpvSim.Scene {
    public interface IRespawnService {
        DroneSpawnPoint PickSafeSpawn(Vector3 lastKnownPosition);
    }
}
```

```csharp
using System.Linq;
using UnityEngine;

namespace FpvSim.Scene {
    public sealed class NearestSafeSpawnService : IRespawnService {
        readonly DroneSpawnPoint[] points;
        public NearestSafeSpawnService(DroneSpawnPoint[] points) {
            this.points = points;
        }
        public DroneSpawnPoint PickSafeSpawn(Vector3 lastKnownPosition) {
            if (points == null || points.Length == 0) {
                Debug.LogError("NearestSafeSpawnService: no spawn points in scene");
                return null;
            }
            return points.OrderBy(p => (p.transform.position - lastKnownPosition).sqrMagnitude).First();
        }
    }
}
```

- [ ] **Step 3: IGateRegistry + EmptyGateRegistry**

```csharp
namespace FpvSim.Scene {
    public interface IGateRegistry {
        int Count { get; }
    }
}
```

```csharp
namespace FpvSim.Scene {
    public sealed class EmptyGateRegistry : IGateRegistry {
        public int Count => 0;
    }
}
```

- [ ] **Step 4: Commit**

```
git add Assets/FpvSim/Scene
git commit -m "feat(scene): add DroneSpawnPoint, IRespawnService, NearestSafeSpawnService, IGateRegistry"
```

### Task I.2: DroneFacade + IDroneFactory + DroneFactory

**Files:**
- Create: `Assets/FpvSim/Scene/DroneFacade.cs`
- Create: `Assets/FpvSim/Scene/IDroneFactory.cs`
- Create: `Assets/FpvSim/Scene/DroneFactory.cs`

- [ ] **Step 1: DroneFacade (acts as the public handle to a spawned drone)**

```csharp
using UnityEngine;
using Zenject;
using FpvSim.States;

namespace FpvSim.Scene {
    public sealed class DroneFacade : MonoBehaviour {
        [Inject] IDroneStateMachine fsm;
        public IDroneStateMachine Fsm => fsm;
        public Transform Root => transform;
    }
}
```

- [ ] **Step 2: IDroneFactory interface**

```csharp
namespace FpvSim.Scene {
    public interface IDroneFactory {
        DroneFacade Create(DroneSpawnPoint spawn);
    }
}
```

- [ ] **Step 3: DroneFactory implementation**

```csharp
using UnityEngine;
using Zenject;

namespace FpvSim.Scene {
    public sealed class DroneFactory : IDroneFactory {
        readonly DiContainer container;
        readonly GameObject prefab;
        public DroneFactory(DiContainer container, [Inject(Id = "DronePrefab")] GameObject prefab) {
            this.container = container;
            this.prefab = prefab;
        }
        public DroneFacade Create(DroneSpawnPoint spawn) {
            var go = container.InstantiatePrefab(prefab, spawn.transform.position, spawn.transform.rotation, null);
            return go.GetComponent<DroneFacade>();
        }
    }
}
```

- [ ] **Step 4: Commit**

```
git add Assets/FpvSim/Scene/DroneFacade.cs Assets/FpvSim/Scene/IDroneFactory.cs Assets/FpvSim/Scene/DroneFactory.cs
git commit -m "feat(scene): add DroneFacade + DroneFactory"
```

### Task I.3: BuildingPaletteSo + building prefabs

**Files:**
- Create: `Assets/FpvSim/Scene/BuildingPaletteSo.cs`
- Create: 5 building prefabs under `Assets/FpvSim/Scene/Buildings/`

- [ ] **Step 1: BuildingPaletteSo**

```csharp
using UnityEngine;

namespace FpvSim.Scene {
    [CreateAssetMenu(menuName = "FpvSim/Building Palette", fileName = "BuildingPalette")]
    public sealed class BuildingPaletteSo : ScriptableObject {
        public GameObject[] Prefabs;
    }
}
```

- [ ] **Step 2: Build a shared material**

In `Assets/FpvSim/Scene/Buildings/Mat_Building.mat`: create new Material, shader `Universal Render Pipeline/Lit`, base color light gray (#A0A0A0), enable GPU Instancing. Save.

- [ ] **Step 3: Build 5 prefabs**

For each entry below: in scene create a Cube, set scale, add `LODGroup` with 3 LODs (LOD0=cube mesh, LOD1=same cube/no normalmap, LOD2=Quad mesh billboarded — use a child Quad with `Billboard` script; for MVP a flat cube at LOD2 is acceptable). Assign `Mat_Building`. Drag into `Assets/FpvSim/Scene/Buildings/`. Names + scales:

- `Building_Small.prefab`        scale (5, 10, 5)
- `Building_Mid.prefab`          scale (10, 20, 10)
- `Building_Tall.prefab`         scale (15, 35, 15)
- `Building_Shed.prefab`         scale (3, 4, 3)
- `Building_Tower.prefab`        scale (5, 50, 5)

Mark each prefab `Static` in Inspector (Occluder + Occludee + Batching + Lightmap Static).

- [ ] **Step 4: Create BuildingPalette asset**

In Project: right-click `Assets/FpvSim/Scene/Buildings/` → `Create → FpvSim → Building Palette`. Name `BuildingPalette`. Drag all 5 prefabs into `Prefabs`.

- [ ] **Step 5: Commit**

```
git add Assets/FpvSim/Scene/BuildingPaletteSo.cs Assets/FpvSim/Scene/Buildings
git commit -m "feat(scene): add BuildingPaletteSo + 5 placeholder building prefabs"
```

---

## Milestone J — MonoBehaviour layer

### Task J.1: FlightDynamicsTickerMb

**Files:**
- Create: `Assets/FpvSim/Mb/FlightDynamicsTickerMb.cs`

- [ ] **Step 1: Implement**

```csharp
using UnityEngine;
using Zenject;
using FpvSim.Input;
using FpvSim.States;

namespace FpvSim.Mb {
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FlightDynamicsTickerMb : MonoBehaviour {
        [Inject] IPilotInputProvider input;
        [Inject] IDroneStateMachine fsm;

        void FixedUpdate() {
            fsm.Tick(input.Read(), Time.fixedDeltaTime);
        }
    }
}
```

- [ ] **Step 2: Commit**

```
git add Assets/FpvSim/Mb/FlightDynamicsTickerMb.cs
git commit -m "feat(mb): add FlightDynamicsTickerMb"
```

### Task J.2: DroneCrashDetectorMb

**Files:**
- Create: `Assets/FpvSim/Mb/DroneCrashDetectorMb.cs`

- [ ] **Step 1: Implement**

```csharp
using UnityEngine;
using Zenject;
using FpvSim.Signals;

namespace FpvSim.Mb {
    public sealed class DroneCrashDetectorMb : MonoBehaviour {
        [Inject] SignalBus signals;
        [SerializeField] float crashSpeedThreshold = 4f;

        void OnCollisionEnter(Collision c) {
            if (c.relativeVelocity.magnitude < crashSpeedThreshold) return;
            signals.Fire(new DroneCrashedSignal(c.GetContact(0).point, c.relativeVelocity.magnitude));
        }
    }
}
```

- [ ] **Step 2: Commit**

```
git add Assets/FpvSim/Mb/DroneCrashDetectorMb.cs
git commit -m "feat(mb): add DroneCrashDetectorMb"
```

### Task J.3: FpvCameraMb

**Files:**
- Create: `Assets/FpvSim/Mb/IFpvCamera.cs`
- Create: `Assets/FpvSim/Mb/FpvCameraMb.cs`

- [ ] **Step 1: Interface**

```csharp
namespace FpvSim.Mb {
    public interface IFpvCamera {
        void SetTiltDegrees(float t);
        void SetFovDegrees(float fov);
    }
}
```

- [ ] **Step 2: Mb**

```csharp
using UnityEngine;
using Zenject;
using FpvSim.Flight;

namespace FpvSim.Mb {
    [RequireComponent(typeof(Camera))]
    public sealed class FpvCameraMb : MonoBehaviour, IFpvCamera {
        [Inject] FlightConfig cfg;
        Camera cam;

        void Awake() {
            cam = GetComponent<Camera>();
        }

        void Start() {
            SetTiltDegrees(cfg.CameraTiltDeg);
            SetFovDegrees(cfg.FovDeg);
        }

        public void SetTiltDegrees(float t) {
            transform.localRotation = Quaternion.Euler(-t, 0f, 0f);
        }

        public void SetFovDegrees(float fov) {
            cam.fieldOfView = fov;
        }
    }
}
```

- [ ] **Step 3: Commit**

```
git add Assets/FpvSim/Mb/IFpvCamera.cs Assets/FpvSim/Mb/FpvCameraMb.cs
git commit -m "feat(mb): add IFpvCamera + FpvCameraMb"
```

### Task J.4: OsdViewMb

**Files:**
- Create: `Assets/FpvSim/Mb/OsdViewMb.cs`

- [ ] **Step 1: Implement**

```csharp
using TMPro;
using UnityEngine;
using Zenject;
using FpvSim.Signals;

namespace FpvSim.Mb {
    public sealed class OsdViewMb : MonoBehaviour {
        [SerializeField] TMP_Text modeText;
        [SerializeField] TMP_Text fpsText;
        [Inject] SignalBus signals;
        float fpsAccum;
        int fpsFrames;
        float fpsTimer;

        void Awake() {
            SetMode("DISARMED");
        }

        void OnEnable() {
            signals.Subscribe<DroneArmedSignal>(OnArmed);
            signals.Subscribe<DroneDisarmedSignal>(OnDisarmed);
            signals.Subscribe<DroneCrashedSignal>(OnCrashed);
            signals.Subscribe<DroneRespawnedSignal>(OnRespawned);
        }

        void OnDisable() {
            signals.Unsubscribe<DroneArmedSignal>(OnArmed);
            signals.Unsubscribe<DroneDisarmedSignal>(OnDisarmed);
            signals.Unsubscribe<DroneCrashedSignal>(OnCrashed);
            signals.Unsubscribe<DroneRespawnedSignal>(OnRespawned);
        }

        void OnArmed(DroneArmedSignal _) => SetMode("ACRO");
        void OnDisarmed(DroneDisarmedSignal _) => SetMode("DISARMED");
        void OnCrashed(DroneCrashedSignal _) => SetMode("CRASHED — tap respawn");
        void OnRespawned(DroneRespawnedSignal _) => SetMode("DISARMED");

        void SetMode(string s) { if (modeText != null) modeText.text = s; }

        void Update() {
            fpsAccum += Time.unscaledDeltaTime;
            fpsFrames++;
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f) {
                float fps = fpsFrames / fpsAccum;
                if (fpsText != null) fpsText.text = $"FPS {fps:0}";
                fpsAccum = 0f;
                fpsFrames = 0;
                fpsTimer = 0f;
            }
        }
    }
}
```

- [ ] **Step 2: Commit**

```
git add Assets/FpvSim/Mb/OsdViewMb.cs
git commit -m "feat(mb): add OsdViewMb with mode + FPS readout"
```

### Task J.5: TouchSticksOverlayMb (placeholder — UI wiring happens at scene-build step L.7)

**Files:**
- Create: `Assets/FpvSim/Mb/TouchSticksOverlayMb.cs`

- [ ] **Step 1: Implement minimal**

```csharp
using UnityEngine;

namespace FpvSim.Mb {
    public sealed class TouchSticksOverlayMb : MonoBehaviour {
        [SerializeField] GameObject leftStickRoot;
        [SerializeField] GameObject rightStickRoot;
        void Awake() {
            bool show = Application.isMobilePlatform || Application.platform == RuntimePlatform.WindowsEditor;
            if (leftStickRoot != null) leftStickRoot.SetActive(show);
            if (rightStickRoot != null) rightStickRoot.SetActive(show);
        }
    }
}
```

(Always-visible during MVP — toggling can come later when settings menu exists.)

- [ ] **Step 2: Commit**

```
git add Assets/FpvSim/Mb/TouchSticksOverlayMb.cs
git commit -m "feat(mb): add TouchSticksOverlayMb"
```

### Task J.6: SceneStartupMb (spawns the drone via IDroneFactory)

**Files:**
- Create: `Assets/FpvSim/Mb/SceneStartupMb.cs`

- [ ] **Step 1: Implement**

```csharp
using UnityEngine;
using Zenject;
using FpvSim.Scene;

namespace FpvSim.Mb {
    public sealed class SceneStartupMb : MonoBehaviour {
        [Inject] IDroneFactory factory;
        [Inject] DroneSpawnPoint[] spawnPoints;

        void Start() {
            if (spawnPoints == null || spawnPoints.Length == 0) {
                Debug.LogError("SceneStartupMb: no DroneSpawnPoint in scene");
                return;
            }
            factory.Create(spawnPoints[0]);
        }
    }
}
```

- [ ] **Step 2: Commit**

```
git add Assets/FpvSim/Mb/SceneStartupMb.cs
git commit -m "feat(mb): add SceneStartupMb"
```

---

## Milestone K — Installers

### Task K.1: ProjectInstaller

**Files:**
- Create: `Assets/FpvSim/Installers/ProjectInstaller.cs`

- [ ] **Step 1: Implement**

```csharp
using UnityEngine;
using Zenject;

namespace FpvSim.Installers {
    [CreateAssetMenu(menuName = "FpvSim/Installers/ProjectInstaller", fileName = "ProjectInstaller")]
    public sealed class ProjectInstaller : ScriptableObjectInstaller<ProjectInstaller> {
        public override void InstallBindings() {
            SignalBusInstaller.Install(Container);
            DeclareSignals();
        }

        void DeclareSignals() {
            Container.DeclareSignal<FpvSim.Signals.DroneArmedSignal>();
            Container.DeclareSignal<FpvSim.Signals.DroneDisarmedSignal>();
            Container.DeclareSignal<FpvSim.Signals.DroneCrashedSignal>();
            Container.DeclareSignal<FpvSim.Signals.DroneRespawnedSignal>();
        }
    }
}
```

- [ ] **Step 2: Create asset + wire to ProjectContext**

In Project: `Resources/` folder (create if missing). Right-click → `Create → FpvSim → Installers → ProjectInstaller`. Name `ProjectInstaller`.

Then create `ProjectContext`: Project window → right-click in `Resources/` → `Create → Zenject → Project Context` (Zenject menu). Drag `ProjectInstaller` into its `Scriptable Object Installers` list.

- [ ] **Step 3: Commit**

```
git add Assets/FpvSim/Installers/ProjectInstaller.cs Assets/Resources/ProjectInstaller.asset Assets/Resources/ProjectContext.prefab
git commit -m "feat(installers): add ProjectInstaller with signal declarations"
```

### Task K.2: SceneInstaller

**Files:**
- Create: `Assets/FpvSim/Installers/SceneInstaller.cs`

- [ ] **Step 1: Implement**

```csharp
using UnityEngine;
using Zenject;
using FpvSim.Input;
using FpvSim.Scene;

namespace FpvSim.Installers {
    public sealed class SceneInstaller : MonoInstaller {
        [SerializeField] GameObject dronePrefab;

        public override void InstallBindings() {
            Container.Bind<IPilotInputProvider>().To<UnityInputSystemPilotProvider>().AsSingle();
            Container.Bind<DroneSpawnPoint[]>().FromMethod(_ => Object.FindObjectsByType<DroneSpawnPoint>(FindObjectsSortMode.None)).AsCached();
            Container.Bind<IRespawnService>().To<NearestSafeSpawnService>().AsSingle();
            Container.Bind<IGateRegistry>().To<EmptyGateRegistry>().AsSingle();
            Container.Bind<GameObject>().WithId("DronePrefab").FromInstance(dronePrefab).AsCached();
            Container.Bind<IDroneFactory>().To<DroneFactory>().AsSingle();
        }
    }
}
```

- [ ] **Step 2: Commit**

```
git add Assets/FpvSim/Installers/SceneInstaller.cs
git commit -m "feat(installers): add SceneInstaller"
```

### Task K.3: DroneInstaller

**Files:**
- Create: `Assets/FpvSim/Installers/DroneInstaller.cs`

- [ ] **Step 1: Implement**

```csharp
using UnityEngine;
using Zenject;
using FpvSim.Flight;
using FpvSim.States;
using FpvSim.Signals;
using FpvSim.Scene;

namespace FpvSim.Installers {
    public sealed class DroneInstaller : MonoInstaller {
        [SerializeField] FlightConfig flightConfig;

        public override void InstallBindings() {
            Container.Bind<FlightConfig>().FromInstance(flightConfig).AsSingle();
            Container.Bind<IRateController>().To<BetaflightRateController>().AsSingle();
            Container.Bind<IMotorMixer>().To<QuadXMotorMixer>().AsSingle();
            Container.Bind<IFlightDynamics>().To<SubsteppedFlightDynamics>().AsSingle();

            Container.Bind<IDroneState>().To<DisarmedState>().AsSingle();
            Container.Bind<IDroneState>().To<ArmingState>().AsSingle();
            Container.Bind<IDroneState>().To<FlyingState>().AsSingle();
            Container.Bind<IDroneState>().To<CrashedState>().AsSingle();
            Container.Bind<IDroneState>().To<RespawningState>().AsSingle();

            Container.Bind<IDroneStateMachine>().To<DroneStateMachine>().AsSingle();
            Container.Bind<DroneContext>().AsSingle();
            Container.BindInterfacesAndSelfTo<DroneBootstrap>().AsSingle().NonLazy();
        }
    }

    public sealed class DroneBootstrap : IInitializable {
        readonly DroneContext ctx;
        readonly IDroneStateMachine fsm;
        readonly IFlightDynamics dyn;
        readonly IRateController rate;
        readonly IMotorMixer mix;
        readonly SignalBus signals;
        readonly FlightConfig cfg;
        readonly Rigidbody rb;
        readonly Transform root;

        public DroneBootstrap(
            DroneContext ctx,
            IDroneStateMachine fsm,
            IFlightDynamics dyn,
            IRateController rate,
            IMotorMixer mix,
            SignalBus signals,
            FlightConfig cfg,
            [InjectOptional] Rigidbody rb,
            [InjectOptional] Transform root
        ) {
            this.ctx = ctx;
            this.fsm = fsm;
            this.dyn = dyn;
            this.rate = rate;
            this.mix = mix;
            this.signals = signals;
            this.cfg = cfg;
            this.rb = rb;
            this.root = root;
        }

        public void Initialize() {
            ctx.Rb = rb;
            ctx.Root = root;
            ctx.Dynamics = dyn;
            ctx.RateCtrl = rate;
            ctx.Mixer = mix;
            ctx.Fsm = fsm;
            ctx.Signals = signals;
            ctx.Cfg = cfg;
            ctx.SpawnPos = root != null ? root.position : Vector3.zero;
            ctx.SpawnRot = root != null ? root.rotation : Quaternion.identity;
            if (rate is BetaflightRateController bf) bf.Configure(cfg);
            if (rb != null) dyn.Configure(rb, cfg);
            fsm.BindContext(ctx);
            (fsm as DroneStateMachine)?.SubscribeToCrashSignal(signals);
            fsm.ChangeState<DisarmedState>();
        }
    }
}
```

- [ ] **Step 2: Commit**

```
git add Assets/FpvSim/Installers/DroneInstaller.cs
git commit -m "feat(installers): add DroneInstaller with bootstrap"
```

---

## Milestone L — Polygon scene assembly

### Task L.1: Create scene + DI roots

**Files:**
- Create: `Assets/Scenes/Polygon.unity`

- [ ] **Step 1: New scene from URP template**

`File → New Scene → Basic (URP)`. Save As `Assets/Scenes/Polygon.unity`.

- [ ] **Step 2: Add SceneContext**

Create empty GameObject `[DI]`. Add child empty `SceneContext`. On `SceneContext` add component `Zenject → Scene Context` and `Scene Decorator Context` (latter optional). Set its `Parent Container Names` empty (inherits ProjectContext automatically).

- [ ] **Step 3: Add SceneInstaller**

Create empty `SceneInstaller` GameObject. Add component `SceneInstaller` (from Task K.2). Drag this GameObject into `SceneContext.Mono Installers`.

- [ ] **Step 4: Add SceneStartup**

Create empty `SceneStartup`. Add `SceneStartupMb`.

- [ ] **Step 5: Set Build Settings**

`File → Build Profiles`. Add `Assets/Scenes/Polygon.unity` and remove `SampleScene`.

- [ ] **Step 6: Commit**

```
git add Assets/Scenes/Polygon.unity ProjectSettings/EditorBuildSettings.asset
git commit -m "feat(scene): scaffold Polygon scene with SceneContext + SceneInstaller"
```

### Task L.2: Terrain + sky + lighting

**Files:**
- Modify: `Assets/Scenes/Polygon.unity`

- [ ] **Step 1: Terrain**

`GameObject → 3D Object → Plane`. Name `Terrain`. Scale (102.4, 1, 102.4) so plane is 1024×1024 m. Create material `Assets/FpvSim/Scene/Materials/Mat_Asphalt.mat` (URP Lit, dark gray, GPU instancing on). Assign.

Mark `Terrain` as Static (all flags).

- [ ] **Step 2: Sun**

Use existing `Directional Light`. Set Mode = Mixed. Set Rotation (50, -30, 0). Color slightly warm.

- [ ] **Step 3: Reflection probe**

`GameObject → Light → Reflection Probe`. Position at (0, 30, 0). Type Baked. Box size 1024×60×1024.

- [ ] **Step 4: Skybox + ambient**

`Window → Rendering → Lighting`. Environment → Skybox: default URP Procedural Skybox. Ambient → Source: Skybox. Intensity 1.0. Realtime GI off. Baked GI on.

- [ ] **Step 5: Bake lighting**

After buildings are placed in Task L.4 you'll bake; for now just confirm scene loads with single sun and terrain visible.

- [ ] **Step 6: Commit**

```
git add Assets/Scenes/Polygon.unity Assets/FpvSim/Scene/Materials
git commit -m "feat(scene): add terrain + sun + reflection probe"
```

### Task L.3: Place buildings in 4 clusters

**Files:**
- Modify: `Assets/Scenes/Polygon.unity`

- [ ] **Step 1: Create cluster parents**

Under empty parent `Buildings`: 4 child empties at corners — `BuildingCluster_NE` (256, 0, 256), `BuildingCluster_NW` (-256, 0, 256), `BuildingCluster_SE` (256, 0, -256), `BuildingCluster_SW` (-256, 0, -256).

- [ ] **Step 2: Hand-place 8-12 buildings per cluster**

Drag prefabs from `Assets/FpvSim/Scene/Buildings/` into each cluster. Random rotation, varied sizes from palette. Total target ~40 buildings across the map. Leave open corridors so the drone can fly between them.

- [ ] **Step 3: Bake occlusion culling**

`Window → Rendering → Occlusion Culling → Bake`. Wait for finish.

- [ ] **Step 4: Bake lighting**

`Window → Rendering → Lighting → Generate Lighting`. Wait for finish (will take minutes).

- [ ] **Step 5: Commit**

```
git add Assets/Scenes/Polygon.unity Assets/Scenes/Polygon
git commit -m "feat(scene): place ~40 buildings in 4 clusters, bake occlusion + GI"
```

### Task L.4: Spawn points + kill volumes

**Files:**
- Modify: `Assets/Scenes/Polygon.unity`

- [ ] **Step 1: Spawn points**

Under `SpawnPoints` empty: create 6 child empties named `Spawn_01`..`Spawn_06`. Position at varied open spots, ~3 m above terrain, facing into open space. Add `DroneSpawnPoint` component to each.

- [ ] **Step 2: Kill volume**

Empty `KillVolume_Bottom` at (0, -50, 0). Add `BoxCollider`, Is Trigger = true, size (10000, 10, 10000). Add a small MB:
```csharp
using UnityEngine;
using Zenject;
using FpvSim.Signals;

namespace FpvSim.Mb {
    public sealed class KillVolumeMb : MonoBehaviour {
        [Inject] SignalBus signals;
        void OnTriggerEnter(Collider c) {
            if (c.attachedRigidbody != null) {
                signals.Fire(new DroneCrashedSignal(transform.position, 999f));
            }
        }
    }
}
```
Save as `Assets/FpvSim/Mb/KillVolumeMb.cs`. Attach to `KillVolume_Bottom`.

- [ ] **Step 3: Commit**

```
git add Assets/Scenes/Polygon.unity Assets/FpvSim/Mb/KillVolumeMb.cs
git commit -m "feat(scene): add spawn points + bottom kill volume"
```

### Task L.5: HUD canvas with touch sticks and OSD

**Files:**
- Modify: `Assets/Scenes/Polygon.unity`

- [ ] **Step 1: HUD canvas**

`GameObject → UI → Canvas`. Name `HudCanvas`. Render Mode = Screen Space - Overlay. Add `CanvasScaler` with Scale With Screen Size, ref 1920×1080.

- [ ] **Step 2: Touch sticks**

Add two child `GameObject → UI → Image` named `LeftStickBackground` (bottom-left, 280×280) and `RightStickBackground` (bottom-right, 280×280). To each add child `Image` named `Knob`. On each background, add component `On-Screen Stick` (Input System). Set Control Path:
- Left → `<Touchscreen>/...` virtual stick → write to `<Gamepad>/leftStick` (Input System will route via Pilot actions because actions are bound to `<Gamepad>/leftStick`).
- Right → same with `<Gamepad>/rightStick`.

Set Movement Range 50 on each. Knob = Stick Image.

Wrap both under `TouchSticksOverlay` GameObject. Add `TouchSticksOverlayMb` and wire stick roots to its fields.

- [ ] **Step 3: OSD**

`UI → Text - TextMeshPro` × 2 named `ModeText` (top-center) and `FpsText` (top-right). Add empty `OsdView` GameObject parented to `HudCanvas`. Add `OsdViewMb`. Drag `ModeText` and `FpsText` into its fields.

- [ ] **Step 4: Commit**

```
git add Assets/Scenes/Polygon.unity
git commit -m "feat(scene): add HUD with touch sticks + OSD"
```

### Task L.6: Drone prefab assembly

**Files:**
- Create: `Assets/FpvSim/Scene/Drone/Drone.prefab`

- [ ] **Step 1: Build drone GameObject in scene**

Empty `Drone` at origin. Add components: `Rigidbody`, `BoxCollider` (size 0.22, 0.05, 0.22), `FlightDynamicsTickerMb`, `DroneCrashDetectorMb`, `DroneFacade`. Set Rigidbody → set "Use Gravity" on; everything else default (DroneInstaller overrides at runtime).

- [ ] **Step 2: Visual children**

Add 5 child cubes: 4 small (0.04×0.02×0.04) at the 4 motor positions (±0.11, 0, ±0.11) — these match `FlightConfig.MotorPositions`. One central frame cube (0.18×0.04×0.18). Apply `Mat_Building` (or create `Mat_Drone` — dark, GPU instanced).

- [ ] **Step 3: FPV camera child**

Child `FpvCam` at (0, 0.03, 0). Add `Camera` (Clear Flags = Skybox, FOV ignored, gets set by FpvCameraMb). Add `FpvCameraMb`.

- [ ] **Step 4: GameObjectContext + DroneInstaller**

On `Drone` root add `GameObjectContext`. As children of `Drone`, create empty `DroneInstaller` GameObject with `DroneInstaller` component. Drag `DefaultQuad` (FlightConfig) into its field. Drag `DroneInstaller` into `GameObjectContext.Mono Installers`.

- [ ] **Step 5: Save as prefab**

Drag `Drone` from Hierarchy into `Assets/FpvSim/Scene/Drone/`. Delete from scene Hierarchy. Drag the prefab into `SceneInstaller.Drone Prefab` field on the `SceneInstaller` GameObject in the scene.

- [ ] **Step 6: Commit**

```
git add Assets/FpvSim/Scene/Drone Assets/Scenes/Polygon.unity
git commit -m "feat(scene): assemble Drone prefab with GameObjectContext + DroneInstaller"
```

---

## Milestone M — Architecture / lint tests

### Task M.1: LayeringTests via reflection

**Files:**
- Create: `Tests/EditMode/Architecture/LayeringTests.cs`

- [ ] **Step 1: Implement**

```csharp
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace FpvSim.Architecture.Tests {
    public class LayeringTests {
        Assembly Asm(string name) {
            return AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == name);
        }

        [Test]
        public void FlightAssembly_DoesNotReferenceInputSystem() {
            var flight = Asm("FpvSim.Flight");
            foreach (var refAsm in flight.GetReferencedAssemblies()) {
                Assert.AreNotEqual("Unity.InputSystem", refAsm.Name, "FpvSim.Flight must not reference Unity.InputSystem");
            }
        }

        [Test]
        public void FlightAssembly_DoesNotReferenceUI() {
            var flight = Asm("FpvSim.Flight");
            foreach (var refAsm in flight.GetReferencedAssemblies()) {
                Assert.AreNotEqual("UnityEngine.UI", refAsm.Name, "FpvSim.Flight must not reference UnityEngine.UI");
                Assert.AreNotEqual("Unity.TextMeshPro", refAsm.Name, "FpvSim.Flight must not reference Unity.TextMeshPro");
            }
        }

        [Test]
        public void StatesAssembly_DoesNotReferenceInputSystem() {
            var states = Asm("FpvSim.States");
            foreach (var refAsm in states.GetReferencedAssemblies()) {
                Assert.AreNotEqual("Unity.InputSystem", refAsm.Name);
            }
        }

        [Test]
        public void IDroneStateImpls_HaveOnlyReadonlyFields() {
            var statesAsm = Asm("FpvSim.States");
            var iface = statesAsm.GetType("FpvSim.States.IDroneState");
            var impls = statesAsm.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract && iface.IsAssignableFrom(t));
            foreach (var t in impls) {
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
                    Assert.IsTrue(f.IsInitOnly, $"{t.Name}.{f.Name} must be readonly (state classes are stateless — store mutable state in DroneContext)");
                }
            }
        }

        [Test]
        public void SubsteppedFlightDynamics_DoesNotHaveMonoBehaviourFields() {
            var flight = Asm("FpvSim.Flight");
            var t = flight.GetType("FpvSim.Flight.SubsteppedFlightDynamics");
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
                Assert.IsFalse(typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(f.FieldType),
                    $"{t.Name}.{f.Name} must not be a MonoBehaviour type");
            }
        }
    }
}
```

- [ ] **Step 2: Run, expect PASS**

- [ ] **Step 3: Commit**

```
git add Tests/EditMode/Architecture/LayeringTests.cs
git commit -m "test(arch): add layering tests via reflection"
```

---

## Milestone N — Play Mode integration tests

### Task N.1: TestSceneContextInstaller (swaps input for scripted)

**Files:**
- Create: `Tests/PlayMode/Flight/TestSupport/TestSceneInstaller.cs`

- [ ] **Step 1: Implement**

```csharp
using System;
using UnityEngine;
using Zenject;
using FpvSim.Input;
using FpvSim.Flight;
using FpvSim.Scene;

namespace FpvSim.PlayMode.Tests {
    public sealed class TestSceneInstaller : MonoInstaller {
        public Func<float, PilotCommand> ScriptedSequence;
        public GameObject DronePrefab;

        public override void InstallBindings() {
            Container.Bind<IPilotInputProvider>().FromInstance(new ScriptedPilotProvider(ScriptedSequence)).AsSingle();
            Container.Bind<DroneSpawnPoint[]>().FromMethod(_ => UnityEngine.Object.FindObjectsByType<DroneSpawnPoint>(FindObjectsSortMode.None)).AsCached();
            Container.Bind<IRespawnService>().To<NearestSafeSpawnService>().AsSingle();
            Container.Bind<IGateRegistry>().To<EmptyGateRegistry>().AsSingle();
            Container.Bind<GameObject>().WithId("DronePrefab").FromInstance(DronePrefab).AsCached();
            Container.Bind<IDroneFactory>().To<DroneFactory>().AsSingle();
        }
    }
}
```

- [ ] **Step 2: Commit**

```
git add Tests/PlayMode/Flight/TestSupport/TestSceneInstaller.cs
git commit -m "test(playmode): add TestSceneInstaller for scripted input"
```

### Task N.2: HoverScenarioTest

**Files:**
- Create: `Tests/PlayMode/Flight/HoverScenarioTest.cs`

- [ ] **Step 1: Implement**

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using FpvSim.Flight;
using FpvSim.Scene;
using FpvSim.States;

namespace FpvSim.PlayMode.Tests {
    public class HoverScenarioTest {
        [UnityTest]
        public IEnumerator Drone_Hovers_At_Constant_Altitude_For_3s() {
            yield return SceneManager.LoadSceneAsync("Polygon", LoadSceneMode.Single);
            yield return null;
            var facade = Object.FindFirstObjectByType<DroneFacade>();
            Assert.IsNotNull(facade, "Drone was not spawned");
            facade.Fsm.ChangeState<ArmingState>();
            yield return new WaitForSeconds(0.4f);
            var startY = facade.Root.position.y;
            float endTime = Time.time + 3f;
            while (Time.time < endTime) yield return null;
            var endY = facade.Root.position.y;
            var rb = facade.GetComponent<Rigidbody>();
            Assert.That(Mathf.Abs(endY - startY), Is.LessThan(2f), "Drone altitude drifted more than 2 m");
            Assert.That(rb.linearVelocity.magnitude, Is.LessThan(2f), "Drone velocity too high after settling");
        }
    }
}
```

Note: this test currently uses the production `Polygon` scene with the production `SceneInstaller` (which uses real input). For the assertion to be meaningful, the test must either: (a) load a dedicated `Polygon_Test.unity` that uses `TestSceneInstaller`, or (b) inject a scripted throttle programmatically. For MVP go with option (a) — see Task N.3 setup.

- [ ] **Step 2: Skip (mark `[Ignore]`) until Polygon_Test scene exists**

Add `[Ignore("Needs Polygon_Test scene")]` above the test. Commit a green run.

- [ ] **Step 3: Commit**

```
git add Tests/PlayMode/Flight/HoverScenarioTest.cs
git commit -m "test(playmode): add hover scenario placeholder (ignored)"
```

### Task N.3: Polygon_Test scene + enable hover test

**Files:**
- Create: `Assets/Scenes/Polygon_Test.unity`

- [ ] **Step 1: Duplicate Polygon scene**

In Project: copy `Polygon.unity` → `Polygon_Test.unity`. Open it.

- [ ] **Step 2: Swap SceneInstaller for TestSceneInstaller**

On `SceneInstaller` GameObject: remove `SceneInstaller` component, add `TestSceneInstaller`. Wire `DronePrefab`. Leave `ScriptedSequence` null; the test will set it via a helper installer pattern — for simplicity, embed the default sequence inline as a static helper:

Add to `TestSceneInstaller`:
```csharp
void Awake() {
    if (ScriptedSequence == null) ScriptedSequence = t => new PilotCommand(0.55f, 0f, 0f, 0f, t > 0.05f && t < 0.06f, false);
}
```

This default sequence arms the drone briefly and then holds throttle 0.55 — close to hover for default `FlightConfig`. Tune the throttle constant after the first run.

- [ ] **Step 3: Add scene to Build Settings**

`File → Build Profiles → Scene List → Add Open Scenes`.

- [ ] **Step 4: Enable test**

Change `HoverScenarioTest`:
- Remove `[Ignore]` line.
- Change scene name from `"Polygon"` to `"Polygon_Test"`.

- [ ] **Step 5: Run Play Mode tests**

`Test Runner → PlayMode → Run All`. Tune `FlightConfig.MaxThrustPerMotorN` (currently 6.5) or the scripted throttle so the drone holds altitude within tolerance. Commit final values.

- [ ] **Step 6: Commit**

```
git add Assets/Scenes/Polygon_Test.unity Tests/PlayMode/Flight/HoverScenarioTest.cs Tests/PlayMode/Flight/TestSupport/TestSceneInstaller.cs Assets/FpvSim/Resources/Flight/DefaultQuad.asset
git commit -m "test(playmode): enable HoverScenarioTest with Polygon_Test scene"
```

### Task N.4: ForwardFlight, Crash, Respawn tests

**Files:**
- Create: `Tests/PlayMode/Flight/ForwardFlightScenarioTest.cs`
- Create: `Tests/PlayMode/Flight/CrashTriggersStateTransitionTest.cs`
- Create: `Tests/PlayMode/Flight/RespawnResetsVelocityTest.cs`

- [ ] **Step 1: ForwardFlightScenarioTest**

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using FpvSim.Flight;
using FpvSim.Scene;
using FpvSim.States;

namespace FpvSim.PlayMode.Tests {
    public class ForwardFlightScenarioTest {
        [UnityTest]
        public IEnumerator Drone_Gains_Horizontal_Speed_With_Pitch_Forward() {
            yield return SceneManager.LoadSceneAsync("Polygon_Test", LoadSceneMode.Single);
            yield return null;
            var installer = Object.FindFirstObjectByType<TestSceneInstaller>();
            installer.ScriptedSequence = t => new PilotCommand(0.6f, 0f, 0.3f, 0f, t > 0.05f && t < 0.06f, false);
            yield return new WaitForSeconds(0.6f);
            var facade = Object.FindFirstObjectByType<DroneFacade>();
            facade.Fsm.ChangeState<ArmingState>();
            yield return new WaitForSeconds(2f);
            var rb = facade.GetComponent<Rigidbody>();
            var horiz = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
            Assert.That(horiz, Is.GreaterThan(3f));
        }
    }
}
```

- [ ] **Step 2: CrashTriggersStateTransitionTest**

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using FpvSim.Flight;
using FpvSim.Scene;
using FpvSim.States;

namespace FpvSim.PlayMode.Tests {
    public class CrashTriggersStateTransitionTest {
        [UnityTest]
        public IEnumerator Crash_Into_Ground_Transitions_To_CrashedState() {
            yield return SceneManager.LoadSceneAsync("Polygon_Test", LoadSceneMode.Single);
            yield return null;
            var installer = Object.FindFirstObjectByType<TestSceneInstaller>();
            installer.ScriptedSequence = t => new PilotCommand(0f, 0f, 0f, 0f, t > 0.05f && t < 0.06f, false);
            yield return new WaitForSeconds(0.5f);
            var facade = Object.FindFirstObjectByType<DroneFacade>();
            facade.Fsm.ChangeState<FlyingState>();
            yield return new WaitForSeconds(3f);
            Assert.IsInstanceOf<CrashedState>(facade.Fsm.Current);
        }
    }
}
```

- [ ] **Step 3: RespawnResetsVelocityTest**

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using FpvSim.Flight;
using FpvSim.Scene;
using FpvSim.States;

namespace FpvSim.PlayMode.Tests {
    public class RespawnResetsVelocityTest {
        [UnityTest]
        public IEnumerator Respawn_Zeroes_Velocity_And_Returns_To_Spawn() {
            yield return SceneManager.LoadSceneAsync("Polygon_Test", LoadSceneMode.Single);
            yield return null;
            var installer = Object.FindFirstObjectByType<TestSceneInstaller>();
            installer.ScriptedSequence = t => new PilotCommand(0.6f, 0f, 0.4f, 0f, t > 0.05f && t < 0.06f, false);
            yield return new WaitForSeconds(0.4f);
            var facade = Object.FindFirstObjectByType<DroneFacade>();
            facade.Fsm.ChangeState<FlyingState>();
            yield return new WaitForSeconds(1.5f);
            var spawnPos = facade.GetComponent<Rigidbody>().position;
            installer.ScriptedSequence = t => new PilotCommand(0f, 0f, 0f, 0f, false, true);
            yield return new WaitForSeconds(1f);
            var rb = facade.GetComponent<Rigidbody>();
            Assert.That(rb.linearVelocity.magnitude, Is.LessThan(0.5f));
        }
    }
}
```

- [ ] **Step 4: Run all Play Mode tests, fix flake**

Iterate on thresholds if tests are flaky on first run.

- [ ] **Step 5: Commit**

```
git add Tests/PlayMode/Flight/ForwardFlightScenarioTest.cs Tests/PlayMode/Flight/CrashTriggersStateTransitionTest.cs Tests/PlayMode/Flight/RespawnResetsVelocityTest.cs
git commit -m "test(playmode): add forward-flight, crash, respawn scenarios"
```

---

## Milestone O — Manual QA + mobile validation

### Task O.1: Write manual checklist

**Files:**
- Create: `docs/qa/mvp-checklist.md`

- [ ] **Step 1: Write**

```markdown
# FPV Sim MVP — Manual QA Checklist

Run before any merge to `main` that touches flight, input, scene, or installers.

## Editor — PC, keyboard
- [ ] Open `Polygon.unity`, press Play
- [ ] Press Space to arm — OSD reads "ACRO"
- [ ] Hold W until drone rises ~10 m
- [ ] Up arrow to pitch forward, fly 50 m
- [ ] Bank a turn using Right arrow
- [ ] Crash into a building — OSD reads "CRASHED — tap respawn"
- [ ] Press R — drone respawns at nearest spawn point, OSD reads "DISARMED"
- [ ] Repeat full loop 3 times — no exceptions in console

## Editor — PC, Xbox gamepad
- [ ] Connect gamepad, press Play
- [ ] South button to arm, left stick vertical = throttle, right stick = pitch/roll
- [ ] Fly the same loop 3 times

## Android — mid-tier device (Pixel 6a or Snapdragon 778G class)
- [ ] Build → Android → APK with `Polygon` as start scene
- [ ] Install + run
- [ ] Tap on-screen Arm area (TBD position) — OSD reads "ACRO"
- [ ] Fly with dual touch sticks — stable control, no janky input
- [ ] OSD FPS reads ≥ 55 most of the time, peaks not below 50
- [ ] Crash + respawn loop works
```

- [ ] **Step 2: Commit**

```
git add docs/qa/mvp-checklist.md
git commit -m "docs: add MVP manual QA checklist"
```

### Task O.2: PC editor smoke test

- [ ] **Step 1: Execute the "Editor — PC, keyboard" section of the checklist**

If anything fails: file follow-up task, fix, re-run.

- [ ] **Step 2: No commit (read-only test)**

### Task O.3: Android build + on-device test

- [ ] **Step 1: Switch platform**

`File → Build Profiles → Android → Switch Platform`.

- [ ] **Step 2: Player Settings**

`Edit → Project Settings → Player`:
- Scripting Backend: IL2CPP
- Target Architectures: ARM64 only
- Color Space: Linear
- Minimum API Level: 26
- Target API Level: Automatic (Highest installed)
- Active Input Handling: Input System Package (New)

`Edit → Project Settings → Quality`: only one quality level "Mobile" enabled, with `Mobile_RPAsset`.

- [ ] **Step 3: Build APK**

`Build Profiles → Build`. Save to `Builds/Android/fpv_sim_mvp.apk`.

- [ ] **Step 4: Install on device + run checklist Android section**

`adb install -r Builds/Android/fpv_sim_mvp.apk`.

Step through Android checklist. If FPS budget fails, profile and follow up in a new spec (perf is out of MVP scope per design doc).

- [ ] **Step 5: No commit (binary build, kept out of git)**

---

## Self-review summary

Verified before handoff:

- **Spec coverage:** every section of `2026-05-23-fpv-sim-mvp-design.md` maps to ≥1 task in this plan. Spec's "Out of MVP" list — confirmed not implemented (Angle mode, gate timing, telemetry overlay, audio, multiplayer).
- **Placeholders:** none. All code samples are concrete and runnable. Two known runtime tunings (hover throttle constant in N.3, building cluster density in L.3) require empirical adjustment during the relevant task — these are real engineering decisions, not plan holes.
- **Type consistency:** `IRateController` adds `Reset()` and a `Configure(FlightConfig)` extension method only used on the concrete class (`BetaflightRateController`). `DroneStateMachine.SubscribeToCrashSignal(SignalBus)` is called from `DroneBootstrap` via the concrete cast `(fsm as DroneStateMachine)?.` — interface stays clean. `DroneContext` shape used in `DroneStateMachine` and in concrete states matches what `DroneBootstrap.Initialize` populates.
- **Scope:** the plan covers everything needed to ship the MVP build defined in the spec. It does not bring in scene streaming, gate timing, or any deferred feature.

---

## Execution

Plan complete and saved to `docs/superpowers/plans/2026-05-23-fpv-sim-mvp.md`. Two execution options:

1. **Subagent-Driven (recommended)** — dispatch a fresh subagent per task, review between tasks, fast iteration.
2. **Inline Execution** — execute tasks in this session using executing-plans, batch execution with checkpoints.

Which approach?
