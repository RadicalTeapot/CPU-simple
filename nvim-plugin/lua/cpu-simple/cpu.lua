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

return M