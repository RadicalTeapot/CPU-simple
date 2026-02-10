-- Operand parsing and resolution helpers for source-based annotations/highlighting.

local M = {}

local function strip_comment(line_text)
  local start_idx = string.find(line_text, ";", 1, true)
  if not start_idx then
    return line_text
  end
  return string.sub(line_text, 1, start_idx - 1)
end

local function trim(value)
  return (value:gsub("^%s+", ""):gsub("%s+$", ""))
end

local function normalize(value)
  return value:lower():gsub("%s+", "")
end

local function safe_tonumber(value)
  local n = tonumber(value)
  if type(n) ~= "number" then
    return nil
  end
  return n
end

local function resolve_symbol_address(symbols, name)
  if not symbols then
    return nil
  end

  local entry = symbols[name] or symbols[name:lower()]
  if not entry then
    return nil
  end
  return safe_tonumber(entry.address)
end

local function resolve_register_value(registers, register_index)
  if type(register_index) ~= "number" then
    return nil
  end
  if not registers then
    return nil
  end

  local array_index = register_index + 1
  local value = registers[array_index] or registers[tostring(array_index)]
  return safe_tonumber(value)
end

local function resolve_memory_value(memory, address)
  if not memory or type(address) ~= "number" then
    return nil
  end

  local index = address + 1
  local value = memory[index]
  return safe_tonumber(value)
end

local function address_in_bounds(memory, address)
  if type(address) ~= "number" or address < 0 then
    return false
  end
  if not memory then
    return true
  end
  return address < #memory
end

local function find_bracket_ranges(line_text)
  local ranges = {}
  local start_col0 = nil
  local code = strip_comment(line_text)

  for idx = 1, #code do
    local ch = code:sub(idx, idx)
    if ch == "[" then
      start_col0 = idx - 1
    elseif ch == "]" and start_col0 ~= nil then
      local end_col0 = idx - 1
      table.insert(ranges, {
        expression = code:sub(start_col0 + 1, end_col0 + 1),
        start_col0 = start_col0,
        end_col0 = end_col0,
      })
      start_col0 = nil
    end
  end

  return ranges
end

local function hex_width(ctx, value)
  local base = (ctx and ctx.hex_width) or 2
  if type(value) ~= "number" then
    return base
  end

  local digits = #string.format("%X", math.max(0, value))
  if digits > base then
    return digits
  end
  return base
end

local function format_hex(value, min_width)
  local width = min_width or 2
  if type(value) ~= "number" then
    return "?"
  end
  return string.format("0x%0" .. tostring(width) .. "X", value)
end

--- Find the memory expression under cursor.
---@param line_text string Source line text
---@param cursor_col0 number 0-based cursor column
---@return table|nil { expression, start_col0, end_col0 }
function M.find_memory_expr_at_cursor(line_text, cursor_col0)
  if type(line_text) ~= "string" or type(cursor_col0) ~= "number" then
    return nil
  end

  for _, range in ipairs(find_bracket_ranges(line_text)) do
    if cursor_col0 >= range.start_col0 and cursor_col0 <= range.end_col0 then
      return range
    end
  end

  return nil
end

--- Resolve a memory expression into address/value.
---@param memory_expression string Expression like "[label + #0x01]"
---@param ctx table { symbols, registers, memory, max_address? }
---@return table|nil { expression, address, value }
function M.resolve_memory_expr(memory_expression, ctx)
  if type(memory_expression) ~= "string" then
    return nil
  end

  local expression = trim(memory_expression)
  local inner = expression
  if inner:sub(1, 1) == "[" and inner:sub(-1) == "]" then
    inner = inner:sub(2, -2)
  end
  inner = normalize(inner)
  if inner == "" then
    return nil
  end

  local address = nil

  local immediate_hex = inner:match("^#0x([0-9a-f]+)$")
  if immediate_hex then
    address = tonumber(immediate_hex, 16)
  end

  if address == nil then
    local label_name, op, offset_hex = inner:match("^([%a_][%w_]*)([+-])#0x([0-9a-f]+)$")
    if label_name then
      local base = resolve_symbol_address(ctx and ctx.symbols, label_name)
      local offset = tonumber(offset_hex, 16)
      if base ~= nil and offset ~= nil then
        address = (op == "+") and (base + offset) or (base - offset)
      end
    end
  end

  if address == nil then
    local label_name = inner:match("^([%a_][%w_]*)$")
    if label_name then
      address = resolve_symbol_address(ctx and ctx.symbols, label_name)
    end
  end

  if address == nil then
    local register_str, offset_hex = inner:match("^r(%d+)%+#0x([0-9a-f]+)$")
    if register_str then
      local register_value = resolve_register_value(ctx and ctx.registers, tonumber(register_str))
      local offset = tonumber(offset_hex, 16)
      if register_value ~= nil and offset ~= nil then
        address = register_value + offset
      end
    end
  end

  if address == nil then
    local register_str = inner:match("^r(%d+)$")
    if register_str then
      address = resolve_register_value(ctx and ctx.registers, tonumber(register_str))
    end
  end

  if type(address) ~= "number" then
    return nil
  end
  if address < 0 then
    return nil
  end
  if ctx and ctx.max_address and address > ctx.max_address then
    return nil
  end
  if not address_in_bounds(ctx and ctx.memory, address) then
    return nil
  end

  return {
    expression = expression,
    address = address,
    value = resolve_memory_value(ctx and ctx.memory, address),
  }
end

--- Collect registers and memory expressions used in a source line.
---@param line_text string
---@return table { registers: string[], memory_expressions: string[] }
function M.collect_line_operands(line_text)
  if type(line_text) ~= "string" then
    return {
      registers = {},
      memory_expressions = {},
    }
  end

  local code = strip_comment(line_text)
  local registers = {}
  local memory_expressions = {}
  local seen_registers = {}
  local seen_memory = {}

  for _, range in ipairs(find_bracket_ranges(code)) do
    local expression = trim(range.expression)
    local key = normalize(expression)
    if expression ~= "" and not seen_memory[key] then
      seen_memory[key] = true
      table.insert(memory_expressions, expression)
    end
  end

  for register in code:gmatch("%f[%a_]([rR]%d+)%f[^%w_]") do
    local key = register:lower()
    if not seen_registers[key] then
      seen_registers[key] = true
      table.insert(registers, key)
    end
  end

  return {
    registers = registers,
    memory_expressions = memory_expressions,
  }
end

--- Build concise virtual text for resolved operands on a source line.
---@param line_text string
---@param ctx table { symbols, registers, memory, hex_width? }
---@return string
function M.build_pc_virtual_text(line_text, ctx)
  local operands = M.collect_line_operands(line_text)
  local chunks = {}

  for _, register in ipairs(operands.registers) do
    local register_index = tonumber(register:sub(2))
    local value = resolve_register_value(ctx and ctx.registers, register_index)
    if value ~= nil then
      table.insert(chunks, string.format("%s=%s", register, format_hex(value, hex_width(ctx, value))))
    else
      table.insert(chunks, string.format("%s=?", register))
    end
  end

  for _, expression in ipairs(operands.memory_expressions) do
    local resolved = M.resolve_memory_expr(expression, ctx)
    if resolved and resolved.address ~= nil then
      local addr_width = hex_width(ctx, resolved.address)
      local value_width = hex_width(ctx, resolved.value)
      local value_text = resolved.value ~= nil and format_hex(resolved.value, value_width) or "??"
      table.insert(chunks, string.format("%s->%s=%s", normalize(expression), format_hex(resolved.address, addr_width), value_text))
    else
      table.insert(chunks, string.format("%s->?", normalize(expression)))
    end
  end

  return table.concat(chunks, "  ")
end

return M
