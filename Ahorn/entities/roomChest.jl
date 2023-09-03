module EeveeHelperRoomChest

using ..Ahorn, Maple

@mapdef Entity "EeveeHelper/RoomChest" RoomChest(x::Integer, y::Integer, room::String="", exitOnDeath::Bool=false)

const placements = Ahorn.PlacementDict(
    "Room Chest (Eevee Helper)" => Ahorn.EntityPlacement(
        RoomChest
    )
)

function Ahorn.selection(entity::RoomChest)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 10, y - 20, 20, 20)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::RoomChest, room::Maple.Room)
    Ahorn.drawSprite(ctx, "objects/EeveeHelper/roomChest/lid", 0, -8, jx=0.5, jy=1.0)
    Ahorn.drawSprite(ctx, "objects/EeveeHelper/roomChest/body", 0, 1, jx=0.5, jy=1.0)
end

end