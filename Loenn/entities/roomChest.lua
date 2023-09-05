local drawableSprite = require("structs.drawable_sprite")

local roomChest = {
    name = "EeveeHelper/RoomChest",
    depth = 5,
    placements = {
        name = "default",
        data = {
            room = "",
            exitOnDeath = false,
        }
    },
    sprite = function (room, entity)
        return {
            drawableSprite.fromTexture("objects/EeveeHelper/roomChest/lid", entity):addPosition(0, -8):setJustification(.5, 1),
            drawableSprite.fromTexture("objects/EeveeHelper/roomChest/body", entity):addPosition(0, 1):setJustification(.5, 1)
        }
    end,
}

return roomChest