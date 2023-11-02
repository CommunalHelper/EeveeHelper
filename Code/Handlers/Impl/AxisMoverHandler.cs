using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;

namespace Celeste.Mod.EeveeHelper.Handlers.Impl;

internal class AxisMoverHandler : EntityHandler, IMoveable
{
	private DynamicData entityData;
	private Tuple<string, bool>[] axesHandler;

	public AxisMoverHandler(Entity entity, params Tuple<string, bool>[] singleAxisHandler) : base(entity)
	{
		entityData = new DynamicData(entity);
		axesHandler = singleAxisHandler;
	}

	public bool Move(Vector2 move, Vector2? liftSpeed)
	{
		if (Entity is Platform p)
		{
			p.MoveH(move.X);
			p.MoveV(move.Y);
		}
		else
		{
			Entity.Position += move;
		}

		foreach (var pair in axesHandler)
		{
			entityData.Set(pair.Item1, entityData.Get<float>("startY") + (pair.Item2 ? move.Y : move.X));
		}

		return true;
	}

	public void PreMove() { }
}
