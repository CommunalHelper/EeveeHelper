module EeveeHelperCoreZoneToggle

using ..Ahorn, Maple

@mapdef Entity "EeveeHelper/CoreZoneToggle" CoreZoneToggle(x::Integer, y::Integer, zoneId::String="", onlyFire::Bool=false, onlyIce::Bool=false, persistent::Bool=false)

const placements = Ahorn.PlacementDict(
    "Core Zone Toggle (Eevee Helper)" => Ahorn.EntityPlacement(
        CoreZoneToggle
    )
)

function switchSprite(entity::CoreZoneToggle)
    onlyIce = get(entity.data, "onlyIce", false)
    onlyFire = get(entity.data, "onlyFire", false)

    if onlyIce
        return "objects/coreFlipSwitch/switch13.png"

    elseif onlyFire
        return "objects/coreFlipSwitch/switch15.png"

    else
        return "objects/coreFlipSwitch/switch01.png"
    end
end

function Ahorn.selection(entity::CoreZoneToggle)
    x, y = Ahorn.position(entity)
    sprite = switchSprite(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CoreZoneToggle, room::Maple.Room)
    sprite = switchSprite(entity)

    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end