local t = dofile(vim.env.CPU_SIMPLE_REPO_ROOT .. "/nvim-plugin/tests/bootstrap.lua")
t.setup_paths()
t.reset_cpu_modules()

vim.notify = function() end

local cpu = require("cpu-simple")
local deps = require("cpu-simple.core.deps")
local events = require("cpu-simple.events")

deps.reset()
events.clear()

local display_stub, display_calls = t.make_display_stub()
local backend_stub = {
  start = function() return true end,
  stop = function() end,
  send = function() return true end,
  is_running = function() return false end,
}
local assembler_stub = t.make_assembler_stub()
local state_stub = {
  status = nil,
  stack = nil,
  memory = nil,
  breakpoints = {},
  loaded_program = false,
}

deps.set("display", display_stub)
deps.set("backend", backend_stub)
deps.set("assembler", assembler_stub)
deps.set("state", state_stub)
deps.set("events", events)
deps.set("commands", require("cpu-simple.commands"))
deps.set("operand_resolver", t.make_operand_resolver_stub())

cpu.setup({ lsp_path = nil })

local commands = vim.api.nvim_get_commands({ builtin = false })

local expected = {
  "CpuBackendStart",
  "CpuBackendStop",
  "CpuBackendStatus",
  "CpuAssemble",
  "CpuLoad",
  "CpuRun",
  "CpuStep",
  "CpuStepOver",
  "CpuStepOut",
  "CpuReset",
  "CpuStatus",
  "CpuToggleBp",
  "CpuClearBp",
  "CpuNextBp",
  "CpuPrevBp",
  "CpuGotoDef",
  "CpuGotoPC",
  "CpuRunToCursor",
  "CpuToggleStatus",
  "CpuToggleStack",
  "CpuToggleMemory",
  "CpuToggleDump",
  "CpuToggleAssembled",
  "CpuOpenSidebar",
  "CpuCloseSidebar",
}

for _, name in ipairs(expected) do
  t.assert_contains(commands, name, "command should be registered: " .. name)
end

vim.cmd("CpuToggleDump")
t.assert_equal(display_calls.toggle_memory, 1, "CpuToggleDump should toggle memory panel")

print("commands_spec: OK")
