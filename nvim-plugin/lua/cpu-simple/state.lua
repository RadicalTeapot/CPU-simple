-- CPU state module for cpu-simple
-- Stores CPU state separately from backend process management

local M = {}

-- CPU state (populated by backend responses)
M.status = nil   -- { cycles, pc, sp, registers, flags }
M.stack = nil    -- Array of stack values
M.memory = nil   -- Array of memory values
M.breakpoints = {}  -- Array of breakpoint objects (just { address = number } for now)
M.is_halted = false
M.loaded_program = false  -- Whether a program is loaded

--- Update CPU status from backend response
---@param json table Parsed JSON object from backend
function M.update_status(json)
  if not json then
    return
  end

  local registers = {}
  if json.registers then
    for reg, val in pairs(json.registers) do
      registers[reg] = val
    end
  end

  local memory_changes = {}
  if json.memory_changes then
    for _, change in pairs(json.memory_changes) do
      local addr = change.Key
      local val = change.Value
      memory_changes[addr] = val
    end
  end

  local stack_changes = {}
  if json.stack_changes then
    for _, change in pairs(json.stack_changes) do
      local index = change.Key
      local val = change.Value
      stack_changes[index] = val
    end
  end

  M.status = {
    cycles = json.cycle,
    pc = json.pc,
    sp = json.sp,
    registers = registers,
    flags = {
      zero = json.zero_flag == "True",
      carry = json.carry_flag == "True",
    },
    memory_changes = memory_changes,
    stack_changes = stack_changes,
    loaded_program = json.loaded_program == "True",
  }
end

--- Update stack from backend response
---@param json table Parsed JSON object from backend
function M.update_stack(json)
  if not json then
    return
  end

  local stack_values = {}
  if json.stack then
    for _, val in ipairs(json.stack) do
      table.insert(stack_values, val)
    end
  end

  M.stack = stack_values
end

--- Update memory from backend response
---@param json table Parsed JSON object from backend
function M.update_memory(json)
  if not json then
    return
  end

  local memory_values = {}
  if json.memory then
    for _, val in ipairs(json.memory) do
      table.insert(memory_values, val)
    end
  end

  M.memory = memory_values
end

--- Update breakpoint list from backend response
---@param json table Parsed JSON object from backend
function M.set_breakpoints(json)
  if not json then
    return
  end

  local breakpoints = {}
  if json.breakpoints then
    for _, addr in ipairs(json.breakpoints) do
      table.insert(breakpoints, { address = addr })
    end
  end

  M.breakpoints = breakpoints
end

--- Clear all CPU state
function M.clear()
  M.status = nil
  M.stack = nil
  M.memory = nil
  M.is_halted = false
  M.loaded_program = false
end

return M
