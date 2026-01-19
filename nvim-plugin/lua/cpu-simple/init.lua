-- cpu-simple: Neovim plugin for the CPU-Simple emulator
-- Entry point and command implementations

local M = {}

-- Default configuration
M.defaults = {
  -- Path to Backend executable
  backend_path = ".Backend.exe",
  -- Path to Assembler executable
  assembler_path = "Assembler.exe",
  -- Assembler options
  assembler_options = {
    emit_debug = false,
  },
  -- CPU configuration
  memory_size = 256,
  stack_size = 16,
  registers = 4,
  -- Working directory (defaults to cwd)
  cwd = nil,
}

-- Current configuration
M.config = {}

-- Module references
M.backend = nil
M.assembler = nil
M.display = nil
M.cpu = nil

--- Setup the plugin with user configuration
---@param opts table|nil User configuration options
function M.setup(opts)
  M.config = vim.tbl_deep_extend("force", M.defaults, opts or {})

  -- Load submodules
  M.backend = require("cpu-simple.backend")
  M.assembler = require("cpu-simple.assembler")
  M.display = require("cpu-simple.display")
  M.cpu = require("cpu-simple.cpu")

  -- Register commands
  M.register_commands()
end

--- Register user commands
function M.register_commands()
  vim.api.nvim_create_user_command("CpuStart", function()
    M.start()
  end, {
    desc = "Start the CPU backend process",
  })

  vim.api.nvim_create_user_command("CpuStop", function()
    M.stop()
  end, {
    desc = "Stop the CPU backend process",
  })

  vim.api.nvim_create_user_command("CpuAssemble", function()
    M.assemble()
  end, {
    desc = "Assemble the current buffer to machine code",
  })

  vim.api.nvim_create_user_command("CpuLoad", function(cmd_opts)
    M.load(cmd_opts.args ~= "" and cmd_opts.args or nil)
  end, {
    desc = "Load machine code into the CPU",
    nargs = "?",
    complete = "file",
  })

  vim.api.nvim_create_user_command("CpuRun", function()
    M.run()
  end, {
    desc = "Run the loaded program",
  })

  vim.api.nvim_create_user_command("CpuStep", function()
    M.step()
  end, {
    desc = "Execute one CPU instruction",
  })

  vim.api.nvim_create_user_command("CpuReset", function()
    M.reset()
  end, {
    desc = "Reset the CPU",
  })
end

--- Start the backend process
function M.start()
  if not M.backend then
    M.backend = require("cpu-simple.backend")
  end

  M.backend.start({
    backend_path = M.config.backend_path,
    memory_size = M.config.memory_size,
    stack_size = M.config.stack_size,
    registers = M.config.registers,
    cwd = M.config.cwd,
  })
end

--- Stop the backend process
function M.stop()
  if not M.backend then
    vim.notify("Backend module not loaded", vim.log.levels.ERROR)
    return
  end

  M.backend.stop()
end

--- Assemble the current buffer
function M.assemble()
  if not M.assembler then
    M.assembler = require("cpu-simple.assembler")
  end

  M.assembler.assemble_buffer({
    assembler_path = M.config.assembler_path,
    assembler_options = M.config.assembler_options,
    cwd = M.config.cwd,
  })
end

--- Load machine code into the CPU
---@param path string|nil Path to the binary file (defaults to last assembled)
function M.load(path)
  if not M.backend then
    M.backend = require("cpu-simple.backend")
  end

  if not M.backend.is_running() then
    vim.notify("Backend is not running. Starting it.", vim.log.levels.INFO)
    M.start()
  end

  -- Use provided path or fall back to last assembled
  local file_path = path
  if not file_path then
    if not M.assembler then
      M.assembler = require("cpu-simple.assembler")
    end
    file_path = M.assembler.get_last_output()
  end

  if not file_path then
    vim.notify("No file to load. Provide a path or run :CpuAssemble first", vim.log.levels.ERROR)
    return
  end

  if vim.fn.filereadable(file_path) == 0 then
    vim.notify("File not found: " .. file_path, vim.log.levels.ERROR)
    return
  end

  -- Convert to absolute path
  file_path = vim.fn.fnamemodify(file_path, ":p")

  -- Send load command to backend
  M.backend.send("load " .. file_path)
end

--- Run the loaded program
function M.run()
  if not M.cpu then
    M.cpu = require("cpu-simple.cpu")
  end

  M.cpu.run()
end

--- Execute one CPU instruction
function M.step()
    if not M.cpu then
        M.cpu = require("cpu-simple.cpu")
    end
    
    M.cpu.step()
end

--- Reset the CPU
function M.reset()
    if not M.cpu then
        M.cpu = require("cpu-simple.cpu")
    end
    
    M.cpu.reset()
end

--- Send a raw command to the backend
---@param cmd string Command to send
function M.send(cmd)
  if not M.backend then
    M.backend = require("cpu-simple.backend")
  end

  if not M.backend.is_running() then
    vim.notify("Backend is not running. Start it with :CpuStart", vim.log.levels.ERROR)
    return
  end

  M.backend.send(cmd)
end

--- Check if the backend is running
---@return boolean
function M.is_running()
  if not M.backend then
    return false
  end
  return M.backend.is_running()
end

return M
