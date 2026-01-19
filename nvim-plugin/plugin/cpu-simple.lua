-- Plugin loader for cpu-simple
-- This file is auto-loaded by Neovim when the plugin is installed

-- Prevent loading twice
if vim.g.loaded_cpu_simple then
  return
end
vim.g.loaded_cpu_simple = true

-- Lazy-load the plugin on first command use
-- Commands are registered in setup(), but we provide stubs here
-- so users can call commands before explicit setup()
local function ensure_setup()
  local cpu_simple = require("cpu-simple")
  if vim.tbl_isempty(cpu_simple.config) then
    cpu_simple.setup({})
  end
  return cpu_simple
end

-- Create commands that auto-setup if needed
vim.api.nvim_create_user_command("CpuBackendStart", function()
  ensure_setup().backend_start()
end, {
  desc = "Start the CPU backend process",
})

vim.api.nvim_create_user_command("CpuBackendStop", function()
  ensure_setup().backend_stop()
end, {
  desc = "Stop the CPU backend process",
})

vim.api.nvim_create_user_command("CpuBackendStatus", function() 
  ensure_setup().backend_status()
end, {
  desc = "Get the CPU backend status",
})

vim.api.nvim_create_user_command("CpuAssemble", function()
  ensure_setup().assemble()
end, {
  desc = "Assemble the current buffer to machine code",
})

vim.api.nvim_create_user_command("CpuLoad", function(opts)
  ensure_setup().load(opts.args ~= "" and opts.args or nil)
end, {
  desc = "Load machine code into the CPU",
  nargs = "?",
  complete = "file",
})

vim.api.nvim_create_user_command("CpuRun", function()
  ensure_setup().run()
end, {
  desc = "Run the loaded program",
})

vim.api.nvim_create_user_command("CpuStep", function()
  ensure_setup().step()
end, {
  desc = "Execute one CPU instruction",
})

vim.api.nvim_create_user_command("CpuReset", function()
  ensure_setup().reset()
end, {
  desc = "Reset the CPU",
})

vim.api.nvim_create_user_command("CpuStatus", function()
  ensure_setup().status()
end, {
  desc = "Get the current CPU status",
})

-- Optional: Create a command to send raw commands
vim.api.nvim_create_user_command("CpuSend", function(opts)
  ensure_setup().send(opts.args)
end, {
  desc = "Send a raw command to the CPU backend",
  nargs = "+",
})
