local M = {}

function M.setup(ctx, api)
  if ctx.runtime.events_registered then
    return
  end

  local events = ctx.deps.get("events")

  events.on(events.STATUS_UPDATED, function()
    api.maybe_request_dump()
    api.highlight_pc()
    api.update_cursor_memory_highlight()
    api.update_pc_virtual_text()
    vim.cmd("redrawstatus")
  end)

  events.on(events.MEMORY_UPDATED, function()
    ctx.runtime.pending_dump_request = false
    api.update_cursor_memory_highlight()
    api.update_pc_virtual_text()
  end)

  events.on(events.BACKEND_STARTED, function()
    ctx.runtime.pending_dump_request = false
    vim.cmd("redrawstatus")
  end)

  events.on(events.BACKEND_STOPPED, function()
    local display = ctx.deps.get("display")

    ctx.runtime.pending_dump_request = false
    api.clear_pc_virtual_text()
    if display and display.memory and display.memory.set_cursor_address then
      display.memory.set_cursor_address(nil)
    end
    vim.cmd("redrawstatus")
  end)

  events.on(events.BREAKPOINT_UPDATED, function()
    api.highlight_breakpoints()
  end)

  ctx.runtime.events_registered = true
end

return M
