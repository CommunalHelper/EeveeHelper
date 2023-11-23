local lenientCeilingPopController = {
    name = "EeveeHelper/LenientCeilingPopController",
    texture = "objects/EeveeHelper/lenientCeilingPopController/icon",
    depth = -1000000,

    placements = {
        {
            name = "default",
            data = {
                leniencyFrames = 3
            }
        }
    },

    fieldInformation = {
        leniencyFrames = {
            fieldType = "integer"
        }
    }
}

return lenientCeilingPopController