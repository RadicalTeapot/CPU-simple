local M = {}

function M.new()
  return {
    pending_dump_request = false,
    commands_registered = false,
    keymaps_registered = false,
    events_registered = false,
    autocmds_registered = false,
    lsp_autocmd_registered = false,
  }
end

return M
