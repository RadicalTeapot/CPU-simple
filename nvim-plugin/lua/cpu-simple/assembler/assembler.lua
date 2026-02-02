-- Assembler module for cpu-simple
-- Invokes the Assembler executable to compile assembly to machine code

local M = {}

local uv = vim.loop

local events = require("cpu-simple.events")

-- Configuration (set by init.lua)
M.config = {
  assembler_path = "Assembler.exe",
  assembler_options = {
    emit_debug = true,
  },
  cwd = nil,
}

-- Last assembled output path (for CpuLoad default)
M.last_output_path = nil
M.last_output_content = {}

-- Debug info from the .dbg file (spans and symbols)
M.last_debug_info = nil

-- Source buffer info for matching cursor events
M.last_source_bufnr = nil
M.last_source_path = nil

local function store_last_output(path)
  M.last_output_path = path
  M.last_output_content = {}

  -- Read binary content and store as hex lines
  local file = io.open(path, "rb")
  if file then
    local content = file:read("*a")
    file:close()
    -- Convert binary content to hex representation
    local hex_lines = {}
    for i = 1, #content do
      local byte = string.byte(content, i)
      table.insert(hex_lines, string.format("%02X", byte))
    end
    -- Group hex bytes into lines of 16 bytes
    local formatted_lines = {}
    for i = 1, #hex_lines, 16 do
      local line = table.concat(vim.list_slice(hex_lines, i, i + 15), " ")
      table.insert(formatted_lines, line)
    end
    M.last_output_content = formatted_lines
  end
end

--- Assemble the current buffer to machine code
---@param config table Configuration options
---@param bufnr number|nil Buffer number to assemble
function M.assemble_buffer(config, bufnr)
  M.config = vim.tbl_extend("force", M.config, config or {})

  -- Get buffer content
  local lines = vim.api.nvim_buf_get_lines(bufnr, 0, -1, false)
  local content = table.concat(lines, "\n")

  -- Store source buffer info for cursor-based highlighting
  M.last_source_bufnr = bufnr
  M.last_source_path = vim.api.nvim_buf_get_name(bufnr)

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
  local debug_file_path = nil
  if M.config.assembler_options.emit_debug then
    debug_file_path = temp_dir .. "/" .. basename .. ".dbg"
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
        vim.notify("Assembled successfully: " .. output_path, vim.log.levels.INFO)
        store_last_output(output_path)
        -- Load debug info if available
        if debug_file_path then
          local ok, debug_info = pcall(dofile, debug_file_path)
          if ok and debug_info then
            M.last_debug_info = debug_info
          else
            M.last_debug_info = nil
          end
        else
          M.last_debug_info = nil
        end
        events.emit(events.ASSEMBLED, {
          source_bufnr = bufnr,
          output_path = output_path,
          debug_path = debug_file_path,
        })
      else
        local err_msg = table.concat(stderr_output, "")
        if err_msg == "" then
          err_msg = "Assembler exited with code " .. code
        end
        vim.notify("Assembly failed: " .. err_msg, vim.log.levels.ERROR)
      end
    end)
  end)

  if not handle then
    vim.notify("Failed to start assembler: " .. tostring(pid), vim.log.levels.ERROR)
    stdout:close()
    stderr:close()
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

return M
