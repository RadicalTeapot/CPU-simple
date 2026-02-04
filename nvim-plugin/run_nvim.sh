#!/bin/sh

# Resolve the base folder (parent of script directory)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
BASE_FOLDER="$(cd "$SCRIPT_DIR/.." && pwd)"
APP_NAME="nvim-plugin"

# Set XDG directories to the base folder and NVIM_APPNAME
# (nvim appends NVIM_APPNAME to all XDG paths)
export XDG_CONFIG_HOME="$BASE_FOLDER"
export XDG_DATA_HOME="$BASE_FOLDER"
export XDG_STATE_HOME="$BASE_FOLDER"
export XDG_CACHE_HOME="${TMPDIR:-/tmp}"
export NVIM_APPNAME="$APP_NAME"

# Call nvim with all arguments
nvim "$@"
