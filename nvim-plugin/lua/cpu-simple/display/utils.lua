-- Display module utilities for cpu-simple
-- Provides helper functions for buffer and window management

local M = {}

local BREAKPOINT_HL_GROUP = "CpuSimpleBreakpoint"

-- Highlight namespaces
local breakpoint_ns = vim.api.nvim_create_namespace("cpu_simple_source_breakpoint")

--- Get or create a scratch buffer
---@param opts table|nil Options { name?: string, modifiable?: true|false }
---@return number bufnr
function M.create_scratch_buffer(opts)
    opts = opts or {}
    local name = opts.name or "[CPU-Simple]"
    local modifiable = opts.modifiable or false
    
    -- Create new scratch buffer
    local bufnr = vim.api.nvim_create_buf(false, true)
    
    -- Set buffer options
    vim.api.nvim_set_option_value("buftype", "nofile", { buf = bufnr })
    vim.api.nvim_set_option_value("bufhidden", "hide", { buf = bufnr })
    vim.api.nvim_set_option_value("swapfile", false, { buf = bufnr })
    vim.api.nvim_set_option_value("modifiable", modifiable, { buf = bufnr })
    vim.api.nvim_buf_set_name(bufnr, name)
    
    return bufnr
end

--- Create and open a window for the given buffer
---@param bufnr number Buffer number to display
---@param opts table|nil Options { title?: string, split?: "vertical"|"horizontal"|"float" }
---@return number winnr
function M.create_window(bufnr, opts)
    opts = opts or {}
    local split = opts.split or "horizontal"
    local title = opts.title or "CPU-Simple"
    
    -- Open window based on split type
    local winnr
    if split == "float" then
        local width = math.floor(vim.o.columns * 0.8)
        local height = math.floor(vim.o.lines * 0.6)
        local row = math.floor((vim.o.lines - height) / 2)
        local col = math.floor((vim.o.columns - width) / 2)
        
        winnr = vim.api.nvim_open_win(bufnr, true, {
            relative = "editor",
            width = width,
            height = height,
            row = row,
            col = col,
            style = "minimal",
            border = "rounded",
            title = " " .. title .. " ",
            title_pos = "center",
        })
    elseif split == "vertical" then
        vim.cmd("vsplit")
        winnr = vim.api.nvim_get_current_win()
        vim.api.nvim_win_set_buf(winnr, bufnr)
    else
        -- horizontal split (default)
        vim.cmd("split")
        winnr = vim.api.nvim_get_current_win()
        vim.api.nvim_win_set_buf(winnr, bufnr)

        -- Resize to fit content, max 15 lines
        local line_count = vim.api.nvim_buf_line_count(bufnr)
        local height = math.min(line_count + 1, 15)
        vim.api.nvim_win_set_height(winnr, height)
    end
    
    -- Set window-local options
    vim.api.nvim_set_option_value("number", false, { win = winnr })
    vim.api.nvim_set_option_value("relativenumber", false, { win = winnr })
    vim.api.nvim_set_option_value("wrap", false, { win = winnr })

    return winnr
end

function M.highlight_breakpoint_line(bufnr, line_nr)
    vim.api.nvim_buf_add_highlight(bufnr, breakpoint_ns, BREAKPOINT_HL_GROUP, line_nr - 1, 0, -1)
end

function M.clear_breakpoint_highlights(bufnr)
    vim.api.nvim_buf_clear_namespace(bufnr, breakpoint_ns, 0, -1)
end

return M