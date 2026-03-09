#!/usr/bin/env bash
# register-pecmd.sh
# Registers the .pecmd file extension for the current user on Linux and macOS.
# Usage: ./register-pecmd.sh [--console-path <path>] [--unregister]

set -euo pipefail

CONSOLE_PATH=""
UNREGISTER=0

while [[ $# -gt 0 ]]; do
    case $1 in
        --console-path) CONSOLE_PATH="$2"; shift 2 ;;
        --unregister)   UNREGISTER=1; shift ;;
        *) echo "Unknown argument: $1"; exit 1 ;;
    esac
done

PLATFORM="$(uname -s)"
WRAPPER="$HOME/.local/bin/pecmd"
DESKTOP_FILE="$HOME/.local/share/applications/payrollconsole.desktop"
MIME_FILE="$HOME/.local/share/mime/packages/application-x-pecmd.xml"

# ── Unregister ────────────────────────────────────────────────────────────────
if [[ $UNREGISTER -eq 1 ]]; then
    echo "Removing .pecmd registration..."
    rm -f "$WRAPPER" "$DESKTOP_FILE" "$MIME_FILE"
    if [[ "$PLATFORM" == "Linux" ]]; then
        update-mime-database "$HOME/.local/share/mime" 2>/dev/null || true
        update-desktop-database "$HOME/.local/share/applications" 2>/dev/null || true
    fi
    echo "Done."
    exit 0
fi

# ── Resolve PayrollConsole path ───────────────────────────────────────────────
if [[ -z "$CONSOLE_PATH" ]]; then
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    if [[ -x "$SCRIPT_DIR/PayrollEngine.PayrollConsole" ]]; then
        CONSOLE_PATH="$SCRIPT_DIR/PayrollEngine.PayrollConsole"
    elif command -v PayrollEngine.PayrollConsole &>/dev/null; then
        CONSOLE_PATH="$(command -v PayrollEngine.PayrollConsole)"
    else
        echo "Error: PayrollConsole not found. Use --console-path to specify the location." >&2
        exit 1
    fi
fi

CONSOLE_PATH="$(realpath "$CONSOLE_PATH")"
echo "Using PayrollConsole: $CONSOLE_PATH"

# ── Shell wrapper ($HOME/.local/bin/pecmd) ────────────────────────────────────
mkdir -p "$HOME/.local/bin"
cat > "$WRAPPER" <<EOF
#!/usr/bin/env bash
exec "$CONSOLE_PATH" "\$@"
EOF
chmod +x "$WRAPPER"
echo "Wrapper created: $WRAPPER"

# ── Linux: MIME + desktop integration ────────────────────────────────────────
if [[ "$PLATFORM" == "Linux" ]]; then
    mkdir -p "$HOME/.local/share/mime/packages"
    cat > "$MIME_FILE" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<mime-info xmlns="http://www.freedesktop.org/standards/shared-mime-info">
  <mime-type type="application/x-pecmd">
    <comment>Payroll Engine Command File</comment>
    <glob pattern="*.pecmd"/>
  </mime-type>
</mime-info>
EOF
    update-mime-database "$HOME/.local/share/mime" 2>/dev/null || true

    mkdir -p "$HOME/.local/share/applications"
    cat > "$DESKTOP_FILE" <<EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=Payroll Console
Comment=Payroll Engine Command Runner
Exec=$CONSOLE_PATH %f
MimeType=application/x-pecmd;
NoDisplay=true
EOF
    update-desktop-database "$HOME/.local/share/applications" 2>/dev/null || true
    xdg-mime default payrollconsole.desktop application/x-pecmd 2>/dev/null || true
    echo "MIME and desktop integration registered."
fi

# ── macOS: note on double-click support ──────────────────────────────────────
if [[ "$PLATFORM" == "Darwin" ]]; then
    echo ""
    echo "macOS: Shell wrapper registered. Terminal usage (./file.pecmd or pecmd file.pecmd) works now."
    echo "For double-click support, place PayrollConsole.app in /Applications and associate"
    echo "via: duti -s com.payrollengine.console .pecmd all"
fi

echo ""
echo "Registration complete."
echo "To undo: ./register-pecmd.sh --unregister"
