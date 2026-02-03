-- Highlighting logic for cpu-simple display
-- Centralizes all highlight groups, namespaces, and highlighting functions

local M = {}

-- ============================================================================
-- Highlight Groups
-- ============================================================================

-- Highlight group names
M.groups = {
    CURRENT_SPAN = "CpuSimpleCurrentSpan",
    BREAKPOINT = "CpuSimpleBreakpoint",
    BREAKPOINT_PC = "CpuSimpleBreakpointPC",
}

--- Define all highlight groups (should be called once at plugin load)
function M.define_highlight_groups()
    -- Highlight for current span in assembled panel
    vim.api.nvim_set_hl(0, M.groups.CURRENT_SPAN, { link = "Visual", default = true })
    -- Highlight for breakpoints in source and assembled panels
    vim.api.nvim_set_hl(0, M.groups.BREAKPOINT, { link = "Error", default = true })
    -- Highlight for program counter line in source and assembled panels
    vim.api.nvim_set_hl(0, M.groups.BREAKPOINT_PC, { link = "WarningMsg", default = true })
end

-- ============================================================================
-- Namespaces
-- ============================================================================

-- Namespace for source buffer breakpoints
M.ns_source_breakpoint = vim.api.nvim_create_namespace("cpu_simple_source_breakpoint")
-- Namespace for source buffer program counter
M.ns_source_pc = vim.api.nvim_create_namespace("cpu_simple_source_pc")
-- Namespace for assembled buffer byte highlighting
M.ns_assembled_highlight = vim.api.nvim_create_namespace("cpu_simple_assembled_highlight")
-- Namespace for assembled buffer breakpoints
M.ns_assembled_breakpoint = vim.api.nvim_create_namespace("cpu_simple_assembled_breakpoint")

-- ============================================================================
-- Source Buffer Highlighting
-- ============================================================================

--- Highlight a breakpoint line in a source buffer
---@param bufnr number Buffer number
---@param line_nr number 1-based line number
function M.highlight_breakpoint_line(bufnr, line_nr)
    vim.api.nvim_buf_add_highlight(bufnr, M.ns_source_breakpoint, M.groups.BREAKPOINT, line_nr - 1, 0, -1)
end

--- Clear all breakpoint highlights from a source buffer
---@param bufnr number Buffer number
function M.clear_breakpoint_highlights(bufnr)
    vim.api.nvim_buf_clear_namespace(bufnr, M.ns_source_breakpoint, 0, -1)
end

--- Highlight the program counter line in a source buffer
---@param bufnr number Buffer number
---@param line_nr number 1-based line number
function M.highlight_pc_line(bufnr, line_nr)
    vim.api.nvim_buf_add_highlight(bufnr, M.ns_source_pc, M.groups.BREAKPOINT_PC, line_nr - 1, 0, -1)
end

--- Clear program counter highlight from a source buffer
---@param bufnr number Buffer number
function M.clear_pc_highlight(bufnr)
    vim.api.nvim_buf_clear_namespace(bufnr, M.ns_source_pc, 0, -1)
end

-- ============================================================================
-- Assembled Buffer Highlighting
-- ============================================================================

-- Constants for assembled buffer layout
local BYTES_PER_ROW = 16
local CHARS_PER_BYTE = 3 -- "XX " format

--- Highlight a range of bytes in an assembled buffer
--- Bytes are displayed as "XX " (3 chars each), 16 per row
---@param bufnr number Buffer number
---@param start_byte number 0-based start byte offset
---@param end_byte number 0-based end byte offset (inclusive)
function M.highlight_assembled_byte_range(bufnr, start_byte, end_byte)
    if not bufnr or not vim.api.nvim_buf_is_valid(bufnr) then
        return
    end
    
    -- Clear previous span highlights
    vim.api.nvim_buf_clear_namespace(bufnr, M.ns_assembled_highlight, 0, -1)
    
    -- Highlight each byte in the range
    for byte_offset = start_byte, end_byte do
        local row = math.floor(byte_offset / BYTES_PER_ROW)
        local col_byte = byte_offset % BYTES_PER_ROW
        local col_start = col_byte * CHARS_PER_BYTE
        local col_end = col_start + 2 -- Just the "XX" part, not the space
        vim.api.nvim_buf_add_highlight(bufnr, M.ns_assembled_highlight, M.groups.CURRENT_SPAN, row, col_start, col_end)
    end
end

--- Clear all span highlights from an assembled buffer
---@param bufnr number Buffer number
function M.clear_assembled_highlights(bufnr)
    if bufnr and vim.api.nvim_buf_is_valid(bufnr) then
        vim.api.nvim_buf_clear_namespace(bufnr, M.ns_assembled_highlight, 0, -1)
        vim.api.nvim_buf_clear_namespace(bufnr, M.ns_assembled_breakpoint, 0, -1)
    end
end

--- Clear only breakpoint highlights from an assembled buffer
---@param bufnr number Buffer number
function M.clear_assembled_breakpoint_highlights(bufnr)
    if bufnr and vim.api.nvim_buf_is_valid(bufnr) then
        vim.api.nvim_buf_clear_namespace(bufnr, M.ns_assembled_breakpoint, 0, -1)
    end
end

--- Highlight a breakpoint byte in an assembled buffer
---@param bufnr number Buffer number
---@param address number Address of the breakpoint
function M.highlight_assembled_breakpoint(bufnr, address)
    if not bufnr or not vim.api.nvim_buf_is_valid(bufnr) then
        return
    end
    
    local row = math.floor(address / BYTES_PER_ROW)
    local col_byte = address % BYTES_PER_ROW
    local col_start = col_byte * CHARS_PER_BYTE
    local col_end = col_start + 2
    
    vim.api.nvim_buf_add_highlight(bufnr, M.ns_assembled_breakpoint, M.groups.BREAKPOINT, row, col_start, col_end)
end

-- ============================================================================
-- High-level Highlighting Functions
-- ============================================================================

-- Track last highlighted line to avoid redundant updates
local last_highlighted_line = nil

--- Reset the cursor highlight tracking state
function M.reset_cursor_highlight_state()
    last_highlighted_line = nil
end

--- Setup CursorMoved autocmd to highlight assembled bytes for current source line
---@param get_address_span_fn function Function that returns { start_address, end_address } or nil
---@param get_assembled_bufnr_fn function Function that returns the assembled buffer number or nil
function M.setup_cursor_highlight(get_address_span_fn, get_assembled_bufnr_fn)
    vim.api.nvim_create_autocmd("CursorMoved", {
        group = vim.api.nvim_create_augroup("CpuSimpleCursorHighlight", { clear = true }),
        callback = function()
            -- Get current line (0-based)
            local cursor_line = vim.api.nvim_win_get_cursor(0)[1] - 1
            
            -- Guard: skip if line hasn't changed
            if cursor_line == last_highlighted_line then
                return
            end
            last_highlighted_line = cursor_line
            
            local span = get_address_span_fn()
            local bufnr = get_assembled_bufnr_fn()
            if not bufnr then
                return
            end
            if span then
                M.highlight_assembled_byte_range(bufnr, span.start_address, span.end_address)
            else
                M.clear_assembled_highlights(bufnr)
            end
        end,
    })
end

--- Highlight all breakpoints in source and assembled buffers
---@param source_bufnr number Source buffer number
---@param breakpoints table[] Array of breakpoints with .address field
---@param assembled_bufnr number|nil Assembled buffer number (or nil if not visible)
---@param get_source_line_fn function(address) Function to get source line from address
function M.highlight_all_breakpoints(source_bufnr, breakpoints, assembled_bufnr, get_source_line_fn)
    -- Clear existing breakpoint highlights
    M.clear_breakpoint_highlights(source_bufnr)
    if assembled_bufnr then
        M.clear_assembled_breakpoint_highlights(assembled_bufnr)
    end
    
    -- Highlight each breakpoint
    for _, bp in ipairs(breakpoints) do
        local source_line = get_source_line_fn(bp.address)
        if source_line then
            M.highlight_breakpoint_line(source_bufnr, source_line)
        end
        if assembled_bufnr then
            M.highlight_assembled_breakpoint(assembled_bufnr, bp.address)
        end
    end
end

--- Highlight the program counter in source buffer
---@param source_bufnr number Source buffer number
---@param pc_address number|nil Current program counter address
---@param get_source_line_fn function(address) Function to get source line from address
function M.highlight_program_counter(source_bufnr, pc_address, get_source_line_fn)
    -- Clear existing PC highlight
    M.clear_pc_highlight(source_bufnr)
    
    if pc_address then
        local source_line = get_source_line_fn(pc_address)
        if source_line then
            M.highlight_pc_line(source_bufnr, source_line)
        end
    end
end

return M
