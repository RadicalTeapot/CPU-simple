-- Display module for cpu-simple
-- Manages scratch buffers for displaying status, memory, stack output

local utils = require("cpu-simple.display.utils")
local sidebar = require("cpu-simple.display.sidebar")

local PANEL_ID = "dump"
local BUFFER_NAME = "[CPU-Simple-Dump]"

local M = {}

-- Buffer state (window is managed by sidebar)
M.bufnr = nil

--- Get or create the scratch buffer
---@return number bufnr
local function get_or_create_buffer()
  if M.bufnr and vim.api.nvim_buf_is_valid(M.bufnr) then
    return M.bufnr
  end

  M.bufnr = utils.create_scratch_buffer({ name = BUFFER_NAME })

  -- Register with sidebar manager
  sidebar.register_panel(PANEL_ID, M.bufnr)

  return M.bufnr
end

--- Show the dump panel in the sidebar
---@return boolean success
function M.show()
  get_or_create_buffer()
  return sidebar.show_panel(PANEL_ID)
end

--- Hide the dump panel
---@return boolean success
function M.hide()
  return sidebar.hide_panel(PANEL_ID)
end

--- Toggle the dump panel visibility
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

--- Set content in the dump buffer and show panel
---@param lines string[] Lines to display
---@param opts table|nil Options { show?: boolean } - show defaults to true
function M.set_content(lines, opts)
  opts = opts or {}
  local should_show = opts.show ~= false

  local bufnr = get_or_create_buffer()

  vim.api.nvim_set_option_value("modifiable", true, { buf = bufnr })
  vim.api.nvim_buf_set_lines(bufnr, 0, -1, false, lines)
  vim.api.nvim_set_option_value("modifiable", false, { buf = bufnr })

  if should_show then
    M.show()
  end
end

--- Append lines to the scratch buffer
---@param lines string[] Lines to append
function M.append(lines)
  local bufnr = get_or_create_buffer()

  vim.api.nvim_set_option_value("modifiable", true, { buf = bufnr })
  local line_count = vim.api.nvim_buf_line_count(bufnr)
  vim.api.nvim_buf_set_lines(bufnr, line_count, line_count, false, lines)
  vim.api.nvim_set_option_value("modifiable", false, { buf = bufnr })

  -- Scroll to bottom if panel is visible
  local winnr = sidebar.get_panel_winnr(PANEL_ID)
  if winnr then
    local new_count = vim.api.nvim_buf_line_count(bufnr)
    vim.api.nvim_win_set_cursor(winnr, { new_count, 0 })
  end
end

--- Clear the scratch buffer
function M.clear()
  if M.bufnr and vim.api.nvim_buf_is_valid(M.bufnr) then
    vim.api.nvim_set_option_value("modifiable", true, { buf = M.bufnr })
    vim.api.nvim_buf_set_lines(M.bufnr, 0, -1, false, {})
    vim.api.nvim_set_option_value("modifiable", false, { buf = M.bufnr })
  end
end

--- Close the display panel (alias for hide)
function M.close()
  M.hide()
end

--- Focus the panel if visible
---@return boolean success
function M.focus()
  return sidebar.focus_panel(PANEL_ID)
end

--- Get the buffer number
---@return number|nil bufnr
function M.get_buffer()
  return get_or_create_buffer()
end

--- Get the window number (if visible)
---@return number|nil winnr
function M.get_winnr()
  return sidebar.get_panel_winnr(PANEL_ID)
end

return M
