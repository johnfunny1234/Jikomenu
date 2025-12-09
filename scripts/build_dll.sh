#!/usr/bin/env bash
set -euo pipefail

# Build the project and emit the plugin as Jikomenuv1.dll in ./dist.
root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
out_dir="$root_dir/dist"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet CLI is not installed or not on PATH. Please install it and retry." >&2
  exit 1
fi

mkdir -p "$out_dir"

build_args=("$root_dir/StupidTemplate.csproj" -c Release "/p:OutputPath=$out_dir/")
if [[ -n "${GAME_PATH:-}" ]]; then
  build_args+=("/p:GamePath=$GAME_PATH")
fi

dotnet build "${build_args[@]}"

built_dll="$out_dir/Jikomenuv1.dll"
if [[ ! -f "$built_dll" ]]; then
  echo "Jikomenuv1.dll not found in $out_dir after build. Check build output above." >&2
  exit 1
fi

echo "Built plugin at $built_dll"
