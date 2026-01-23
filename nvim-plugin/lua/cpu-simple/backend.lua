-- Backend process manager for cpu-simple
-- Handles async communication with Backend.exe via STDIN/STDOUT/STDERR

local M = {}

local uv = vim.loop

-- Process state
M.handle = nil
M.pid = nil
M.stdin = nil
M.stdout = nil
M.stderr = nil
M.running = false
M.status = nil
M.stack = nil
M.memory = nil

-- Configuration (set by init.lua)
M.config = {
  backend_path = "Backend.exe",
  memory_size = 256,
  stack_size = 16,
  registers = 4,
  cwd = nil,
  update_statusline = nil,
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
    if (M.config.update_statusline) then
      M.config.update_statusline()
    end
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
  M.send("quit")

  -- Give it a moment to exit gracefully, then force kill if needed
  vim.defer_fn(function()
    if M.handle and M.running then
      M.handle:kill("sigterm")
    end
    if (M.config.update_statusline) then
      M.config.update_statusline()
    end
  end, 500)
end

--- Handle stdout data from backend
---@param data string Data received
function M.on_stdout(data)
  vim.schedule(function()
    -- For now, display output via notify
    -- Future: parse and route to display buffer
    local lines = vim.split(data, "\n", { trimempty = true })
    for _, line in ipairs(lines) do
      if line ~= "" then
        M.parse_stdout(line)
      end
    end
  end)
end

function M.parse_stdout(data)
    -- if status line, parse and return table
    -- else display output via notify
    if data:match("^%[STATUS%]") then
        M.set_cpu_status(data)
    elseif data:match("^%[STACK%]") then
        M.set_cpu_stack(data)
    elseif data:match("^%[MEMORY%]") then
        M.set_cpu_memory(data)
    else
        vim.schedule(function()
            vim.notify("[CPU] " .. data, vim.log.levels.INFO)
        end)
    end
end

--- Handle stderr data from backend (logs/errors)
---@param data string Data received
function M.on_stderr(data)
  vim.schedule(function()
    local lines = vim.split(data, "\n", { trimempty = true })
    for _, line in ipairs(lines) do
      if line ~= "" then
        -- Parse log level from backend format: [LOG] or [ERROR]
        local level = vim.log.levels.DEBUG
        if line:match("^%[ERROR%]") then
          level = vim.log.levels.ERROR
        elseif line:match("^%[LOG%]") then
          level = vim.log.levels.DEBUG
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

function M.set_cpu_status(status_line)
    -- status_line = "[STATUS] cycles pc sp r0 r1 r2 r3 zero carry"
    local status = status_line:gsub("^%[STATUS%]%s*", "")
    local cycles, pc, sp, r0, r1, r2, r3, zero, carry = status:match("^(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)%s+(%d+)")
    M.status = {
        cycles = tonumber(cycles),
        pc = tonumber(pc),
        sp = tonumber(sp),
        registers = {
            tonumber(r0),
            tonumber(r1),
            tonumber(r2),
            tonumber(r3),
        },
        flags = {
            zero = tonumber(zero),
            carry = tonumber(carry),
        }
    }

    vim.schedule(function()
        if M.config.update_statusline then
            M.config.update_statusline()
        end
    end)
end

function M.set_cpu_stack(stack_line)
    local stack = stack_line:gsub("^%[STACK%]%s*", "")
    local stack_values = {}
    for value in stack:gmatch("(%d+)") do
        table.insert(stack_values, tonumber(value))
    end
    M.stack = stack_values
end

function M.set_cpu_memory(memory_line)
    local memory = memory_line:gsub("^%[MEMORY%]%s*", "")
    local memory_values = {}
    for value in memory:gmatch("(%d+)") do
        table.insert(memory_values, tonumber(value))
    end
    M.memory = memory_values
end

return M