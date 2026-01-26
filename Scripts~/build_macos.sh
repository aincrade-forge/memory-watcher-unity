#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT_DIR="$ROOT_DIR/Plugins/macOS"
OUT_LIB="$OUT_DIR/libMemoryDiagnostics.dylib"
SRC="$ROOT_DIR/Native~/macOS/MemoryDiagnostics.mm"

if ! command -v clang++ >/dev/null 2>&1; then
  echo "clang++ not found. Install Xcode command line tools." >&2
  exit 1
fi

mkdir -p "$OUT_DIR"
clang++ -dynamiclib -arch arm64 -arch x86_64 -o "$OUT_LIB" "$SRC" -install_name @rpath/libMemoryDiagnostics.dylib
echo "Built: $OUT_LIB"
