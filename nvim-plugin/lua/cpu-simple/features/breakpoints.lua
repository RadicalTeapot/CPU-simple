local M = {}

function M.new(ctx, emulator_feature)
  local feature = {}

  feature.set_breakpoint = emulator_feature.with_running_emulator(function(address)
    local commands = ctx.deps.get("commands")
    local emulator = ctx.deps.get("emulator")
    emulator.send(string.format("%s %d", commands.BREAK_TGL, address))
  end)

  feature.set_breakpoint_at_cursor = emulator_feature.with_running_emulator(function()
    local assembler = ctx.deps.get("assembler")
    local span = assembler.get_address_span_from_current_line()
    if not span then
      vim.notify("No debug info available to set breakpoint", vim.log.levels.ERROR)
      return
    end

    local address = span.start_address
    if not address then
      vim.notify("No address mapped to current line", vim.log.levels.ERROR)
      return
    end

    feature.set_breakpoint(address)
  end)

  feature.clear_all_breakpoints = emulator_feature.with_running_emulator(function()
    local commands = ctx.deps.get("commands")
    local emulator = ctx.deps.get("emulator")
    emulator.send(commands.BREAK_CLR)
  end)

  function feature.highlight_breakpoints()
    local assembler = ctx.deps.get("assembler")
    local state = ctx.deps.get("state")
    local display = ctx.deps.get("display")

    if not assembler.has_debug_info() then
      vim.notify("No debug info available to highlight breakpoints", vim.log.levels.WARN)
      return
    end

    local bufnr = assembler.assembler.last_source_bufnr or vim.api.nvim_get_current_buf()
    display.highlight_breakpoints(state.breakpoints, assembler.get_source_line_from_address, bufnr)
  end

  return feature
end

return M
