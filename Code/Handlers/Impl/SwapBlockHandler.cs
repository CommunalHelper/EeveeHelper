using Celeste.Mod.EeveeHelper.Components;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.EeveeHelper.Handlers.Impl;

public class SwapBlockHandler : EntityHandler, IMoveable, IAnchorProvider
{
	private bool first;
	private DynamicData entityData;
	public const string HandledString = "EeveeHelper_SwapBlockHandled";

	public SwapBlockHandler(Entity entity, bool first) : base(entity)
	{
		this.first = first;

		entityData = DynamicData.For(entity as SwapBlock);

		entityData.Set(HandledString, true);
	}

	public override bool IsInside(EntityContainer container)
	{
		return InsideCheck(container, first, entityData);
	}

	public override Rectangle GetBounds()
	{
		var pos = entityData.Get<Vector2>(first ? "start" : "end");
		return new Rectangle((int)pos.X, (int)pos.Y, 0, 0);
	}

	public bool Move(Vector2 move, Vector2? liftSpeed)
	{
		var anchor = first ? "start" : "end";
		entityData.Set(anchor, entityData.Get<Vector2>(anchor) + move);
		var percent = entityData.Get<float>("lerp");
		var start = entityData.Get<Vector2>("start");
		var target = entityData.Get<Vector2>("end");
		var newPos = Vector2.Lerp(start, target, percent);

		var SwapBlock = Entity as SwapBlock;
		SwapBlock.MoveTo(newPos, SwapBlock.LiftSpeed);
		if (first)
		{
			var rect = entityData.Get<Rectangle>("moveRect");
			rect.X = Math.Min((int)target.X, (int)start.X);
			rect.Y = Math.Min((int)target.Y, (int)start.Y);
			entityData.Set("moveRect", rect);
		}
		return true;
	}

	public void PreMove() { }

	// Don't use default anchor handling
	public List<string> GetAnchors() => new();

	public static bool InsideCheck(EntityContainer container, bool first, DynamicData data)
	{
		var pos = data.Get<Vector2>(first ? "start" : "end");
		return pos.X >= container.Entity.Left && pos.Y >= container.Entity.Top &&
			pos.X + container.Entity.Width <= container.Entity.Right && pos.Y + container.Entity.Height <= container.Entity.Bottom;
	}
}
