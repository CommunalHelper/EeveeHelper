local coreZoneToggle = {
    name = "EeveeHelper/CoreZoneToggle",
    depth = 2000,

    placements = {
        name = "default",
        data = {
            zoneId = "",
            onlyFire = false,
            onlyIce = false,
            persistent = false
        }
    },

    texture = function (room, entity)
        if entity.onlyIce then
            return "objects/coreFlipSwitch/switch13"
        elseif entity.onlyFire then
            return "objects/coreFlipSwitch/switch15"
        else
            return "objects/coreFlipSwitch/switch01"
        end
    end
}

return coreZoneToggle