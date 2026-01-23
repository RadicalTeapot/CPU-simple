-- CPU state module for cpu-simple
-- Stores CPU state separately from backend process management

local M = {}

-- CPU state (populated by backend responses)
M.status = nil   -- { cycles, pc, sp, registers, flags }
M.stack = nil    -- Array of stack values
M.memory = nil   -- Array of memory values

-- Future debugging state
M.breakpoints = {}  -- { [address] = true }
M.is_halted = false

--- Update CPU status from backend response
---@param status_line string "[STATUS] cycles pc sp r0 r1 r2 r3 zero carry"
function M.update_status(status_line)
  local status = status_line:gsub("^%[STATUS%]%s*", "")
  local cycles, pc, sp, r0, r1, r2, r3, zero, carry = status:match(
    "^(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)"
  )

  if cycles then
    M.status = {
      cycles = tonumber(cycles),
      pc = tonumber(pc),
      sp = tonumber(sp),
      registers = {
        tonumber(r0),
        tonumber(r1),
        tonumber(r2),
        tonumber(r3),
      },
      flags = {
        zero = tonumber(zero),
        carry = tonumber(carry),
      },
    }
    return true
  end
  return false
end

--- Update stack from backend response
---@param stack_line string "[STACK] val1 val2 ..."
function M.update_stack(stack_line)
  local stack = stack_line:gsub("^%[STACK%]%s*", "")
  local stack_values = {}
  for value in stack:gmatch("(%d+)") do
    table.insert(stack_values, tonumber(value))
  end
  M.stack = stack_values
end

--- Update memory from backend response
---@param memory_line string "[MEMORY] val1 val2 ..."
function M.update_memory(memory_line)
  local memory = memory_line:gsub("^%[MEMORY%]%s*", "")
  local memory_values = {}
  for value in memory:gmatch("(%d+)") do
    table.insert(memory_values, tonumber(value))
  end
  M.memory = memory_values
end

--- Clear all CPU state
function M.clear()
  M.status = nil
  M.stack = nil
  M.memory = nil
  M.is_halted = false
end

--- Add a breakpoint at the given address
---@param address number Memory address
function M.add_breakpoint(address)
  M.breakpoints[address] = true
end

--- Remove a breakpoint at the given address
---@param address number Memory address
function M.remove_breakpoint(address)
  M.breakpoints[address] = nil
end

--- Check if a breakpoint exists at the given address
---@param address number Memory address
---@return boolean
function M.has_breakpoint(address)
  return M.breakpoints[address] == true
end

--- Clear all breakpoints
function M.clear_breakpoints()
  M.breakpoints = {}
end

return M
