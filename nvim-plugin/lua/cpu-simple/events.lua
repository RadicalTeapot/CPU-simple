-- Event system for cpu-simple
-- Simple pub/sub for reactive updates

local M = {}

-- Event names
M.ASSEMBLED = "program_assembled"
M.STATUS_UPDATED = "status_updated"
M.STACK_UPDATED = "stack_updated"
M.MEMORY_UPDATED = "memory_updated"
M.STEP_COMPLETE = "step_complete"
M.BREAKPOINT_UPDATED = "breakpoint_updated"
M.BREAKPOINT_HIT = "breakpoint_hit"
M.BACKEND_STARTED = "backend_started"
M.BACKEND_STOPPED = "backend_stopped"

-- Registered subscribers for each event ({callback = function, opts = {once = bool}})
M.subscribers = {}

local DEFAULT_OPTS = {
  once = false, -- If true, unsubscribe after first call
}

--- Register a callback for an event
---@param event string Event name
---@param callback function Callback function(data)
---@return function unsubscribe Function to remove the callback
function M.on(event, callback, opts)
  if not M.subscribers[event] then
    M.subscribers[event] = {}
  end
  opts = vim.tbl_extend("force", DEFAULT_OPTS, opts or {})
  table.insert(M.subscribers[event], {callback = callback, opts = opts})

  -- Return unsubscribe function
  return function()
    M.off(event, callback)
  end
end

--- Unregister a callback for an event
---@param event string Event name
---@param callback function Callback to remove
function M.off(event, callback)
  if not M.subscribers[event] then
    return
  end

  for i, subscriber in ipairs(M.subscribers[event]) do
    if subscriber.callback == callback then
      table.remove(M.subscribers[event], i)
      return
    end
  end
end

--- Emit an event to all registered callbacks
---@param event string Event name
---@param data any Data to pass to callbacks
function M.emit(event, data)
  if not M.subscribers[event] then
    return
  end

  for _, subscriber in ipairs(M.subscribers[event]) do
    -- Wrap in pcall to prevent one bad callback from breaking others
    local ok, err = pcall(subscriber.callback, data)
    if not ok then
      vim.schedule(function()
        vim.notify("Event callback error (" .. event .. "): " .. tostring(err), vim.log.levels.ERROR)
      end)
    else
      -- If once option is set, remove the subscriber
      if subscriber.opts.once then
        M.off(event, subscriber.callback)
      end
    end
  end
end

--- Clear all callbacks for an event (or all events if no event specified)
---@param event string|nil Event name, or nil to clear all
function M.clear(event)
  if event then
    M.subscribers[event] = {}
  else
    M.subscribers = {}
  end
end

return M
