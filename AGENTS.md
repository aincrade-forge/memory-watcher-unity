# Repository Guidelines

## Project Structure & Module Organization
- `Runtime/` contains the Unity C# runtime code (`MemoryDiagnosticsManager.cs`) and the asmdef.
- `Plugins/iOS/` contains the iOS native plugin source (`MemoryDiagnostics.mm`).
- `Plugins/macOS/` contains the built macOS dylib (`libMemoryDiagnostics.dylib`) used by Unity at runtime.
- `Native/macOS/` contains macOS native source code kept out of Unity imports.
- `Scripts/` contains helper scripts (e.g., macOS dylib build).

There are no automated tests or sample Unity projects in this repo.

## Build, Test, and Development Commands
- `Scripts/build_macos.sh`  
  Builds `Plugins/macOS/libMemoryDiagnostics.dylib` from `Native/macOS/MemoryDiagnostics.mm`.  
  Requires Xcode command line tools (`clang++`).

No other build/test commands are defined in this repository.

## Coding Style & Naming Conventions
- C#: 4‑space indentation, PascalCase for public members, camelCase for private fields.
- Native (Objective‑C++/C++): 4‑space indentation; keep functions small and error‑tolerant.
- Naming follows “Memory” terminology (e.g., `CurrentMemoryBytes`, `PeakMemoryMB`).
- Avoid per‑sample allocations and exceptions in hot paths.

## Testing Guidelines
- No test framework is currently configured.
- If you add tests, keep them platform‑specific and document how to run them.

## Commit & Pull Request Guidelines
- Commit messages use imperative, short summaries (e.g., “Add Windows memory sampling”).
- Prefer one logical change per commit; include platform names in the subject when relevant.
- PRs should include:
  - Summary of changes and affected platforms.
  - Notes on native binaries updated (if any).
  - Verification steps (e.g., “Built macOS dylib”, “Android JNI call verified”).

## Platform Notes
- iOS/macOS use `phys_footprint` via native `task_info`.
- Android uses `android.os.Debug.getPss()` with JNI signature fallback.
- Windows uses `GetProcessMemoryInfo(...).WorkingSetSize`.
