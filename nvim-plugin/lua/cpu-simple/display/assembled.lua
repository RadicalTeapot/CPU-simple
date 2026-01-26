-- Buffer manager for assembled code display

local utils = require("cpu-simple.display.utils")
local sidebar = require("cpu-simple.display.sidebar")

local M = {}

-- Panel ID for sidebar registration
local PANEL_ID = "assembled"

-- Buffer state (window is managed by sidebar)
M.bufnr = nil

--- Get or create the assembled buffer
---@return number bufnr
local function get_or_create_buffer()
  if M.bufnr and vim.api.nvim_buf_is_valid(M.bufnr) then
    return M.bufnr
  end

  M.bufnr = utils.create_scratch_buffer({
    name = "[CPU-Simple-Assembled]",
    modifiable = false,
  })

  -- Register with sidebar manager
  sidebar.register_panel(PANEL_ID, M.bufnr)

  return M.bufnr
end

--- Show the assembled panel in the sidebar
---@return boolean success
function M.show()
  get_or_create_buffer()
  return sidebar.show_panel(PANEL_ID)
end

--- Hide the assembled panel
---@return boolean success
function M.hide()
  return sidebar.hide_panel(PANEL_ID)
end

--- Toggle the assembled panel visibility
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

--- Check if buffer is valid
---@return boolean
function M.is_valid()
  return M.bufnr ~= nil and vim.api.nvim_buf_is_valid(M.bufnr)
end

--- Get the buffer number
---@return number|nil bufnr
function M.get_buffer()
  return get_or_create_buffer()
end

--- Set content in the assembled buffer
---@param lines string[] Lines to display
function M.set_content(lines)
  local bufnr = get_or_create_buffer()

  vim.api.nvim_set_option_value("modifiable", true, { buf = bufnr })
  vim.api.nvim_buf_set_lines(bufnr, 0, -1, false, lines)
  vim.api.nvim_set_option_value("modifiable", false, { buf = bufnr })
end

--- Clear the assembled buffer
function M.clear()
  if M.bufnr and vim.api.nvim_buf_is_valid(M.bufnr) then
    vim.api.nvim_set_option_value("modifiable", true, { buf = M.bufnr })
    vim.api.nvim_buf_set_lines(M.bufnr, 0, -1, false, {})
    vim.api.nvim_set_option_value("modifiable", false, { buf = M.bufnr })
  end
end

--- Focus the panel if visible
---@return boolean success
function M.focus()
  return sidebar.focus_panel(PANEL_ID)
end

--- Get the window number (if visible)
---@return number|nil winnr
function M.get_winnr()
  return sidebar.get_panel_winnr(PANEL_ID)
end

-- Legacy alias for backward compatibility
M.get_or_create_assembled_panel = function()
  M.show()
  return M.bufnr
end

M.is_open = M.is_visible

return M