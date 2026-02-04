-- Signs module for cpu-simple display
-- Handles sign definition and placement for breakpoints and PC

local M = {}

M.hl_groups = {
    PC_SIGN_GROUP = "CpuSimplePCSignGroup",
    BREAKPOINT_SIGN_GROUP = "CpuSimpleBreakpointSignGroup"
}

-- Sign names
M.names = {
    BREAKPOINT = "CpuSimpleBreakpoint",
    PC = "CpuSimplePC",
}

-- Sign group names (for sign_unplace)
M.groups = {
    BREAKPOINT = "CpuSimpleBreakpointGroup",
    PC = "CpuSimplePCGroup",
}

-- Sign priorities (PC higher than breakpoint so it shows when on same line)
M.priorities = {
    BREAKPOINT = 10,
    PC = 20,
}

-- Configuration reference
local config = nil

--- Setup signs with configuration
--- Must be called after highlight groups are defined
---@param cfg table Signs configuration from plugin config
function M.setup(cfg)
    config = cfg or {}
    
    local breakpoint_text = config.breakpoint_text or "●"
    local pc_text = config.pc_text or "▶"

    M.define_highlight_groups()
    
    -- Define breakpoint sign
    vim.fn.sign_define(M.names.BREAKPOINT, {
        text = breakpoint_text,
        texthl = M.hl_groups.BREAKPOINT_SIGN_GROUP,
        numhl = "",
    })
    
    -- Define PC sign
    vim.fn.sign_define(M.names.PC, {
        text = pc_text,
        texthl = M.hl_groups.PC_SIGN_GROUP,
        numhl = "",
    })
end

function M.define_highlight_groups()
    -- Define highlight groups for breakpoint sign
    vim.api.nvim_set_hl(0, M.hl_groups.BREAKPOINT_SIGN_GROUP, { link = "WarningMsg", default = true })
    -- Define highlight groups for PC sign
    vim.api.nvim_set_hl(0, M.hl_groups.PC_SIGN_GROUP, { link = "CursorLineNr", default = true })
end

--- Place a breakpoint sign in a buffer
---@param bufnr number Buffer number
---@param line_nr number 1-based line number
function M.place_breakpoint_sign(bufnr, line_nr)
    vim.fn.sign_place(0, M.groups.BREAKPOINT, M.names.BREAKPOINT, bufnr, {
        lnum = line_nr,
        priority = M.priorities.BREAKPOINT,
    })
end

--- Place a PC sign in a buffer
---@param bufnr number Buffer number
---@param line_nr number 1-based line number
function M.place_pc_sign(bufnr, line_nr)
    vim.fn.sign_place(0, M.groups.PC, M.names.PC, bufnr, {
        lnum = line_nr,
        priority = M.priorities.PC,
    })
end

--- Clear all breakpoint signs from a buffer
---@param bufnr number Buffer number
function M.clear_breakpoint_signs(bufnr)
    vim.fn.sign_unplace(M.groups.BREAKPOINT, { buffer = bufnr })
end

--- Clear PC sign from a buffer
---@param bufnr number Buffer number
function M.clear_pc_sign(bufnr)
    vim.fn.sign_unplace(M.groups.PC, { buffer = bufnr })
end

return M
