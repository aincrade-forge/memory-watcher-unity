# Memory Watcher — Unity UPM Package

Memory footprint telemetry for Unity (iOS, Android, macOS, Windows). Sampling runs on the main thread via the PlayerLoop with peak tracking.

## Install

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.aincrade.memory-watcher": "https://github.com/aincrade-forge/memory-watcher-unity.git#v0.7.1"
  }
}
```

## Usage

```csharp
using MemoryDiagnostics;

Sampler.Initialize(sampleIntervalSeconds: 1.0f);
Sampler.Instance.OnSample += s =>
{
    // s.currentMemoryMB / s.peakMemoryMB
};
```

One‑shot (no PlayerLoop):

```csharp
var snap = Sampler.SampleOnce();
```

## Configuration

```csharp
Sampler.Initialize(sampleIntervalSeconds: 0.5f);
Sampler.Instance.SetSampleInterval(0.25f);
```

## API

- `Sampler.Initialize(sampleIntervalSeconds)`
- `Sampler.SetSampleInterval(seconds)`
- `Sampler.OnSample(MemoryDiagSnapshot)`
- `Sampler.SampleOnce()`

## Optional Overlay (Sample)

Import the "Overlay" sample from the Package Manager to get `MemoryDiagnosticsOverlay` (safe‑area aware).

```csharp
MemoryDiagnosticsOverlay.Show();
// or choose anchor in code
MemoryDiagnosticsOverlay.Show(MemoryDiagnosticsOverlay.OverlayAnchor.BottomRight);
```

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
