-- Event system for cpu-simple
-- Simple pub/sub for reactive updates

local M = {}

-- Event names
M.STATUS_UPDATED = "status_updated"
M.STACK_UPDATED = "stack_updated"
M.MEMORY_UPDATED = "memory_updated"
M.STEP_COMPLETE = "step_complete"
M.BREAKPOINT_UPDATED = "breakpoint_updated"
M.BREAKPOINT_HIT = "breakpoint_hit"
M.BACKEND_STARTED = "backend_started"
M.BACKEND_STOPPED = "backend_stopped"

-- Registered callbacks
M.callbacks = {}

--- Register a callback for an event
---@param event string Event name
---@param callback function Callback function(data)
---@return function unsubscribe Function to remove the callback
function M.on(event, callback)
  if not M.callbacks[event] then
    M.callbacks[event] = {}
  end
  table.insert(M.callbacks[event], callback)

  -- Return unsubscribe function
  return function()
    M.off(event, callback)
  end
end

--- Unregister a callback for an event
---@param event string Event name
---@param callback function Callback to remove
function M.off(event, callback)
  if not M.callbacks[event] then
    return
  end

  for i, cb in ipairs(M.callbacks[event]) do
    if cb == callback then
      table.remove(M.callbacks[event], i)
      return
    end
  end
end

--- Emit an event to all registered callbacks
---@param event string Event name
---@param data any Data to pass to callbacks
function M.emit(event, data)
  if not M.callbacks[event] then
    return
  end

  for _, callback in ipairs(M.callbacks[event]) do
    -- Wrap in pcall to prevent one bad callback from breaking others
    local ok, err = pcall(callback, data)
    if not ok then
      vim.schedule(function()
        vim.notify("Event callback error (" .. event .. "): " .. tostring(err), vim.log.levels.ERROR)
      end)
    end
  end
end

--- Clear all callbacks for an event (or all events if no event specified)
---@param event string|nil Event name, or nil to clear all
function M.clear(event)
  if event then
    M.callbacks[event] = {}
  else
    M.callbacks = {}
  end
end

return M
