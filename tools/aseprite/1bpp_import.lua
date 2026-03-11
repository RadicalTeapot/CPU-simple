local function loadFileBytes(path)
  local f, err = io.open(path, "rb")
  if not f then
    return nil, err
  end

  local data = f:read("*all")
  f:close()

  if not data then
    return nil, "Failed to read file"
  end

  return data
end

local function createIndexedSpriteFromRom(data, gridSize, gridRows)
  local bytesPerTile = gridSize
  local totalBytes = #data

  if totalBytes == 0 then
    app.alert("The selected file is empty.")
    return
  end

  if totalBytes % bytesPerTile ~= 0 then
    app.alert(
      "Invalid ROM size.\n" ..
      "Expected file size to be a multiple of grid size.\n" ..
      "File size: " .. totalBytes .. " bytes\n" ..
      "Grid size: " .. gridSize
    )
    return
  end

  local tileCount = totalBytes // bytesPerTile

  if gridRows <= 0 then
    app.alert("Grid rows must be a positive number.")
    return
  end

  local gridCols = math.ceil(tileCount / gridRows)

  local width = gridCols * gridSize
  local height = gridRows * gridSize

  local spr = Sprite(width, height, ColorMode.INDEXED)
  spr.filename = ""

  -- Optional: set a simple black/white palette
  local pal = spr.palettes[1]
  pal:setColor(0, Color{ r=0,   g=0,   b=0,   a=255 })
  pal:setColor(1, Color{ r=255, g=255, b=255, a=255 })
  spr:setPalette(pal)

  local img = Image(spr.spec)
  img:clear(0)

  local offset = 1

  for tileIndex = 0, tileCount - 1 do
    local tileX = tileIndex % gridCols
    local tileY = tileIndex // gridCols

    for yoff = 0, gridSize - 1 do
      local byte = string.byte(data, offset)
      offset = offset + 1

      for xoff = 0, gridSize - 1 do
        local bit = (byte >> xoff) & 1
        local colorIndex = (bit ~= 0) and 1 or 0

        img:putPixel(
          tileX * gridSize + xoff,
          tileY * gridSize + yoff,
          colorIndex
        )
      end
    end
  end

  local layer = spr.layers[1]
  local cel = spr.cels[1]

  if cel then
    cel.image = img
  else
    spr:newCel(layer, 1, img, Point(0, 0))
  end

  app.sprite = spr
end

local dlg = Dialog{ title = "Import 1BPP ROM" }

dlg:file{
  id = "importFile",
  label = "ROM file",
  title = "Import 1BPP ROM",
  open = true,
  save = false,
  filetypes = { "rom" },
  entry = true
}

dlg:number{
  id = "gridSize",
  label = "Grid size",
  text = "8"
}

dlg:number{
  id = "gridRows",
  label = "Tile rows",
  text = "1"
}

dlg:button{
  id = "ok",
  text = "OK",
  onclick = function()
    local data = dlg.data

    local gridSize = tonumber(data.gridSize)
    local gridRows = tonumber(data.gridRows)

    if not gridSize or gridSize <= 0 then
      app.alert("Grid size must be a positive number.")
      return
    end

    if not gridRows or gridRows <= 0 then
      app.alert("Tile rows must be a positive number.")
      return
    end

    local bytes, err = loadFileBytes(data.importFile)
    if not bytes then
      app.alert("Failed to open file: " .. err)
      return
    end

    createIndexedSpriteFromRom(bytes, gridSize, gridRows)
    dlg:close()
  end
}

dlg:button{
  id = "cancel",
  text = "Cancel",
  onclick = function()
    dlg:close()
  end
}

dlg:show()