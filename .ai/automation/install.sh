#!/bin/bash

set -euo pipefail

readonly REPO_ROOT="/Users/Boris/RiderProjects/COMMA Workspace 4.0"
readonly WORKER_PATH="${REPO_ROOT}/.ai/automation/worker.sh"
readonly LABEL="pl.comma.workspace4.codex-worker"
readonly PLIST_PATH="${HOME}/Library/LaunchAgents/${LABEL}.plist"
readonly STATE_DIR="${HOME}/Library/Application Support/COMMA AI/Workspace4"
readonly TASK_FILE="${REPO_ROOT}/.ai/task.md"
readonly DOMAIN="gui/${UID}"

if test "$(git -C "$REPO_ROOT" rev-parse --show-toplevel)" != "$REPO_ROOT"; then
    printf '%s\n' 'Repository root mismatch.' >&2
    exit 20
fi
if test "$(git -C "$REPO_ROOT" branch --show-current)" != "workspace-4.0"; then
    printf '%s\n' 'workspace-4.0 is not active.' >&2
    exit 20
fi
if ! command -v codex >/dev/null 2>&1; then
    printf '%s\n' 'Codex CLI is unavailable.' >&2
    exit 20
fi

codex_path="$(command -v codex)"
task_id="$(python3 - "$TASK_FILE" <<'PY'
import re
import sys

with open(sys.argv[1], encoding="utf-8") as handle:
    text = handle.read()
matches = re.findall(r"^-\s*TASK_ID:\s*(.*?)\s*$", text, flags=re.MULTILINE)
if len(matches) != 1 or not matches[0]:
    raise SystemExit("invalid TASK_ID")
print(matches[0])
PY
)"

mkdir -p "$STATE_DIR" "$(dirname "$PLIST_PATH")"
chmod 700 "$STATE_DIR"
printf '%s\n' "$task_id" > "${STATE_DIR}/handled-task-id"
chmod 600 "${STATE_DIR}/handled-task-id"
touch "${STATE_DIR}/worker.log" \
      "${STATE_DIR}/launchd.stdout.log" \
      "${STATE_DIR}/launchd.stderr.log"
chmod 600 "${STATE_DIR}/worker.log" \
          "${STATE_DIR}/launchd.stdout.log" \
          "${STATE_DIR}/launchd.stderr.log"
chmod 700 "$WORKER_PATH" \
          "${REPO_ROOT}/.ai/automation/install.sh" \
          "${REPO_ROOT}/.ai/automation/uninstall.sh"

plist_tmp="$(mktemp "${STATE_DIR}/launch-agent.XXXXXX")"
trap 'rm -f "$plist_tmp"' EXIT
cat > "$plist_tmp" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>${LABEL}</string>
    <key>ProgramArguments</key>
    <array>
        <string>/bin/bash</string>
        <string>${WORKER_PATH}</string>
    </array>
    <key>EnvironmentVariables</key>
    <dict>
        <key>CODEX_BIN</key>
        <string>${codex_path}</string>
        <key>PATH</key>
        <string>$(dirname "$codex_path"):/opt/homebrew/bin:/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin</string>
    </dict>
    <key>RunAtLoad</key>
    <true/>
    <key>StartInterval</key>
    <integer>60</integer>
    <key>ProcessType</key>
    <string>Background</string>
    <key>StandardOutPath</key>
    <string>${STATE_DIR}/launchd.stdout.log</string>
    <key>StandardErrorPath</key>
    <string>${STATE_DIR}/launchd.stderr.log</string>
</dict>
</plist>
EOF

plutil -lint "$plist_tmp" >/dev/null
cp "$plist_tmp" "$PLIST_PATH"
chmod 644 "$PLIST_PATH"

launchctl bootout "${DOMAIN}/${LABEL}" 2>/dev/null || true
launchctl enable "${DOMAIN}/${LABEL}"
launchctl bootstrap "$DOMAIN" "$PLIST_PATH"
launchctl kickstart -k "${DOMAIN}/${LABEL}"

printf 'Installed %s for TASK_ID %s.\n' "$LABEL" "$task_id"
launchctl print "${DOMAIN}/${LABEL}"
