module EeveeHelperCoreZone

using ..Ahorn, Maple

@mapdef Entity "EeveeHelper/CoreZone" CoreZone(x::Integer, y::Integer, width::Integer=8, height::Integer=8,
    zoneId::String="", coreMode::String="Current", colorGradeMask::Bool=false, stylegroundMask::Bool=false)

const placements = Ahorn.PlacementDict(
    "Core Zone (Eevee Helper)" => Ahorn.EntityPlacement(
        CoreZone,
        "rectangle"
    )
)

Ahorn.minimumSize(entity::CoreZone) = 8, 8
Ahorn.resizable(entity::CoreZone) = true, true

Ahorn.editingOptions(entity::CoreZone) = Dict{String, Any}(
    "coreMode" => ["Current", "Inverted", "Hot", "Cold", "None"]
)

Ahorn.selection(entity::CoreZone) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CoreZone, room::Maple.Room)
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.drawRectangle(ctx, 0, 0, width, height, (0.2, 1.0, 1.0, 0.4), (0.2, 1.0, 1.0, 1.0))
end

end