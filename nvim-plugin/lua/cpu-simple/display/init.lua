-- Display module for cpu-simple

local M = {}

-- Core sidebar manager
M.sidebar = require("cpu-simple.display.sidebar")

-- Panel modules
M.assembled = require("cpu-simple.display.assembled")
M.dump = require("cpu-simple.display.dump")
M.utils = require("cpu-simple.display.utils")

--- Setup the display module with configuration
---@param opts table|nil Configuration options
---  - width: number (0-1 for ratio, >1 for absolute columns, default 0.5)
---  - position: "left"|"right" (default "right")
---  - panels: table of panel-specific config { [panel_id] = { height = number } }
function M.setup(opts)
  M.sidebar.setup(opts)
end

--- Show the assembled panel in the sidebar
function M.show_assembled()
  M.assembled.show()
end

--- Show the dump panel in the sidebar
function M.show_dump()
  M.dump.show()
end

--- Toggle the assembled panel
function M.toggle_assembled()
  M.assembled.toggle()
end

--- Toggle the dump panel
function M.toggle_dump()
  M.dump.toggle()
end

-- Open the sidebar
function M.open_sidebar()
  M.sidebar.open_sidebar()
end

--- Close all panels and the sidebar
function M.close_sidebar()
  M.sidebar.close_sidebar()
end

return M