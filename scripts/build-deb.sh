#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
VERSION="0.1.0"
ARCH="amd64"
PKG_NAME="agentkit-totp"
PKG_DIR="$REPO_ROOT/build/${PKG_NAME}_${VERSION}_${ARCH}"

echo "==> Publishing NativeAOT binary..."
dotnet publish "$REPO_ROOT/src/AgentKit.Totp" -r linux-x64 -c Release -o "$REPO_ROOT/build/publish"

echo "==> Preparing Debian package structure..."
rm -rf "$PKG_DIR"
mkdir -p "$PKG_DIR/DEBIAN"
mkdir -p "$PKG_DIR/usr/local/bin"

cp "$REPO_ROOT/debian/control" "$PKG_DIR/DEBIAN/control"
cp "$REPO_ROOT/build/publish/AgentKit.Totp" "$PKG_DIR/usr/local/bin/totp"
chmod 0755 "$PKG_DIR/usr/local/bin/totp"

echo "==> Building .deb package..."
dpkg-deb --build "$PKG_DIR"

DEB_FILE="$REPO_ROOT/build/${PKG_NAME}_${VERSION}_${ARCH}.deb"
echo "==> Package built: $DEB_FILE"
echo "    Size: $(du -h "$DEB_FILE" | cut -f1)"
