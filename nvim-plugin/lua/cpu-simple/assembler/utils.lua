-- Utilities functions for the assembler module

local M = {}

function M.get_address_span_from_line(line, debug_info)
    if not debug_info or not debug_info.spans then
        return nil
    end
    
    -- Find all span matching the current line
    local matching_spans = {}
    for _, span in ipairs(debug_info.spans) do
        if span.line == line then
            table.insert(matching_spans, span)
        end
    end

    local start_address = nil
    local end_address = nil
    for _, span in ipairs(matching_spans) do
        if not start_address or span.start < start_address then
            start_address = span.start
        end
        if not end_address or span.ending > end_address then
            end_address = span.ending
        end
    end
    
    return {start_address = start_address, end_address = end_address}
end

function M.get_source_line_from_address(address, debug_info)
    if not debug_info or not debug_info.spans then
        return nil
    end
    
    for _, span in ipairs(debug_info.spans) do
        if address >= span.start and address <= span.ending then -- inclusive as ending is last byte (not one past)
            return span.line + 1 -- return 1-based line number
        end
    end
    
    return nil
end

return M