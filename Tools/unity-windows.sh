#!/bin/bash
set -e

UNITY_VERSION="6000.5.7f1"
WIN_USER="admin"
PLAY_TIMEOUT=240
SERVER_PORT=7777

MODE="${1:-check}"
METHOD="$2"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$SCRIPT_DIR/.." && pwd)"
PACKAGE="$(cd "$REPO/../unity/com.fomoxa.networking" && pwd)"
PACKAGE_GIT_URL="https://github.com/fomoxa/com.fomoxa.networking.git"
PACKAGE_LOCAL_PATH="file:../../unity/com.fomoxa.networking"
RAPIER="$(cd "$REPO/../networking-rapier/com.fomoxa.networking.rapier" && pwd)"
RAPIER_GIT_URL="https://github.com/fomoxa/networking-rapier.git?path=com.fomoxa.networking.rapier"
RAPIER_LOCAL_PATH="file:../../networking-rapier/com.fomoxa.networking.rapier"
WIN_ROOT="C:\\Users\\$WIN_USER\\redrunner-unity-check"
ROOT="/mnt/c/Users/$WIN_USER/redrunner-unity-check"
DST="$ROOT/RedRunner"
UNITY="/mnt/c/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity.exe"
LOG_DIR="$REPO/Logs"
LOG="$LOG_DIR/unity-windows-$MODE.log"

case "$MODE" in
  check|shots) ;;
  run|play|play-server)
    if [ -z "$METHOD" ]; then
      echo "usage: $0 $MODE Namespace.Class.Method" >&2
      exit 2
    fi
    ;;
  *)
    echo "usage: $0 check | shots | run Namespace.Class.Method | play Namespace.Class.Method | play-server Namespace.Class.Method" >&2
    exit 2
    ;;
esac

stop_check_processes() {
  powershell.exe -NoProfile -Command \
    "Get-CimInstance Win32_Process | Where-Object { (\$_.Name -eq 'Unity.exe' -or \$_.Name -eq 'RedRunner.exe') -and \$_.CommandLine -like '*redrunner-unity-check*' } | Invoke-CimMethod -MethodName Terminate" \
    >/dev/null 2>&1 || true
}

start_server() {
  local SERVER_EXTRA=""
  for argument in ${SERVER_ARGS:-}; do
    SERVER_EXTRA="$SERVER_EXTRA,'$argument'"
  done
  local player="$DST/Builds/Windows/RedRunner.exe"
  if [ ! -f "$player" ]; then
    echo "no player build at $player; run: $0 run RedRunner.PortChecks.PortBuild.Player" >&2
    exit 2
  fi
  rm -f "$DST/server.log"
  powershell.exe -NoProfile -Command \
    "Start-Process -FilePath '$WIN_ROOT\\RedRunner\\Builds\\Windows\\RedRunner.exe' -ArgumentList '-batchmode','-nographics','-server','-port','$SERVER_PORT','-logFile','$WIN_ROOT\\RedRunner\\server.log'$SERVER_EXTRA" \
    >/dev/null 2>&1
  for _ in $(seq 1 60); do
    if grep -q "server listening" "$DST/server.log" 2>/dev/null; then
      return 0
    fi
    sleep 1
  done
  echo "the server did not start; see $DST/server.log" >&2
  stop_check_processes
  exit 1
}

run_player() {
  local arguments="$1"
  powershell.exe -NoProfile -Command \
    "Start-Process -FilePath '$WIN_ROOT\\RedRunner\\Builds\\Windows\\RedRunner.exe' -ArgumentList $arguments" \
    >/dev/null 2>&1
}

if [ "$MODE" = shots ]; then
  SHOTS="$REPO/docs/.gitbook/assets"
  mkdir -p "$SHOTS"
  stop_check_processes
  rm -f "$DST"/shot-*.png
  start_server
  run_player "'-batchmode','-screen-width','1280','-screen-height','720','-screenshot','$WIN_ROOT\\RedRunner\\shot-start-screen.png','-screenshot-after','6','-screenshot-quit','-logFile','$WIN_ROOT\\RedRunner\\shot-start.log'"
  run_player "'-batchmode','-nographics','-bot','127.0.0.1','-bot-seconds','20','-logFile','$WIN_ROOT\\RedRunner\\bot.log'"
  sleep 1
  run_player "'-batchmode','-screen-width','1280','-screen-height','720','-bot','127.0.0.1','-bot-seconds','20','-screenshot','$WIN_ROOT\\RedRunner\\shot-two-players.png','-screenshot-after','5','-logFile','$WIN_ROOT\\RedRunner\\shot-play.log'"
  run_player "'-batchmode','-screen-width','1280','-screen-height','720','-bot','127.0.0.1','-bot-seconds','20','-screenshot','$WIN_ROOT\\RedRunner\\shot-death.png','-screenshot-after','7.5','-logFile','$WIN_ROOT\\RedRunner\\shot-death.log'"
  sleep 25
  stop_check_processes
  for shot in "$DST"/shot-*.png; do
    [ -f "$shot" ] && cp "$shot" "$SHOTS/$(basename "$shot" | sed 's/^shot-//')"
  done
  ls -la "$SHOTS"
  exit 0
fi

mkdir -p "$LOG_DIR" "$DST" "$ROOT/unity"
stop_check_processes
rm -rf "$DST/Temp"

rsync -a --delete \
  --exclude='.git' \
  --exclude='/Library' \
  --exclude='/Logs' \
  --exclude='/Temp' \
  --exclude='/UserSettings' \
  --exclude='/Builds' \
  --exclude='/server.log' \
  --exclude='/bot.log' \
  --exclude='/unity*.log' \
  --exclude='/Assets/PortChecks' \
  --exclude='/Assets/PortChecks.meta' \
  "$REPO/" "$DST/"
rsync -a --delete "$PACKAGE/" "$ROOT/unity/com.fomoxa.networking/"
mkdir -p "$ROOT/networking-rapier"
rsync -a --delete "$RAPIER/" "$ROOT/networking-rapier/com.fomoxa.networking.rapier/"
rm -rf "$DST/Assets/PortChecks"
mkdir -p "$DST/Assets/PortChecks"
rsync -a "$SCRIPT_DIR/PortChecks/" "$DST/Assets/PortChecks/"
sed -i "s|\"$PACKAGE_GIT_URL\"|\"$PACKAGE_LOCAL_PATH\"|" "$DST/Packages/manifest.json"
sed -i "s|\"$RAPIER_GIT_URL\"|\"$RAPIER_LOCAL_PATH\"|" "$DST/Packages/manifest.json"
python3 -c 'import json, sys; path = sys.argv[1]; lock = json.load(open(path)); lock["dependencies"].pop("com.fomoxa.networking", None); lock["dependencies"].pop("com.fomoxa.networking.rapier", None); json.dump(lock, open(path, "w"), indent=2)' "$DST/Packages/packages-lock.json"
rm -f "$DST/unity-$MODE.log"

ARGS=(-batchmode -nographics -projectPath "$WIN_ROOT\\RedRunner" -logFile "$WIN_ROOT\\RedRunner\\unity-$MODE.log")
case "$MODE" in
  check) ARGS+=(-quit) ;;
  run) ARGS+=(-quit -executeMethod "$METHOD") ;;
  play|play-server) ARGS+=(-executeMethod "$METHOD") ;;
esac

if [ "$MODE" = play-server ]; then
  start_server
fi

set +e
(
  cd "$DST"
  if [ "$MODE" = play ] || [ "$MODE" = play-server ]; then
    timeout "$PLAY_TIMEOUT" "$UNITY" "${ARGS[@]}" < /dev/null > /dev/null 2>&1
  else
    "$UNITY" "${ARGS[@]}" < /dev/null > /dev/null 2>&1
  fi
)
EXIT_CODE=$?
set -e

if [ "$EXIT_CODE" -eq 124 ] || [ "$MODE" = play-server ]; then
  stop_check_processes
fi

if [ "$MODE" = play-server ]; then
  cp "$DST/server.log" "$LOG_DIR/unity-windows-server.log" 2>/dev/null || true
fi

cp "$DST/unity-$MODE.log" "$LOG" 2>/dev/null || true

rsync -rc --no-perms --no-times --chmod=D755,F644 \
  --exclude='/.git' \
  --exclude='/Library' \
  --exclude='/Logs' \
  --exclude='/Temp' \
  --exclude='/UserSettings' \
  --exclude='/Builds' \
  --exclude='/server.log' \
  --exclude='/port-dump.txt' \
  --exclude='/Packages/manifest.json' \
  --exclude='/Packages/packages-lock.json' \
  --exclude='/bot.log' \
  --exclude='/Assets/PortChecks' \
  --exclude='/Assets/PortChecks.meta' \
  --exclude='/unity*.log' \
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
if [ "$MODE" = play-server ]; then
  echo "=== server ==="
  grep -n "\[RedRunner\]\|Exception" "$LOG_DIR/unity-windows-server.log" | head -40 || echo "no server lines"
fi
echo "=== full log: $LOG ==="
