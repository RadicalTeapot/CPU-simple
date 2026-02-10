-- Memory panel for cpu-simple
-- Displays memory contents as a hex dump

local utils = require("cpu-simple.display.utils")
local sidebar = require("cpu-simple.display.sidebar")
local highlights = require("cpu-simple.display.highlights")
local state = require("cpu-simple.state")
local events = require("cpu-simple.events")

local PANEL_ID = "memory"
local BUFFER_NAME = "[CPU-Simple-Memory]"

local M = {}

M.bufnr = nil
M.bytes_per_line = 16
M.change_highlight_enabled = true
M.change_highlight_timeout_ms = 1500
M.cursor_address_highlight_enabled = true
M.changed_addresses = {} -- { [address] = expires_at_ms|0 }
M.cursor_address = nil
M.fade_timer = nil

local function now_ms()
  return math.floor(vim.loop.hrtime() / 1000000)
end

local function stop_fade_timer()
  if M.fade_timer then
    M.fade_timer:stop()
    M.fade_timer:close()
    M.fade_timer = nil
  end
end

local function prune_expired_changes()
  if M.change_highlight_timeout_ms <= 0 then
    return false
  end

  local changed = false
  local now = now_ms()
  for address, expires_at in pairs(M.changed_addresses) do
    if expires_at > 0 and now >= expires_at then
      M.changed_addresses[address] = nil
      changed = true
    end
  end

  return changed
end

local function start_fade_timer()
  if not M.change_highlight_enabled or M.change_highlight_timeout_ms <= 0 then
    return
  end
  if next(M.changed_addresses) == nil then
    return
  end
  if M.fade_timer then
    return
  end

  M.fade_timer = vim.loop.new_timer()
  if not M.fade_timer then
    return
  end

  M.fade_timer:start(100, 100, vim.schedule_wrap(function()
    local expired = prune_expired_changes()
    if expired and M.is_visible() and state.memory then
      M.render()
    end

    if not M.change_highlight_enabled or M.change_highlight_timeout_ms <= 0 or next(M.changed_addresses) == nil then
      stop_fade_timer()
    end
  end))
end

local function collect_memory_changes_from_status()
  if not M.change_highlight_enabled then
    return
  end
  if not state.status or not state.status.memory_changes then
    return
  end

  local expires_at = 0
  if M.change_highlight_timeout_ms > 0 then
    expires_at = now_ms() + M.change_highlight_timeout_ms
  end

  for address, _ in pairs(state.status.memory_changes) do
    local numeric_address = tonumber(address)
    if numeric_address ~= nil then
      M.changed_addresses[numeric_address] = expires_at
    end
  end

  if M.change_highlight_timeout_ms > 0 then
    start_fade_timer()
  end
end

--- Configure the memory panel
---@param opts table|nil Options { bytes_per_line?: number, changed_highlight?: table, cursor_address_highlight?: boolean }
function M.setup(opts)
  opts = opts or {}
  if opts.bytes_per_line then
    M.bytes_per_line = opts.bytes_per_line
  end
  if opts.changed_highlight then
    if opts.changed_highlight.enabled ~= nil then
      M.change_highlight_enabled = opts.changed_highlight.enabled == true
    end
    if opts.changed_highlight.timeout_ms ~= nil then
      local timeout_ms = tonumber(opts.changed_highlight.timeout_ms)
      if timeout_ms and timeout_ms >= 0 then
        M.change_highlight_timeout_ms = timeout_ms
      end
    end
  end
  if opts.cursor_address_highlight ~= nil then
    M.cursor_address_highlight_enabled = opts.cursor_address_highlight == true
  end

  if not M.change_highlight_enabled then
    M.changed_addresses = {}
    stop_fade_timer()
  elseif M.change_highlight_timeout_ms > 0 then
    start_fade_timer()
  end
end

--- Get or create the scratch buffer
---@return number bufnr
local function get_or_create_buffer()
  if M.bufnr and vim.api.nvim_buf_is_valid(M.bufnr) then
    return M.bufnr
  end

  M.bufnr = utils.create_scratch_buffer({ name = BUFFER_NAME })
  sidebar.register_panel(PANEL_ID, M.bufnr)

  return M.bufnr
end

--- Show the memory panel in the sidebar
---@return boolean success
function M.show()
  get_or_create_buffer()
  return sidebar.show_panel(PANEL_ID)
end

--- Hide the memory panel
---@return boolean success
function M.hide()
  return sidebar.hide_panel(PANEL_ID)
end

--- Toggle the memory panel visibility
---@return boolean new_visible_state
function M.toggle()
  get_or_create_buffer()
  return sidebar.toggle_panel(PANEL_ID)
end

--- Check if panel is visible
---@return boolean
function M.is_visible()
  return sidebar.is_panel_visible(PANEL_ID)
end

--- Format data as hex dump lines
---@param data table Array of byte values
---@param bytes_per_line number Bytes per line
---@return string[] lines
local function format_hex_dump(data, bytes_per_line)
  local lines = {}
  for i = 1, #data, bytes_per_line do
    local parts = { string.format("%02X:", i - 1) }
    for j = 0, bytes_per_line - 1 do
      if i + j <= #data then
        table.insert(parts, string.format("%02X", data[i + j]))
      end
    end
    table.insert(lines, table.concat(parts, " "))
  end
  return lines
end

--- Render memory data into the buffer
function M.render()
  if not M.is_visible() then
    return
  end

  local bufnr = get_or_create_buffer()
  local lines = {}

  if state.memory and #state.memory > 0 then
    lines = format_hex_dump(state.memory, M.bytes_per_line)
  else
    table.insert(lines, "No memory data available")
  end

  vim.api.nvim_set_option_value("modifiable", true, { buf = bufnr })
  vim.api.nvim_buf_set_lines(bufnr, 0, -1, false, lines)
  vim.api.nvim_set_option_value("modifiable", false, { buf = bufnr })

  -- Highlight the byte at PC
  if state.status and state.status.pc ~= nil and state.memory and state.status.pc < #state.memory then
    highlights.highlight_hex_dump_byte(bufnr, highlights.ns_memory_pc, highlights.groups.PC, state.status.pc, M.bytes_per_line)
  else
    highlights.clear_ns(bufnr, highlights.ns_memory_pc)
  end

  -- Highlight bytes that changed recently.
  if M.change_highlight_enabled and state.memory and next(M.changed_addresses) ~= nil then
    local visible_addresses = {}
    for address, _ in pairs(M.changed_addresses) do
      if address >= 0 and address < #state.memory then
        visible_addresses[address] = true
      end
    end
    highlights.highlight_hex_dump_bytes(
      bufnr,
      highlights.ns_memory_changed,
      highlights.groups.MEMORY_CHANGED,
      visible_addresses,
      M.bytes_per_line
    )
  else
    highlights.clear_ns(bufnr, highlights.ns_memory_changed)
  end

  -- Highlight resolved memory expression under source cursor.
  if M.cursor_address_highlight_enabled and type(M.cursor_address) == "number" and state.memory and M.cursor_address < #state.memory then
    highlights.highlight_hex_dump_byte(
      bufnr,
      highlights.ns_memory_cursor,
      highlights.groups.MEMORY_CURSOR_ADDRESS,
      M.cursor_address,
      M.bytes_per_line
    )
  else
    highlights.clear_ns(bufnr, highlights.ns_memory_cursor)
  end
end

--- Set the memory address currently referenced by source cursor.
---@param address number|nil 0-based memory address
function M.set_cursor_address(address)
  local normalized = address
  if normalized ~= nil then
    normalized = tonumber(normalized)
    if normalized and normalized < 0 then
      normalized = nil
    end
  end

  if M.cursor_address == normalized then
    return
  end

  M.cursor_address = normalized
  if M.is_visible() and state.memory then
    M.render()
  end
end

--- Reset in-memory highlight state (changed bytes and cursor address).
function M.clear_highlight_state()
  M.changed_addresses = {}
  M.cursor_address = nil
  stop_fade_timer()

  if M.bufnr and vim.api.nvim_buf_is_valid(M.bufnr) then
    highlights.clear_ns(M.bufnr, highlights.ns_memory_changed)
    highlights.clear_ns(M.bufnr, highlights.ns_memory_cursor)
    highlights.clear_ns(M.bufnr, highlights.ns_memory_pc)
  end
end

--- Clear the buffer
function M.clear()
  if M.bufnr and vim.api.nvim_buf_is_valid(M.bufnr) then
    vim.api.nvim_set_option_value("modifiable", true, { buf = M.bufnr })
    vim.api.nvim_buf_set_lines(M.bufnr, 0, -1, false, {})
    vim.api.nvim_set_option_value("modifiable", false, { buf = M.bufnr })
  end
end

-- Subscribe to events
events.on(events.MEMORY_UPDATED, function()
  if M.is_visible() then
    M.render()
  end
end)

events.on(events.STATUS_UPDATED, function()
  collect_memory_changes_from_status()
  prune_expired_changes()

  if M.is_visible() and state.memory then
    M.render()
  end
end)

events.on(events.BACKEND_STOPPED, function()
  M.clear_highlight_state()
  if M.is_visible() then
    M.render()
  end
end)

return M
