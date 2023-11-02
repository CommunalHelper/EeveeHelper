using Celeste.Mod.EeveeHelper.Components;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.EeveeHelper.Handlers.Impl;

public class ZipMoverNodeHandler : EntityHandler, IMoveable, IAnchorProvider
{
	private bool first;
	private DynamicData entityData;
	private DynamicData pathRendererData;

	public ZipMoverNodeHandler(Entity entity, bool first) : base(entity)
	{
		this.first = first;

		entityData = DynamicData.For(entity as ZipMover);

		if (first)
		{
			entityData.Set("zipMoverNodeHandled", true);
		}
	}

	public override bool IsInside(EntityContainer container)
	{
		return InsideCheck(container, first, entityData);
	}

	public override Rectangle GetBounds()
	{
		var pos = entityData.Get<Vector2>(first ? "start" : "target");
		return new Rectangle((int)pos.X, (int)pos.Y, 0, 0);
	}

	public bool Move(Vector2 move, Vector2? liftSpeed)
	{
		var anchor = first ? "start" : "target";
		entityData.Set(anchor, entityData.Get<Vector2>(anchor) + move);
		var percent = entityData.Get<float>("percent");
		var start = entityData.Get<Vector2>("start");
		var target = entityData.Get<Vector2>("target");
		var newPos = Vector2.Lerp(start, target, percent);

		var zipMover = Entity as ZipMover;
		zipMover.MoveTo(newPos, zipMover.LiftSpeed);

		if (pathRendererData == null)
		{
			var pathRenderer = entityData.Get<object>("pathRenderer");
			if (pathRenderer != null)
			{
				pathRendererData = new DynamicData(pathRenderer);
			}
		}
		if (pathRendererData != null)
		{
			UpdatePathRenderer(start, target);
		}

		return true;
	}

	private void UpdatePathRenderer(Vector2 newFrom, Vector2 newTo)
	{
		var from = newFrom + new Vector2(Entity.Width / 2f, Entity.Height / 2f);
		var to = newTo + new Vector2(Entity.Width / 2f, Entity.Height / 2f);
		var angle = (from - to).Angle();
		pathRendererData.Set("from", from);
		pathRendererData.Set("to", to);
		pathRendererData.Set("sparkAdd", (from - to).SafeNormalize(5f).Perpendicular());
		pathRendererData.Set("sparkDirFromA", angle + ((float)Math.PI / 8f));
		pathRendererData.Set("sparkDirFromB", angle - ((float)Math.PI / 8f));
		pathRendererData.Set("sparkDirToA", angle + (float)Math.PI - ((float)Math.PI / 8f));
		pathRendererData.Set("sparkDirToB", angle + (float)Math.PI + ((float)Math.PI / 8f));
	}

	public void PreMove() { }

	// Don't use default anchor handling
	public List<string> GetAnchors() => new();


	public static bool InsideCheck(EntityContainer container, bool first, DynamicData data)
	{
		var pos = data.Get<Vector2>(first ? "start" : "target");
		return pos.X >= container.Entity.Left && pos.Y >= container.Entity.Top &&
			pos.X <= container.Entity.Right && pos.Y <= container.Entity.Bottom;
	}
}
