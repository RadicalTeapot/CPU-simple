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

-- Stepping commands
M.STEP_OVER = "stepover"
M.STEP_OUT = "stepout"
M.TICK = "tick"

-- Watchpoint commands
M.WATCH_WRITE = "watchpoint on-write"
M.WATCH_READ = "watchpoint on-read"
M.WATCH_PHASE = "watchpoint on-phase"
M.WATCH_REMOVE = "watchpoint remove"
M.WATCH_CLR = "watchpoint clear"
M.WATCH_LIST = "watchpoint list"

return M
