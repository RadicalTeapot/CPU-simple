local t = dofile(vim.env.CPU_SIMPLE_REPO_ROOT .. "/nvim-plugin/tests/bootstrap.lua")
t.setup_paths()
t.reset_cpu_modules()

local cpu = require("cpu-simple")
local deps = require("cpu-simple.core.deps")

deps.reset()

local running = false
local backend_stub = {
  is_running = function()
    return running
  end,
}

local state_stub = {
  status = nil,
}

deps.set("backend", backend_stub)
deps.set("state", state_stub)

t.assert_equal(cpu.get_statusline(), "Backend stopped", "statusline when backend stopped")

running = true
t.assert_equal(cpu.get_statusline(), "Backend running: no status", "statusline when no status data")

state_stub.status = {
  cycles = 12,
  pc = 3,
  sp = 14,
  flags = { zero = true, carry = false },
  registers = { [1] = 1, [2] = 2, [3] = 3, [4] = 4 },
}

t.assert_equal(
  cpu.get_statusline(),
  "Cyc:12 PC:3 SP:14 Z:1 C:0 R0:1 R1:2 R2:3 R3:4",
  "statusline format should match expected output"
)

print("statusline_spec: OK")
