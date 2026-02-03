-- Plugin loader for cpu-simple
-- This file is auto-loaded by Neovim when the plugin is installed

-- Prevent loading twice
if vim.g.loaded_cpu_simple then
  return
end
vim.g.loaded_cpu_simple = true

-- Lazy-load the plugin on first command use
local function ensure_setup()
  local cpu_simple = require("cpu-simple")
  if vim.tbl_isempty(cpu_simple.config) then
    cpu_simple.setup({})
  end
  return cpu_simple
end

-- Provide a single entry point command that triggers setup
-- All other commands are registered in setup() -> register_commands()
vim.api.nvim_create_user_command("CpuSetup", function(opts)
  local cpu_simple = ensure_setup()
  vim.notify("cpu-simple plugin loaded", vim.log.levels.INFO)
end, {
  desc = "Initialize the cpu-simple plugin",
})
