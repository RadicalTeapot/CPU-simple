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

--- Configure the memory panel
---@param opts table|nil Options { bytes_per_line?: number }
function M.setup(opts)
  opts = opts or {}
  if opts.bytes_per_line then
    M.bytes_per_line = opts.bytes_per_line
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
  if M.is_visible() and state.memory then
    M.render()
  end
end)

return M
