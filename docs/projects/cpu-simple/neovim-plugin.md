# Neovim plugin

## Linux

```lua
local cpu_simple = require("cpu-simple")

-- Add tree-sitter to path
vim.env.PATH =
vim.fn.expand("~/CPU-simple/tree-sitter-grammar/node_modules/.bin")
.. ":" .. vim.env.PATH

-- Make sure to setup `mapleader` and `maplocalleader` before
-- loading lazy.nvim so that mappings are correct.
vim.g.mapleader = " "
vim.cmd('color retrobox')

-- Keymappings for common operations
local opts = { noremap = true }

-- Start/stop backend
vim.keymap.set("n", "<leader>cs", "<cmd>CpuBackendStart<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Start backend" }))
vim.keymap.set("n", "<leader>cq", "<cmd>CpuBackendStop<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Stop backend" }))

-- Assemble and load
vim.keymap.set("n", "<leader>ca", "<cmd>CpuAssemble<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Assemble" }))
vim.keymap.set("n", "<leader>cl", "<cmd>CpuLoad<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Load" }))

-- Execution control
vim.keymap.set("n", "<leader>cr", "<cmd>CpuRun<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Run" }))
vim.keymap.set("n", "<leader>cn", "<cmd>CpuStep<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Step" }))
vim.keymap.set("n", "<leader>cR", "<cmd>CpuReset<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Reset" }))

-- Breakpoints
vim.keymap.set("n", "<leader>cb", "<cmd>CpuToggleBp<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Toggle breakpoint" }))
vim.keymap.set("n", "<leader>cB", "<cmd>CpuClearBp<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Clear breakpoints" }))

-- View panels
vim.keymap.set("n", "<leader>cd", "<cmd>CpuToggleDump<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Toggle dump panel" }))
vim.keymap.set("n", "<leader>ce", "<cmd>CpuToggleAssembled<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Toggle assembled panel" }))
vim.keymap.set("n", "<leader>co", "<cmd>CpuOpenSidebar<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Open sidebar" }))
vim.keymap.set("n", "<leader>cc", "<cmd>CpuCloseSidebar<cr>", vim.tbl_extend("force", opts, { desc = "CPU: Close sidebar" }))

-- Auto-assemble on save for CPU assembly files (optional - comment out if not desired)
-- vim.api.nvim_create_autocmd("BufWritePost", {
--     pattern = "*.csasm", -- Adjust pattern to match your assembly file extensions
--     callback = function()
--         vim.cmd("CpuAssemble")
--     end,
--     desc = "Auto-assemble CPU code on save",
-- })

-- Helper to find executable in common locations
local function find_executable(name, search_paths)
    for _, path in ipairs(search_paths) do
        local full_path = vim.fn.expand(path)
        if vim.fn.executable(full_path) == 1 then
            return full_path
        end
    end
    return name -- fallback to name (hope it's in PATH)
end

-- Detect common build paths for Backend and Assembler
local backend_paths = {
    "../Backend/bin/Debug/net10.0/Backend",
    "./Backend/bin/Debug/net10.0/Backend",
    "../Backend/bin/Release/net10.0/Backend",
    "./Backend/bin/Release/net10.0/Backend",
}

local assembler_paths = {
    "../Assembler/bin/Debug/net10.0/Assembler",
    "./Assembler/bin/Debug/net10.0/Assembler",
    "../Assembler/bin/Release/net10.0/Assembler",
    "./Assembler/bin/Release/net10.0/Assembler",
}

cpu_simple.setup({
    -- Path to Backend executable (auto-detected from common locations)
    backend_path = find_executable("Backend", backend_paths),
    -- Path to Assembler executable (auto-detected from common locations)
    assembler_path = find_executable("Assembler", assembler_paths),
    -- Assembler options
    assembler_options = {
        emit_debug = true, -- Required for breakpoint support
    },
    -- CPU configuration
    memory_size = 256,
    stack_size = 16,
    registers = 4,
    -- Working directory (defaults to cwd)
    cwd = nil,
    -- Sidebar configuration
    sidebar = {
        width = 0.5, -- Ratio of editor width (0.5 = half), or absolute columns if > 1
        position = "right", -- "left" or "right"
        panels = {}, -- Panel-specific config: { [panel_id] = { height = 0 } }
    },
    -- Signs configuration
    signs = {
        use_for_breakpoints = true, -- Use sign column for breakpoints
        use_for_pc = true, -- Use sign column for PC
        breakpoint_text = "●", -- Text shown in sign column for breakpoints
        pc_text = "▶", -- Text shown in sign column for PC
    },
})

-- Register language
vim.treesitter.language.add('csasm')
vim.treesitter.language.register('csasm', { 'csasm' })
vim.filetype.add({ extension = { csasm = "csasm" } })

-- This is the new, correct way to enable highlighting and indentation.
local langs = { "csasm" }

local group = vim.api.nvim_create_augroup('MyTreesitterSetup', { clear = true })
vim.api.nvim_create_autocmd('FileType', {
    group = group,
    pattern = langs,
    callback = function(args)
        -- Enable highlighting for the buffer
        vim.treesitter.start(args.buf)
        
        -- Enable indentation for the buffer
        -- vim.bo[args.buf].indentexpr = "v:lua.require'nvim-treesitter'.indentexpr()"
    end,
})
```

On first run, run `npm install` and `npm run build` inside the `tree-sitter-grammar` folder to install all dependencies

The important part of this is the build script places the compiled parser and queries in the `parser` and `queries/csasm` folders respectively so Neovim can find them.

These 3 lines

```lua
vim.treesitter.language.add('csasm')
vim.treesitter.language.register('csasm', { 'csasm' })
vim.filetype.add({ extension = { csasm = "csasm" } })
```

Then add the language to Neovim, connect the parser to the language and connect the file extension with the language.

### Troubleshooting

#### Error loading the parser

If Neovim error when trying to load the parser with `csasm.so: undefined symbol: tree_sitter_csasm` it means that the symbol in the compiled file name doesn't match what neovim expects.

Try the following:

```bash
cd tree-sitter-grammar
npx tree-sitter generate
npx tree-sitter build -o /tmp/csasm.so
nm -D /tmp/csasm.so | rg 'tree_sitter_'
```

You’ll likely see something like `tree_sitter_<something_else>`.

The language name in the line `vim.treesitter.language.register('csasm', { 'csasm' })` should match that name. (language name is the first argument of the register call).

To fix this either change the Neovim config to have the correct name or change the name in the `grammar.js` file:

```js
module.exports = grammar({
  // Name here is the name to use when loading in neovim (when registering the language)
  name: "csasm",
  ...
})
```

Then rebuild the parser