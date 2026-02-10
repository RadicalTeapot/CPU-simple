#!/bin/sh

set -eu

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

if ! command -v nvim >/dev/null 2>&1; then
  echo "Neovim (nvim) is required to run plugin tests."
  exit 1
fi

status=0
for spec in "$SCRIPT_DIR"/*_spec.lua; do
  echo "[nvim-test] $(basename "$spec")"
  if ! CPU_SIMPLE_REPO_ROOT="$REPO_ROOT" \
    "$REPO_ROOT/nvim-plugin/run_nvim.sh" --headless -u NONE -i NONE \
    +"luafile $spec" \
    +qa; then
    status=1
  fi
done

exit "$status"
