local M = {}

function M.setup(ctx, api)
  if ctx.runtime.autocmds_registered then
    return
  end

  api.setup_cursor_highlight()
  api.setup_memory_cursor_highlight()

  ctx.runtime.autocmds_registered = true
end

return M
