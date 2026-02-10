local M = {}

M.defaults = {
  backend_path = "Backend.exe",
  assembler_path = "Assembler.exe",
  lsp_path = nil,
  assembler_options = {
    emit_debug = false,
  },
  memory_size = 256,
  stack_size = 16,
  registers = 4,
  cwd = nil,
  sidebar = {
    width = 0.5,
    position = "right",
    panels = {
      stack = { bytes_per_line = 16 },
      memory = {
        bytes_per_line = 16,
        changed_highlight = {
          enabled = true,
          timeout_ms = 1500,
        },
        cursor_address_highlight = true,
      },
    },
  },
  source_annotations = {
    pc_operands_virtual_text = {
      enabled = true,
    },
  },
  assemble_panels = {
    assembled = true,
    status = true,
    memory = true,
    stack = true,
  },
  signs = {
    use_for_breakpoints = false,
    use_for_pc = false,
    breakpoint_text = "●",
    pc_text = "▶",
  },
}

function M.resolve(opts)
  return vim.tbl_deep_extend("force", {}, M.defaults, opts or {})
end

return M
