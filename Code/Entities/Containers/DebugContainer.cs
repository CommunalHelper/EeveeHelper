using Celeste.Mod.EeveeHelper.Components;
using Celeste.Mod.EeveeHelper.Handlers;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.EeveeHelper.Entities.Containers;

[CustomEntity("EeveeHelper/DebugContainer")]
public class DebugContainer : Entity, IContainer
{
	public EntityContainer Container { get; private set; }

	public DebugContainer(EntityData data, Vector2 offset) : base(data.Position + offset)
	{
		Collider = new Hitbox(data.Width, data.Height);

		Add(Container = new EntityContainer()
		{
			WhitelistAll = true,
			Mode = data.Enum("containMode", EntityContainer.ContainMode.RoomStart),
			OnAttach = OnAttach
		});
	}

	private void OnAttach(IEntityHandler handler)
	{
		// TODO: Replace entity index with Entity ID

		var entityCount = 0;
		foreach (var entity in Scene.Entities)
		{
			if (entity != this && entity.GetType() == handler.Entity.GetType())
			{
				entityCount++;
			}

			if (entity == handler.Entity)
			{
				break;
			}
		}

		var logText = $"{handler.Entity.GetType().Name} | #{entityCount.ToString(3)} in room";
		if (handler.GetType() != typeof(EntityHandler))
		{
			logText += $" | Handled by: {handler.GetType().FullName}";
		}

		Logger.Log(LogLevel.Info, "EeveeHelper", $"DEBUG - Attached: {logText}");
	}
}
