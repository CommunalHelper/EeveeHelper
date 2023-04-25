local coreZoneStartController = {
    name = "EeveeHelper/CoreZoneStartController",
    texture = "objects/EeveeHelper/coreZoneStartController/icon",
    depth = -1000000,

    placements = {
        name = "default",
        data = {
            zoneId = "",
            coreMode = "Hot",
            startOnly = false,
            persistent = false
        }
    },

    fieldInformation = {
        coreMode = {
            options = { "Hot", "Cold", "None" },
            editable = false
        }
    }
}

return coreZoneStartController