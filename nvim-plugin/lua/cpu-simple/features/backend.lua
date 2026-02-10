local M = {}
local unpack_args = table.unpack or unpack

function M.new(ctx)
  local feature = {}

  function feature.start()
    local backend = ctx.deps.get("backend")
    backend.start({
      backend_path = ctx.config.backend_path,
      memory_size = ctx.config.memory_size,
      stack_size = ctx.config.stack_size,
      registers = ctx.config.registers,
      cwd = ctx.config.cwd,
    })
  end

  function feature.stop()
    local backend = ctx.deps.get("backend")
    backend.stop()
  end

  function feature.status()
    local backend = ctx.deps.get("backend")
    if backend.is_running() then
      vim.notify("CPU backend is running", vim.log.levels.INFO)
    else
      vim.notify("CPU backend is not running", vim.log.levels.WARN)
    end
  end

  function feature.with_running_backend(fn)
    return function(...)
      local backend = ctx.deps.get("backend")
      if backend.is_running() then
        return fn(...)
      end

      local events = ctx.deps.get("events")
      local args = { ... }
      vim.notify("Backend is not running. Starting it.", vim.log.levels.INFO)
      events.on(events.BACKEND_STARTED, function()
        fn(unpack_args(args))
      end, { once = true })
      feature.start()
    end
  end

  feature.send = feature.with_running_backend(function(cmd)
    local backend = ctx.deps.get("backend")
    backend.send(cmd)
  end)

  function feature.is_running()
    local backend = ctx.deps.get("backend")
    return backend.is_running()
  end

  return feature
end

return M
