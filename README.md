# Memory Watcher (iOS) — Unity UPM Package

Unity iOS memory footprint sampler with peak tracking and zero-GC PlayerLoop updates. Designed for multi-project reuse via UPM (git or local path). The package samples memory footprint and exposes events; consumers handle telemetry, UI, alerts, or dumps.

- iOS native plugin under `Plugins/iOS/MemoryDiagnostics.mm`
- Unity runtime manager under `Runtime/MemoryDiagnosticsManager.cs`
- No scene setup required; initialize via `MemoryDiagnosticsManager.Initialize(...)`


## Features

- Current and peak memory footprint (phys_footprint with resident fallback)
- Strongly-typed, main-thread events
- Minimal per-sample overhead (single native call per tick)

## Install (UPM)

### Git URL (recommended)
1. Tag this repository (e.g., `v0.4.0`).
2. In your Unity project `Packages/manifest.json`, add:

```json
{
  "dependencies": {
    "com.aincrade.memory-watcher": "https://github.com/aincrade-forge/memory-watcher-unity.git#v0.4.0"
  }
}
```

### Local path (development)
In `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.aincrade.memory-watcher": "file:/ABSOLUTE/PATH/TO/memory-watcher-unity"
  }
}
```

No files are required under `Assets/` — this is a UPM package.

## Files and Structure

- iOS native: `Plugins/iOS/MemoryDiagnostics.mm`
- Runtime C#: `Runtime/MemoryDiagnosticsManager.cs`
- Assembly definition: `Runtime/MemoryDiagnostics.asmdef`
- Package manifest: `package.json`

## Initialization

- Manual-only: call `MemoryDiagnosticsManager.Initialize(sampleIntervalSeconds: 1.0f)` from your code.
- Recommended: call in a small BeforeSceneLoad initializer.
- No GameObject placement or scene setup required; sampling hooks into the PlayerLoop.

## Events (main thread)

- `OnSample(MemoryDiagSnapshot)`

## Properties (read-only)

- `CurrentFootprintBytes`, `PeakFootprintBytes`
- `CurrentFootprintMB`, `PeakFootprintMB`
- `LatestSnapshot`

## Usage Example

```csharp
using MemoryDiagnostics;
using UnityEngine;

public class MemDiagExample : MonoBehaviour
{
    void OnEnable()
    {
        var md = MemoryDiagnosticsManager.Initialize();
        md.OnSample += OnSample;
    }

    void OnDisable()
    {
        var md = MemoryDiagnosticsManager.Instance;
        if (md == null) return;
        md.OnSample -= OnSample;
    }

    void OnSample(MemoryDiagSnapshot s)
    {
        Debug.Log($"Current: {s.currentFootprintMB:F1} MB, Peak: {s.peakFootprintMB:F1} MB");
        // Send telemetry, update UI, etc.
    }

}
```

## Configuration

Single entrypoint: `MemoryDiagnosticsManager.Initialize(...)`. The plugin does not auto-initialize; call `Initialize(...)` yourself (e.g., from an early initializer) or at runtime via `SetSampleInterval`. Initialization installs a PlayerLoop hook for sampling.

```csharp
using MemoryDiagnostics;

// Single API to initialize and configure

// Programmatic override (optional) before first access
MemoryDiagnosticsManager.Initialize(sampleIntervalSeconds: 0.5f);

// After startup
var md = MemoryDiagnosticsManager.Instance;
md.SetSampleInterval(0.25f);          // adjust sampling rate
```

<!-- Disk writing removed from this package to keep scope minimal and event-only. -->

## Testing Without Embedding a Unity Project

1. Create an empty Unity project (no sample scene required).
2. Add this package via git URL or local path in `Packages/manifest.json`.
3. Open the project once to let Unity import the package; ensure Editor compiles (no iOS native calls run in Editor).
4. (Optional) Add the `MemDiagExample` above to verify events.
5. Build iOS to generate an Xcode project and verify native linking (`Plugins/iOS/MemoryDiagnostics.mm`).

CI idea: generate a minimal Unity project (manifest only) that references this repo by git SHA, run in batchmode to compile scripts, and produce an iOS player/Xcode project for link validation.

## iOS Details

- Minimum iOS: 10.0
- Native footprint via `task_info(..., TASK_VM_INFO)` → `phys_footprint`; fallback to resident size.
- Peak tracked locally per sample in managed code.
- Device model cached via `sysctl hw.machine`; iOS version cached via `UIDevice.systemVersion`.

## Performance Notes

- Exactly one native call per sampling tick (`MD_GetMemoryFootprintBytes`).
- Timestamps use Unix time milliseconds; no `DateTime.ToString` or per-sample formatting.
- No disk I/O performed by this plugin.
- No exceptions thrown to game code; all public events are safe with zero subscribers.

## License

MIT (see `package.json`).
