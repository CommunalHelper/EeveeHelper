local mods = require("mods")

local styleMaskHelperLoaded = mods.hasLoadedMod("StyleMaskHelper")

local coreZone = {
    name = "EeveeHelper/CoreZone",
    depth = 10000,
    fillColor = { 0.2, 1.0, 1.0, 0.4 },
    borderColor = { 0.2, 1.0, 1.0, 1.0 },

    placements = {
        default = {
            data = {
                width = 8,
                height = 8,
                zoneId = "",
                coreMode = "Current",
                colorGradeMask = false,
                stylegroundMask = false
            }
        },
        {
            name = "current",
            data = { coreMode = "Current", width = 8, height = 8 }
        },
        {
            name = "inverted",
            data = { coreMode = "Inverted", width = 8, height = 8 }
        },
        {
            name = "hot",
            data = { coreMode = "Hot", width = 8, height = 8  }
        },
        {
            name = "cold",
            data = { coreMode = "Cold", width = 8, height = 8  }
        }
    },

    fieldInformation = {
        coreMode = {
            options = { "Current", "Inverted", "Hot", "Cold", "None" },
            editable = false
        }
    },

    ignoredFields = function (entity)
        local ignored = {"_name", "_id", "originX", "originY"}

        if not styleMaskHelperLoaded then
            if not entity.colorGradeMask then
                table.insert(ignored, "colorGradeMask")
            end
            if not entity.stylegroundMask then
                table.insert(ignored, "stylegroundMask")
            end
        end

        return ignored
    end,

    associatedMods = function (entity)
        if entity.colorGradeMask or entity.stylegroundMask then
            return {"EeveeHelper", "StyleMaskHelper"}
        else
            return {"EeveeHelper"}
        end
    end
}

return coreZone