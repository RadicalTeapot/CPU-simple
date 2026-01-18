-- Display module for cpu-simple
-- Manages scratch buffers for displaying status, memory, stack output

local M = {}

-- Buffer/window state
M.bufnr = nil
M.winnr = nil

--- Get or create the scratch buffer
---@return number bufnr
local function get_or_create_buffer()
  -- Check if existing buffer is still valid
  if M.bufnr and vim.api.nvim_buf_is_valid(M.bufnr) then
    return M.bufnr
  end

  -- Create new scratch buffer
  M.bufnr = vim.api.nvim_create_buf(false, true)

  -- Set buffer options
  vim.api.nvim_buf_set_option(M.bufnr, "buftype", "nofile")
  vim.api.nvim_buf_set_option(M.bufnr, "bufhidden", "hide")
  vim.api.nvim_buf_set_option(M.bufnr, "swapfile", false)
  vim.api.nvim_buf_set_option(M.bufnr, "modifiable", false)
  vim.api.nvim_buf_set_name(M.bufnr, "[CPU-Simple]")

  return M.bufnr
end

--- Show content in the scratch buffer
---@param lines string[] Lines to display
---@param opts table|nil Options { title?: string, split?: "vertical"|"horizontal"|"float" }
function M.show(lines, opts)
  opts = opts or {}
  local split = opts.split or "horizontal"
  local title = opts.title or "CPU-Simple"

  local bufnr = get_or_create_buffer()

  -- Set buffer content
  vim.api.nvim_buf_set_option(bufnr, "modifiable", true)
  vim.api.nvim_buf_set_lines(bufnr, 0, -1, false, lines)
  vim.api.nvim_buf_set_option(bufnr, "modifiable", false)

  -- Check if window is already open and valid
  if M.winnr and vim.api.nvim_win_is_valid(M.winnr) then
    -- Just focus the existing window
    vim.api.nvim_set_current_win(M.winnr)
    return
  end

  -- Open window based on split type
  if split == "float" then
    local width = math.floor(vim.o.columns * 0.8)
    local height = math.floor(vim.o.lines * 0.6)
    local row = math.floor((vim.o.lines - height) / 2)
    local col = math.floor((vim.o.columns - width) / 2)

    M.winnr = vim.api.nvim_open_win(bufnr, true, {
      relative = "editor",
      width = width,
      height = height,
      row = row,
      col = col,
      style = "minimal",
      border = "rounded",
      title = " " .. title .. " ",
      title_pos = "center",
    })
  elseif split == "vertical" then
    vim.cmd("vsplit")
    M.winnr = vim.api.nvim_get_current_win()
    vim.api.nvim_win_set_buf(M.winnr, bufnr)
  else
    -- horizontal split (default)
    vim.cmd("split")
    M.winnr = vim.api.nvim_get_current_win()
    vim.api.nvim_win_set_buf(M.winnr, bufnr)
    -- Resize to fit content, max 15 lines
    local height = math.min(#lines + 1, 15)
    vim.api.nvim_win_set_height(M.winnr, height)
  end

  -- Set window-local options
  vim.api.nvim_win_set_option(M.winnr, "number", false)
  vim.api.nvim_win_set_option(M.winnr, "relativenumber", false)
  vim.api.nvim_win_set_option(M.winnr, "wrap", false)
end

--- Append lines to the scratch buffer
---@param lines string[] Lines to append
function M.append(lines)
  local bufnr = get_or_create_buffer()

  vim.api.nvim_buf_set_option(bufnr, "modifiable", true)
  local line_count = vim.api.nvim_buf_line_count(bufnr)
  vim.api.nvim_buf_set_lines(bufnr, line_count, line_count, false, lines)
  vim.api.nvim_buf_set_option(bufnr, "modifiable", false)

  -- Scroll to bottom if window is open
  if M.winnr and vim.api.nvim_win_is_valid(M.winnr) then
    local new_count = vim.api.nvim_buf_line_count(bufnr)
    vim.api.nvim_win_set_cursor(M.winnr, { new_count, 0 })
  end
end

--- Clear the scratch buffer
function M.clear()
  if M.bufnr and vim.api.nvim_buf_is_valid(M.bufnr) then
    vim.api.nvim_buf_set_option(M.bufnr, "modifiable", true)
    vim.api.nvim_buf_set_lines(M.bufnr, 0, -1, false, {})
    vim.api.nvim_buf_set_option(M.bufnr, "modifiable", false)
  end
end

--- Close the display window
function M.close()
  if M.winnr and vim.api.nvim_win_is_valid(M.winnr) then
    vim.api.nvim_win_close(M.winnr, true)
    M.winnr = nil
  end
end

--- Check if display is visible
---@return boolean
function M.is_visible()
  return M.winnr ~= nil and vim.api.nvim_win_is_valid(M.winnr)
end

return M
