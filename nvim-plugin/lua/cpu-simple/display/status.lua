-- Status panel for cpu-simple
-- Displays CPU registers, flags, PC, SP, and cycle count

local utils = require("cpu-simple.display.utils")
local sidebar = require("cpu-simple.display.sidebar")
local state = require("cpu-simple.state")
local events = require("cpu-simple.events")

local PANEL_ID = "status"
local BUFFER_NAME = "[CPU-Simple-Status]"

local M = {}

M.bufnr = nil

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

--- Show the status panel in the sidebar
---@return boolean success
function M.show()
  get_or_create_buffer()
  return sidebar.show_panel(PANEL_ID)
end

--- Hide the status panel
---@return boolean success
function M.hide()
  return sidebar.hide_panel(PANEL_ID)
end

--- Toggle the status panel visibility
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

--- Render CPU status into the buffer
function M.render()
  if not M.is_visible() then
    return
  end

  local bufnr = get_or_create_buffer()
  local s = state.status
  local lines = {}

  if s then
    local zero = s.flags.zero and 1 or 0
    local carry = s.flags.carry and 1 or 0

    table.insert(lines, string.format("Cycle: %d   PC: 0x%02X   SP: 0x%02X",
      s.cycles, s.pc, s.sp))
    table.insert(lines, string.format("Flags: Z=%d C=%d", zero, carry))
    table.insert(lines, string.format("R0: 0x%02X  R1: 0x%02X  R2: 0x%02X  R3: 0x%02X",
      s.registers[1], s.registers[2], s.registers[3], s.registers[4]))
  else
    table.insert(lines, "No CPU status available")
  end

  vim.api.nvim_set_option_value("modifiable", true, { buf = bufnr })
  vim.api.nvim_buf_set_lines(bufnr, 0, -1, false, lines)
  vim.api.nvim_set_option_value("modifiable", false, { buf = bufnr })
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
events.on(events.STATUS_UPDATED, function()
  if M.is_visible() then
    M.render()
  end
end)

return M
