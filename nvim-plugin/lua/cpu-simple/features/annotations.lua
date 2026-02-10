local M = {}

local function get_memory_panel_config(config)
  local sidebar_cfg = config.sidebar or {}
  local panels_cfg = sidebar_cfg.panels or {}
  return panels_cfg.memory or {}
end

local function is_pc_virtual_text_enabled(config)
  local source_annotations = config.source_annotations or {}
  local pc_annotations = source_annotations.pc_operands_virtual_text or {}
  return pc_annotations.enabled ~= false
end

local function annotation_hex_width(state)
  if state and state.memory and #state.memory > 0x100 then
    return 4
  end
  return 2
end

function M.new(ctx)
  local feature = {}

  function feature.clear_pc_virtual_text()
    local display = ctx.deps.get("display")
    local assembler = ctx.deps.get("assembler")

    if not display or not display.highlights then
      return
    end

    local bufnr = assembler.assembler and assembler.assembler.last_source_bufnr or nil
    if bufnr and vim.api.nvim_buf_is_valid(bufnr) then
      display.highlights.clear_pc_virtual_text(bufnr)
    end
  end

  function feature.maybe_request_dump()
    if ctx.runtime.pending_dump_request then
      return
    end

    local backend = ctx.deps.get("backend")
    if not backend.is_running() then
      return
    end

    local display = ctx.deps.get("display")
    local assembler = ctx.deps.get("assembler")
    local state = ctx.deps.get("state")
    local commands = ctx.deps.get("commands")

    local needs_dump_for_visible_panels = (display.stack.is_visible() and not state.stack)
      or (display.memory.is_visible() and not state.memory)

    local needs_dump_for_virtual_text = is_pc_virtual_text_enabled(ctx.config)
      and state.status
      and state.loaded_program
      and assembler.has_debug_info()
      and not state.memory

    if not (needs_dump_for_visible_panels or needs_dump_for_virtual_text) then
      return
    end

    ctx.runtime.pending_dump_request = true
    local sent = backend.send(commands.DUMP)
    if not sent then
      ctx.runtime.pending_dump_request = false
    end
  end

  function feature.setup_cursor_highlight()
    local display = ctx.deps.get("display")

    display.highlights.setup_cursor_highlight(
      function()
        local assembler = ctx.deps.get("assembler")
        if not assembler.has_debug_info() then
          return nil
        end
        return assembler.get_address_span_from_current_line()
      end,
      function()
        local display_mod = ctx.deps.get("display")
        return display_mod.assembled.get_buffer()
      end
    )
  end

  function feature.setup_memory_cursor_highlight()
    local group = vim.api.nvim_create_augroup("CpuSimpleMemoryCursorAddress", { clear = true })
    vim.api.nvim_create_autocmd("CursorMoved", {
      group = group,
      callback = function()
        ctx.api.update_cursor_memory_highlight()
      end,
    })
  end

  function feature.update_cursor_memory_highlight()
    local display = ctx.deps.get("display")
    local assembler = ctx.deps.get("assembler")
    local state = ctx.deps.get("state")
    local operand_resolver = ctx.deps.get("operand_resolver")

    local memory_cfg = get_memory_panel_config(ctx.config)
    if memory_cfg.cursor_address_highlight == false then
      display.memory.set_cursor_address(nil)
      return
    end

    if not assembler.has_debug_info() then
      display.memory.set_cursor_address(nil)
      return
    end

    local source_bufnr = assembler.assembler.last_source_bufnr
    local current_bufnr = vim.api.nvim_get_current_buf()
    if not source_bufnr or current_bufnr ~= source_bufnr then
      display.memory.set_cursor_address(nil)
      return
    end

    local cursor = vim.api.nvim_win_get_cursor(0)
    local line_nr = cursor[1]
    local col0 = cursor[2]
    local line_text = vim.api.nvim_buf_get_lines(current_bufnr, line_nr - 1, line_nr, false)[1] or ""
    local range = operand_resolver.find_memory_expr_at_cursor(line_text, col0)
    if not range then
      display.memory.set_cursor_address(nil)
      return
    end

    local symbols = assembler.assembler.last_debug_info and assembler.assembler.last_debug_info.symbols or nil
    local resolved = operand_resolver.resolve_memory_expr(range.expression, {
      symbols = symbols,
      registers = state.status and state.status.registers or nil,
      memory = state.memory,
      max_address = ctx.config.memory_size - 1,
    })

    display.memory.set_cursor_address(resolved and resolved.address or nil)
  end

  function feature.update_pc_virtual_text()
    local display = ctx.deps.get("display")
    local assembler = ctx.deps.get("assembler")
    local state = ctx.deps.get("state")
    local operand_resolver = ctx.deps.get("operand_resolver")

    if not is_pc_virtual_text_enabled(ctx.config) then
      feature.clear_pc_virtual_text()
      return
    end

    if not state.status or not state.loaded_program then
      feature.clear_pc_virtual_text()
      return
    end

    if not assembler.has_debug_info() then
      feature.clear_pc_virtual_text()
      return
    end

    local source_bufnr = assembler.assembler.last_source_bufnr
    if not source_bufnr or not vim.api.nvim_buf_is_valid(source_bufnr) then
      feature.clear_pc_virtual_text()
      return
    end

    local pc_address = state.status.pc
    if pc_address == nil then
      feature.clear_pc_virtual_text()
      return
    end

    local source_line = assembler.get_source_line_from_address(pc_address)
    if not source_line then
      feature.clear_pc_virtual_text()
      return
    end

    local line_text = vim.api.nvim_buf_get_lines(source_bufnr, source_line - 1, source_line, false)[1] or ""
    local symbols = assembler.assembler.last_debug_info and assembler.assembler.last_debug_info.symbols or nil
    local virtual_text = operand_resolver.build_pc_virtual_text(line_text, {
      symbols = symbols,
      registers = state.status.registers,
      memory = state.memory,
      hex_width = annotation_hex_width(state),
      max_address = ctx.config.memory_size - 1,
    })

    display.highlights.set_pc_virtual_text(source_bufnr, source_line, virtual_text, display.highlights.groups.PC_VIRTUAL_TEXT)
  end

  function feature.highlight_pc()
    local assembler = ctx.deps.get("assembler")
    local state = ctx.deps.get("state")
    local display = ctx.deps.get("display")

    if not assembler.has_debug_info() then
      vim.notify("No debug info available to highlight PC", vim.log.levels.WARN)
      return
    end

    local pc_address = state.status and state.status.pc
    local pc_span = nil
    if pc_address and assembler.assembler.last_debug_info and assembler.assembler.last_debug_info.spans then
      pc_span = assembler.utils.get_address_span_from_address(pc_address, assembler.assembler.last_debug_info)
    end

    local bufnr = assembler.assembler.last_source_bufnr or vim.api.nvim_get_current_buf()
    display.highlight_pc(pc_address, assembler.get_source_line_from_address, pc_span, bufnr)
  end

  return feature
end

return M
