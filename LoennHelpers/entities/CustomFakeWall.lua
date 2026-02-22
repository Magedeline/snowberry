-- Snowberry compatibility fix for ChroniaHelper's CustomFakeWall entity
-- This provides proper fake wall functionality using Snowberry's helpers

local sb = require("#Snowberry.Editor.LoennInterop.LoennShims")
local fakeTilesHelper = require("helpers.fake_tiles")

-- Default tile type if none specified
local function getDefaultTileType()
    return "3"  -- Default stone tile
end

-- Safe entity property access using Snowberry's SafeEntityProperty
local function safeGet(entity, key, default)
    return sb.SafeEntityProperty(entity, key, default)
end

local customFakeWall = {}

customFakeWall.name = "ChroniaHelper/CustomFakeWall"
customFakeWall.depth = 8999
customFakeWall.canResize = {true, true}

-- Default placement data
customFakeWall.placements = {
    {
        name = "default",
        data = {
            width = 8,
            height = 8,
            tiletype = getDefaultTileType(),
            playTransitionReveal = false
        }
    }
}

-- Safe sprite function that handles nil values using Snowberry's SafeAutotile
function customFakeWall.sprite(room, entity)
    -- Safely get entity properties with defaults
    local width = safeGet(entity, "width", 8)
    local height = safeGet(entity, "height", 8)
    local x = safeGet(entity, "x", 0)
    local y = safeGet(entity, "y", 0)
    local tiletype = safeGet(entity, "tiletype", getDefaultTileType())
    
    -- Use Snowberry's safe autotile system for rendering
    local matrix = sb.SafeAutotile("tilesFg", tiletype, width, height)
    
    return {
        _type = "tileGrid",
        color = {1.0, 1.0, 1.0, 0.7}, -- Semi-transparent like fake walls
        x = x,
        y = y,
        matrix = matrix
    }
end

-- Field information for the entity editor
function customFakeWall.fieldInformation(room, entity)
    return {
        tiletype = {
            fieldType = "snowberry:tileset",
            options = fakeTilesHelper.getTilesOptions("tilesFg")
        },
        playTransitionReveal = {
            fieldType = "boolean"
        }
    }
end

-- Selection rectangle for the entity
function customFakeWall.rectangle(room, entity)
    local width = safeGet(entity, "width", 8)
    local height = safeGet(entity, "height", 8)
    local x = safeGet(entity, "x", 0)
    local y = safeGet(entity, "y", 0)
    
    return x, y, width, height
end

-- Minimum size constraints
function customFakeWall.minimumSize(room, entity)
    return 8, 8
end

return customFakeWall