-- Display module for cpu-simple

local M = {}

-- Core sidebar manager
M.sidebar = require("cpu-simple.display.sidebar")

-- Panel modules
M.assembled = require("cpu-simple.display.assembled")
M.status_panel = require("cpu-simple.display.status")
M.stack = require("cpu-simple.display.stack")
M.memory = require("cpu-simple.display.memory")
M.utils = require("cpu-simple.display.utils")

-- Highlighting modules
M.highlights = require("cpu-simple.display.highlights")
M.signs = require("cpu-simple.display.signs")

-- Configuration reference (set by setup)
local config = nil

--- Setup the display module with configuration
---@param opts table|nil Configuration options
---  - width: number (0-1 for ratio, >1 for absolute columns, default 0.5)
---  - position: "left"|"right" (default "right")
---  - panels: table of panel-specific config { [panel_id] = { height = number } }
---  - signs: table of signs configuration
function M.setup(opts)
  config = opts or {}
  M.sidebar.setup(config.sidebar)
  M.highlights.define_highlight_groups()
  M.signs.setup(config.signs)

  -- Forward panel-specific config
  local panels = config.sidebar and config.sidebar.panels or {}
  if panels.stack then
    M.stack.setup(panels.stack)
  end
  if panels.memory then
    M.memory.setup(panels.memory)
  end
end

--- Show the assembled panel in the sidebar
function M.show_assembled()
  M.assembled.show()
end

--- Toggle the assembled panel
function M.toggle_assembled()
  M.assembled.toggle()
end

--- Toggle the status panel
function M.toggle_status()
  M.status_panel.toggle()
end

--- Toggle the stack panel
function M.toggle_stack()
  M.stack.toggle()
end

--- Toggle the memory panel
function M.toggle_memory()
  M.memory.toggle()
end

-- Open the sidebar
function M.open_sidebar()
  M.sidebar.open_sidebar()
end

--- Close all panels and the sidebar
function M.close_sidebar()
  M.sidebar.close_sidebar()
end

-- ============================================================================
-- Highlighting Functions
-- ============================================================================

--- Highlight all breakpoints in source and assembled buffers
---@param breakpoints table[] Array of breakpoints with .address field
---@param get_source_line_fn function(address) Function to get source line from address
function M.highlight_breakpoints(breakpoints, get_source_line_fn)
  local bufnr = vim.api.nvim_get_current_buf()
  local assembled_bufnr = M.assembled.is_visible() and M.assembled.get_buffer() or nil
  local use_signs = config and config.signs and config.signs.use_for_breakpoints
  
  -- Clear existing breakpoint highlights/signs
  if use_signs then
    M.signs.clear_breakpoint_signs(bufnr)
  else
    M.highlights.clear_breakpoint_highlights(bufnr)
  end
  if assembled_bufnr then
    M.highlights.clear_assembled_breakpoint_highlights(assembled_bufnr)
  end
  
  -- Highlight each breakpoint
  for _, bp in ipairs(breakpoints) do
    local source_line = get_source_line_fn(bp.address)
    if source_line then
      if use_signs then
        M.signs.place_breakpoint_sign(bufnr, source_line)
      else
        M.highlights.highlight_breakpoint_line(bufnr, source_line)
      end
    end
    if assembled_bufnr then
      M.highlights.highlight_assembled_breakpoint(assembled_bufnr, bp.address)
    end
  end
end

--- Highlight the program counter in source buffer
---@param pc_address number|nil Current program counter address
---@param get_source_line_fn function(address) Function to get source line from address
function M.highlight_pc(pc_address, get_source_line_fn)
  local bufnr = vim.api.nvim_get_current_buf()
  local use_signs = config and config.signs and config.signs.use_for_pc
  
  -- Clear existing PC highlight/sign
  if use_signs then
    M.signs.clear_pc_sign(bufnr)
  else
    M.highlights.clear_pc_highlight(bufnr)
  end
  
  if pc_address then
    local source_line = get_source_line_fn(pc_address)
    if source_line then
      if use_signs then
        M.signs.place_pc_sign(bufnr, source_line)
      else
        M.highlights.highlight_pc_line(bufnr, source_line)
      end
    end
  end
end

return M