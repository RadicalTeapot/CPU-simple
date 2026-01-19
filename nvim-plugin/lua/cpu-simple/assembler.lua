-- Assembler module for cpu-simple
-- Invokes the Assembler executable to compile assembly to machine code

local M = {}

local uv = vim.loop

-- Configuration (set by init.lua)
M.config = {
  assembler_path = "Assembler.exe",
  assembler_options = {
    emit_debug = false,
  },
  cwd = nil,
}

-- Last assembled output path (for CpuLoad default)
M.last_output = nil

--- Assemble the current buffer to machine code
---@param config table Configuration options
---@param callback function|nil Optional callback(success, output_path, error_msg)
function M.assemble_buffer(config, callback)
  M.config = vim.tbl_extend("force", M.config, config or {})

  -- Get current buffer content
  local bufnr = vim.api.nvim_get_current_buf()
  local lines = vim.api.nvim_buf_get_lines(bufnr, 0, -1, false)
  local content = table.concat(lines, "\n")

  -- Get buffer name for temp file naming
  local bufname = vim.api.nvim_buf_get_name(bufnr)
  local basename = vim.fn.fnamemodify(bufname, ":t:r")
  if basename == "" then
    basename = "untitled"
  end

  -- Create temp directory if needed
  local temp_dir = vim.fn.stdpath("cache") .. "/cpu-simple"
  vim.fn.mkdir(temp_dir, "p")

  -- Temp file paths
  local input_path = temp_dir .. "/" .. basename .. ".csasm" -- csasm -> cpu-simple assembly
  local output_path = temp_dir .. "/" .. basename .. ".bin"

  -- Write buffer content to temp file
  local input_file = io.open(input_path, "w")
  if not input_file then
    local msg = "Failed to create temp file: " .. input_path
    vim.notify(msg, vim.log.levels.ERROR)
    if callback then callback(false, nil, msg) end
    return
  end
  input_file:write(content)
  input_file:close()

  -- Spawn assembler process
  local stdout = uv.new_pipe(false)
  local stderr = uv.new_pipe(false)

  local stderr_output = {}
  local stdout_output = {}

  local args = { input_path, "-o", output_path }
  if M.config.assembler_options.emit_debug then
    local debug_file_path = temp_dir .. "/" .. basename .. ".dbg"
    table.insert(args, "-d")
    table.insert(args, debug_file_path)
  end

  local spawn_opts = {
    args = args,
    stdio = { nil, stdout, stderr },
    cwd = M.config.cwd or vim.fn.getcwd(),
  }

  local handle, pid = uv.spawn(M.config.assembler_path, spawn_opts, function(code, signal)
    -- Close handles
    stdout:close()
    stderr:close()

    vim.schedule(function()
      if code == 0 then
        M.last_output = output_path
        vim.notify("Assembled successfully: " .. output_path, vim.log.levels.INFO)
        if callback then callback(true, output_path, nil) end
      else
        local err_msg = table.concat(stderr_output, "")
        if err_msg == "" then
          err_msg = "Assembler exited with code " .. code
        end
        vim.notify("Assembly failed: " .. err_msg, vim.log.levels.ERROR)
        if callback then callback(false, nil, err_msg) end
      end
    end)
  end)

  if not handle then
    vim.notify("Failed to start assembler: " .. tostring(pid), vim.log.levels.ERROR)
    stdout:close()
    stderr:close()
    if callback then callback(false, nil, "Failed to start assembler") end
    return
  end

  -- Collect stdout
  stdout:read_start(function(err, data)
    if data then
      table.insert(stdout_output, data)
    end
  end)

  -- Collect stderr
  stderr:read_start(function(err, data)
    if data then
      table.insert(stderr_output, data)
    end
  end)
end

--- Get the last assembled output path
---@return string|nil
function M.get_last_output()
  return M.last_output
end

return M
