#!/usr/bin/env bash
set -euo pipefail

# Build the project and emit the plugin as Jikomenuv1.dll in ./dist.
root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
out_dir="$root_dir/dist"

mkdir -p "$out_dir"

dotnet build "$root_dir/StupidTemplate.csproj" -c Release

# Copy the compiled plugin to a predictable location and name.
# The project AssemblyName controls the produced DLL name.
built_dll=$(find "$root_dir/bin/Release" -maxdepth 3 -name 'Jikomenuv1.dll' | head -n 1)
if [[ -z "$built_dll" ]]; then
  echo "Jikomenuv1.dll not found after build. Check build output above." >&2
  exit 1
fi

cp "$built_dll" "$out_dir/Jikomenuv1.dll"
echo "Copied plugin to $out_dir/Jikomenuv1.dll"
