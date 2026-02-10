-- Sidebar manager for cpu-simple
-- Provides IDE-like sidebar with toggleable, horizontally stacked panels

local utils = require("cpu-simple.display.utils")

local M = {}

-- Configuration with defaults
M.config = {
    width = 0.5, -- Ratio of editor width (0.5 = half)
    position = "right", -- "left" or "right"
    panels = {}, -- Panel-specific config: { [panel_id] = { height = 0 } }
}

-- State tracking
M.panels = {} -- Ordered list of { id, bufnr, winnr, visible, saved_state }
M.sidebar_winnr = nil -- Reference to the first/main sidebar window
M.autocmd_group = nil -- Autocmd group for WinClosed events

--- Setup the sidebar manager with user configuration
---@param opts table|nil Configuration options
function M.setup(opts)
    opts = opts or {}
    M.config = vim.tbl_deep_extend("force", M.config, opts)
    
    -- Create autocmd group for window events
    M.autocmd_group = vim.api.nvim_create_augroup("CpuSimpleSidebar", { clear = true })
end

--- Get panel by ID
---@param panel_id string
---@return table|nil panel
local function get_panel(panel_id)
    for _, panel in ipairs(M.panels) do
        if panel.id == panel_id then
            return panel
        end
    end
    return nil
end

--- Get the last visible panel's window number
---@return number|nil winnr
local function get_last_visible_winnr()
    for i = #M.panels, 1, -1 do
        local panel = M.panels[i]
        if panel.visible and panel.winnr and vim.api.nvim_win_is_valid(panel.winnr) then
            return panel.winnr
        end
    end
    return nil
end

--- Count visible panels
---@return number count
local function count_visible_panels()
    local count = 0
    for _, panel in ipairs(M.panels) do
        if panel.visible and panel.winnr and vim.api.nvim_win_is_valid(panel.winnr) then
            count = count + 1
        end
    end
    return count
end

--- Check if any panel is visible
---@return boolean
function M.is_any_visible()
    return count_visible_panels() > 0
end

--- Check if sidebar is open
---@return boolean
function M.is_sidebar_open()
    return M.sidebar_winnr ~= nil and vim.api.nvim_win_is_valid(M.sidebar_winnr)
end

--- Calculate sidebar width in columns
---@return number width
local function calculate_sidebar_width()
    if M.config.width <= 1 then
        return math.floor(vim.o.columns * M.config.width)
    end
    return M.config.width -- Absolute value if > 1
end

--- Get panel height configuration
---@param panel_id string
---@return number height (0 = auto)
local function get_panel_height(panel_id)
    if M.config.panels[panel_id] and M.config.panels[panel_id].height then
        return M.config.panels[panel_id].height
    end
    return 0 -- Default: auto height
end

--- Save panel state
---@param panel table
local function save_panel_state(panel)
    if not panel.winnr or not vim.api.nvim_win_is_valid(panel.winnr) then
        return
    end

    panel.saved_state.cursor = vim.api.nvim_win_get_cursor(panel.winnr)
    panel.saved_state.topline = vim.fn.getwininfo(panel.winnr)[1].topline
end

--- Restore panel state after showing
---@param panel table
local function restore_panel_state(panel)
    if not panel.winnr or not vim.api.nvim_win_is_valid(panel.winnr) then
        return
    end
    
    -- Restore cursor position
    if panel.saved_state.cursor then
        pcall(vim.api.nvim_win_set_cursor, panel.winnr, panel.saved_state.cursor)
    end
    
    -- Restore scroll position
    if panel.saved_state.topline then
        vim.api.nvim_win_call(panel.winnr, function()
            vim.fn.winrestview({ topline = panel.saved_state.topline })
        end)
    end
end

--- Setup autocmd to handle window close events
---@param panel table
local function setup_win_close_autocmd(panel)
    if not M.autocmd_group then
        M.autocmd_group = vim.api.nvim_create_augroup("CpuSimpleSidebar", { clear = true })
    end
    
    vim.api.nvim_create_autocmd("WinClosed", {
        group = M.autocmd_group,
        pattern = tostring(panel.winnr),
        once = true,
        callback = function()
            -- Mark panel as hidden
            panel.visible = false
            panel.winnr = nil
            
            -- Check if we need to close the sidebar
            vim.schedule(function()
                if not M.is_any_visible() then
                    M.close_sidebar()
                end
            end)
        end,
    })
end

--- Open the sidebar (creates the first vertical split)
---@return number winnr The sidebar window number
function M.open_sidebar()
    if M.is_sidebar_open() then
        return M.sidebar_winnr
    end

    -- Save current window to return focus later
    local current_win = vim.api.nvim_get_current_win()
    
    -- Create vertical split on the right
    if M.config.position == "right" then
        vim.cmd("botright vsplit")
    else
        vim.cmd("topleft vsplit")
    end
    
    M.sidebar_winnr = vim.api.nvim_get_current_win()
    
    -- Set sidebar width
    local width = calculate_sidebar_width()
    vim.api.nvim_win_set_width(M.sidebar_winnr, width)

    -- Restore previously visible panels
    for _, panel in ipairs(M.panels) do
        if panel.saved_state.visible then
            M.show_panel(panel.id)
        end
    end
    
    -- Restore focus to previous window
    vim.api.nvim_set_current_win(current_win)
    
    return M.sidebar_winnr
end

--- Close the entire sidebar
function M.close_sidebar()
    -- Save state and close all panel windows
    for _, panel in ipairs(M.panels) do
        save_panel_state(panel)
        if panel.visible and panel.winnr and vim.api.nvim_win_is_valid(panel.winnr) then
            panel.visible = false -- Mark as hidden but keep visibility state
            vim.api.nvim_win_close(panel.winnr, true)
        end        
        panel.winnr = nil
    end
    
    -- Close sidebar window
    if M.is_sidebar_open() then
        vim.api.nvim_win_close(M.sidebar_winnr, true)
        M.sidebar_winnr = nil
    end    
end

--- Register a panel with the sidebar manager
---@param panel_id string Unique identifier for the panel
---@param bufnr number Buffer number for the panel
---@return table panel The registered panel object
function M.register_panel(panel_id, bufnr)
    local existing = get_panel(panel_id)
    if existing then
        -- Update buffer if provided
        if bufnr then
            existing.bufnr = bufnr
        end
        return existing
    end
    
    local panel = {
        id = panel_id,
        bufnr = bufnr,
        winnr = nil,
        visible = false,
        saved_state = {
            visible = false,
        },
    }
    
    table.insert(M.panels, panel)
    return panel
end

--- Show a panel in the sidebar
---@param panel_id string Panel identifier
---@return boolean success
function M.show_panel(panel_id)
    local panel = get_panel(panel_id)
    if not panel then
        vim.notify("Panel '" .. panel_id .. "' not registered", vim.log.levels.ERROR)
        return false
    end
    
    if not panel.bufnr or not vim.api.nvim_buf_is_valid(panel.bufnr) then
        vim.notify("Panel '" .. panel_id .. "' has no valid buffer", vim.log.levels.ERROR)
        return false
    end
        
    -- Ensure sidebar is open (restoring previous panels)
    if not M.is_sidebar_open() then
        M.open_sidebar()
    end

    -- Already visible after restoring?
    if panel.visible and panel.winnr and vim.api.nvim_win_is_valid(panel.winnr) then
        -- vim.api.nvim_set_current_win(panel.winnr)
        return true
    end
       
    if count_visible_panels() == 0 then
        -- No visible panels, use sidebar window
        panel.winnr = M.sidebar_winnr
        vim.api.nvim_win_set_buf(panel.winnr, panel.bufnr)
    else
        -- Find the last visible panel to split below
        local last_winnr = get_last_visible_winnr()
        -- Split below the last panel
        panel.winnr = vim.api.nvim_open_win(panel.bufnr, false, {
            split = "below",
            win = last_winnr,
        })
    end
    
    panel.visible = true
    panel.saved_state.visible = true
    
    -- Set panel height if configured
    local height = get_panel_height(panel_id)
    if height > 0 and panel.winnr then
        vim.api.nvim_win_set_height(panel.winnr, height)
    end
    
    -- Set window options
    if panel.winnr and vim.api.nvim_win_is_valid(panel.winnr) then
        vim.api.nvim_set_option_value("number", false, { win = panel.winnr })
        vim.api.nvim_set_option_value("relativenumber", false, { win = panel.winnr })
        vim.api.nvim_set_option_value("wrap", false, { win = panel.winnr })
        vim.api.nvim_set_option_value("winfixwidth", true, { win = panel.winnr })
    end
    
    -- Restore saved state if any
    restore_panel_state(panel)
    
    -- Setup autocmd to handle manual window close
    setup_win_close_autocmd(panel)
    
    return true
end

--- Hide a panel (but keep its buffer and state)
---@param panel_id string Panel identifier
---@return boolean success
function M.hide_panel(panel_id)
    local panel = get_panel(panel_id)
    if not panel then
        return false
    end

    panel.saved_state.visible = false
    
    if not panel.visible or not panel.winnr or not vim.api.nvim_win_is_valid(panel.winnr) then
        panel.visible = false
        panel.winnr = nil
        return true
    end
    
    -- Save state before closing
    save_panel_state(panel)
    
    -- Close the window
    vim.api.nvim_win_close(panel.winnr, true)
    panel.visible = false
    panel.winnr = nil
    
    -- Check if sidebar should be closed
    if not M.is_any_visible() then
        M.close_sidebar()
    end
    
    return true
end

--- Toggle a panel's visibility
---@param panel_id string Panel identifier
---@return boolean new_visible_state
function M.toggle_panel(panel_id)
    local panel = get_panel(panel_id)
    if not panel then
        vim.notify("Panel '" .. panel_id .. "' not registered", vim.log.levels.ERROR)
        return false
    end
    
    if panel.visible and panel.winnr and vim.api.nvim_win_is_valid(panel.winnr) then
        M.hide_panel(panel_id)
        return false
    else
        M.show_panel(panel_id)
        return true
    end
end

--- Get panel window number (for external use)
---@param panel_id string Panel identifier
---@return number|nil winnr
function M.get_panel_winnr(panel_id)
    local panel = get_panel(panel_id)
    if panel and panel.visible and panel.winnr and vim.api.nvim_win_is_valid(panel.winnr) then
        return panel.winnr
    end
    return nil
end

--- Get panel buffer number (for external use)
---@param panel_id string Panel identifier
---@return number|nil bufnr
function M.get_panel_bufnr(panel_id)
    local panel = get_panel(panel_id)
    if panel then
        return panel.bufnr
    end
    return nil
end

--- Check if a specific panel is visible
---@param panel_id string Panel identifier
---@return boolean
function M.is_panel_visible(panel_id)
    local panel = get_panel(panel_id)
    return panel ~= nil and panel.visible and panel.winnr ~= nil and vim.api.nvim_win_is_valid(panel.winnr)
end

--- Get list of all registered panel IDs
---@return string[]
function M.get_panel_ids()
    local ids = {}
    for _, panel in ipairs(M.panels) do
        table.insert(ids, panel.id)
    end
    return ids
end

--- Get list of visible panel IDs (in order)
---@return string[]
function M.get_visible_panel_ids()
    local ids = {}
    for _, panel in ipairs(M.panels) do
        if panel.visible and panel.winnr and vim.api.nvim_win_is_valid(panel.winnr) then
            table.insert(ids, panel.id)
        end
    end
    return ids
end

return M
