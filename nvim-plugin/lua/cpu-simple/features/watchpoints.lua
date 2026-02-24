local M = {}

local micro_phases = {
  "FetchOpcode", "FetchOperand", "FetchOperand16Low", "FetchOperand16High",
  "MemoryRead", "MemoryWrite", "JumpToInterrupt",
  "AluOp", "EffectiveAddrComputation", "ValueComposition", "Done",
}

M.phases = micro_phases

function M.new(ctx, backend_feature)
  local feature = {}

  local commands = ctx.deps.get("commands")
  local backend = ctx.deps.get("backend")

  feature.add_write_watchpoint = backend_feature.with_running_backend(function(address)
    backend.send(string.format("%s %d", commands.WATCH_WRITE, address))
  end)

  feature.add_read_watchpoint = backend_feature.with_running_backend(function(address)
    backend.send(string.format("%s %d", commands.WATCH_READ, address))
  end)

  feature.add_phase_watchpoint = backend_feature.with_running_backend(function(phase)
    backend.send(string.format("%s %s", commands.WATCH_PHASE, phase))
  end)

  feature.remove_watchpoint = backend_feature.with_running_backend(function(id)
    backend.send(string.format("%s %d", commands.WATCH_REMOVE, id))
  end)

  feature.clear_all_watchpoints = backend_feature.with_running_backend(function()
    backend.send(commands.WATCH_CLR)
  end)

  function feature.list_watchpoints()
    local state = ctx.deps.get("state")
    if #state.watchpoints == 0 then
      vim.notify("No watchpoints set.", vim.log.levels.INFO)
    else
      local lines = vim.tbl_map(function(wp)
        return string.format("[%d] %s", wp.id, wp.description)
      end, state.watchpoints)
      vim.notify("Watchpoints:\n" .. table.concat(lines, "\n"), vim.log.levels.INFO)
    end
  end

  return feature
end

return M
