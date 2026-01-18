$currentEnv = @{
    xdgConfigHome=$env:XDG_CONFIG_HOME
    xdgDataHome=$env:XDG_DATA_HOME
    xdgStateHome=$env:XDG_STATE_HOME
    xdgCache=$env:XDG_CACHE_HOME
    nvimAppName=$env:NVIM_APPNAME
};

# Update environment
$baseFolder = Resolve-Path (Join-Path $PSScriptRoot "..");
$AppName = "nvim-plugin";
$env:XDG_CONFIG_HOME=$baseFolder;
$env:XDG_DATA_HOME=$baseFolder;
$env:XDG_STATE_HOME=$baseFolder;
$env:XDG_CACHE_HOME=$env:TEMP;

# Set nvim app name (it will be appended to all XDG variables)
$env:NVIM_APPNAME = $AppName;

# Build argument list
$argList = @()
if ($Stdin) {
    $argList += $Stdin
}
if ($RemainingArguments) {
    $argList += $RemainingArguments
}

# Call vim
nvim @argList

# Restore environment
$env:NVIM_APPNAME = $currentEnv.nvimAppName;
$env:XDG_CONFIG_HOME = $currentEnv.xdgConfigHome;
$env:XDG_DATA_HOME = $currentEnv.xdgDataHome;
$env:XDG_STATE_HOME = $currentEnv.xdgStateHome;
$env:XDG_CACHE_HOME = $currentEnv.xdgCache;