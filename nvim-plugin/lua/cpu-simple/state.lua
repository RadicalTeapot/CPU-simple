-- CPU state module for cpu-simple
-- Stores CPU state separately from backend process management

local M = {}

-- CPU state (populated by backend responses)
M.status = nil   -- { cycles, pc, sp, registers, flags }
M.stack = nil    -- Array of stack values
M.memory = nil   -- Array of memory values
M.breakpoints = {}  -- Array of breakpoint objects (just { address = number } for now)
M.is_halted = false

--- Update CPU status from backend response
---@param status_line string "[STATUS] cycles pc sp r0 r1 r2 r3 zero carry memoryChanges stackChanges"
function M.update_status(status_line)
  local status = status_line:gsub("^%[STATUS%]%s*", "")

  local cycles, pc, sp, r0, r1, r2, r3, zero, carry, memoryChanges, stackChanges = status:match(
    "^(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+%((.*)%)%s+%((.*)%)%s*$"
  )

  if cycles then
    local memory_changes = {}
    if memoryChanges then
      for addr, val in memoryChanges:gmatch("(%d+)%s+(%d+)") do
        memory_changes[tonumber(addr)] = tonumber(val)
      end
    end

    local stack_changes = {}
    if stackChanges then
      for addr, val in stackChanges:gmatch("(%d+)%s+(%d+)") do
        stack_changes[tonumber(addr)] = tonumber(val)
      end
    end

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
      memory_changes = memory_changes,
      stack_changes = stack_changes,
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

--- Update breakpoint list from backend response
---@param breakpoint_line string "[BP] addr1 addr2 ..."
function M.set_breakpoints(breakpoint_line)
  local bp_list = breakpoint_line:gsub("^%[BP%]%s*", "")
  local breakpoints = {}
  for addr in bp_list:gmatch("(%d+)") do
    table.insert(breakpoints, { address = tonumber(addr) })
  end
  M.breakpoints = breakpoints
end

return M
