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

-- Track last highlighted line to avoid redundant updates
local last_highlighted_line = nil

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
    -- Also highlight source and assembled panels based on PC
    vim.cmd("redrawstatus")
  end)
  events.on(events.BACKEND_STARTED, function()
    vim.cmd("redrawstatus")
  end)
  events.on(events.BACKEND_STOPPED, function()
    vim.cmd("redrawstatus")
  end)
  events.on(events.BREAKPOINT_UPDATED, function()
    M.highlight_breakpoints()
  end)
  
  -- Setup CursorMoved autocmd for assembled panel highlighting
  M.setup_cursor_highlight()
end

--- Setup CursorMoved autocmd to highlight assembled bytes for current source line
function M.setup_cursor_highlight()
  vim.api.nvim_create_autocmd("CursorMoved", {
    group = vim.api.nvim_create_augroup("CpuSimpleCursorHighlight", { clear = true }),
    callback = function()
      if not assembler then
        return
      end
      -- Get current line (0-based)
      local cursor_line = vim.api.nvim_win_get_cursor(0)[1] - 1
      
      -- Guard: skip if line hasn't changed
      if cursor_line == last_highlighted_line then
        return
      end
      last_highlighted_line = cursor_line
      
      local span = assembler.get_address_span_from_current_line()
      
      if span then
        display.assembled.highlight_byte_range(span.start_address, span.end_address)
      else
        display.assembled.clear_highlights()
      end
    end,
  })
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
  
  vim.api.nvim_create_user_command("CpuBreakToggle", function(cmd_opts)
    if (#cmd_opts.args == 0) then
      M.set_breakpoint_at_cursor()
      return
    end
    
    local address = tonumber(cmd_opts.args)
    if not address then
      vim.notify("Invalid address: " .. cmd_opts.args, vim.log.levels.ERROR)
      return
    end
    M.set_breakpoint(address)
  end, {
    desc = "Toggle breakpoint at the given address (or at cursor line if no address given)",
    nargs = "?", -- 0 or 1 argument
  })

  vim.api.nvim_create_user_command("CpuBreakClear", function()
    M.clear_all_breakpoints()
  end, {
    desc = "Clear all breakpoints",
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

--- Helper to ensure backend is running before executing a command
---@param fn function Function to execute if backend is running
---@return function Wrapped function
local function with_running_backend(fn)
  return function(...)
    if not backend then
      backend = require("cpu-simple.backend")
    end
    if not events then
      events = require("cpu-simple.events")
    end

    if not backend.is_running() then
      vim.notify("Backend is not running. Starting it.", vim.log.levels.INFO)
      events.on(events.BACKEND_STARTED, function(...)
        fn(...)
      end, { once = true })
      M.backend_start()
      return
    end
    
    return fn(...)
  end
end

--- Assemble the current buffer
M.assemble = with_running_backend(function()
  if not assembler then
    assembler = require("cpu-simple.assembler")
  end
  if not display then
    display = require("cpu-simple.display")
  end
  
  events.on(events.ASSEMBLED, function(data)
    -- Show assembled panel when assembly is done
    display.assembled.set_content(assembler.get_last_output_content())
    display.assembled.show()
  end, { once = true })
  assembler.assemble_current_buffer({
    assembler_path = M.config.assembler_path,
    assembler_options = M.config.assembler_options,
    cwd = M.config.cwd,
  })
end)

--- Load machine code into the CPU
---@param path string|nil Path to the binary file (defaults to last assembled)
M.load = with_running_backend(function(path)
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
end)

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

--- Set a breakpoint at the given address
---@param address number Address to set breakpoint at
M.set_breakpoint = with_running_backend(function(address)
  if not commands then
    commands = require("cpu-simple.commands")
  end
  backend.send(string.format("%s %d", commands.BREAK_TGL, address))
end)

--- Set a breakpoint at the address of the current cursor line
M.set_breakpoint_at_cursor = with_running_backend(function()
  -- Assembler is required to map source lines to addresses
  if not assembler then
    assembler = require("cpu-simple.assembler")
  end

  -- Get address span for current line
  local span = assembler.get_address_span_from_current_line()
  if not span then
    vim.notify("No debug info available to set breakpoint", vim.log.levels.ERROR)
    return
  end

  -- Set breakpoint at start address of span
  local address = span.start_address
  if not address then
    vim.notify("No address mapped to current line", vim.log.levels.ERROR)
    return
  end
  M.set_breakpoint(address)
end)

--- Clear all breakpoints
M.clear_all_breakpoints = with_running_backend(function()
  if not commands then
    commands = require("cpu-simple.commands")
  end
  backend.send(commands.BREAK_CLEAR_ALL)
end)

function M.highlight_breakpoints()
  if not assembler then
    assembler = require("cpu-simple.assembler")
  end
  if not state then
    state = require("cpu-simple.state")
  end
  if not display then
    display = require("cpu-simple.display")
  end

  if not assembler.has_debug_info() then
    vim.notify("No debug info available to highlight breakpoints", vim.log.levels.WARN)
    return
  end
  
  local bufnr = vim.api.nvim_get_current_buf()

  -- Clear existing highlights
  display.utils.clear_breakpoint_highlights(bufnr)
  if display.assembled.is_visible() then
    display.assembled.clear_breakpoint_highlights()
  end
  
  -- Highlight breakpoints in source and assembled panels
  for _, bp in ipairs(state.breakpoints) do
    local source_line = assembler.get_source_line_from_address(bp.address)
    if source_line then
      display.utils.highlight_breakpoint_line(bufnr, source_line)
    end
    if display.assembled.is_visible() then
      display.assembled.highlight_breakpoint(bp.address)
    end
  end
end

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
