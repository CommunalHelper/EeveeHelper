module EeveeHelperInputToggleBlock

using ..Ahorn, Maple

@mapdef Entity "EeveeHelper/InputToggleBlock" InputToggleBlock(x::Integer, y::Integer, width::Integer=16, height::Integer=16,
    texture::String="EeveeHelper/inputToggleBlock", type::String="Grab", tutorialFlag::String="", time::Number=0.5, cancellable::Bool=true)

function inputBlockFinalizer(entity)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 16))
    height = Int(get(entity.data, "height", 16))

    entity.data["nodes"] = [(x + width, y)]
end

const placements = Ahorn.PlacementDict(
    "Input Toggle Block (Eevee Helper)" => Ahorn.EntityPlacement(
        InputToggleBlock,
        "rectangle",
        Dict{String, Any}(),
        inputBlockFinalizer
    )
)

Ahorn.editingOptions(entity::InputToggleBlock) = Dict{String, Any}(
    "type" => ["Grab", "Jump", "Dash", "Custom"]
)

Ahorn.nodeLimits(entity::InputToggleBlock) = 1, 1

Ahorn.minimumSize(entity::InputToggleBlock) = 16, 16
Ahorn.resizable(entity::InputToggleBlock) = true, true

function Ahorn.selection(entity::InputToggleBlock)
    x, y = Ahorn.position(entity)
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(stopX, stopY, width, height)]
end

const spriteTypes = Dict{String, String}(
    "Grab" => "grab",
    "Jump" => "jump",
    "Dash" => "dash",
    "Custom" => "custom"
)

const colorTypes = Dict{String, Any}(
    "Grab" => (1.0, 0.0, 1.0),
    "Jump" => (1.0, 1.0, 0.0),
    "Dash" => (0.0, 1.0, 1.0),
    "Custom" => (0.0, 0.0, 1.0)
)

function renderInputBlock(ctx::Ahorn.Cairo.CairoContext, x::Int, y::Int, width::Int, height::Int, sprite::String, type::String)
    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    frame = "objects/$sprite/block_$(spriteTypes[type])"

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + height - 8, 8, 16, 8, 8)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x, y + (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, frame, x + width - 8, y + (i - 1) * 8, 16, 8, 8, 8)
    end

    for i in 2:tilesWidth - 1, j in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + (j - 1) * 8, 8, 8, 8, 8)
    end

    Ahorn.drawImage(ctx, frame, x, y, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y, 16, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x, y + height - 8, 0, 16, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y + height - 8, 16, 16, 8, 8)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::InputToggleBlock, room::Maple.Room)
    sprite = get(entity.data, "texture", "EeveeHelper/inputToggleBlock")
    type = get(entity.data, "type", "Grab")
    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    renderInputBlock(ctx, stopX, stopY, width, height, sprite, type)
    Ahorn.drawArrow(ctx, startX + width / 2, startY + height / 2, stopX + width / 2, stopY + height / 2, Ahorn.colors.selection_selected_fc, headLength=6)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::InputToggleBlock, room::Maple.Room)
    sprite = get(entity.data, "texture", "EeveeHelper/inputToggleBlock")
    type = get(entity.data, "type", "Grab")

    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    startX = x + width / 2
    startY = y + height / 2
    stopX += width / 2
    stopY += height / 2

    r, g, b = colorTypes[type]

    Ahorn.drawLines(ctx, [(startX, startY), (stopX, stopY)], (r, g, b, 0.2), thickness=4)
    Ahorn.drawImage(ctx, "objects/$sprite/node_$(spriteTypes[type])", stopX - 4, stopY - 4, 0, 0, 8, 8)
    renderInputBlock(ctx, x, y, width, height, sprite, type)
end

end