-- Command constants for cpu-simple
-- Centralizes all backend command strings for maintainability

local M = {}

-- Core CPU commands
M.RUN = "run"
M.STEP = "step"
M.RESET = "reset"
M.STATUS = "status"
M.DUMP = "dump"
M.LOAD = "load"
M.QUIT = "quit"
M.BREAK_TGL = "breakpoint toggle"
M.BREAK_CLR = "breakpoint clear"
M.BREAK_GET = "breakpoint list"
M.RUN_TO = "run to_address"

-- Future debugging commands (placeholders)
M.STEP_OVER = "stepover"
M.STEP_INTO = "stepinto"

return M
