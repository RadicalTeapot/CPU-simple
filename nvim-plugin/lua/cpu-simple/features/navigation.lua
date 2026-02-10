local M = {}

local function sort_breakpoint_lines(state, assembler)
  local bp_lines = {}
  for _, bp in ipairs(state.breakpoints) do
    local line = assembler.get_source_line_from_address(bp.address)
    if line then
      table.insert(bp_lines, line)
    end
  end
  table.sort(bp_lines)
  return bp_lines
end

function M.new(ctx, backend_feature)
  local feature = {}

  function feature.goto_next_breakpoint()
    local assembler = ctx.deps.get("assembler")
    local state = ctx.deps.get("state")

    if not assembler.has_debug_info() then
      vim.notify("No debug info available", vim.log.levels.WARN)
      return
    end
    if #state.breakpoints == 0 then
      vim.notify("No breakpoints set", vim.log.levels.INFO)
      return
    end

    local bp_lines = sort_breakpoint_lines(state, assembler)
    if #bp_lines == 0 then
      return
    end

    local cursor_line = vim.api.nvim_win_get_cursor(0)[1]
    for _, line in ipairs(bp_lines) do
      if line > cursor_line then
        vim.api.nvim_win_set_cursor(0, { line, 0 })
        return
      end
    end

    vim.api.nvim_win_set_cursor(0, { bp_lines[1], 0 })
  end

  function feature.goto_prev_breakpoint()
    local assembler = ctx.deps.get("assembler")
    local state = ctx.deps.get("state")

    if not assembler.has_debug_info() then
      vim.notify("No debug info available", vim.log.levels.WARN)
      return
    end
    if #state.breakpoints == 0 then
      vim.notify("No breakpoints set", vim.log.levels.INFO)
      return
    end

    local bp_lines = sort_breakpoint_lines(state, assembler)
    if #bp_lines == 0 then
      return
    end

    local cursor_line = vim.api.nvim_win_get_cursor(0)[1]
    for i = #bp_lines, 1, -1 do
      if bp_lines[i] < cursor_line then
        vim.api.nvim_win_set_cursor(0, { bp_lines[i], 0 })
        return
      end
    end

    vim.api.nvim_win_set_cursor(0, { bp_lines[#bp_lines], 0 })
  end

  function feature.goto_definition()
    local assembler = ctx.deps.get("assembler")

    if not assembler.has_debug_info() then
      vim.notify("No debug info available", vim.log.levels.WARN)
      return
    end

    local word = vim.fn.expand("<cword>")
    if not word or word == "" then
      vim.notify("No word under cursor", vim.log.levels.WARN)
      return
    end

    local symbols = assembler.assembler.last_debug_info and assembler.assembler.last_debug_info.symbols or nil
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

  function feature.goto_PC()
    local assembler = ctx.deps.get("assembler")
    local state = ctx.deps.get("state")

    local status = state.status
    if not status then
      vim.notify("No CPU status available", vim.log.levels.WARN)
      return
    end

    local pc_line = assembler.get_source_line_from_address(status.pc)
    if not pc_line then
      vim.notify("Could not find PC line", vim.log.levels.WARN)
      return
    end

    vim.api.nvim_win_set_cursor(0, { pc_line, 0 })
  end

  feature.run_to_cursor = backend_feature.with_running_backend(function()
    local assembler = ctx.deps.get("assembler")
    local commands = ctx.deps.get("commands")
    local backend = ctx.deps.get("backend")
    local state = ctx.deps.get("state")

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

  return feature
end

return M
