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
  if json.Registers then
    for reg, val in pairs(json.Registers) do
      registers[reg] = tonumber(val)
    end
  end

  local memory_changes = {}
  if json.MemoryChanges then
    for _, change in pairs(json.MemoryChanges) do
      local addr = tonumber(change.Key)
      local val = tonumber(change.Value)
      memory_changes[addr] = val
    end
  end

  local stack_changes = {}
  if json.StackChanges then
    for _, change in pairs(json.StackChanges) do
      local index = tonumber(change.Key)
      local val = tonumber(change.Value)
      stack_changes[index] = val
    end
  end

  M.status = {
    cycles = tonumber(json.Cycle),
    pc = tonumber(json.PC),
    sp = tonumber(json.SP),
    registers = registers,
    flags = {
      zero = json.ZeroFlag == "True",
      carry = json.CarryFlag == "True",
    },
    memory_changes = memory_changes,
    stack_changes = stack_changes,
    loaded_program = json.LoadedProgram == "True",
  }
end

--- Update stack from backend response
---@param json table Parsed JSON object from backend
function M.update_stack(json)
  if not json then
    return
  end

  local stack_values = {}
  if json.Stack then
    for _, val in ipairs(json.Stack) do
      table.insert(stack_values, tonumber(val))
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
  if json.Memory then
    for _, val in ipairs(json.Memory) do
      table.insert(memory_values, tonumber(val))
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
  if json.Breakpoints then
    for _, addr_str in ipairs(json.Breakpoints) do
      local addr = tonumber(addr_str)
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
  M.loaded_program = nil
end

return M
