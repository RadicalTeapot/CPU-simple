-- Utilities functions for the assembler module

local M = {}

function M.get_address_span_from_line(line, debug_info)
    if not debug_info or not debug_info.spans then
        return nil
    end
    
    -- Find all span matching the current line
    local matching_spans = {}
    for _, span in ipairs(debug_info.spans) do
        if span.source_line == line then
            table.insert(matching_spans, span)
        end
    end

    local start_address = nil
    local end_address = nil
    for _, span in ipairs(matching_spans) do
        if not start_address or span.start_address < start_address then
            start_address = span.start_address
        end
        if not end_address or span.end_address > end_address then
            end_address = span.end_address
        end
    end
    
    return {start_address = start_address, end_address = end_address}
end

--- Get the address span (start_address, end_address) that contains the given address
---@param address number Address to look up
---@param debug_info table Debug info with spans
---@return table|nil {start_address = number, end_address = number} or nil
function M.get_address_span_from_address(address, debug_info)
    if not debug_info or not debug_info.spans then
        return nil
    end

    for _, span in ipairs(debug_info.spans) do
        if address >= span.start_address and address <= span.end_address then
            return { start_address = span.start_address, end_address = span.end_address }
        end
    end

    return nil
end

function M.get_source_line_from_address(address, debug_info)
    if not debug_info or not debug_info.spans then
        return nil
    end
    
    for _, span in ipairs(debug_info.spans) do
        if address >= span.start_address and address <= span.end_address then -- inclusive as end_address is last byte (not one past)
            return span.source_line + 1 -- return 1-based line number
        end
    end
    
    return nil
end

return M