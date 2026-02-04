-- Assembler module for cpu-simple

local M = {}

-- Core assembler functionality
M.assembler = require("cpu-simple.assembler.assembler")
M.utils = require("cpu-simple.assembler.utils")

--- Assemble the current buffer to machine code
---@param config table Configuration options
function M.assemble_current_buffer(config)
    local bufnr = vim.api.nvim_get_current_buf()
    M.assembler.assemble_buffer(config, bufnr)
end

--- Get the address span for the current cursor line
---@return table|nil {start_address = number, end_address = number} or nil
function M.get_address_span_from_current_line()
    local debug_info = M.assembler.last_debug_info
    if not debug_info or not debug_info.spans then
        vim.notify("No debug info available", vim.log.levels.ERROR)
        return nil
    end
    
    local bufnr = vim.api.nvim_get_current_buf()
    local last_assembled_buf = M.assembler.last_source_bufnr
    if not last_assembled_buf or bufnr ~= last_assembled_buf then
        vim.notify("Not in the last assembled source buffer, assemble first running :CpuAssemble", vim.log.levels.WARN)
        return nil
    end
    
    local cursor = vim.api.nvim_win_get_cursor(0)
    local line = cursor[1] - 1 -- 0-based line number
    return M.utils.get_address_span_from_line(line, debug_info)
end

--- Get the source line for a given address
---@param address number Address to look up
function M.get_source_line_from_address(address)
    local debug_info = M.assembler.last_debug_info
    if not debug_info or not debug_info.spans then
        vim.notify("No debug info available", vim.log.levels.ERROR)
        return nil
    end
    
    return M.utils.get_source_line_from_address(address, debug_info)
end

function M.has_debug_info()
    return M.assembler.last_debug_info ~= nil
end

--- Get the last assembled output path
---@return string|nil
function M.get_last_output_path()
    return M.assembler.last_output_path
end

--- Get the last assembled output content as hex lines
---@return string[]
function M.get_last_output_content()
    return M.assembler.last_output_content
end

--- Get the source file path that was last assembled
---@return string|nil
function M.get_last_source_path()
    return M.assembler.last_source_path
end

return M