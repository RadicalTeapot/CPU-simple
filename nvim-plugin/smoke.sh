#!/bin/sh

set -eu

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

if ! command -v nvim >/dev/null 2>&1; then
  echo "Neovim (nvim) is required to run the smoke test."
  exit 1
fi

TMP_LUA="$(mktemp)"
cleanup() {
  rm -f "$TMP_LUA"
}
trap cleanup EXIT

cat > "$TMP_LUA" <<'LUA'
local repo_root = vim.env.CPU_SIMPLE_REPO_ROOT
if not repo_root or repo_root == "" then
  error("CPU_SIMPLE_REPO_ROOT is not set")
end

package.path = table.concat({
  repo_root .. "/nvim-plugin/lua/?.lua",
  repo_root .. "/nvim-plugin/lua/?/init.lua",
  package.path,
}, ";")

local cpu = require("cpu-simple")
local resolver = require("cpu-simple.operand_resolver")
local highlights = require("cpu-simple.display.highlights")
local memory = require("cpu-simple.display.memory")
local state = require("cpu-simple.state")
local events = require("cpu-simple.events")

assert(cpu.defaults.sidebar.panels.memory.changed_highlight.timeout_ms == 1500)
assert(cpu.defaults.sidebar.panels.memory.cursor_address_highlight == true)
assert(cpu.defaults.source_annotations.pc_operands_virtual_text.enabled == true)

local range = resolver.find_memory_expr_at_cursor("lda r0, [r1 + #0x01]", 12)
assert(range and range.expression == "[r1 + #0x01]")

local mem = {}
for i = 1, 256 do
  mem[i] = 0
end
mem[34] = 0x7F

local resolved = resolver.resolve_memory_expr("[r1 + #0x01]", {
  registers = { [2] = 0x20 },
  memory = mem,
  max_address = 255,
})
assert(resolved and resolved.address == 0x21 and resolved.value == 0x7F)

local text = resolver.build_pc_virtual_text("lda r0, [r1 + #0x01]", {
  registers = { [1] = 0x11, [2] = 0x20 },
  memory = mem,
  hex_width = 2,
  max_address = 255,
})
assert(text:find("r0=0x11"))
assert(text:find("r1=0x20"))
assert(text:find("%[r1%+#0x01%]%->0x21=0x7F"))

vim.notify = function() end

cpu.setup({ lsp_path = nil })

local bufnr = vim.api.nvim_create_buf(false, true)
vim.api.nvim_buf_set_lines(bufnr, 0, -1, false, { "nop" })
highlights.set_pc_virtual_text(bufnr, 1, "smoke")
local marks = vim.api.nvim_buf_get_extmarks(bufnr, highlights.ns_source_pc_virtual_text, 0, -1, {})
assert(#marks == 1)
highlights.clear_pc_virtual_text(bufnr)
marks = vim.api.nvim_buf_get_extmarks(bufnr, highlights.ns_source_pc_virtual_text, 0, -1, {})
assert(#marks == 0)

memory.setup({
  changed_highlight = {
    enabled = true,
    timeout_ms = 0,
  },
})
state.memory = { 0, 0, 0, 0 }
state.status = {
  pc = 0,
  memory_changes = { [0] = 1, [2] = 3 },
}
events.emit(events.STATUS_UPDATED, {})
assert(memory.changed_addresses[0] ~= nil)
assert(memory.changed_addresses[2] ~= nil)

print("cpu-simple nvim smoke: OK")
LUA

CPU_SIMPLE_REPO_ROOT="$REPO_ROOT" \
  "$SCRIPT_DIR/run_nvim.sh" --headless -u NONE -i NONE \
  +"luafile $TMP_LUA" \
  +qa
