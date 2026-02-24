local M = {}

function M.setup(ctx)
  if ctx.runtime.keymaps_registered then
    return
  end

  local group = vim.api.nvim_create_augroup("CpuSimpleKeymaps", { clear = true })

  vim.api.nvim_create_autocmd("FileType", {
    group = group,
    pattern = "csasm",
    callback = function(args)
      local opts = { buffer = args.buf, noremap = true }

      vim.keymap.set("n", "]b", "<cmd>CpuNextBp<cr>", vim.tbl_extend("force", opts, { desc = "Next breakpoint" }))
      vim.keymap.set("n", "[b", "<cmd>CpuPrevBp<cr>", vim.tbl_extend("force", opts, { desc = "Previous breakpoint" }))
      vim.keymap.set("n", "gd", "<cmd>CpuGotoDef<cr>", vim.tbl_extend("force", opts, { desc = "Go to symbol definition" }))
      vim.keymap.set("n", "]p", "<cmd>CpuGotoPC<cr>", vim.tbl_extend("force", opts, { desc = "Go to PC" }))

      vim.keymap.set("n", "<leader>cs", "<cmd>CpuBackendStart<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Start backend" }))
      vim.keymap.set("n", "<leader>cq", "<cmd>CpuBackendStop<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Stop backend" }))

      vim.keymap.set("n", "<leader>ca", "<cmd>CpuAssemble<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Assemble" }))
      vim.keymap.set("n", "<leader>cl", "<cmd>CpuLoad<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Load" }))

      vim.keymap.set("n", "<leader>cr", "<cmd>CpuRun<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Run" }))
      vim.keymap.set("n", "<leader>cn", "<cmd>CpuStep<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Step" }))
      vim.keymap.set("n", "<leader>ct", "<cmd>CpuTick<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Tick" }))
      vim.keymap.set("n", "<leader>cN", "<cmd>CpuStepOver<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Step over" }))
      vim.keymap.set("n", "<leader>cO", "<cmd>CpuStepOut<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Step out" }))
      vim.keymap.set("n", "<leader>cR", "<cmd>CpuReset<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Reset" }))

      vim.keymap.set("n", "<leader>cb", "<cmd>CpuToggleBp<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Toggle breakpoint" }))
      vim.keymap.set("n", "<leader>cB", "<cmd>CpuClearBp<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Clear breakpoints" }))

      vim.keymap.set("n", "<leader>cd", "<cmd>CpuToggleDump<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Toggle dump panel" }))
      vim.keymap.set("n", "<leader>ce", "<cmd>CpuToggleAssembled<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Toggle assembled panel" }))
      vim.keymap.set("n", "<leader>co", "<cmd>CpuOpenSidebar<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Open sidebar" }))
      vim.keymap.set("n", "<leader>cc", "<cmd>CpuCloseSidebar<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Close sidebar" }))
    end,
  })

  ctx.runtime.keymaps_registered = true
end

return M
