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
  -- Path to Language Server executable (nil to disable LSP)
  lsp_path = nil,
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
    panels = { -- Panel-specific config: { [panel_id] = { height = 0 } }
      stack = { bytes_per_line = 16 },
      memory = { bytes_per_line = 16 },
    },
  },
  -- Signs configuration (alternative to line highlighting)
  signs = {
    use_for_breakpoints = false, -- Use sign column for breakpoints instead of line highlight
    use_for_pc = false, -- Use sign column for PC instead of line highlight
    breakpoint_text = "●", -- Text shown in sign column for breakpoints
    pc_text = "▶", -- Text shown in sign column for PC
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
  
  -- Setup display with sidebar and signs configuration
  display.setup({
    sidebar = M.config.sidebar,
    signs = M.config.signs,
  })
  
  -- Register commands
  M.register_commands()
  
  -- Subscribe to status updates for statusline and highlighting
  events.on(events.STATUS_UPDATED, function()
    M.highlight_pc()
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

  -- Auto-request dump when stack/memory panels are visible but have no data
  events.on(events.STATUS_UPDATED, function()
    if not display then
      display = require("cpu-simple.display")
    end
    local needs_dump = (display.stack.is_visible() and not state.stack)
      or (display.memory.is_visible() and not state.memory)
    if needs_dump and backend and backend.is_running() then
      backend.send(commands.DUMP)
    end
  end)

  M.set_keymaps()

  -- Setup CursorMoved autocmd for assembled panel highlighting
  M.setup_cursor_highlight()

  -- Start LSP if configured
  M.start_lsp()
end

function M.set_keymaps()
  -- Buffer-local keymaps for .csasm files
  vim.api.nvim_create_autocmd("FileType", {
    pattern = "csasm",
    callback = function(args)
      local opts = { buffer = args.buf, noremap = true }
      -- Movement
      vim.keymap.set("n", "]b", function() M.goto_next_breakpoint() end, vim.tbl_extend("force", opts, { desc = "Next breakpoint" }))
      vim.keymap.set("n", "[b", function() M.goto_prev_breakpoint() end, vim.tbl_extend("force", opts, { desc = "Previous breakpoint" }))
      vim.keymap.set("n", "gd", function() M.goto_definition() end, vim.tbl_extend("force", opts, { desc = "Go to symbol definition" }))
      vim.keymap.set("n", "]p", function() M.goto_PC() end, vim.tbl_extend("force", opts, { desc = "Go to PC" }))

      -- Start/stop backend
      vim.keymap.set("n", "<leader>cs", "<cmd>CpuBackendStart<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Start backend" }))
      vim.keymap.set("n", "<leader>cq", "<cmd>CpuBackendStop<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Stop backend" }))

      -- Assemble and load
      vim.keymap.set("n", "<leader>ca", "<cmd>CpuAssemble<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Assemble" }))
      vim.keymap.set("n", "<leader>cl", "<cmd>CpuLoad<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Load" }))

      -- Execution control
      vim.keymap.set("n", "<leader>cr", "<cmd>CpuRun<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Run" }))
      vim.keymap.set("n", "<leader>cn", "<cmd>CpuStep<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Step" }))
      vim.keymap.set("n", "<leader>cR", "<cmd>CpuReset<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Reset" }))

      -- Breakpoints
      vim.keymap.set("n", "<leader>cb", "<cmd>CpuToggleBp<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Toggle breakpoint" }))
      vim.keymap.set("n", "<leader>cB", "<cmd>CpuClearBp<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Clear breakpoints" }))

      -- View panels
      vim.keymap.set("n", "<leader>cd", "<cmd>CpuToggleDump<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Toggle dump panel" }))
      vim.keymap.set("n", "<leader>ce", "<cmd>CpuToggleAssembled<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Toggle assembled panel" }))
      vim.keymap.set("n", "<leader>co", "<cmd>CpuOpenSidebar<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Open sidebar" }))
      vim.keymap.set("n", "<leader>cc", "<cmd>CpuCloseSidebar<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Close sidebar" }))
    end,
  })
end

--- Start the LSP server for csasm files
function M.start_lsp()
  if not M.config.lsp_path then
    return
  end

  vim.api.nvim_create_autocmd("FileType", {
    pattern = "csasm",
    callback = function()
      vim.lsp.start({
        cmd = { M.config.lsp_path },
        name = "csasm-lsp",
        root_dir = vim.fn.getcwd(),
      })
    end,
  })
end

--- Setup CursorMoved autocmd to highlight assembled bytes for current source line
function M.setup_cursor_highlight()
  if not display then
    display = require("cpu-simple.display")
  end
  display.highlights.setup_cursor_highlight(
  function()
    if not assembler then
      return nil
    end
    if not assembler.has_debug_info() then
      return nil
    end
    return assembler.get_address_span_from_current_line()
  end,
  function()
    if not display then
      return nil
    end
    return display.assembled.get_buffer()
  end
)
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
  
  vim.api.nvim_create_user_command("CpuToggleBp", function(cmd_opts)
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
  
  vim.api.nvim_create_user_command("CpuClearBp", function()
    M.clear_all_breakpoints()
  end, {
    desc = "Clear all breakpoints",
  })

  vim.api.nvim_create_user_command("CpuNextBp", function()
    M.goto_next_breakpoint()
  end, {
    desc = "Go to next breakpoint",
  })

  vim.api.nvim_create_user_command("CpuPrevBp", function()
    M.goto_prev_breakpoint()
  end, {
    desc = "Go to previous breakpoint",
  })

  vim.api.nvim_create_user_command("CpuGotoDef", function()
    M.goto_definition()
  end, {
    desc = "Go to symbol definition",
  })

  vim.api.nvim_create_user_command("CpuGotoPC", function()
    M.goto_PC()
  end, {
    desc = "Go to PC"
  })

  vim.api.nvim_create_user_command("CpuRunToCursor", function()
    M.run_to_cursor()
  end, {
    desc = "Run to cursor line",
  })
  
  -- Panel toggle commands
  vim.api.nvim_create_user_command("CpuToggleStatus", function()
    if not display then
      display = require("cpu-simple.display")
    end
    display.toggle_status()
  end, {
    desc = "Toggle the CPU status panel",
  })

  vim.api.nvim_create_user_command("CpuToggleStack", function()
    if not display then
      display = require("cpu-simple.display")
    end
    display.toggle_stack()
  end, {
    desc = "Toggle the CPU stack panel",
  })

  vim.api.nvim_create_user_command("CpuToggleMemory", function()
    if not display then
      display = require("cpu-simple.display")
    end
    display.toggle_memory()
  end, {
    desc = "Toggle the CPU memory panel",
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

--- Assemble the current buffer, shows the assembled code in the sidebar, and auto-loads it into the CPU
M.assemble = with_running_backend(function()
  if not assembler then
    assembler = require("cpu-simple.assembler")
  end
  if not display then
    display = require("cpu-simple.display")
  end
  if not events then
    events = require("cpu-simple.events")
  end
  if not assembler then
    assembler = require("cpu-simple.assembler")
  end
  
  events.on(events.ASSEMBLED, function(data)
    -- Show all panels when assembly is done
    display.assembled.set_content(assembler.get_last_output_content())
    display.assembled.show()
    display.status_panel.show()
    display.memory.show()
    display.stack.show()
    M.load() -- Auto-load assembled code into CPU
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
  if not assembler then
    assembler = require("cpu-simple.assembler")
  end
  if not commands then
    commands = require("cpu-simple.commands")
  end
  if not state then
    state = require("cpu-simple.state")
  end
  if not backend then
    backend = require("cpu-simple.backend")
  end
  
  -- Use provided path or fall back to last assembled
  local file_path = path
  if not file_path then
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

  -- For now we optimistically set the loaded program immediately, but ideally this should be done in response to a backend event confirming the load was successful
  vim.schedule(function()
    state.loaded_program = file_path
    vim.notify("Loaded program: " .. state.loaded_program, vim.log.levels.INFO)
  end)
end)

--- Run the loaded program
M.run = with_running_backend(function()
  if not commands then
    commands = require("cpu-simple.commands")
  end
  if not state then
    state = require("cpu-simple.state")
  end
  if not backend then
    backend = require("cpu-simple.backend")
  end
  
  if not state.loaded_program then
    vim.notify("No program loaded. Use :CpuLoad or :CpuAssemble first.", vim.log.levels.ERROR)
    return
  end

  backend.send(commands.RUN)
end)

--- Execute one CPU instruction
M.step = with_running_backend(function()
  if not commands then
    commands = require("cpu-simple.commands")
  end
  if not state then
    state = require("cpu-simple.state")
  end
  if not backend then
    backend = require("cpu-simple.backend")
  end
  
  if not state.loaded_program then
    vim.notify("No program loaded. Use :CpuLoad or :CpuAssemble first.", vim.log.levels.ERROR)
    return
  end

  backend.send(commands.STEP)
end)

--- Reset the CPU
M.reset = with_running_backend(function()
  if not commands then
    commands = require("cpu-simple.commands")
  end
  if not backend then
    backend = require("cpu-simple.backend")
  end
  backend.send(commands.RESET)
end)

--- Get the CPU status
M.status = with_running_backend(function()
  if not commands then
    commands = require("cpu-simple.commands")
  end
  if not backend then
    backend = require("cpu-simple.backend")
  end
  backend.send(commands.STATUS)
end)

--- Set a breakpoint at the given address
---@param address number Address to set breakpoint at
M.set_breakpoint = with_running_backend(function(address)
  if not commands then
    commands = require("cpu-simple.commands")
  end
  if not backend then
    backend = require("cpu-simple.backend")
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
  if not backend then
    backend = require("cpu-simple.backend")
  end
  backend.send(commands.BREAK_CLR)
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
  
  display.highlight_breakpoints(state.breakpoints, assembler.get_source_line_from_address)
end

function M.highlight_pc()
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
    vim.notify("No debug info available to highlight PC", vim.log.levels.WARN)
    return
  end
  
  local pc_address = state.status and state.status.pc

  -- Look up the instruction span containing PC from debug info
  local pc_span = nil
  if pc_address and assembler.assembler.last_debug_info and assembler.assembler.last_debug_info.spans then
    pc_span = assembler.utils.get_address_span_from_address(pc_address, assembler.assembler.last_debug_info)
  end

  display.highlight_pc(pc_address, assembler.get_source_line_from_address, pc_span)
end

--- Run the CPU until it reaches the address at the cursor line
M.run_to_cursor = with_running_backend(function()
  if not assembler then
    assembler = require("cpu-simple.assembler")
  end
  if not commands then
    commands = require("cpu-simple.commands")
  end
  if not backend then
    backend = require("cpu-simple.backend")
  end
  if not state then
    state = require("cpu-simple.state")
  end

  if not state.loaded_program then
    vim.notify("No program loaded. Use :CpuLoad or :CpuAssemble first.", vim.log.levels.ERROR)
    return
  end

  local span = assembler.get_address_span_from_current_line()
  if not span or not span.start_address then
    vim.notify("No address mapped to current line", vim.log.levels.ERROR)
    return
  end
  backend.send(string.format("%s %d", commands.RUN_TO, span.start_address))
end)

--- Jump to the next breakpoint in the source buffer
function M.goto_next_breakpoint()
  if not assembler then
    assembler = require("cpu-simple.assembler")
  end
  if not state then
    state = require("cpu-simple.state")
  end

  if not assembler.has_debug_info() then
    vim.notify("No debug info available", vim.log.levels.WARN)
    return
  end
  if #state.breakpoints == 0 then
    vim.notify("No breakpoints set", vim.log.levels.INFO)
    return
  end

  -- Collect source lines for all breakpoints
  local bp_lines = {}
  for _, bp in ipairs(state.breakpoints) do
    local line = assembler.get_source_line_from_address(bp.address)
    if line then
      table.insert(bp_lines, line)
    end
  end
  if #bp_lines == 0 then
    return
  end
  table.sort(bp_lines)

  local cursor_line = vim.api.nvim_win_get_cursor(0)[1]
  -- Find first breakpoint line > cursor
  for _, line in ipairs(bp_lines) do
    if line > cursor_line then
      vim.api.nvim_win_set_cursor(0, { line, 0 })
      return
    end
  end
  -- Wrap around to first
  vim.api.nvim_win_set_cursor(0, { bp_lines[1], 0 })
end

--- Jump to the previous breakpoint in the source buffer
function M.goto_prev_breakpoint()
  if not assembler then
    assembler = require("cpu-simple.assembler")
  end
  if not state then
    state = require("cpu-simple.state")
  end

  if not assembler.has_debug_info() then
    vim.notify("No debug info available", vim.log.levels.WARN)
    return
  end
  if #state.breakpoints == 0 then
    vim.notify("No breakpoints set", vim.log.levels.INFO)
    return
  end

  local bp_lines = {}
  for _, bp in ipairs(state.breakpoints) do
    local line = assembler.get_source_line_from_address(bp.address)
    if line then
      table.insert(bp_lines, line)
    end
  end
  if #bp_lines == 0 then
    return
  end
  table.sort(bp_lines)

  local cursor_line = vim.api.nvim_win_get_cursor(0)[1]
  -- Find last breakpoint line < cursor
  for i = #bp_lines, 1, -1 do
    if bp_lines[i] < cursor_line then
      vim.api.nvim_win_set_cursor(0, { bp_lines[i], 0 })
      return
    end
  end
  -- Wrap around to last
  vim.api.nvim_win_set_cursor(0, { bp_lines[#bp_lines], 0 })
end

--- Jump to the definition of the symbol under the cursor
function M.goto_definition()
  if not assembler then
    assembler = require("cpu-simple.assembler")
  end

  if not assembler.has_debug_info() then
    vim.notify("No debug info available", vim.log.levels.WARN)
    return
  end

  local word = vim.fn.expand("<cword>")
  if not word or word == "" then
    vim.notify("No word under cursor", vim.log.levels.WARN)
    return
  end

  local symbols = assembler.assembler.last_debug_info.symbols
  if not symbols or not symbols[word] then
    vim.notify("Symbol not found: " .. word, vim.log.levels.WARN)
    return
  end

  local address = symbols[word].address
  local line = assembler.get_source_line_from_address(address)
  if not line then
    vim.notify("No source line for symbol: " .. word, vim.log.levels.WARN)
    return
  end

  vim.api.nvim_win_set_cursor(0, { line, 0 })
end

-- Jump to PC
function M.goto_PC()
  if not assembler then
    assembler = require("cpu-simple.assembler")
  end
  if not state then
    state = require("cpu-simple.state")
  end

  local pc_line = assembler.get_source_line_from_address(state.status.pc)
  if not pc_line then
    vim.notify("Could not find PC line", vim.log.levels.WARN)
    return
  end
  vim.api.nvim_win_set_cursor(0, { pc_line, 0 })
end

--- Send a raw command to the backend
---@param cmd string Command to send
M.send = with_running_backend(function(cmd)
  if not backend then
    backend = require("cpu-simple.backend")
  end
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
  

  local zero = s.flags.zero and 1 or 0
  local carry = s.flags.carry and 1 or 0
  return string.format("Cyc:%d PC:%d SP:%d Z:%d C:%d R0:%d R1:%d R2:%d R3:%d",
    s.cycles, s.pc, s.sp, zero, carry,
    s.registers[1], s.registers[2], s.registers[3], s.registers[4])
end

return M
