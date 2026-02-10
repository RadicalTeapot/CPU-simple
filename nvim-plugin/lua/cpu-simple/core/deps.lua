local M = {}

local MODULE_PATHS = {
  backend = "cpu-simple.backend",
  assembler = "cpu-simple.assembler",
  display = "cpu-simple.display",
  state = "cpu-simple.state",
  events = "cpu-simple.events",
  commands = "cpu-simple.commands",
  operand_resolver = "cpu-simple.operand_resolver",
}

M._cache = {}
M._providers = {}

local function load_default(name)
  local module_path = MODULE_PATHS[name]
  if not module_path then
    error("Unknown dependency: " .. tostring(name))
  end

  local ok, mod = pcall(require, module_path)
  if not ok then
    error(string.format("Failed to load dependency '%s' (%s): %s", name, module_path, tostring(mod)))
  end
  return mod
end

function M.get(name)
  if M._cache[name] ~= nil then
    return M._cache[name]
  end

  local provider = M._providers[name]
  local mod = provider and provider() or load_default(name)
  M._cache[name] = mod
  return mod
end

function M.get_many(names)
  local out = {}
  for _, name in ipairs(names) do
    out[name] = M.get(name)
  end
  return out
end

function M.with(names, fn)
  return function(...)
    return fn(M.get_many(names), ...)
  end
end

function M.set(name, value)
  M._cache[name] = value
end

function M.override(name, provider)
  M._providers[name] = provider
  M._cache[name] = nil
end

function M.clear_cache()
  M._cache = {}
end

function M.reset()
  M._cache = {}
  M._providers = {}
end

return M
