local sprite = app.sprite

-- Check constraints
if sprite == nil then
  app.alert("No Sprite...")
  return
end
if sprite.colorMode ~= ColorMode.INDEXED then
  app.alert("Sprite needs to be indexed")
  return
end

local function get1bppTileData(img, x, y, gridSize)
	local values = {}
    for yoff = 0, gridSize - 1 do
        local byte = 0
        for xoff = 0, gridSize - 1 do
            local pix = img:getPixel(x * gridSize + xoff, y * gridSize + yoff)
            local bit = (pix ~= 0) and 1 or 0
            byte = byte | (bit << xoff)
        end
        values[yoff + 1] = byte
    end

    return string.pack("<" .. string.rep("B", gridSize), table.unpack(values))
end

local function exportFrame(f, gridSize)
	local img = Image(sprite.spec)
	img:drawSprite(sprite, 1) -- Use first frame

    local xCount = sprite.width // gridSize
    local yCount = sprite.height // gridSize
    for spriteY = 0,yCount-1 do -- row by row
        for spriteX = 0,xCount-1 do
	        f:write(get1bppTileData(img, spriteX, spriteY, gridSize))
        end
    end
end


local dlg = Dialog()
dlg:file{ id="exportFile",
          label="File",
          title="1BPP Export",
          open=false,
          save=true,
          filetypes={"rom"},
          entry=true
        }
dlg:number{ id="gridSize", label="Grid size", text="8"}
dlg:button{ id="ok", text="OK", onclick=function()
    local data = dlg.data

    local gridSize = tonumber(data.gridSize)
    if not gridSize or gridSize <= 0 then
        app.alert("Grid size must be a positive number")
        return
    end

    if sprite.width % gridSize ~= 0 or sprite.height % gridSize ~= 0 then
        app.alert("Sprite dimensions must be divisible by grid size")
        return
    end

	local f, err = io.open(data.exportFile, "wb")
    if not f then
        app.alert("Failed to open file: " .. err)
        return
    end
	exportFrame(f, gridSize)
	io.close(f)
    dlg:close()
end }
dlg:button{ id="cancel", text="Cancel", onclick=function() dlg:close() end }
dlg:show()