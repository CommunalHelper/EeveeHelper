module EeveeHelperRoomChestExit

using ..Ahorn, Maple

@mapdef Entity "EeveeHelper/RoomChestExit" RoomChestExit(x::Integer, y::Integer, width::Integer=8, height::Integer=8, visible::Bool=false)

const placements = Ahorn.PlacementDict(
    "Room Chest Exit (Eevee Helper)" => Ahorn.EntityPlacement(
        RoomChestExit,
        "rectangle"
    ),
)

Ahorn.minimumSize(entity::RoomChestExit) = 8, 8
Ahorn.resizable(entity::RoomChestExit) = true, true

Ahorn.selection(entity::RoomChestExit) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::RoomChestExit, room::Maple.Room)
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.drawRectangle(ctx, 0, 0, width, height, (1.0, 0.7, 0.75, 0.4), (1.0, 0.7, 0.75, 1.0))
end

end