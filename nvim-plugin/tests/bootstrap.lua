local M = {}

function M.setup_paths()
  local repo_root = vim.env.CPU_SIMPLE_REPO_ROOT
  if not repo_root or repo_root == "" then
    error("CPU_SIMPLE_REPO_ROOT is not set")
  end

  package.path = table.concat({
    repo_root .. "/nvim-plugin/lua/?.lua",
    repo_root .. "/nvim-plugin/lua/?/init.lua",
    package.path,
  }, ";")

  return repo_root
end

function M.reset_cpu_modules()
  for name, _ in pairs(package.loaded) do
    if name:match("^cpu%-simple") then
      package.loaded[name] = nil
    end
  end
end

function M.assert_true(value, message)
  if not value then
    error(message or "assert_true failed")
  end
end

function M.assert_equal(actual, expected, message)
  if actual ~= expected then
    error((message or "assert_equal failed") .. string.format(" (expected=%s, actual=%s)", tostring(expected), tostring(actual)))
  end
end

function M.assert_contains(tbl, key, message)
  if tbl[key] == nil then
    error(message or ("missing key: " .. tostring(key)))
  end
end

function M.make_display_stub()
  local calls = {
    toggle_status = 0,
    toggle_stack = 0,
    toggle_memory = 0,
    toggle_assembled = 0,
    open_sidebar = 0,
    close_sidebar = 0,
  }

  local stub = {
    setup = function() end,
    toggle_status = function() calls.toggle_status = calls.toggle_status + 1 end,
    toggle_stack = function() calls.toggle_stack = calls.toggle_stack + 1 end,
    toggle_memory = function() calls.toggle_memory = calls.toggle_memory + 1 end,
    toggle_assembled = function() calls.toggle_assembled = calls.toggle_assembled + 1 end,
    open_sidebar = function() calls.open_sidebar = calls.open_sidebar + 1 end,
    close_sidebar = function() calls.close_sidebar = calls.close_sidebar + 1 end,
    highlight_breakpoints = function() end,
    highlight_pc = function() end,
    assembled = {
      is_visible = function() return false end,
      get_buffer = function() return nil end,
      set_content = function() end,
      show = function() end,
    },
    status_panel = {
      show = function() end,
    },
    stack = {
      is_visible = function() return false end,
      show = function() end,
    },
    memory = {
      is_visible = function() return false end,
      show = function() end,
      set_cursor_address = function() end,
      clear_highlight_state = function() end,
    },
    highlights = {
      groups = { PC_VIRTUAL_TEXT = "CpuSimplePCVirtualText" },
      setup_cursor_highlight = function() end,
      clear_pc_virtual_text = function() end,
      set_pc_virtual_text = function() end,
    },
  }

  return stub, calls
end

function M.make_assembler_stub(overrides)
  local stub = {
    assembler = {
      last_source_bufnr = nil,
      last_debug_info = nil,
    },
    utils = {
      get_address_span_from_address = function() return nil end,
    },
    assemble_current_buffer = function() end,
    get_last_output_path = function() return nil end,
    get_last_output_content = function() return {} end,
    get_address_span_from_current_line = function() return nil end,
    get_source_line_from_address = function() return nil end,
    has_debug_info = function() return false end,
  }

  if overrides then
    for k, v in pairs(overrides) do
      stub[k] = v
    end
  end

  return stub
end

function M.make_operand_resolver_stub()
  return {
    find_memory_expr_at_cursor = function() return nil end,
    resolve_memory_expr = function() return nil end,
    build_pc_virtual_text = function() return "" end,
  }
end

return M
