local utils = require("utils")
local drawableSprite = require("structs.drawable_sprite")

local smwPlatform = {
    name = "EeveeHelper/SMWPlatform",
    depth = -60,
    minimumSize = {16, 0},
    canResize = {true, false},

    placements = {
        default = {
            data = {
                width = 40,
                height = 8,
                texturePath = "objects/EeveeHelper/smwPlatform",
                moveSpeed = 100.0,
                fallSpeed = 200.0,
                gravity = 200.0,
                startDelay = 0.0,
                direction = "Right",
                flag = "",
                moveBehaviour = "Linear",
                easing = "SineInOut",
                easeDuration = 2.0,
                easeTrackDirection = false,
                notFlag = false,
                startOnTouch = true,
                stopAtNode = false,
                stopAtEnd = false,
                moveOnce = false,
                disableBoost = false,
                legacyFlagStopBehaviour = false,
            }
        },
        {
            name = "linear",
            data = {
                width = 40,
                height = 8,
                moveBehaviour = "Linear"
            }
        },
        {
            name = "easing",
            data = {
                width = 40,
                height = 8,
                moveBehaviour = "Easing"
            }
        }
    },

    fieldInformation = {
        direction = {
            options = { "Left", "Right" },
            editable = false,
        },
        moveBehaviour = {
            options = { "Linear", "Easing" },
            editable = false,
        },
        easing = {
            options = { "Linear", "SineIn", "SineOut", "SineInOut", "QuadIn", "QuadOut", "QuadInOut", "CubeIn", "CubeOut", "CubeInOut", "QuintIn", "QuintOut", "QuintInOut", "ExpoIn", "ExpoOut", "ExpoInOut" },
            editable = false
        }
    },

    ignoredFields = function (entity)
        local ignored = {"_name", "_id", "originX", "originY", "height"}

        if entity.moveBehaviour == "Linear" then
            table.insert(ignored, "easing")
            table.insert(ignored, "easeDuration")
            table.insert(ignored, "easeTrackDirection")

        elseif entity.moveBehaviour == "Easing" then
            table.insert(ignored, "moveSpeed")

        end

        return ignored
    end,

    selection = function (room, entity)
        return utils.rectangle(entity.x - entity.width / 2, entity.y - 8, entity.width, 8)
    end,

    sprite = function (room, entity)
        local sprites = {}

        local texturePath = entity.texturePath or "objects/EeveeHelper/smwPlatform"
        local platformPath = string.format("%s/platform", texturePath)
        local gearPath = string.format("%s/gear00", texturePath)

        local width = entity.width or 16
        local tiles = width / 8
        for i = 1, tiles, 1 do
            local tx = 1
            if i == 1 then
                tx = 0
            elseif i == tiles then
                tx = 2
            end

            local sprite = drawableSprite.fromTexture(platformPath, entity)
            sprite:useRelativeQuad(tx * 8, 0, 8, 8)
            sprite:addPosition((i - 1) * 8 - width / 2, -8)
            table.insert(sprites, sprite)
        end

        table.insert(sprites, drawableSprite.fromTexture(gearPath, entity))

        return sprites
    end
}

return smwPlatform