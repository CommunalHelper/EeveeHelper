using Celeste.Mod.EeveeHelper.Handlers;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Registry;

namespace Celeste.Mod.EeveeHelper.Components;

[Tracked(true)]
public class EntityContainer : Component
{
	public enum ContainMode
	{
		FlagChanged,
		RoomStart,
		Always,
		DelayedRoomStart
	}

	public List<IEntityHandler> Contained = [];
	public Dictionary<Entity, List<IEntityHandler>> HandlersFor = [];
	public Dictionary<string, HashSet<int>> Blacklist = [];
	public Dictionary<string, HashSet<int>> Whitelist = [];

	public ContainMode Mode = ContainMode.RoomStart;
	public string ContainFlag;
	public bool NotFlag;
	public bool ForceStandardBehavior;
	public bool IgnoreContainerBounds;
	public bool WhitelistAll;

	public Func<Entity, bool> IsValid;
	public Func<Entity, bool> DefaultIgnored;
	public Action<IEntityHandler> OnAttach;
	public Action<IEntityHandler> OnDetach;

	public bool Attached;
	public bool CollideWithContained;

	private List<IEntityHandler> containedSaved = [];
	private bool updatedOnce;

	public EntityContainer() : base(true, true) { }

	public EntityContainer(EntityData data) : this()
	{
		var whitelistString = data.Attr("whitelist");
		if (whitelistString.Equals("all", StringComparison.OrdinalIgnoreCase))
			WhitelistAll = true;
		else
			Whitelist = ParseEntityTypeList(data.Attr("whitelist"));

		Blacklist = ParseEntityTypeList(data.Attr("blacklist"));
		Mode = data.Enum("containMode", ContainMode.FlagChanged);
		var flag = EeveeUtils.ParseFlagAttr(data.Attr("containFlag"));
		ContainFlag = flag.Item1;
		NotFlag = flag.Item2;
		ForceStandardBehavior = data.Bool("forceStandardBehavior", true);
		IgnoreContainerBounds = data.Bool("ignoreContainerBounds");
	}

	public override void EntityAwake()
	{
		base.EntityAwake();

		Attached = string.IsNullOrEmpty(ContainFlag) || SceneAs<Level>().Session.GetFlag(ContainFlag) != NotFlag;

		if (Attached && Mode != ContainMode.DelayedRoomStart)
		{
			AttachInside(true);
		}

		updatedOnce = false;
	}

	public override void Update()
	{
		base.Update();
		Cleanup();

		var newAttached = string.IsNullOrEmpty(ContainFlag) || SceneAs<Level>().Session.GetFlag(ContainFlag) != NotFlag;

		if (!updatedOnce)
		{
			if (newAttached && Mode == ContainMode.DelayedRoomStart)
			{
				Attached = newAttached;

				AttachInside(true);
			}

			updatedOnce = true;
		}

		if (Mode != ContainMode.Always)
		{
			if (newAttached != Attached)
			{
				Attached = newAttached;

				if (Attached)
				{
					AttachInside();
				}
				else
				{
					DetachAll();
				}
			}
		}
		else
		{
			var attachChanged = newAttached != Attached;

			Attached = newAttached;

			if (Attached)
			{
				DetachOutside();
				AttachInside();
			}
			else if (attachChanged)
			{
				DetachAll();
			}
		}
	}

	public virtual List<IEntityHandler> GetHandlersFor(Entity entity)
	{
		if (entity != null && HandlersFor.TryGetValue(entity, out var handlers))
			return handlers;

		return new List<IEntityHandler>();
	}

	public virtual bool HasHandlerFor<T>(Entity entity)
	{
		if (entity != null && HandlersFor.TryGetValue(entity, out var handlers))
			return handlers.Any(h => h is T);

		return false;
	}

	public virtual bool IsFirstHandler(IEntityHandler handler)
	{
		if (HandlersFor.TryGetValue(handler.Entity, out var handlers))
			return handlers[0] == handler;

		return true;
	}

	public virtual List<Entity> GetEntities()
	{
		var list = new List<Entity>();
		foreach (var handler in Contained)
		{
			if (!list.Contains(handler.Entity))
			{
				list.Add(handler.Entity);
			}
		}
		return list;
	}

	protected virtual void AddContained(IEntityHandler handler)
	{
		handler.OnAttach(this);
		handler.Entity.AddOrAppendContainer(this.Entity as IContainer);
		Contained.Add(handler);

		if (!HandlersFor.TryGetValue(handler.Entity, out var handlers))
			HandlersFor[handler.Entity] = handlers = [];
		handlers.Add(handler);
	}

	protected virtual void RemoveContained(IEntityHandler handler)
	{
		Contained.Remove(handler);
		var handlers = HandlersFor[handler.Entity];
		handlers.Remove(handler);
		if (handlers.Count == 0)
			HandlersFor.Remove(handler.Entity);
		handler.OnDetach(this);
	}

	/// <summary>
	/// Parses a comma separated list of C# short type names or entity SIDs, with each type name or SID optionally followed by specific indices to affect, separated by colons.
	/// </summary>
	/// <param name="list">
	/// A comma separated list of C# short type names or entity SIDs, with each type name or SID optionally followed by specific indices to affect, separated by colons (e.g. <c>"MoveBlock,dashBlock,Spring:2,refill:1:2:4"</c>).
	/// If no indices are specified for a type name/SID, an index of <c>-1</c> will be added by default.
	/// </param>
	/// <returns>A dictionary containing C# short type names and entity SIDs as its keys, with hash sets containing their affected indices as its values.</returns>
	protected static Dictionary<string, HashSet<int>> ParseEntityTypeList(string list)
	{
		var result = new Dictionary<string, HashSet<int>>();
		foreach (string entry in list.Split(',', StringSplitOptions.RemoveEmptyEntries))
		{
			var split = entry.Split(':');

			var entityType = split[0];
			if (!result.TryGetValue(entityType, out var affectedIndices))
				result[entityType] = affectedIndices = [];

			if (split.Length >= 2)
			{
				for (int i = 1; i < split.Length; i++)
				{
					if (int.TryParse(split[i], out var affectedIndex))
					{
						affectedIndices.Add(affectedIndex);
					}
				}
			}
			else
			{
				affectedIndices.Add(-1);
			}
		}

		return result;
	}

	/// <summary>
	/// Checks if a type list contains an entity's C# short type name or entity SID.
	/// If the entity has no SID in its <see cref="Entity.SourceData"/>, uses the SIDs given by <see cref="EntityRegistry.GetKnownSidsFromType"/> instead.
	/// </summary>
	/// <param name="typeList">A dictionary containing C# short type names and entity SIDs as its keys.</param>
	/// <param name="entity">An entity with the type or SID to locate in the type list.</param>
	/// <returns> <see langword="true"/> if the type list contains the entity's C# short type name or entity SID; otherwise, <see langword="false"/>.</returns>
	protected static bool TypeListContains(Dictionary<string, HashSet<int>> typeList, Entity entity)
	{
		var type = entity.GetType();

		if (typeList.ContainsKey(type.Name))
			return true;

		if (entity.SourceData?.Name is { } sourceSid
			? typeList.ContainsKey(sourceSid)
			: EntityRegistry.GetKnownSidsFromType(type).Any(typeList.ContainsKey))
			return true;

		return false;
	}

	/// <summary>
	/// Checks if a type list contains an entity's C# short type name or entity SID, and at least one of their sets of affected indices pass a specified check.
	/// If the entity has no SID in its <see cref="Entity.SourceData"/>, uses the SIDs given by <see cref="EntityRegistry.GetKnownSidsFromType"/> instead.
	/// </summary>
	/// <param name="typeList">A dictionary containing C# short type names and entity SIDs as its keys, with hash sets containing their affected indices as its values.</param>
	/// <param name="entity">An entity with the type or SID to locate in the type list.</param>
	/// <param name="affectedIndicesCheck">A check to run on the type list's affected indices for the entity's type or SID.</param>
	/// <returns> <see langword="true"/> if the type list contains the entity's C# short type name or SID and the affected indices for either of them pass the specified <paramref name="affectedIndicesCheck"/>; otherwise, <see langword="false"/>.</returns>
	protected static bool TypeListContains(Dictionary<string, HashSet<int>> typeList, Entity entity, Func<HashSet<int>, bool> affectedIndicesCheck)
	{
		var type = entity.GetType();

		if (typeList.TryGetValue(type.Name, out var affectedIndices) && affectedIndicesCheck(affectedIndices))
			return true;

		if (entity.SourceData?.Name is { } sourceSid
			? typeList.TryGetValue(sourceSid, out affectedIndices) && affectedIndicesCheck(affectedIndices)
			: EntityRegistry.GetKnownSidsFromType(type).Any(typeSid => typeList.TryGetValue(typeSid, out affectedIndices) && affectedIndicesCheck(affectedIndices)))
			return true;

		return false;
	}

	protected virtual bool WhitelistCheck(Entity entity)
	{
		if (TypeListContains(Blacklist, entity, affectedIndices => affectedIndices.Contains(-1)))
			return false;

		if (WhitelistAll)
			return true;

		if (Whitelist.Count == 0)
			return !((DefaultIgnored?.Invoke(entity) ?? false) || entity is Player || entity is SolidTiles || entity is BackgroundTiles || entity is Decal || entity is Trigger || entity is WindController);

		return TypeListContains(Whitelist, entity);
	}

	protected virtual bool WhitelistCheckIndex(Entity entity, int index)
	{
		if (TypeListContains(Blacklist, entity, affectedIndices => affectedIndices.Contains(-1) || affectedIndices.Contains(index)))
			return false;

		if (WhitelistAll || Whitelist.Count == 0)
			return true;

		return TypeListContains(Whitelist, entity, affectedIndices => affectedIndices.Contains(-1) || affectedIndices.Contains(index));
	}

	protected virtual void AttachInside(bool first = false)
	{
		if (first || (Mode != ContainMode.RoomStart && Mode != ContainMode.DelayedRoomStart))
		{
			var entityCountsInside = new Dictionary<Type, int>();
			foreach (var entity in Scene.Entities)
			{
				if (entity != Entity && WhitelistCheck(entity) && (IsValid?.Invoke(entity) ?? true))
				{
					var entityType = entity.GetType();
					entityCountsInside.TryAdd(entityType, 0);

					var anyInside = false;
					var handlers = EntityHandler.CreateAll(entity, this, ForceStandardBehavior);
					foreach (var handler in handlers)
					{
						if (IgnoreContainerBounds || handler.IsInside(this))
						{
							anyInside = true;

							if ((!Contained.Contains(handler) || Mode != ContainMode.Always) && WhitelistCheckIndex(entity, entityCountsInside[entityType] + 1))
							{
								AddContained(handler);
								OnAttach?.Invoke(handler);
							}
						}
					}

					if (anyInside)
						entityCountsInside[entityType]++;
				}
			}
		}
		else
		{
			Cleanup();
			foreach (var handler in containedSaved)
			{
				AddContained(handler);
				OnAttach?.Invoke(handler);
			}
			containedSaved.Clear();
		}
	}

	protected virtual void DetachAll()
	{
		var lastContained = new List<IEntityHandler>(Contained);
		if (Mode == ContainMode.RoomStart || Mode == ContainMode.DelayedRoomStart)
			containedSaved = lastContained;

		foreach (var handler in lastContained)
		{
			RemoveContained(handler);
			OnDetach?.Invoke(handler);
		}
	}

	protected virtual void DetachOutside()
	{
		var toRemove = new List<IEntityHandler>();
		foreach (var handler in Contained)
		{
			if (!IgnoreContainerBounds && !handler.IsInside(this))
			{
				toRemove.Add(handler);
			}
		}
		foreach (var handler in toRemove)
		{
			RemoveContained(handler);
			OnDetach?.Invoke(handler);
		}
	}

	protected void Cleanup()
	{
		Contained.RemoveAll(e => e.Entity?.Scene == null);
	}

	public bool CheckCollision(Entity entity)
	{
		if (IgnoreContainerBounds)
			return true;

		if (entity.Collider == null)
			return entity.X >= Entity.Left && entity.Y >= Entity.Top && entity.X <= Entity.Right && entity.Y <= Entity.Bottom;

		var collidable = entity.Collidable;
		var parentCollidable = Entity.Collidable;
		entity.Collidable = true;
		Entity.Collidable = true;
		CollideWithContained = true;

		var result = Entity.CollideCheck(entity);

		CollideWithContained = false;
		entity.Collidable = collidable;
		Entity.Collidable = parentCollidable;

		return result;
	}

	internal bool CheckDecal(Decal decal)
	{
		if (decal.textures.Count == 0)
			return false;

		var decalTexture = decal.textures[0];
		var decalRect = new Rectangle((int)(decal.Position.X - (decalTexture.ClipRect.Width / 2)), (int)(decal.Position.Y - (decalTexture.ClipRect.Height / 2)), decalTexture.ClipRect.Width, decalTexture.ClipRect.Height);
		return Collide.CheckRect(Entity, decalRect);
	}

	public Rectangle GetContainedBounds()
	{
		var bounds = new Rectangle();

		var first = true;
		foreach (var handler in Contained)
		{
			var rect = handler.GetBounds();
			bounds = first ? rect : Rectangle.Union(bounds, rect);
			first = false;
		}

		return bounds;
	}

	public void DestroyContained()
	{
		foreach (var handler in Contained)
			handler.Destroy();

		Contained.Clear();
	}
}
