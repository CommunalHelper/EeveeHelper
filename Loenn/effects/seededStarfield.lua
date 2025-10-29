local seededStarfield = {}

seededStarfield.name = "EeveeHelper/SeededStarfield"

seededStarfield.defaultData = {
    color = "ffffff",
    scrollx = 1.0,
    scrolly = 1.0,
    speed = 1.0,
    seed = 0
}

seededStarfield.fieldInformation = {
    color = {
        fieldType = "color",
        allowEmpty = true
    },
    seed = {
        fieldType = "integer"
    }
}

return seededStarfield