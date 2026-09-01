#!/bin/bash

set -euo pipefail

readonly LABEL="pl.comma.workspace4.codex-worker"
readonly DOMAIN="gui/${UID}"
readonly PLIST_PATH="${HOME}/Library/LaunchAgents/${LABEL}.plist"

launchctl bootout "${DOMAIN}/${LABEL}" 2>/dev/null || true
launchctl disable "${DOMAIN}/${LABEL}"

printf '%s\n' \
    "Stopped and disabled ${LABEL}." \
    "The plist remains at ${PLIST_PATH}." \
    'Repository code and worker state were not removed.'
