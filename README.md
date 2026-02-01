# Memory Watcher — Unity UPM Package

Memory footprint telemetry for Unity (iOS, Android, macOS, Windows). Sampling runs on the main thread via the PlayerLoop with peak tracking.

## Install

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.aincrade.memory-watcher": "https://github.com/aincrade-forge/memory-watcher-unity.git#v0.8.0"
  }
}
```

## Usage

```csharp
using Aincrad;

MemoryDiagnostics.Initialize(sampleIntervalSeconds: 1.0f);
MemoryDiagnostics.Instance.OnSample += s =>
{
    // s.currentMemoryMB / s.peakMemoryMB
};
```

Fully qualified if you prefer:

```csharp
Aincrad.MemoryDiagnostics.Initialize(sampleIntervalSeconds: 1.0f);
```

One‑shot (no PlayerLoop):

```csharp
var snap = MemoryDiagnostics.SampleOnce();
```

## Configuration

```csharp
MemoryDiagnostics.Initialize(sampleIntervalSeconds: 0.5f);
MemoryDiagnostics.Instance.SetSampleInterval(0.25f);
```

## API

- `MemoryDiagnostics.Initialize(sampleIntervalSeconds)`
- `MemoryDiagnostics.SetSampleInterval(seconds)`
- `MemoryDiagnostics.OnSample(MemoryDiagSnapshot)`
- `MemoryDiagnostics.SampleOnce()`

## Optional Overlay (Sample)

Import the "Overlay" sample from the Package Manager to get `MemoryDiagnosticsOverlay` (safe‑area aware).

```csharp
using Aincrad;

MemoryDiagnosticsOverlay.Show();
// or choose anchor in code
MemoryDiagnosticsOverlay.Show(MemoryDiagnosticsOverlay.OverlayAnchor.BottomRight);
```

`MemoryDiagnosticsOverlay` supports top‑left, top‑right, bottom‑left, and bottom‑right anchors. Configure `_anchor`, `_margin`, and `_size` in the inspector, or pass an anchor to `Show(anchor)`. Positions are safe‑area aware.

## Platform Metrics

- iOS/macOS: `task_info(..., TASK_VM_INFO)` → `phys_footprint` (resident fallback).
- Android: `android.os.Debug.getPss()` (KB → bytes).
- Windows: `GetProcessMemoryInfo(...).WorkingSetSize`.

### Android Performance Note

`Debug.getPss()` can be relatively heavy (it walks process memory stats). Avoid per-frame sampling; prefer a 1–5s interval for overlays and longer for background telemetry. If you only need occasional readings, use `MemoryDiagnostics.SampleOnce()` sparingly (main thread recommended on Android).

## macOS Native Source

- Source: `Native~/macOS/MemoryDiagnostics.mm`
- Build: `Scripts~/build_macos.sh` (universal arm64 + x86_64)

## License

MIT (see [LICENSE](LICENSE)).
