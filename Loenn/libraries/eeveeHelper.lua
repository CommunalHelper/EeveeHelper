local loadedState = require("loaded_state")
local entities = require("entities")

local eeveeHelper = {}

function eeveeHelper.getAllSIDs()
    local sids = {}
    for k, v in pairs(entities.registeredEntities) do
        table.insert(sids, k)
    end
    table.sort(sids)

    return sids
end

function eeveeHelper.getMapSIDs()
    if not loadedState.map then return eeveeHelper.getAllSIDs() end

    local sidsInMap = {}
    for _, room in pairs(loadedState.map.rooms) do
        for _, entity in pairs(room.entities) do
            sidsInMap[entity._name] = true
        end
    end

    local sids = {}
    for k, v in pairs(sidsInMap) do
        table.insert(sids, k)
    end
    table.sort(sids)

    return sids
end

return eeveeHelper