local t = dofile(vim.env.CPU_SIMPLE_REPO_ROOT .. "/nvim-plugin/tests/bootstrap.lua")
t.setup_paths()
t.reset_cpu_modules()

vim.notify = function() end

local cpu = require("cpu-simple")
local deps = require("cpu-simple.core.deps")
local events = require("cpu-simple.events")

deps.reset()
events.clear()

local starts = 0
local sends = {}
local running = false

local backend_stub = {
  start = function()
    starts = starts + 1
    running = true
    events.emit(events.BACKEND_STARTED, { pid = 123 })
    return true
  end,
  stop = function()
    running = false
  end,
  send = function(cmd)
    table.insert(sends, cmd)
    return true
  end,
  is_running = function()
    return running
  end,
}

local display_stub = t.make_display_stub()
local state_stub = {
  status = nil,
  stack = nil,
  memory = nil,
  breakpoints = {},
  loaded_program = true,
}

deps.set("display", display_stub)
deps.set("backend", backend_stub)
deps.set("assembler", t.make_assembler_stub())
deps.set("state", state_stub)
deps.set("events", events)
deps.set("commands", require("cpu-simple.commands"))
deps.set("operand_resolver", t.make_operand_resolver_stub())

cpu.setup({ lsp_path = nil })

cpu.run()
cpu.run()

t.assert_equal(starts, 1, "backend should auto-start once")
t.assert_equal(#sends, 2, "run should send two commands")
t.assert_equal(sends[1], "run", "first command should be run")
t.assert_equal(sends[2], "run", "second command should be run")

print("backend_lifecycle_spec: OK")
