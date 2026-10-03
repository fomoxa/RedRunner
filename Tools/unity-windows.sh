#!/bin/bash
set -e

UNITY_VERSION="6000.5.7f1"
WIN_USER="admin"
PLAY_TIMEOUT=180

MODE="${1:-check}"
METHOD="$2"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$SCRIPT_DIR/.." && pwd)"
PACKAGE="$(cd "$REPO/../fomoxa-unity/com.fomoxa.unity" && pwd)"
WIN_ROOT="C:\\Users\\$WIN_USER\\redrunner-unity-check"
ROOT="/mnt/c/Users/$WIN_USER/redrunner-unity-check"
DST="$ROOT/RedRunner"
UNITY="/mnt/c/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity.exe"
LOG_DIR="$REPO/Logs"
LOG="$LOG_DIR/unity-windows-$MODE.log"

case "$MODE" in
  check) ;;
  run|play)
    if [ -z "$METHOD" ]; then
      echo "usage: $0 $MODE Namespace.Class.Method" >&2
      exit 2
    fi
    ;;
  *)
    echo "usage: $0 check | run Namespace.Class.Method | play Namespace.Class.Method" >&2
    exit 2
    ;;
esac

stop_check_editors() {
  powershell.exe -NoProfile -Command \
    "Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" | Where-Object { \$_.CommandLine -like '*redrunner-unity-check*' } | Invoke-CimMethod -MethodName Terminate" \
    >/dev/null 2>&1 || true
}

mkdir -p "$LOG_DIR" "$DST" "$ROOT/fomoxa-unity"
stop_check_editors
rm -rf "$DST/Temp"

rsync -a --delete \
  --exclude='.git' \
  --exclude='/Library' \
  --exclude='/Logs' \
  --exclude='/Temp' \
  --exclude='/UserSettings' \
  --exclude='/Assets/PortChecks' \
  --exclude='/Assets/PortChecks.meta' \
  "$REPO/" "$DST/"
rsync -a --delete "$PACKAGE/" "$ROOT/fomoxa-unity/com.fomoxa.unity/"
rm -rf "$DST/Assets/PortChecks"
mkdir -p "$DST/Assets/PortChecks"
rsync -a "$SCRIPT_DIR/PortChecks/" "$DST/Assets/PortChecks/"
rm -f "$DST/unity.log"

ARGS=(-batchmode -nographics -projectPath "$WIN_ROOT\\RedRunner" -logFile "$WIN_ROOT\\RedRunner\\unity.log")
case "$MODE" in
  check) ARGS+=(-quit) ;;
  run) ARGS+=(-quit -executeMethod "$METHOD") ;;
  play) ARGS+=(-executeMethod "$METHOD") ;;
esac

set +e
(
  cd "$DST"
  if [ "$MODE" = play ]; then
    timeout "$PLAY_TIMEOUT" "$UNITY" "${ARGS[@]}" < /dev/null > /dev/null 2>&1
  else
    "$UNITY" "${ARGS[@]}" < /dev/null > /dev/null 2>&1
  fi
)
EXIT_CODE=$?
set -e

if [ "$EXIT_CODE" -eq 124 ]; then
  stop_check_editors
fi

cp "$DST/unity.log" "$LOG" 2>/dev/null || true

rsync -rc --no-perms --no-times --chmod=D755,F644 \
  --exclude='/.git' \
  --exclude='/Library' \
  --exclude='/Logs' \
  --exclude='/Temp' \
  --exclude='/UserSettings' \
  --exclude='/Assets/PortChecks' \
  --exclude='/Assets/PortChecks.meta' \
  --exclude='/unity.log' \
  --exclude='*.csproj' \
  --exclude='*.sln' \
  --exclude='*.slnx' \
  "$DST/" "$REPO/"

echo "=== exit code: $EXIT_CODE ==="
echo "=== errors ==="
grep -n "error CS\|Aborting batchmode\|Fatal Error\|\[Fomoxa\].*\(error\|fail\)" "$LOG" || echo "none found"
if [ "$MODE" != check ]; then
  echo "=== result ==="
  grep -n "PORT-CHECK" "$LOG" || echo "no PORT-CHECK line"
fi
echo "=== full log: $LOG ==="
