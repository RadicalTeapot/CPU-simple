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

  -- Extract memory_changes and stack_changes from traces
  local memory_changes = {}
  local stack_changes = {}
  if json.traces then
    for _, trace in ipairs(json.traces) do
      if trace.bus and trace.bus.direction == "Write" then
        -- Classify based on phase: MemoryWrite on main memory vs stack
        if trace.phase == "MemoryWrite" then
          -- Check sp_before vs sp_after to distinguish stack writes from memory writes
          if trace.sp_before ~= trace.sp_after then
            stack_changes[trace.bus.address] = trace.bus.data
          else
            memory_changes[trace.bus.address] = trace.bus.data
          end
        end
      end
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

  -- Apply incremental changes to existing memory/stack arrays
  if M.memory then
    for addr, val in pairs(memory_changes) do
      -- addr is 0-based, M.memory is 1-based
      M.memory[addr + 1] = val
    end
  end

  if M.stack then
    for index, val in pairs(stack_changes) do
      -- index is 0-based, M.stack is 1-based
      M.stack[index + 1] = val
    end
  end
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
