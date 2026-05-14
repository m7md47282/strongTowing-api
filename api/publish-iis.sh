#!/usr/bin/env bash
# Build the Strong Towing API for the Windows / IIS host (api.strongtowing.net).
#
# Usage:
#   ./publish-iis.sh                # default → ./publish-release
#   ./publish-iis.sh /custom/path   # override output folder
#
# Why this script exists:
#   The production server is Windows. `dotnet publish` defaults to the build
#   host RID, which on macOS bundles only `darwin-arm64/node` for Playwright
#   and breaks the `/api/quotes/pdf` endpoint when the bundle is copied to IIS.
#   Always publish through this script (or the IIS.pubxml profile) so the
#   Windows Playwright driver is included.

set -euo pipefail

cd "$(dirname "$0")"

OUT_DIR="${1:-./publish-release}"

echo "→ Cleaning $OUT_DIR"
rm -rf "$OUT_DIR"

echo "→ Publishing StrongTowing.API (win-x64, framework-dependent, Playwright=win)"
dotnet publish StrongTowing.API/StrongTowing.API.csproj \
  -c Release \
  -r win-x64 \
  --self-contained false \
  -p:PlaywrightPlatform=win \
  -o "$OUT_DIR"

NODE_EXE="$OUT_DIR/.playwright/node/win32_x64/node.exe"
if [[ ! -f "$NODE_EXE" ]]; then
  echo "✗ Expected Playwright Windows node binary not found: $NODE_EXE" >&2
  echo "  The bundle will fail on IIS. Check Microsoft.Playwright version + PlaywrightPlatform." >&2
  exit 1
fi

echo "✓ Published to $OUT_DIR"
echo "✓ Verified Playwright Windows driver present: $NODE_EXE"
echo
echo "Next steps:"
echo "  1. Copy '$OUT_DIR' to the IIS server (e.g. C:\\inetpub\\strongtowing-api)."
echo "  2. Recycle the App Pool."
echo "  3. First PDF call will download Chromium under {ContentRoot}\\.pw-browsers"
echo "     (requires outbound HTTPS + write access for the App Pool identity)."
