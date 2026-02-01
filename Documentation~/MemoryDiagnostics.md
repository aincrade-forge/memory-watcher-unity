# Memory Diagnostics (iOS)

Event-driven iOS memory telemetry with minimal overhead. This document mirrors README with concise usage notes.

## Quick Start
- Install via UPM (git tag or local path)
- Call `MemoryDiagnostics.Initialize(...)` early (e.g., BeforeSceneLoad)
- Subscribe to `MemoryDiagnostics.Instance` events

## Key Events
- `OnSessionStart`, `OnSample`, `OnLowMemoryWarning`, `OnAppStateChanged`, `OnSessionEnd`

## Properties
`CurrentFootprintBytes`, `PeakFootprintBytes`, `CurrentFootprintMB`, `PeakFootprintMB`, `DeviceModel`, `OSVersion`, `TotalRamBytes`, `LowMemoryWarningCount`, `SessionId`, `CurrentAppState`, `LatestSnapshot`.

## iOS Implementation Notes
- Uses `TASK_VM_INFO.phys_footprint` (fallback to resident size)
- Total RAM via `NSProcessInfo.physicalMemory`
- Device model via `sysctl hw.machine`, iOS version via `UIDevice.systemVersion`
- Peak tracked locally in managed code per session

<!-- Disk writing removed from this package to keep scope minimal and event-only. -->

## Runtime Configuration

Single entrypoint API:

```csharp
using Aincrad;

// Programmatic: before instance exists
MemoryDiagnostics.Initialize(sampleIntervalSeconds: 0.5f);

// After instance exists
var md = MemoryDiagnostics.Instance;
md.SetSampleInterval(0.25f);
```

One‑shot sampling:

```csharp
var snap = MemoryDiagnostics.SampleOnce();
```
