local M = {}

function M.register(ctx, api)
  if ctx.runtime.commands_registered then
    return
  end

  local create = vim.api.nvim_create_user_command

  create("CpuBackendStart", function()
    api.backend_start()
  end, {
    desc = "Start the CPU backend process",
  })

  create("CpuBackendStop", function()
    api.backend_stop()
  end, {
    desc = "Stop the CPU backend process",
  })

  create("CpuBackendStatus", function()
    api.backend_status()
  end, {
    desc = "Get backend process status",
  })

  create("CpuAssemble", function()
    api.assemble()
  end, {
    desc = "Assemble the current buffer to machine code",
  })

  create("CpuLoad", function(cmd_opts)
    api.load(cmd_opts.args ~= "" and cmd_opts.args or nil)
  end, {
    desc = "Load machine code into the CPU",
    nargs = "?",
    complete = "file",
  })

  create("CpuRun", function()
    api.run()
  end, {
    desc = "Run the loaded program",
  })

  create("CpuStep", function()
    api.step()
  end, {
    desc = "Execute one CPU instruction",
  })

  create("CpuTick", function()
    api.tick()
  end, {
    desc = "Execute one CPU micro-tick",
  })

  create("CpuStepOver", function()
    api.step_over()
  end, {
    desc = "Step over the current instruction",
  })

  create("CpuStepOut", function()
    api.step_out()
  end, {
    desc = "Step out of the current subroutine",
  })

  create("CpuReset", function()
    api.reset()
  end, {
    desc = "Reset the CPU",
  })

  create("CpuStatus", function()
    api.status()
  end, {
    desc = "Get the current CPU status",
  })

  create("CpuToggleBp", function(cmd_opts)
    if #cmd_opts.args == 0 then
      api.set_breakpoint_at_cursor()
      return
    end

    local address = tonumber(cmd_opts.args)
    if not address then
      vim.notify("Invalid address: " .. cmd_opts.args, vim.log.levels.ERROR)
      return
    end

    api.set_breakpoint(address)
  end, {
    desc = "Toggle breakpoint at address (or at cursor if no address given)",
    nargs = "?",
  })

  create("CpuClearBp", function()
    api.clear_all_breakpoints()
  end, {
    desc = "Clear all breakpoints",
  })

  create("CpuNextBp", function()
    api.goto_next_breakpoint()
  end, {
    desc = "Go to next breakpoint",
  })

  create("CpuPrevBp", function()
    api.goto_prev_breakpoint()
  end, {
    desc = "Go to previous breakpoint",
  })

  create("CpuGotoDef", function()
    api.goto_definition()
  end, {
    desc = "Go to symbol definition",
  })

  create("CpuGotoPC", function()
    api.goto_PC()
  end, {
    desc = "Go to PC",
  })

  create("CpuRunToCursor", function()
    api.run_to_cursor()
  end, {
    desc = "Run to cursor line",
  })

  create("CpuToggleStatus", function()
    api.toggle_status()
  end, {
    desc = "Toggle the CPU status panel",
  })

  create("CpuToggleStack", function()
    api.toggle_stack()
  end, {
    desc = "Toggle the CPU stack panel",
  })

  create("CpuToggleMemory", function()
    api.toggle_memory()
  end, {
    desc = "Toggle the CPU memory panel",
  })

  -- Backward-compatible alias used by existing keymaps/docs.
  create("CpuToggleDump", function()
    api.toggle_dump()
  end, {
    desc = "Toggle the CPU dump panel",
  })

  create("CpuToggleAssembled", function()
    api.toggle_assembled()
  end, {
    desc = "Toggle the assembled code panel",
  })

  create("CpuOpenSidebar", function()
    api.open_sidebar()
  end, {
    desc = "Open the sidebar",
  })

  create("CpuCloseSidebar", function()
    api.close_sidebar()
  end, {
    desc = "Close all sidebar panels",
  })

  ctx.runtime.commands_registered = true
end

return M
