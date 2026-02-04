-- Backend process manager for cpu-simple
-- Handles async communication with Backend.exe via STDIN/STDOUT/STDERR

local M = {}

local uv = vim.loop
local state = require("cpu-simple.state")
local events = require("cpu-simple.events")
local commands = require("cpu-simple.commands")

-- Process state
M.handle = nil
M.pid = nil
M.stdin = nil
M.stdout = nil
M.stderr = nil
M.running = false

-- Configuration (set by start())
M.config = {
  backend_path = "Backend.exe",
  memory_size = 256,
  stack_size = 16,
  registers = 4,
  cwd = nil,
}

--- Start the backend process
---@param config table Configuration options
---@return boolean success
function M.start(config)
  if M.running then
    vim.notify("Backend is already running", vim.log.levels.WARN)
    return false
  end

  M.config = vim.tbl_extend("force", M.config, config or {})

  -- Create pipes for stdio
  M.stdin = uv.new_pipe(false)
  M.stdout = uv.new_pipe(false)
  M.stderr = uv.new_pipe(false)

  local args = {
    "-m", tostring(M.config.memory_size),
    "-s", tostring(M.config.stack_size),
    "--registers", tostring(M.config.registers),
  }

  local spawn_opts = {
    args = args,
    stdio = { M.stdin, M.stdout, M.stderr },
    cwd = M.config.cwd or vim.fn.getcwd(),
  }

  M.handle, M.pid = uv.spawn(M.config.backend_path, spawn_opts, function(code, signal)
    M.on_exit(code, signal)
  end)

  if not M.handle then
    vim.schedule(function()
      vim.notify("Failed to start backend: " .. tostring(M.pid), vim.log.levels.ERROR)
    end)
    M.cleanup()
    return false
  end

  M.running = true

  -- Read stdout
  M.stdout:read_start(function(err, data)
    if err then
      vim.schedule(function()
        vim.notify("Backend stdout error: " .. err, vim.log.levels.ERROR)
      end)
      return
    end
    if data then
      M.on_stdout(data)
    end
  end)

  -- Read stderr (logging/errors from backend)
  M.stderr:read_start(function(err, data)
    if err then
      vim.schedule(function()
        vim.notify("Backend stderr error: " .. err, vim.log.levels.ERROR)
      end)
      return
    end
    if data then
      M.on_stderr(data)
    end
  end)

  vim.schedule(function()
    vim.notify("Backend started (PID: " .. M.pid .. ")", vim.log.levels.INFO)
    events.emit(events.BACKEND_STARTED, { pid = M.pid })
  end)

  return true
end

--- Send a command to the backend via STDIN
---@param cmd string Command to send (without newline)
function M.send(cmd)
  if not M.running or not M.stdin then
    vim.notify("Backend is not running", vim.log.levels.ERROR)
    return false
  end

  M.stdin:write(cmd .. "\n", function(err)
    if err then
      vim.schedule(function()
        vim.notify("Failed to send command: " .. err, vim.log.levels.ERROR)
      end)
    end
  end)

  return true
end

--- Stop the backend process
function M.stop()
  if not M.running then
    vim.notify("Backend is not running", vim.log.levels.WARN)
    return
  end

  -- Send quit command gracefully
  M.send(commands.QUIT)

  -- Give it a moment to exit gracefully, then force kill if needed
  vim.defer_fn(function()
    if M.handle and M.running then
      M.handle:kill("sigterm")
    end
  end, 500)
end

--- Handle stdout data from backend
---@param data string Data received
function M.on_stdout(data)
  vim.schedule(function()
    local lines = vim.split(data, "\n", { trimempty = true })
    for _, line in ipairs(lines) do
      if line ~= "" then
        M.parse_stdout(line)
      end
    end
  end)
end

--- Parse stdout line and update state/emit events
---@param data string Single line of output
function M.parse_stdout(data)
  -- Parse JSON response
  local ok, json = pcall(vim.json.decode, data)
  if not ok then
    vim.notify("[CPU] Invalid JSON: " .. data, vim.log.levels.WARN)
    return
  end

  -- Check for Type attribute and handle accordingly
  local msg_type = json.type
  if not msg_type then
    vim.notify("[CPU] Missing type attribute in JSON", vim.log.levels.WARN)
    return
  end

  if msg_type == "status" then
    state.update_status(json)
    events.emit(events.STATUS_UPDATED, state.status)
  elseif msg_type == "stack_dump" then
    state.update_stack(json)
    events.emit(events.STACK_UPDATED, state.stack)
  elseif msg_type == "memory_dump" then
    state.update_memory(json)
    events.emit(events.MEMORY_UPDATED, state.memory)
  elseif msg_type == "breakpoint_list" then
    state.set_breakpoints(json)
    events.emit(events.BREAKPOINT_UPDATED, {})
  elseif msg_type == "breakpoint_hit" then
    vim.notify("Breakpoint hit!", vim.log.levels.INFO)
    events.emit(events.BREAKPOINT_HIT, {address = json.address})
  else
    -- Fallback for unknown types
    vim.notify("[CPU] Unknown message type: " .. msg_type, vim.log.levels.INFO)
  end
end

--- Handle stderr data from backend (logs/errors)
---@param data string Data received
function M.on_stderr(data)
  vim.schedule(function()
    local lines = vim.split(data, "\n", { trimempty = true })
    for _, line in ipairs(lines) do
      if line ~= "" then
        -- Parse log level from backend format: [ERROR] only for now
        local level = vim.log.levels.DEBUG
        if line:match("^%[ERROR%]") then
          level = vim.log.levels.ERROR
        else
          level = vim.log.levels.DEBUG
          line = "[LOG] " .. line
        end
        vim.notify(line, level)
      end
    end
  end)
end

--- Handle process exit
---@param code number Exit code
---@param signal number Signal that caused exit
function M.on_exit(code, signal)
  vim.schedule(function()
    M.running = false
    M.cleanup()
    state.clear()
    events.emit(events.BACKEND_STOPPED, { code = code, signal = signal })

    if code == 0 then
      vim.notify("Backend exited", vim.log.levels.INFO)
    else
      vim.notify("Backend exited with code " .. code .. " (signal: " .. signal .. ")", vim.log.levels.WARN)
    end
  end)
end

--- Cleanup process handles
function M.cleanup()
  if M.stdin then
    M.stdin:close()
    M.stdin = nil
  end
  if M.stdout then
    M.stdout:close()
    M.stdout = nil
  end
  if M.stderr then
    M.stderr:close()
    M.stderr = nil
  end
  if M.handle then
    M.handle:close()
    M.handle = nil
  end
  M.pid = nil
  M.running = false
end

--- Check if backend is running
---@return boolean
function M.is_running()
  return M.running
end

return M