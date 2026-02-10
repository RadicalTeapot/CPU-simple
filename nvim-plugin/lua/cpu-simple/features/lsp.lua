local M = {}

function M.new(ctx)
  local feature = {}

  function feature.start()
    if not ctx.config.lsp_path or ctx.runtime.lsp_autocmd_registered then
      return
    end

    local group = vim.api.nvim_create_augroup("CpuSimpleLsp", { clear = true })
    vim.api.nvim_create_autocmd("FileType", {
      group = group,
      pattern = "csasm",
      callback = function()
        vim.lsp.start({
          cmd = { ctx.config.lsp_path },
          name = "csasm-lsp",
          root_dir = vim.fn.getcwd(),
        })
      end,
    })

    ctx.runtime.lsp_autocmd_registered = true
  end

  return feature
end

return M
