local M = {}

local function ensure_program_loaded(state)
  if not state.loaded_program then
    vim.notify("No program loaded. Use :CpuLoad or :CpuAssemble first.", vim.log.levels.ERROR)
    return false
  end
  return true
end

function M.new(ctx, emulator_feature)
  local feature = {}

  feature.assemble = emulator_feature.with_running_emulator(function()
    local assembler = ctx.deps.get("assembler")
    local display = ctx.deps.get("display")
    local events = ctx.deps.get("events")

    events.on(events.ASSEMBLED, function()
      display.assembled.set_content(assembler.get_last_output_content())

      local panels = ctx.config.assemble_panels or {}
      if panels.assembled ~= false then
        display.assembled.show()
      end
      if panels.status ~= false then
        display.status_panel.show()
      end
      if panels.memory ~= false then
        display.memory.show()
      end
      if panels.stack ~= false then
        display.stack.show()
      end

      ctx.api.load()
    end, { once = true })

    assembler.assemble_current_buffer({
      assembler_path = ctx.config.assembler_path,
      assembler_options = ctx.config.assembler_options,
      cwd = ctx.config.cwd,
    })
  end)

  feature.load = emulator_feature.with_running_emulator(function(path)
    local assembler = ctx.deps.get("assembler")
    local display = ctx.deps.get("display")
    local commands = ctx.deps.get("commands")
    local state = ctx.deps.get("state")
    local emulator = ctx.deps.get("emulator")

    local file_path = path or assembler.get_last_output_path()
    if not file_path then
      vim.notify("No file to load. Provide a path or run :CpuAssemble first", vim.log.levels.ERROR)
      return
    end

    if vim.fn.filereadable(file_path) == 0 then
      vim.notify("File not found: " .. file_path, vim.log.levels.ERROR)
      return
    end

    file_path = vim.fn.fnamemodify(file_path, ":p")

    state.memory = nil
    state.stack = nil
    ctx.runtime.pending_dump_request = false
    ctx.api.clear_pc_virtual_text()
    if display.memory and display.memory.clear_highlight_state then
      display.memory.clear_highlight_state()
    end

    emulator.send(commands.LOAD .. " " .. file_path)

    vim.schedule(function()
      state.loaded_program = file_path
      vim.notify("Loaded program: " .. state.loaded_program, vim.log.levels.INFO)
    end)
  end)

  feature.run = emulator_feature.with_running_emulator(function()
    local state = ctx.deps.get("state")
    if not ensure_program_loaded(state) then
      return
    end

    local emulator = ctx.deps.get("emulator")
    local commands = ctx.deps.get("commands")
    emulator.send(commands.RUN)
  end)

  feature.step = emulator_feature.with_running_emulator(function()
    local state = ctx.deps.get("state")
    if not ensure_program_loaded(state) then
      return
    end

    local emulator = ctx.deps.get("emulator")
    local commands = ctx.deps.get("commands")
    emulator.send(commands.STEP)
  end)

  feature.tick = emulator_feature.with_running_emulator(function()
    local state = ctx.deps.get("state")
    if not ensure_program_loaded(state) then
      return
    end

    local emulator = ctx.deps.get("emulator")
    local commands = ctx.deps.get("commands")
    emulator.send(commands.TICK)
  end)

  feature.step_over = emulator_feature.with_running_emulator(function()
    local state = ctx.deps.get("state")
    if not ensure_program_loaded(state) then
      return
    end

    local emulator = ctx.deps.get("emulator")
    local commands = ctx.deps.get("commands")
    emulator.send(commands.STEP_OVER)
  end)

  feature.step_out = emulator_feature.with_running_emulator(function()
    local state = ctx.deps.get("state")
    if not ensure_program_loaded(state) then
      return
    end

    local emulator = ctx.deps.get("emulator")
    local commands = ctx.deps.get("commands")
    emulator.send(commands.STEP_OUT)
  end)

  feature.reset = emulator_feature.with_running_emulator(function()
    local emulator = ctx.deps.get("emulator")
    local commands = ctx.deps.get("commands")
    emulator.send(commands.RESET)
  end)

  feature.status = emulator_feature.with_running_emulator(function()
    local emulator = ctx.deps.get("emulator")
    local commands = ctx.deps.get("commands")
    emulator.send(commands.STATUS)
  end)

  return feature
end

return M
