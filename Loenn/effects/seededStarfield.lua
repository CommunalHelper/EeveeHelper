local seededStarfield = {}

seededStarfield.name = "EeveeHelper/SeededStarfield"

seededStarfield.defaultData = {
    textureDir = "particles/starfield",
    color = "ffffff",
    alpha = 1.0,
    scrollx = 1.0,
    scrolly = 1.0,
    speed = 1.0,
    seed = 0
}

seededStarfield.fieldOrder = {
    "only", "exclude", "tag", "flag",
    "textureDir", "color", "alpha", "notflag",
    "seed", "speed", "scrollx", "scrolly"
}

seededStarfield.fieldInformation = {
    color = {
        fieldType = "color",
        allowEmpty = true
    },
    alpha = {
        minimumValue = 0.0,
        maximumValue = 1.0
    },
    seed = {
        fieldType = "integer"
    }
}

return seededStarfield