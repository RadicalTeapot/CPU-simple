local t = dofile(vim.env.CPU_SIMPLE_REPO_ROOT .. "/nvim-plugin/tests/bootstrap.lua")
t.setup_paths()
t.reset_cpu_modules()

vim.notify = function() end

local cpu = require("cpu-simple")
local deps = require("cpu-simple.core.deps")
local events = require("cpu-simple.events")

deps.reset()
events.clear()

local tmpfile = vim.fn.tempname()
vim.fn.writefile({ "00", "01" }, tmpfile)

local sends = {}
local backend_stub = {
  start = function() return true end,
  stop = function() end,
  send = function(cmd)
    table.insert(sends, cmd)
    return true
  end,
  is_running = function()
    return true
  end,
}

local display_stub, _ = t.make_display_stub()
local clear_highlight_calls = 0
display_stub.memory.clear_highlight_state = function()
  clear_highlight_calls = clear_highlight_calls + 1
end

local assembler_stub = t.make_assembler_stub({
  get_last_output_path = function()
    return tmpfile
  end,
})

local state_stub = {
  status = nil,
  stack = { 1, 2, 3 },
  memory = { 4, 5, 6 },
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
cpu.load()

local ok = vim.wait(250, function()
  return state_stub.loaded_program and state_stub.loaded_program ~= false
end)
t.assert_true(ok, "load should schedule loaded_program assignment")

t.assert_equal(state_stub.memory, nil, "memory cache should be cleared")
t.assert_equal(state_stub.stack, nil, "stack cache should be cleared")
t.assert_equal(clear_highlight_calls, 1, "memory highlight cache should be cleared")

t.assert_equal(#sends, 1, "exactly one backend command should be sent")
local expected_path = vim.fn.fnamemodify(tmpfile, ":p")
t.assert_equal(sends[1], "load " .. expected_path, "load command should use absolute path")
t.assert_equal(state_stub.loaded_program, expected_path, "loaded_program should be set to absolute path")

vim.fn.delete(tmpfile)

print("load_flow_spec: OK")
