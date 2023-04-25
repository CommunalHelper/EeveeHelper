module EeveeHelperCoreZoneStartController

using ..Ahorn, Maple

@mapdef Entity "EeveeHelper/CoreZoneStartController" CoreZoneStartController(x::Integer, y::Integer,
    zoneId::String="", coreMode::String="Hot", startOnly::Bool=false, persistent::Bool=false)

const placements = Ahorn.PlacementDict(
    "Core Zone Start Controller (Eevee Helper)" => Ahorn.EntityPlacement(
        CoreZoneStartController
    )
)

Ahorn.editingOptions(entity::CoreZoneStartController) = Dict{String, Any}(
    "coreMode" => ["Hot", "Cold", "None"]
)

function Ahorn.selection(entity::CoreZoneStartController)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 12, y - 12, 24, 24)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CoreZoneStartController, room::Maple.Room)
    Ahorn.drawSprite(ctx, "objects/EeveeHelper/coreZoneStartController/icon.png", 0, 0)
end

end