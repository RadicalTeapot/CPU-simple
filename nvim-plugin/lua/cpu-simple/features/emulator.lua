local M = {}
local unpack_args = table.unpack or unpack

function M.new(ctx)
  local feature = {}

  function feature.start()
    local emulator = ctx.deps.get("emulator")
    emulator.start({
      emulator_path = ctx.config.emulator_path,
      memory_size = ctx.config.memory_size,
      stack_size = ctx.config.stack_size,
      registers = ctx.config.registers,
      cwd = ctx.config.cwd,
    })
  end

  function feature.stop()
    local emulator = ctx.deps.get("emulator")
    emulator.stop()
  end

  function feature.status()
    local emulator = ctx.deps.get("emulator")
    if emulator.is_running() then
      vim.notify("CPU emulator is running", vim.log.levels.INFO)
    else
      vim.notify("CPU emulator is not running", vim.log.levels.WARN)
    end
  end

  function feature.with_running_emulator(fn)
    return function(...)
      local emulator = ctx.deps.get("emulator")
      if emulator.is_running() then
        return fn(...)
      end

      local events = ctx.deps.get("events")
      local args = { ... }
      vim.notify("Emulator is not running. Starting it.", vim.log.levels.INFO)
      events.on(events.EMULATOR_STARTED, function()
        fn(unpack_args(args))
      end, { once = true })
      feature.start()
    end
  end

  feature.send = feature.with_running_emulator(function(cmd)
    local emulator = ctx.deps.get("emulator")
    emulator.send(cmd)
  end)

  function feature.is_running()
    local emulator = ctx.deps.get("emulator")
    return emulator.is_running()
  end

  return feature
end

return M
