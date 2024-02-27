local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local drawableNinePatch = require("structs.drawable_nine_patch")
local utils = require("utils")

local inputToggleBlock = {
    name = "EeveeHelper/InputToggleBlock",
    depth = -9999,
    nodeVisibility = "never",
    nodeLimits = {1, 1},
    minimumSize = {16, 16},

    placements = {
        default = {
            data = {
                width = 16,
                height = 16,
                texture = "EeveeHelper/inputToggleBlock",
                type = "Grab",
                tutorialFlag = "",
                time = 0.5,
                cancellable = true
            }
        },
        {
            name = "grab",
            data = { type = "Grab", width = 16, height = 16 }
        },
        {
            name = "jump",
            data = { type = "Jump", width = 16, height = 16 }
        },
        {
            name = "dash",
            data = { type = "Dash", width = 16, height = 16 }
        },
        {
            name = "custom",
            data = { type = "Custom", width = 16, height = 16 }
        }
    },

    fieldInformation = {
        type = {
            options = { "Grab", "Jump", "Dash", "Custom" },
            editable = false
        }
    },

    ignoredFieldsMultiple = { "x", "y", "width", "height", "nodes", "type", "tutorialFlag" },
}

local pathColors = {
    ["Grab"] = {1.0, 0.0, 1.0, 0.2},
    ["Jump"] = {1.0, 1.0, 0.0, 0.2},
    ["Dash"] = {0.0, 1.0, 1.0, 0.2},
    ["Custom"] = {0.0, 0.0, 1.0, 0.2},
}

local function addNodeSprites(sprites, entity, nodeTexture, centerX, centerY, centerNodeX, centerNodeY)
    local nodeSprite = drawableSprite.fromTexture(nodeTexture, entity)

    nodeSprite:setPosition(centerNodeX, centerNodeY)
    nodeSprite:setJustification(0.5, 0.5)

    local points = {centerX, centerY, centerNodeX, centerNodeY}

    local pathColor = pathColors[entity.type or "Grab"] or pathColors["Grab"]
    local pathLine = drawableLine.fromPoints(points, pathColor, 2)

    pathLine.depth = 5000

    for _, sprite in ipairs(pathLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    table.insert(sprites, nodeSprite)
end

local function addBlockSprites(sprites, entity, blockTexture, x, y, width, height)
    local ninePatchOptions = {
        mode = "fill",
        borderMode = "repeat"
    }
    local frameNinePatch = drawableNinePatch.fromTexture(blockTexture, ninePatchOptions, x, y, width, height)
    local frameSprites = frameNinePatch:getDrawableSprite()

    for _, sprite in ipairs(frameSprites) do
        table.insert(sprites, sprite)
    end
end

function inputToggleBlock.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeX, nodeY = nodes[1].x, nodes[1].y

    local centerX, centerY = x + halfWidth, y + halfHeight
    local centerNodeX, centerNodeY = nodeX + halfWidth, nodeY + halfHeight

    local textureType = string.lower(entity.type or "Grab")
    local texturePath = "objects/" .. (entity.texture or "EeveeHelper/inputToggleBlock")
    local blockTexturePath = texturePath .. "/block_" .. textureType
    local nodeTexturePath = texturePath .. "/node_" .. textureType

    addNodeSprites(sprites, entity, nodeTexturePath, centerX, centerY, centerNodeX, centerNodeY)
    addBlockSprites(sprites, entity, blockTexturePath, x, y, width, height)

    return sprites
end

function inputToggleBlock.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeX, nodeY = nodes[1].x, nodes[1].y
    local nodeWidth, nodeHeight = 8, 8
    local centerNodeX, centerNodeY = nodeX + halfWidth, nodeY + halfHeight

    local mainRectangle = utils.rectangle(x, y, width, height)
    local nodeRectangle = utils.rectangle(centerNodeX - math.floor(nodeWidth / 2), centerNodeY - math.floor(nodeHeight / 2), nodeWidth, nodeHeight)

    return mainRectangle, {nodeRectangle}
end

return inputToggleBlock