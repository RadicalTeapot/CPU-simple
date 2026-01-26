-- cpu-simple: Neovim plugin for the CPU-Simple emulator
-- Entry point and command implementations

local M = {}

-- Module references (lazy-loaded)
local backend = nil
local assembler = nil
local display = nil
local state = nil
local events = nil
local commands = nil

-- Default configuration
M.defaults = {
  -- Path to Backend executable
  backend_path = "Backend.exe",
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
  -- Sidebar configuration
  sidebar = {
    width = 0.5, -- Ratio of editor width (0.5 = half), or absolute columns if > 1
    position = "right", -- "left" or "right"
    panels = {}, -- Panel-specific config: { [panel_id] = { height = 0 } }
  },
}

-- Current configuration
M.config = {}

--- Setup the plugin with user configuration
---@param opts table|nil User configuration options
function M.setup(opts)
  M.config = vim.tbl_deep_extend("force", M.defaults, opts or {})

  -- Load submodules
  backend = require("cpu-simple.backend")
  assembler = require("cpu-simple.assembler")
  display = require("cpu-simple.display")
  state = require("cpu-simple.state")
  events = require("cpu-simple.events")
  commands = require("cpu-simple.commands")

  -- Setup sidebar with configuration
  display.setup(M.config.sidebar)

  -- Register commands
  M.register_commands()

  -- Subscribe to status updates for statusline
  events.on(events.STATUS_UPDATED, function()
    vim.cmd("redrawstatus")
  end)
  events.on(events.BACKEND_STARTED, function()
    vim.cmd("redrawstatus")
  end)
  events.on(events.BACKEND_STOPPED, function()
    vim.cmd("redrawstatus")
  end)
end

--- Register user commands
function M.register_commands()
  vim.api.nvim_create_user_command("CpuBackendStart", function()
    M.backend_start()
  end, {
    desc = "Start the CPU backend process",
  })

  vim.api.nvim_create_user_command("CpuBackendStop", function()
    M.backend_stop()
  end, {
    desc = "Stop the CPU backend process",
  })

  vim.api.nvim_create_user_command("CpuBackendStatus", function() 
    M.status()
  end, {
    desc = "Get the CPU backend status",
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

  vim.api.nvim_create_user_command("CpuStatus", function()
    M.status()
  end, {
    desc = "Get the current CPU status",
  })

  vim.api.nvim_create_user_command("CpuDump", function()
    M.dump()
  end, {
    desc = "Get full CPU dump",
  })

  -- Panel toggle commands
  vim.api.nvim_create_user_command("CpuToggleDump", function()
    if not display then
      display = require("cpu-simple.display")
    end
    display.toggle_dump()
  end, {
    desc = "Toggle the CPU dump panel",
  })

  vim.api.nvim_create_user_command("CpuToggleAssembled", function()
    if not display then
      display = require("cpu-simple.display")
    end
    display.toggle_assembled()
  end, {
    desc = "Toggle the assembled code panel",
  })

  vim.api.nvim_create_user_command("CpuOpenSidebar", function()
    if not display then
      display = require("cpu-simple.display")
    end
    display.open_sidebar()
  end, {
    desc = "Open the sidebar",
  })

  vim.api.nvim_create_user_command("CpuCloseSidebar", function()
    if not display then
      display = require("cpu-simple.display")
    end
    display.close_sidebar()
  end, {
    desc = "Close all sidebar panels",
  })
end

--- Start the backend process
function M.backend_start()
  if not backend then
    backend = require("cpu-simple.backend")
  end

  backend.start({
    backend_path = M.config.backend_path,
    memory_size = M.config.memory_size,
    stack_size = M.config.stack_size,
    registers = M.config.registers,
    cwd = M.config.cwd,
  })
end

--- Report backend status
function M.backend_status()
  if not backend then
    backend = require("cpu-simple.backend")
  end

  if backend.is_running() then
    vim.notify("CPU backend is running", vim.log.levels.INFO)
  else
    vim.notify("CPU backend is not running", vim.log.levels.WARN)
  end
end

--- Stop the backend process
function M.backend_stop()
  if not backend then
    vim.notify("Backend module not loaded", vim.log.levels.ERROR)
    return
  end

  backend.stop()
end

--- Assemble the current buffer
function M.assemble()
  if not assembler then
    assembler = require("cpu-simple.assembler")
  end
  if not display then
    display = require("cpu-simple.display")
  end

  assembler.assemble_buffer({
    assembler_path = M.config.assembler_path,
    assembler_options = M.config.assembler_options,
    cwd = M.config.cwd,
  }, function(success, output_path, debug_file_path, err_msg)
    if success then
      -- Read output path as binary and display in assembled panel
      display.assembled.set_content(assembler.get_last_output_content())
      display.assembled.show()
    end
  end)
end

--- Load machine code into the CPU
---@param path string|nil Path to the binary file (defaults to last assembled)
function M.load(path)
  if not backend then
    backend = require("cpu-simple.backend")
  end
  if not commands then
    commands = require("cpu-simple.commands")
  end

  if not backend.is_running() then
    vim.notify("Backend is not running. Starting it.", vim.log.levels.INFO)
    M.backend_start()
  end

  -- Use provided path or fall back to last assembled
  local file_path = path
  if not file_path then
    if not assembler then
      assembler = require("cpu-simple.assembler")
    end
    file_path = assembler.get_last_output_path()
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
  backend.send(commands.LOAD .. " " .. file_path)
end

--- Helper to ensure backend is running before executing a command
---@param fn function Function to execute if backend is running
---@return function Wrapped function
local function with_running_backend(fn)
  return function(...)
    if not backend then
      backend = require("cpu-simple.backend")
    end
    if not backend.is_running() then
      vim.notify("Backend is not running. Start it with :CpuBackendStart", vim.log.levels.ERROR)
      return
    end
    return fn(...)
  end
end

--- Run the loaded program
M.run = with_running_backend(function()
  if not commands then
    commands = require("cpu-simple.commands")
  end
  backend.send(commands.RUN)
end)

--- Execute one CPU instruction
M.step = with_running_backend(function()
  if not commands then
    commands = require("cpu-simple.commands")
  end
  backend.send(commands.STEP)
end)

--- Reset the CPU
M.reset = with_running_backend(function()
  if not commands then
    commands = require("cpu-simple.commands")
  end
  backend.send(commands.RESET)
end)

--- Get the CPU status
M.status = with_running_backend(function()
  if not commands then
    commands = require("cpu-simple.commands")
  end
  backend.send(commands.STATUS)
end)

--- Get full CPU dump
M.dump = with_running_backend(function()
  if not commands then
    commands = require("cpu-simple.commands")
  end
  if not state then
    state = require("cpu-simple.state")
  end
  if not display then
    display = require("cpu-simple.display")
  end

  backend.send(commands.DUMP)

  -- Display dump in floating window after a short delay to allow response
  -- TODO Use event/callback when dump response is received instead of delay
  vim.defer_fn(function()
    local lines = {}

    if state.status then
      table.insert(lines, "=== CPU STATUS ===")
      table.insert(lines, string.format("Cycles: %d  PC: %d  SP: %d",
        state.status.cycles, state.status.pc, state.status.sp))
      table.insert(lines, string.format("Flags - Z: %d  C: %d",
        state.status.flags.zero, state.status.flags.carry))
      table.insert(lines, string.format("R0: %d  R1: %d  R2: %d  R3: %d",
        state.status.registers[1], state.status.registers[2],
        state.status.registers[3], state.status.registers[4]))
      if state.status.memory_changes then
        table.insert(lines, "Memory Changes:")
        for addr, val in pairs(state.status.memory_changes) do
          table.insert(lines, string.format("  [%d] = %d", addr, val))
        end
      end
      if state.status.stack_changes then
        table.insert(lines, "Stack Changes:")
        for addr, val in pairs(state.status.stack_changes) do
          table.insert(lines, string.format("  [%d] = %d", addr, val))
        end
      end
      table.insert(lines, "")
    end

    if state.stack then
      table.insert(lines, "=== STACK ===")
      for i, val in ipairs(state.stack) do
        table.insert(lines, string.format("[%d]: %d", i - 1, val))
      end
      table.insert(lines, "")
    end

    if state.memory then
      table.insert(lines, "=== MEMORY ===")
      for addr, val in ipairs(state.memory) do
        table.insert(lines, string.format("[%d]: %d", addr - 1, val))
      end
    end

    if #lines == 0 then
      table.insert(lines, "No CPU state available. Run a program first.")
    end

    display.dump.set_content(lines)
  end, 100)
end)

--- Send a raw command to the backend
---@param cmd string Command to send
M.send = with_running_backend(function(cmd)
  backend.send(cmd)
end)

--- Check if the backend is running
---@return boolean
function M.is_running()
  if not backend then
    return false
  end
  return backend.is_running()
end

--- Get formatted status line for display
---@return string Status line text
function M.get_statusline()
  if not backend then
    return "Backend not loaded"
  end
  if not state then
    state = require("cpu-simple.state")
  end

  if not backend.is_running() then
    return "Backend stopped"
  end

  local s = state.status
  if not s then
    return "Backend running: no status"
  end

  return string.format("Cyc:%d PC:%d SP:%d Z:%d C:%d R0:%d R1:%d R2:%d R3:%d",
    s.cycles, s.pc, s.sp, s.flags.zero, s.flags.carry,
    s.registers[1], s.registers[2], s.registers[3], s.registers[4])
end

return M
