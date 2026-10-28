#!/usr/bin/env bash
# Keeps the dev machine's screen on while UI checks run (Cinnamon/X11): start [hours] | stop | status.
# Saves the current idle settings, disables idle lock/blanking, and restores them on stop or after the time cap.
set -u
state="${XDG_RUNTIME_DIR:-/tmp}/aoceditor-awake.state"
export DISPLAY="${DISPLAY:-:0}"
keys=("org.cinnamon.desktop.session idle-delay" "org.cinnamon.settings-daemon.plugins.power sleep-display-ac" "org.cinnamon.settings-daemon.plugins.power sleep-display-battery")

stop() {
  [ -f "$state" ] || { echo "awake: not active"; return 0; }
  while IFS="|" read -r kind key value; do
    case "$kind" in
      gs) gsettings set ${key} "${value}" ;;
      pid) kill "$value" 2>/dev/null ;;
    esac
  done <"$state"
  xset s default +dpms 2>/dev/null
  rm -f "$state"
  echo "awake: settings restored"
}

case "${1:-status}" in
  start)
    [ -f "$state" ] && { echo "awake: already active"; exit 0; }
    hours="${2:-2}"
    for k in "${keys[@]}"; do
      v=$(gsettings get $k 2>/dev/null) || continue
      echo "gs|$k|$v" >>"$state"
      gsettings set $k 0
    done
    xset s off s noblank -dpms 2>/dev/null
    # shortcut: watchdog is a plain sleep, so a reboot cancels it; the settings are then restored by dconf defaults only if the user resets them
    (sleep $((hours * 3600)); "$0" stop) >/dev/null 2>&1 &
    echo "pid|watchdog|$!" >>"$state"
    echo "awake: screen kept on for up to ${hours}h; run '$0 stop' when done"
    ;;
  stop) stop ;;
  status) [ -f "$state" ] && echo "awake: active" || echo "awake: not active" ;;
  *) echo "usage: $0 start [hours] | stop | status"; exit 2 ;;
esac
