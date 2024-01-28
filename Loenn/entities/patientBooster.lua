local utils = require("utils")

local patientBooster = {
    name = "EeveeHelper/PatientBooster",
    depth = -8500,
    placements = {
        default = {
            data = {
                red = false,
                sprite = "",
                respawnDelay = 1.0,
                refillDashes = "",
                refillStamina = true,
            }
        },
        {
            name = "default",
            data = {
                red = false,
            }
        },
        {
            name = "red",
            data = {
                red = true,
            }
        }
    },
    texture = function (room, entity)
        return entity.red and "objects/EeveeHelper/patientBooster/boosterRed00" or "objects/EeveeHelper/patientBooster/booster00"
    end,
    selection = function (room, entity)
        return utils.rectangle(entity.x - 11, entity.y - 9, 22, 18)
    end,
}

return patientBooster
