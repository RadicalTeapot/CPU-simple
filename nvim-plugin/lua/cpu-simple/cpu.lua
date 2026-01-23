-- Cpu module for cpu-simple
-- Main interface for managing the CPU via backend process

local M = {}
-- Configuration (set by setup())
M.config = {}

--- Run the loaded program
function M.run()
    if not M.backend then
        M.backend = require("cpu-simple.backend")
    end
    
    if not M.backend.is_running() then
        vim.notify("Backend is not running. Start it with :CpuStart", vim.log.levels.ERROR)
        return
    end
    
    M.backend.send("run")
end

--- Execute one CPU instruction
function M.step()
    if not M.backend then
        M.backend = require("cpu-simple.backend")
    end
    
    if not M.backend.is_running() then
        vim.notify("Backend is not running. Start it with :CpuStart", vim.log.levels.ERROR)
        return
    end
    
    M.backend.send("step")
end

--- Reset the CPU
function M.reset()
    if not M.backend then
        M.backend = require("cpu-simple.backend")
    end
    
    if not M.backend.is_running() then
        vim.notify("Backend is not running. Start it with :CpuStart", vim.log.levels.ERROR)
        return
    end
    
    M.backend.send("reset")
end

-- Get the CPU status
function M.status()
    if not M.backend then
        M.backend = require("cpu-simple.backend")
    end
    
    if not M.backend.is_running() then
        vim.notify("Backend is not running. Start it with :CpuStart", vim.log.levels.ERROR)
        return
    end
    
    M.backend.send("status")
end

-- Get full CPU dump
function M.dump()
    if not M.backend then
        M.backend = require("cpu-simple.backend")
    end
    
    if not M.backend.is_running() then
        vim.notify("Backend is not running. Start it with :CpuStart", vim.log.levels.ERROR)
        return
    end
    
    M.backend.send("dump")
    
    -- TODO Wait for response and display nicely
    -- Open a new buffer in a floating window to display the dump
    vim.schedule(function()
        local buf = vim.api.nvim_create_buf(false, true)
        local width = math.floor(vim.o.columns * 0.8)
        local height = math.floor(vim.o.lines * 0.8)
        local row = math.floor((vim.o.lines - height) / 2)
        
        local opts = {
            style = "minimal",
            relative = "editor",
            width = width,
            height = height,
            row = row,
            col = math.floor((vim.o.columns - width) / 2),
            border = "single",
        }
        local win = vim.api.nvim_open_win(buf, true, opts)
        vim.api.nvim_buf_set_option(buf, "bufhidden", "wipe")
        
        local lines = {}
        if (M.backend.status) then
            table.insert(lines, "=== CPU DUMP ===")
            table.insert(lines, string.format("Cycles: %d, PC: %d SP: %d", M.backend.status.cycles, M.backend.status.pc, M.backend.status.sp))
            table.insert(lines, string.format("Flags - Z: %d C: %d", M.backend.status.flags.zero, M.backend.status.flags.carry))
            table.insert(lines, string.format("R0: %d R1: %d R2: %d R3: %d", M.backend.status.registers[1], M.backend.status.registers[2], M.backend.status.registers[3], M.backend.status.registers[4]))
        end
        
        if (M.backend.stack) then
            table.insert(lines, "=== STACK ===")
            for i, val in ipairs(M.backend.stack) do
                table.insert(lines, string.format("[%d]: %d", i - 1, val))
            end
        end

        if (M.backend.memory) then
            table.insert(lines, "=== MEMORY ===")
            for addr, val in pairs(M.backend.memory) do
                table.insert(lines, string.format("[%d]: %d", addr, val))
            end
        end

        vim.api.nvim_buf_set_lines(buf, 0, -1, false, lines)
    end)
end

return M