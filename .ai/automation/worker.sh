#!/bin/bash

set -euo pipefail

readonly REPO_ROOT="/Users/Boris/RiderProjects/COMMA Workspace 4.0"
readonly REQUIRED_BRANCH="workspace-4.0"
readonly STATE_DIR="${HOME}/Library/Application Support/COMMA AI/Workspace4"
readonly LOG_FILE="${STATE_DIR}/worker.log"
readonly HANDLED_TASK_FILE="${STATE_DIR}/handled-task-id"
readonly BLOCKED_MARKER_FILE="${STATE_DIR}/blocked-task.json"
readonly LOCK_DIR="${STATE_DIR}/worker.lock"
readonly TASK_FILE="${REPO_ROOT}/.ai/task.md"
readonly HANDOFF_FILE="${REPO_ROOT}/.ai/handoff.md"
readonly CODEX_BIN="${CODEX_BIN:-codex}"

log_message()
{
    printf '%s [%s] %s\n' \
        "$(date -u '+%Y-%m-%dT%H:%M:%SZ')" \
        "$1" \
        "$2"
}

read_task_field()
{
    python3 - "$1" "$2" <<'PY'
import re
import sys

path, key = sys.argv[1:]
with open(path, encoding="utf-8") as handle:
    text = handle.read()

matches = re.findall(
    rf"^-\s*{re.escape(key)}:\s*(.*?)\s*$",
    text,
    flags=re.MULTILINE,
)
if len(matches) != 1 or not matches[0]:
    raise SystemExit(f"expected exactly one non-empty {key} field")
print(matches[0])
PY
}

repo_is_clean()
{
    test -z "$(git -C "$1" status --porcelain --untracked-files=all)"
}

allowlist_accepts()
{
    python3 - "$1" "$2" <<'PY'
import json
import sys

allowlist = json.loads(sys.argv[1])
path = sys.argv[2]
if not isinstance(allowlist, list) or not all(isinstance(item, str) for item in allowlist):
    raise SystemExit(2)

allowed = any(
    path.startswith(item) if item.endswith("/") else path == item
    for item in allowlist
)
raise SystemExit(0 if allowed else 1)
PY
}

path_is_blocked()
{
    python3 - "$1" <<'PY'
import pathlib
import sys

path = sys.argv[1]
parts = [part.lower() for part in pathlib.PurePosixPath(path).parts]
base = parts[-1] if parts else ""
blocked_parts = {".codex", "output", "bin", "obj"}
blocked_names = {
    "auth.json",
    "id_rsa",
    "id_ed25519",
    "credentials.json",
}
blocked_suffixes = {
    ".key", ".pem", ".p12", ".pfx", ".crt", ".cer", ".der",
    ".jks", ".keystore",
}
blocked = (
    any(part in blocked_parts for part in parts)
    or base in blocked_names
    or base == ".env"
    or base.startswith(".env.")
    or pathlib.PurePosixPath(base).suffix.lower() in blocked_suffixes
)
raise SystemExit(0 if blocked else 1)
PY
}

task_was_handled()
{
    local task_id="$1"
    local state_file="${2:-$HANDLED_TASK_FILE}"
    test -f "$state_file" || return 1
    test "$(sed -n '1p' "$state_file")" = "$task_id"
}

acquire_lock()
{
    if mkdir "$LOCK_DIR" 2>/dev/null; then
        printf '%s\n' "$$" > "${LOCK_DIR}/pid"
        return 0
    fi

    local existing_pid=""
    if test -f "${LOCK_DIR}/pid"; then
        existing_pid="$(sed -n '1p' "${LOCK_DIR}/pid")"
    fi

    if test -n "$existing_pid" && kill -0 "$existing_pid" 2>/dev/null; then
        log_message INFO "another worker process is active; skipping this cycle"
        return 1
    fi

    rm -f "${LOCK_DIR}/pid"
    rmdir "$LOCK_DIR" 2>/dev/null || true
    if ! mkdir "$LOCK_DIR" 2>/dev/null; then
        log_message INFO "worker lock was acquired by another process; skipping this cycle"
        return 1
    fi
    printf '%s\n' "$$" > "${LOCK_DIR}/pid"
}

release_lock()
{
    rm -f "${LOCK_DIR}/pid"
    rmdir "$LOCK_DIR" 2>/dev/null || true
}

validate_repo_changes()
{
    local output_file="$1"
    local trusted_task_file="$2"
    local repo_root="${3:-$REPO_ROOT}"
    python3 - "$trusted_task_file" "$repo_root" "$output_file" <<'PY'
import json
import pathlib
import re
import subprocess
import sys

task_file, repo_root, output_file = sys.argv[1:]
with open(task_file, encoding="utf-8") as handle:
    task_text = handle.read()

matches = re.findall(
    r"^-\s*ALLOWED_PATHS_JSON:\s*(.*?)\s*$",
    task_text,
    flags=re.MULTILINE,
)
if len(matches) != 1:
    raise SystemExit("invalid ALLOWED_PATHS_JSON field")

allowlist = json.loads(matches[0])
if (
    not isinstance(allowlist, list)
    or not allowlist
    or not all(isinstance(item, str) and item for item in allowlist)
):
    raise SystemExit("allowlist must be a non-empty JSON string array")

def git_paths(*arguments):
    data = subprocess.check_output(
        ["git", "-C", repo_root, *arguments],
    )
    return {
        item.decode("utf-8", "surrogateescape")
        for item in data.split(b"\0")
        if item
    }

unmerged = git_paths("diff", "--name-only", "-z", "--diff-filter=U")
if unmerged:
    raise SystemExit("unmerged paths detected")

paths = git_paths("diff", "--no-renames", "--name-only", "-z", "HEAD")
paths.update(git_paths("ls-files", "--others", "--exclude-standard", "-z"))
if not paths:
    raise SystemExit("task produced no repository changes")

blocked_parts = {".codex", "output", "bin", "obj"}
blocked_names = {
    "auth.json",
    "id_rsa",
    "id_ed25519",
    "credentials.json",
}
blocked_suffixes = {
    ".key", ".pem", ".p12", ".pfx", ".crt", ".cer", ".der",
    ".jks", ".keystore",
}

def is_blocked(path):
    pure_path = pathlib.PurePosixPath(path)
    parts = [part.lower() for part in pure_path.parts]
    base = parts[-1] if parts else ""
    return (
        any(part in blocked_parts for part in parts)
        or base in blocked_names
        or base == ".env"
        or base.startswith(".env.")
        or pathlib.PurePosixPath(base).suffix.lower() in blocked_suffixes
    )

def is_allowed(path):
    return any(
        path.startswith(item) if item.endswith("/") else path == item
        for item in allowlist
    )

for path in sorted(paths):
    if pathlib.PurePosixPath(path).is_absolute() or ".." in pathlib.PurePosixPath(path).parts:
        raise SystemExit(f"unsafe path: {path!r}")
    if is_blocked(path):
        raise SystemExit(f"blocked secret or local artifact path: {path}")
    if not is_allowed(path):
        raise SystemExit(f"path outside ALLOWED_PATHS_JSON: {path}")

with open(output_file, "w", encoding="utf-8") as handle:
    json.dump(sorted(paths), handle)
PY
}

change_fingerprint()
{
    python3 - "$1" <<'PY'
import hashlib
import os
import subprocess
import sys

repo_root = sys.argv[1]

def git_bytes(*arguments):
    return subprocess.check_output(["git", "-C", repo_root, *arguments])

digest = hashlib.sha256()
for label, arguments in (
    (b"status", ("status", "--porcelain=v1", "-z", "--untracked-files=all")),
    (b"diff", ("diff", "--binary", "--no-ext-diff", "HEAD")),
):
    payload = git_bytes(*arguments)
    digest.update(label + b"\0" + len(payload).to_bytes(8, "big") + payload)

untracked = git_bytes("ls-files", "--others", "--exclude-standard", "-z").split(b"\0")
for raw_path in sorted(path for path in untracked if path):
    path = raw_path.decode("utf-8", "surrogateescape")
    full_path = os.path.join(repo_root, path)
    if os.path.islink(full_path):
        payload = os.fsencode(os.readlink(full_path))
        kind = b"symlink"
    else:
        with open(full_path, "rb") as handle:
            payload = handle.read()
        kind = b"file"
    digest.update(b"untracked\0" + kind + b"\0" + raw_path + b"\0")
    digest.update(hashlib.sha256(payload).digest())

print(digest.hexdigest())
PY
}

write_blocked_marker()
{
    local task_id="$1"
    local head="$2"
    local fingerprint="$3"
    local resume_used="$4"
    local marker_tmp
    marker_tmp="$(mktemp "${STATE_DIR}/blocked-task.XXXXXX")"
    python3 - "$marker_tmp" "$task_id" "$REQUIRED_BRANCH" "$head" "$fingerprint" "$resume_used" <<'PY'
import json
import sys

path, task_id, branch, head, fingerprint, resume_used = sys.argv[1:]
data = {
    "version": 1,
    "task_id": task_id,
    "branch": branch,
    "head": head,
    "fingerprint": fingerprint,
    "resume_used": resume_used == "YES",
}
with open(path, "w", encoding="utf-8") as handle:
    json.dump(data, handle, sort_keys=True, separators=(",", ":"))
    handle.write("\n")
PY
    chmod 600 "$marker_tmp"
    mv "$marker_tmp" "$BLOCKED_MARKER_FILE"
}

blocked_marker_matches()
{
    local marker_file="$1"
    local task_id="$2"
    local head="$3"
    local fingerprint="$4"
    python3 - "$marker_file" "$task_id" "$REQUIRED_BRANCH" "$head" "$fingerprint" <<'PY'
import json
import sys

path, task_id, branch, head, fingerprint = sys.argv[1:]
try:
    with open(path, encoding="utf-8") as handle:
        data = json.load(handle)
except (OSError, ValueError):
    raise SystemExit(1)

expected = {
    "version": 1,
    "task_id": task_id,
    "branch": branch,
    "head": head,
    "fingerprint": fingerprint,
}
if not isinstance(data, dict) or any(data.get(key) != value for key, value in expected.items()):
    raise SystemExit(1)
if set(data) != {*expected, "resume_used"} or not isinstance(data["resume_used"], bool):
    raise SystemExit(1)
PY
}

blocked_marker_resume_unused()
{
    python3 - "$1" <<'PY'
import json
import sys

with open(sys.argv[1], encoding="utf-8") as handle:
    data = json.load(handle)
raise SystemExit(0 if data.get("resume_used") is False else 1)
PY
}

task_file_matches_head()
{
    git -C "${1:-$REPO_ROOT}" diff --quiet HEAD -- "${2:-.ai/task.md}"
}

stage_validated_paths()
{
    python3 - "$REPO_ROOT" "$1" <<'PY'
import json
import subprocess
import sys

repo_root, paths_file = sys.argv[1:]
with open(paths_file, encoding="utf-8") as handle:
    paths = json.load(handle)
subprocess.run(
    ["git", "-C", repo_root, "add", "-A", "--", *paths],
    check=True,
)
PY
}

run_self_tests()
{
    local test_root
    test_root="$(mktemp -d "${TMPDIR:-/tmp}/comma-worker-tests.XXXXXX")"
    trap "rm -rf -- '$test_root'" EXIT

    local task_fixture="${test_root}/task.md"
    printf '%s\n' \
        '- TASK_ID: TEST-001' \
        '- STATUS: READY' \
        '- ALLOWED_PATHS_JSON: [".ai/automation/", ".ai/report.md"]' \
        > "$task_fixture"

    test "$(read_task_field "$task_fixture" TASK_ID)" = "TEST-001"
    test "$(read_task_field "$task_fixture" STATUS)" = "READY"
    allowlist_accepts '[".ai/automation/", ".ai/report.md"]' '.ai/automation/worker.sh'
    allowlist_accepts '[".ai/automation/", ".ai/report.md"]' '.ai/report.md'
    if allowlist_accepts '[".ai/automation/", ".ai/report.md"]' 'COMMA.App/App.axaml'; then
        return 1
    fi
    path_is_blocked '.codex/auth.json'
    path_is_blocked 'output/result.pdf'
    if path_is_blocked '.ai/report.md'; then
        return 1
    fi

    local handled_fixture="${test_root}/handled-task-id"
    printf '%s\n' 'TEST-001' > "$handled_fixture"
    task_was_handled 'TEST-001' "$handled_fixture"
    if task_was_handled 'TEST-002' "$handled_fixture"; then
        return 1
    fi

    local marker_fixture="${test_root}/blocked-task.json"
    python3 - "$marker_fixture" <<'PY'
import json
import sys
json.dump({
    "version": 1,
    "task_id": "TEST-001",
    "branch": "workspace-4.0",
    "head": "a" * 40,
    "fingerprint": "b" * 64,
    "resume_used": False,
}, open(sys.argv[1], "w", encoding="utf-8"), sort_keys=True)
PY
    blocked_marker_matches "$marker_fixture" 'TEST-001' "$(printf 'a%.0s' {1..40})" "$(printf 'b%.0s' {1..64})"
    blocked_marker_resume_unused "$marker_fixture"
    if blocked_marker_matches "$marker_fixture" 'TEST-002' "$(printf 'a%.0s' {1..40})" "$(printf 'b%.0s' {1..64})"; then
        return 1
    fi
    if blocked_marker_matches "$marker_fixture" 'TEST-001' "$(printf 'c%.0s' {1..40})" "$(printf 'b%.0s' {1..64})"; then
        return 1
    fi
    if blocked_marker_matches "$marker_fixture" 'TEST-001' "$(printf 'a%.0s' {1..40})" "$(printf 'c%.0s' {1..64})"; then
        return 1
    fi

    local dirty_repo="${test_root}/dirty-repo"
    mkdir -p "$dirty_repo"
    git -C "$dirty_repo" init -q
    printf '%s\n' 'baseline' > "${dirty_repo}/fixture.txt"
    git -C "$dirty_repo" add fixture.txt
    git -C "$dirty_repo" \
        -c user.name='COMMA Worker Test' \
        -c user.email='worker-test@invalid' \
        commit -q -m baseline
    repo_is_clean "$dirty_repo"
    printf '%s\n' 'dirty' >> "${dirty_repo}/fixture.txt"
    if repo_is_clean "$dirty_repo"; then
        return 1
    fi

    local recovery_repo="${test_root}/recovery-repo"
    mkdir -p "${recovery_repo}/.ai"
    git -C "$recovery_repo" init -q
    printf '%s\n' \
        '- TASK_ID: TEST-001' \
        '- STATUS: READY' \
        '- ALLOWED_PATHS_JSON: [".ai/report.md"]' \
        > "${recovery_repo}/.ai/task.md"
    printf '%s\n' 'baseline' > "${recovery_repo}/.ai/report.md"
    git -C "$recovery_repo" add .ai/task.md .ai/report.md
    git -C "$recovery_repo" \
        -c user.name='COMMA Worker Test' \
        -c user.email='worker-test@invalid' \
        commit -q -m baseline
    local trusted_task="${test_root}/trusted-task.md"
    git -C "$recovery_repo" show HEAD:.ai/task.md > "$trusted_task"
    printf '%s\n' \
        '- TASK_ID: TEST-001' \
        '- STATUS: READY' \
        '- ALLOWED_PATHS_JSON: [".ai/report.md", "COMMA.App/"]' \
        > "${recovery_repo}/.ai/task.md"
    mkdir -p "${recovery_repo}/COMMA.App"
    printf '%s\n' 'forbidden' > "${recovery_repo}/COMMA.App/forbidden.txt"
    if task_file_matches_head "$recovery_repo" .ai/task.md; then
        return 1
    fi
    if validate_repo_changes "${test_root}/paths.json" "$trusted_task" "$recovery_repo" 2>/dev/null; then
        return 1
    fi

    printf '%s\n' 'SELF_TEST_PASS parsing allowlist blocked-path repeat-task dirty-repo blocked-marker trusted-allowlist'
}

run_cycle()
{
    local resume_blocked="${1:-NO}"
    if test "$(git -C "$REPO_ROOT" rev-parse --show-toplevel)" != "$REPO_ROOT"; then
        log_message ERROR "repository root mismatch"
        return 20
    fi
    if test "$(git -C "$REPO_ROOT" branch --show-current)" != "$REQUIRED_BRANCH"; then
        log_message ERROR "required branch is not active"
        return 20
    fi

    local task_id
    local task_status
    local current_head
    local current_fingerprint
    if ! task_id="$(read_task_field "$TASK_FILE" TASK_ID)" || \
       ! task_status="$(read_task_field "$TASK_FILE" STATUS)"; then
        log_message ERROR "task metadata is invalid"
        return 40
    fi
    if ! [[ "$task_id" =~ ^[A-Za-z0-9._-]+$ ]]; then
        log_message ERROR "TASK_ID contains unsupported characters"
        return 40
    fi
    if ! repo_is_clean "$REPO_ROOT"; then
        current_head="$(git -C "$REPO_ROOT" rev-parse HEAD)"
        current_fingerprint="$(change_fingerprint "$REPO_ROOT")"
        if blocked_marker_matches "$BLOCKED_MARKER_FILE" "$task_id" \
            "$current_head" "$current_fingerprint"; then
            if test "$resume_blocked" = "NO"; then
                log_message INFO "blocked task preserved; waiting for approved recovery"
                return 0
            fi
            if ! blocked_marker_resume_unused "$BLOCKED_MARKER_FILE"; then
                log_message ERROR "blocked recovery was already used; manual review required"
                return 20
            fi
            if ! task_file_matches_head; then
                log_message ERROR "blocked recovery rejected: local task differs from HEAD"
                return 40
            fi
        else
            log_message ERROR "repository is dirty; no action taken"
            return 20
        fi
    elif test "$resume_blocked" = "YES"; then
        log_message ERROR "blocked recovery requires an exactly matching preserved state"
        return 20
    fi

    if test "$resume_blocked" = "NO"; then
        if ! git -C "$REPO_ROOT" fetch origin "$REQUIRED_BRANCH"; then
            log_message ERROR "git fetch failed"
            return 50
        fi

        local local_head
        local remote_head
        local_head="$(git -C "$REPO_ROOT" rev-parse HEAD)"
        remote_head="$(git -C "$REPO_ROOT" rev-parse "origin/${REQUIRED_BRANCH}")"
        if test "$local_head" != "$remote_head"; then
            if git -C "$REPO_ROOT" merge-base --is-ancestor HEAD "origin/${REQUIRED_BRANCH}"; then
                if ! git -C "$REPO_ROOT" merge --ff-only "origin/${REQUIRED_BRANCH}"; then
                    log_message ERROR "safe fast-forward failed"
                    return 50
                fi
            elif git -C "$REPO_ROOT" merge-base --is-ancestor \
                "origin/${REQUIRED_BRANCH}" HEAD; then
                local ahead_task_id=""
                if ahead_task_id="$(read_task_field "$TASK_FILE" TASK_ID)" && \
                   task_was_handled "$ahead_task_id"; then
                    log_message INFO \
                        "local branch is ahead, but TASK_ID ${ahead_task_id} is already handled; nothing to do"
                    return 0
                fi
                log_message ERROR "local branch is ahead with an unhandled task"
                return 20
            else
                log_message ERROR "local and remote branches are divergent"
                return 20
            fi
        fi

        if ! repo_is_clean "$REPO_ROOT"; then
            log_message ERROR "repository became dirty after fast-forward"
            return 20
        fi
        if ! task_id="$(read_task_field "$TASK_FILE" TASK_ID)" || \
           ! task_status="$(read_task_field "$TASK_FILE" STATUS)"; then
            log_message ERROR "task metadata is invalid after fast-forward"
            return 40
        fi
    fi

    local trusted_task
    trusted_task="$(mktemp "${STATE_DIR}/trusted-task.XXXXXX")"
    if ! git -C "$REPO_ROOT" show HEAD:.ai/task.md > "$trusted_task"; then
        rm -f "$trusted_task"
        log_message ERROR "could not read trusted task metadata from HEAD"
        return 40
    fi
    if ! task_id="$(read_task_field "$trusted_task" TASK_ID)" || \
       ! task_status="$(read_task_field "$trusted_task" STATUS)" || \
       ! [[ "$task_id" =~ ^[A-Za-z0-9._-]+$ ]]; then
        rm -f "$trusted_task"
        log_message ERROR "trusted task metadata is invalid"
        return 40
    fi
    if test "$task_status" != "READY"; then
        rm -f "$trusted_task"
        log_message INFO "task status is not READY; nothing to do"
        return 0
    fi
    if task_was_handled "$task_id"; then
        rm -f "$trusted_task"
        log_message INFO "TASK_ID ${task_id} was already handled; nothing to do"
        return 0
    fi

    if test "$resume_blocked" = "YES"; then
        local preflight_paths
        preflight_paths="$(mktemp "${STATE_DIR}/validated-paths.XXXXXX")"
        if ! validate_repo_changes "$preflight_paths" "$trusted_task"; then
            rm -f "$preflight_paths" "$trusted_task"
            log_message ERROR "blocked recovery rejected: preserved changes failed validation"
            return 40
        fi
        rm -f "$preflight_paths"
        write_blocked_marker "$task_id" "$(git -C "$REPO_ROOT" rev-parse HEAD)" \
            "$(change_fingerprint "$REPO_ROOT")" YES
    fi

    local attempt_head
    attempt_head="$(git -C "$REPO_ROOT" rev-parse HEAD)"
    local prompt
    prompt="$(printf '%s\n' \
        'Pracuj wyłącznie w repozytorium /Users/Boris/RiderProjects/COMMA Workspace 4.0 na gałęzi workspace-4.0.' \
        'Przeczytaj w całości AGENTS.md, .ai/context.md i .ai/task.md.' \
        "Wykonaj zadanie ${task_id}, jego testy oraz zaktualizuj .ai/report.md i .ai/handoff.md." \
        'Modyfikuj wyłącznie ścieżki dozwolone przez ALLOWED_PATHS_JSON.' \
        'Nie odczytuj ani nie ujawniaj sekretów. Nie używaj trybu danger-full-access, YOLO, resetu, rebase ani force push.' \
        'Nie wykonuj git commit ani git push; bezpieczny worker zrobi to po walidacji.')"

    log_message INFO "starting Codex for TASK_ID ${task_id}"
    if ! "$CODEX_BIN" \
        -a never \
        -s workspace-write \
        exec \
        -C "$REPO_ROOT" \
        --ephemeral \
        "$prompt"; then
        rm -f "$trusted_task"
        log_message ERROR "Codex execution failed for TASK_ID ${task_id}"
        return 30
    fi

    if test "$(git -C "$REPO_ROOT" branch --show-current)" != "$REQUIRED_BRANCH" || \
       test "$(git -C "$REPO_ROOT" rev-parse HEAD)" != "$attempt_head"; then
        rm -f "$trusted_task"
        log_message ERROR "branch or HEAD changed during Codex execution"
        return 40
    fi

    local handoff_status
    if ! handoff_status="$(read_task_field "$HANDOFF_FILE" STATUS)"; then
        rm -f "$trusted_task"
        log_message ERROR "handoff metadata is invalid"
        return 40
    fi
    if test "$handoff_status" = "BLOCKED"; then
        local blocked_paths
        blocked_paths="$(mktemp "${STATE_DIR}/validated-paths.XXXXXX")"
        if ! validate_repo_changes "$blocked_paths" "$trusted_task"; then
            rm -f "$blocked_paths" "$trusted_task"
            log_message ERROR "blocked task changes failed validation"
            return 40
        fi
        rm -f "$blocked_paths"
        local resume_used="NO"
        if test "$resume_blocked" = "YES"; then
            resume_used="YES"
        fi
        write_blocked_marker "$task_id" "$attempt_head" \
            "$(change_fingerprint "$REPO_ROOT")" "$resume_used"
        rm -f "$trusted_task"
        log_message INFO "blocked task preserved; waiting for approved recovery"
        return 0
    fi

    local auto_commit_push
    local commit_message
    if ! auto_commit_push="$(read_task_field "$trusted_task" AUTO_COMMIT_PUSH)" || \
       ! commit_message="$(read_task_field "$trusted_task" COMMIT_MESSAGE)"; then
        rm -f "$trusted_task"
        log_message ERROR "completion metadata is invalid"
        return 40
    fi
    if test "$auto_commit_push" != "YES"; then
        rm -f "$trusted_task"
        log_message ERROR "AUTO_COMMIT_PUSH is not YES"
        return 40
    fi
    if test "$handoff_status" != "COMPLETED"; then
        rm -f "$trusted_task"
        log_message ERROR "handoff status is not COMPLETED"
        return 40
    fi

    local paths_file
    paths_file="$(mktemp "${STATE_DIR}/validated-paths.XXXXXX")"
    if ! validate_repo_changes "$paths_file" "$trusted_task"; then
        rm -f "$paths_file" "$trusted_task"
        log_message ERROR "repository change validation failed"
        return 40
    fi
    if ! stage_validated_paths "$paths_file"; then
        rm -f "$paths_file" "$trusted_task"
        log_message ERROR "could not stage validated paths"
        return 40
    fi
    rm -f "$paths_file" "$trusted_task"

    if git -C "$REPO_ROOT" diff --cached --quiet; then
        log_message ERROR "no validated changes are staged"
        return 40
    fi
    if ! git -C "$REPO_ROOT" commit -m "$commit_message"; then
        log_message ERROR "commit failed; state preserved for manual review"
        return 50
    fi
    if ! git -C "$REPO_ROOT" push origin \
        "${REQUIRED_BRANCH}:${REQUIRED_BRANCH}"; then
        log_message ERROR "push rejected or failed; no force push attempted"
        return 50
    fi

    local handled_tmp
    handled_tmp="$(mktemp "${STATE_DIR}/handled-task-id.XXXXXX")"
    printf '%s\n' "$task_id" > "$handled_tmp"
    chmod 600 "$handled_tmp"
    mv "$handled_tmp" "$HANDLED_TASK_FILE"
    rm -f "$BLOCKED_MARKER_FILE"
    log_message INFO "TASK_ID ${task_id} committed, pushed, and marked handled"
}

if test "${1:-}" = "--self-test"; then
    run_self_tests
    exit 0
fi

if test -n "${1:-}" && test "${1:-}" != "--resume-blocked"; then
    printf '%s\n' 'Usage: worker.sh [--resume-blocked|--self-test]' >&2
    exit 40
fi

mkdir -p "$STATE_DIR"
chmod 700 "$STATE_DIR"
touch "$LOG_FILE"
chmod 600 "$LOG_FILE"
exec >> "$LOG_FILE" 2>&1

log_message INFO "worker cycle started"
if ! acquire_lock; then
    exit 0
fi
trap release_lock EXIT INT TERM

if run_cycle "$(test "${1:-}" = "--resume-blocked" && printf YES || printf NO)"; then
    cycle_status=0
else
    cycle_status=$?
fi
log_message INFO "worker cycle finished with exit code ${cycle_status}"
exit "$cycle_status"
