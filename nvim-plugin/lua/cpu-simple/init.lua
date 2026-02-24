-- cpu-simple: Neovim plugin for the CPU-Simple emulator
-- Entry point that wires feature modules together.

local config_mod = require("cpu-simple.core.config")
local deps = require("cpu-simple.core.deps")
local runtime_mod = require("cpu-simple.core.runtime")
local commands_core = require("cpu-simple.core.commands")
local keymaps_core = require("cpu-simple.core.keymaps")
local events_core = require("cpu-simple.core.events")
local autocmds_core = require("cpu-simple.core.autocmds")

local backend_factory = require("cpu-simple.features.backend")
local program_factory = require("cpu-simple.features.program")
local breakpoints_factory = require("cpu-simple.features.breakpoints")
local watchpoints_factory = require("cpu-simple.features.watchpoints")
local navigation_factory = require("cpu-simple.features.navigation")
local annotations_factory = require("cpu-simple.features.annotations")
local lsp_factory = require("cpu-simple.features.lsp")

local M = {}

M.defaults = config_mod.defaults
M.config = {}

local runtime = runtime_mod.new()
local ctx = {
  config = M.config,
  deps = deps,
  runtime = runtime,
  api = M,
}

local backend_feature = nil
local program_feature = nil
local breakpoints_feature = nil
local watchpoints_feature = nil
local navigation_feature = nil
local annotations_feature = nil
local lsp_feature = nil

local function ensure_features()
  if backend_feature then
    return
  end

  backend_feature = backend_factory.new(ctx)
  annotations_feature = annotations_factory.new(ctx)
  breakpoints_feature = breakpoints_factory.new(ctx, backend_feature)
  watchpoints_feature = watchpoints_factory.new(ctx, backend_feature)
  navigation_feature = navigation_factory.new(ctx, backend_feature)
  program_feature = program_factory.new(ctx, backend_feature)
  lsp_feature = lsp_factory.new(ctx)
end

function M.register_commands()
  ensure_features()
  commands_core.register(ctx, M)
end

function M.set_keymaps()
  ensure_features()
  keymaps_core.setup(ctx)
end

function M.setup(opts)
  M.config = config_mod.resolve(opts)
  ctx.config = M.config
  ensure_features()

  local display = deps.get("display")
  display.setup({
    sidebar = M.config.sidebar,
    signs = M.config.signs,
  })

  M.register_commands()
  events_core.setup(ctx, M)
  M.set_keymaps()
  autocmds_core.setup(ctx, M)
  M.start_lsp()
end

function M.start_lsp()
  ensure_features()
  lsp_feature.start()
end

function M.backend_start()
  ensure_features()
  backend_feature.start()
end

function M.backend_stop()
  ensure_features()
  backend_feature.stop()
end

function M.backend_status()
  ensure_features()
  backend_feature.status()
end

function M.assemble()
  ensure_features()
  program_feature.assemble()
end

function M.load(path)
  ensure_features()
  program_feature.load(path)
end

function M.run()
  ensure_features()
  program_feature.run()
end

function M.step()
  ensure_features()
  program_feature.step()
end

function M.tick()
  ensure_features()
  program_feature.tick()
end

function M.step_over()
  ensure_features()
  program_feature.step_over()
end

function M.step_out()
  ensure_features()
  program_feature.step_out()
end

function M.reset()
  ensure_features()
  program_feature.reset()
end

function M.status()
  ensure_features()
  program_feature.status()
end

function M.set_breakpoint(address)
  ensure_features()
  breakpoints_feature.set_breakpoint(address)
end

function M.set_breakpoint_at_cursor()
  ensure_features()
  breakpoints_feature.set_breakpoint_at_cursor()
end

function M.clear_all_breakpoints()
  ensure_features()
  breakpoints_feature.clear_all_breakpoints()
end

function M.highlight_breakpoints()
  ensure_features()
  breakpoints_feature.highlight_breakpoints()
end

function M.add_write_watchpoint(address)
  ensure_features()
  watchpoints_feature.add_write_watchpoint(address)
end

function M.add_read_watchpoint(address)
  ensure_features()
  watchpoints_feature.add_read_watchpoint(address)
end

function M.add_phase_watchpoint(phase)
  ensure_features()
  watchpoints_feature.add_phase_watchpoint(phase)
end

function M.remove_watchpoint(id)
  ensure_features()
  watchpoints_feature.remove_watchpoint(id)
end

function M.clear_all_watchpoints()
  ensure_features()
  watchpoints_feature.clear_all_watchpoints()
end

function M.list_watchpoints()
  ensure_features()
  watchpoints_feature.list_watchpoints()
end

function M.goto_next_breakpoint()
  ensure_features()
  navigation_feature.goto_next_breakpoint()
end

function M.goto_prev_breakpoint()
  ensure_features()
  navigation_feature.goto_prev_breakpoint()
end

function M.goto_definition()
  ensure_features()
  navigation_feature.goto_definition()
end

function M.goto_PC()
  ensure_features()
  navigation_feature.goto_PC()
end

function M.run_to_cursor()
  ensure_features()
  navigation_feature.run_to_cursor()
end

function M.clear_pc_virtual_text()
  ensure_features()
  annotations_feature.clear_pc_virtual_text()
end

function M.maybe_request_dump()
  ensure_features()
  annotations_feature.maybe_request_dump()
end

function M.setup_cursor_highlight()
  ensure_features()
  annotations_feature.setup_cursor_highlight()
end

function M.setup_memory_cursor_highlight()
  ensure_features()
  annotations_feature.setup_memory_cursor_highlight()
end

function M.update_cursor_memory_highlight()
  ensure_features()
  annotations_feature.update_cursor_memory_highlight()
end

function M.update_pc_virtual_text()
  ensure_features()
  annotations_feature.update_pc_virtual_text()
end

function M.highlight_pc()
  ensure_features()
  annotations_feature.highlight_pc()
end

function M.toggle_status()
  local display = deps.get("display")
  display.toggle_status()
end

function M.toggle_stack()
  local display = deps.get("display")
  display.toggle_stack()
end

function M.toggle_memory()
  local display = deps.get("display")
  display.toggle_memory()
end

function M.toggle_dump()
  -- Keep compatibility with existing keymaps/docs naming.
  M.toggle_memory()
end

function M.toggle_assembled()
  local display = deps.get("display")
  display.toggle_assembled()
end

function M.open_sidebar()
  local display = deps.get("display")
  display.open_sidebar()
end

function M.close_sidebar()
  local display = deps.get("display")
  display.close_sidebar()
end

function M.send(cmd)
  ensure_features()
  backend_feature.send(cmd)
end

function M.is_running()
  ensure_features()
  return backend_feature.is_running()
end

function M.get_statusline()
  local backend = deps.get("backend")
  if not backend.is_running() then
    return "Backend stopped"
  end

  local state = deps.get("state")
  local s = state.status
  if not s then
    return "Backend running: no status"
  end

  local zero = s.flags.zero and 1 or 0
  local carry = s.flags.carry and 1 or 0
  return string.format(
    "Cyc:%d PC:%d SP:%d Z:%d C:%d R0:%d R1:%d R2:%d R3:%d",
    s.cycles,
    s.pc,
    s.sp,
    zero,
    carry,
    s.registers[1],
    s.registers[2],
    s.registers[3],
    s.registers[4]
  )
end

-- Test helper for injecting mocked dependencies.
function M._set_dependency(name, value)
  deps.set(name, value)
end

return M
