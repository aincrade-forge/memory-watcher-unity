# Memory Watcher — Unity UPM Package

Memory footprint telemetry for Unity (iOS, Android, macOS, Windows). Sampling runs on the main thread via the PlayerLoop with peak tracking.

## Install

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.aincrade.memory-watcher": "https://github.com/aincrade-forge/memory-watcher-unity.git#v0.6.6"
  }
}
```

## Usage

```csharp
using MemoryDiagnostics;

MemoryDiagnosticsManager.Initialize(sampleIntervalSeconds: 1.0f);
MemoryDiagnosticsManager.Instance.OnSample += s =>
{
    // s.currentMemoryMB / s.peakMemoryMB
};

// Optional overlay (safe‑area aware)
MemoryDiagnosticsOverlay.Show();
// or choose anchor in code
MemoryDiagnosticsOverlay.Show(MemoryDiagnosticsOverlay.OverlayAnchor.BottomRight);
```

## Configuration

```csharp
MemoryDiagnosticsManager.Initialize(sampleIntervalSeconds: 0.5f);
MemoryDiagnosticsManager.Instance.SetSampleInterval(0.25f);
```

## API

- `MemoryDiagnosticsManager.Initialize(sampleIntervalSeconds)`
- `MemoryDiagnosticsManager.SetSampleInterval(seconds)`
- `MemoryDiagnosticsManager.OnSample(MemoryDiagSnapshot)`
- `MemoryDiagnosticsOverlay.Show()`
- `MemoryDiagnosticsOverlay.Show(anchor)`

## Overlay Placement

`MemoryDiagnosticsOverlay` supports top‑left, top‑right, bottom‑left, and bottom‑right anchors. Configure `_anchor`, `_margin`, and `_size` in the inspector, or pass an anchor to `Show(anchor)`. Positions are safe‑area aware.

## Platform Metrics

- iOS/macOS: `task_info(..., TASK_VM_INFO)` → `phys_footprint` (resident fallback).
- Android: `android.os.Debug.getPss()` (KB → bytes).
- Windows: `GetProcessMemoryInfo(...).WorkingSetSize`.

## macOS Native Source

- Source: `Native~/macOS/MemoryDiagnostics.mm`
- Build: `Scripts~/build_macos.sh` (universal arm64 + x86_64)

## License

MIT (see [LICENSE](LICENSE)).
