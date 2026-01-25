# Memory Watcher — Unity UPM Package

Unity iOS/Android/macOS/Windows memory footprint sampler with peak tracking and zero-GC PlayerLoop updates.


## Features

- Current and peak memory footprint (platform-appropriate source)
- Main-thread sampling via PlayerLoop (no GameObject)
- Minimal per-sample overhead (single native call per tick)

## Install (UPM)

Add this to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.aincrade.memory-watcher": "https://github.com/aincrade-forge/memory-watcher-unity.git#v0.6.0"
  }
}
```
No files are required under `Assets/` — this is a UPM package.

## Initialization

Call `MemoryDiagnosticsManager.Initialize(sampleIntervalSeconds: 1.0f)` from your code (e.g., in a small BeforeSceneLoad initializer). No GameObject placement required.

## Events (main thread)

- `OnSample(MemoryDiagSnapshot)`

## Properties (read-only)

- `CurrentMemoryBytes`, `PeakMemoryBytes`
- `CurrentMemoryMB`, `PeakMemoryMB`
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
        Debug.Log($"Current: {s.currentMemoryMB:F1} MB, Peak: {s.peakMemoryMB:F1} MB");
        // Send telemetry, update UI, etc.
    }

}
```

## Configuration

```csharp
using MemoryDiagnostics;

MemoryDiagnosticsManager.Initialize(sampleIntervalSeconds: 0.5f);

var md = MemoryDiagnosticsManager.Instance;
md.SetSampleInterval(0.25f); // adjust sampling rate
```

## API (minimal)

- `MemoryDiagnosticsManager.Initialize(sampleIntervalSeconds)`
- `MemoryDiagnosticsManager.SetSampleInterval(seconds)`
- `MemoryDiagnosticsManager.OnSample(MemoryDiagSnapshot)`

## Platform Details

- iOS: `task_info(..., TASK_VM_INFO)` → `phys_footprint`, fallback to resident size.
- Android: `android.os.Debug.getPss()` (KB) converted to bytes.
- macOS: `task_info(..., TASK_VM_INFO)` → `phys_footprint`, fallback to resident size.
- Windows: `GetProcessMemoryInfo(...).WorkingSetSize`.
- Peak tracked locally per sample in managed code.

## Performance Notes

- Exactly one native call per sampling tick (`MD_GetMemoryFootprintBytes`).
- No disk I/O performed by this plugin.
- No per-sample managed allocations inside the package.

## License

MIT (see [LICENSE](LICENSE)).
