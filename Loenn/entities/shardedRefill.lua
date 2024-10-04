local drawableSprite = require("structs.drawable_sprite")

local shardedRefill = {
	name = "EeveeHelper/RefillShard",
	depth = -1000000,
	nodeLimits = {1, -1},
	nodeLineRenderType = "fan",
	nodeVisibility = "always",
	nodeDepth = -100,

	placements = {
		name = "default",
		data = {
			spawnRefill = false,
			twoDashes = false,
			resetOnGround = false,
			oneUse = false,
			collectAmount = 0,
		}
	},

	fieldInformation = {
		collectAmount = {
			fieldType = "integer"
		}
	},

	sprite = function(room, entity)
		local twoDashes = entity.twoDashes
		local spawnRefill = entity.spawnRefill

		local path = twoDashes and "objects/refillTwo/" or "objects/refill/"

		local sprite = drawableSprite.fromTexture(path .. "outline", entity):setJustification(0.5, 0.5)

		if not spawnRefill then
			sprite:setAlpha(0.5)
		end

		return sprite
	end,

	nodeSprite = function(room, entity, node, nodeIndex)
		local twoDashes = entity.twoDashes

		local path = twoDashes and "objects/EeveeHelper/refillShard/two00" or "objects/EeveeHelper/refillShard/one00"

		local sprite = drawableSprite.fromTexture(path, node):setJustification(0.5, 0.5)
		local borderSprite = drawableSprite.fromTexture("objects/EeveeHelper/refillShard/border", node):setJustification(0.5, 0.5)

		return { borderSprite, sprite }
	end
}

return shardedRefill