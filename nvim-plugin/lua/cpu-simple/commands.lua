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

-- Future debugging commands (placeholders)
M.BREAK = "break"
M.CLEAR_BREAK = "clear"
M.CONTINUE = "continue"
M.STEP_OVER = "stepover"
M.STEP_INTO = "stepinto"

return M
