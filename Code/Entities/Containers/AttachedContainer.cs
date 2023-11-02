using Celeste.Mod.EeveeHelper.Components;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.EeveeHelper.Entities;

[CustomEntity("EeveeHelper/AttachedContainer")]
public class AttachedContainer : Entity, IContainer
{
	public EntityContainer Container => _Container;
	private EntityContainer.ContainMode attachMode;
	private string attachFlag;
	private bool notAttachFlag;
	private string attachTo;
	private bool restrictToNode;
	private bool onlyX;
	private bool onlyY;
	private bool matchCollidable;
	private bool matchVisible;
	private bool destroyable;
	private Vector2? node;

	public EntityContainerMover _Container;
	private StaticMover mover;
	private bool attached;
	private Entity customAttached;
	private Vector2 lastAttachedPos;
	private Entity firstAttached = null;
	private Tuple<bool, bool> lastAttachedState = Tuple.Create(true, true);
	private Dictionary<Entity, bool> lastMatchCollidable = new();
	private Dictionary<Entity, bool> lastMatchVisible = new();
	private Dictionary<Entity, Tuple<bool, bool, bool>> lastStates = new();
	private bool legacyDestroySometimesAnyway;
	private bool updatedOnce;

	public AttachedContainer(EntityData data, Vector2 offset) : base(data.Position + offset)
	{
		Collider = new Hitbox(data.Width, data.Height);
		Depth = Depths.Top - 11;

		attachMode = data.Enum("attachMode", EntityContainer.ContainMode.RoomStart);
		var parsedFlag = EeveeUtils.ParseFlagAttr(data.Attr("attachFlag"));
		attachFlag = parsedFlag.Item1;
		notAttachFlag = parsedFlag.Item2;
		attachTo = data.Attr("attachTo");
		restrictToNode = data.Bool("restrictToNode");
		onlyX = data.Bool("onlyX");
		onlyY = data.Bool("onlyY");
		matchCollidable = data.Bool("matchCollidable");
		matchVisible = data.Bool("matchVisible");
		destroyable = data.Bool("destroyable");
		legacyDestroySometimesAnyway = !data.Has("destroyable");

		var nodes = data.NodesOffset(offset);
		if (nodes.Length > 0)
		{
			node = nodes[0] - Center;
		}

		Add(_Container = new EntityContainerMover(data)
		{
			OnFit = OnFit,
			IsValid = e => !IsValid(e),
			DefaultIgnored = e => e is AttachedContainer
		});

		if (string.IsNullOrEmpty(attachTo) && attachMode == EntityContainer.ContainMode.RoomStart)
		{
			Add(mover = new StaticMover
			{
				JumpThruChecker = (entity) => IsRiding(entity, true),
				SolidChecker = (entity) => IsRiding(entity, true),
				OnMove = (amount) => { if (attached) { OnMove(amount); } },
				OnShake = (amount) => { if (attached) { OnMoveSprites(amount); } },
				OnEnable = () => { if (attached) { OnEnable(); } },
				OnDisable = () => { if (attached) { OnDisable(); } },
				OnDestroy = () => { if (attached && (destroyable || legacyDestroySometimesAnyway)) { RemoveSelf(); } }
			});
		}
	}

	private void OnFit(Vector2 pos, float width, float height)
	{
		var lastCenter = Center;
		Position = pos;
		Collider.Width = width;
		Collider.Height = height;
		_Container.DoMoveAction(() => Center = lastCenter);
	}

	public override void Awake(Scene scene)
	{
		attached = string.IsNullOrEmpty(attachFlag) || SceneAs<Level>().Session.GetFlag(attachFlag) != notAttachFlag;

		if (attachMode == EntityContainer.ContainMode.RoomStart)
		{
			TryAttach(true);
			if (!attached)
			{
				customAttached = null;
			}
		}
		else if (attachMode != EntityContainer.ContainMode.DelayedRoomStart)
		{
			if (attached)
			{
				TryAttach();
			}
		}

		updatedOnce = false;

		base.Awake(scene);
	}

	public override void Update()
	{
		base.Update();

		var newAttached = string.IsNullOrEmpty(attachFlag) || SceneAs<Level>().Session.GetFlag(attachFlag) != notAttachFlag;

		if (!updatedOnce)
		{
			if (newAttached && attachMode == EntityContainer.ContainMode.DelayedRoomStart)
			{
				attached = newAttached;

				TryAttach(true);
			}

			updatedOnce = true;
		}

		if (attachMode != EntityContainer.ContainMode.Always)
		{
			if (newAttached != attached)
			{
				attached = newAttached;

				if (attached)
				{
					TryAttach();
				}
				else
				{
					customAttached = null;
				}
			}
		}
		else
		{
			var attachChanged = newAttached != attached;

			attached = newAttached;

			if (attached && customAttached == null)
			{
				TryAttach();
			}
			else if (!attached)
			{
				customAttached = null;
			}
		}

		if (customAttached != null)
		{
			var newPos = EeveeUtils.GetPosition(customAttached);
			if (newPos != lastAttachedPos)
			{
				var delta = newPos - lastAttachedPos;
				if (onlyX) delta.Y = 0f;
				if (onlyY) delta.X = 0f;
				_Container.DoMoveAction(() => Position += delta);
				lastAttachedPos = newPos;
			}
			// legacy check
			if (legacyDestroySometimesAnyway && customAttached.Scene == null)
			{
				RemoveSelf();
				return;
			}
		}

		var attachedTo = customAttached ?? mover?.Platform;

		if (attachedTo != null && attachedTo.Scene == null)
		{
			attachedTo = null;
			if (destroyable)
			{
				RemoveSelf();
				return;
			}
		}

		var currentState = attachedTo != null ? Tuple.Create(attachedTo.Collidable, attachedTo.Visible) : Tuple.Create(true, true);
		if (attachedTo != null)
		{
			foreach (var entity in _Container.GetEntities())
			{
				if (matchCollidable && currentState.Item1 != lastAttachedState.Item1)
				{
					if (!currentState.Item1)
					{
						lastMatchCollidable[entity] = entity.Collidable;
						entity.Collidable = false;
					}
					else
					{
						entity.Collidable = lastMatchCollidable[entity];
						lastMatchCollidable.Remove(entity);
					}
				}
				if (matchVisible && currentState.Item2 != lastAttachedState.Item2)
				{
					if (!currentState.Item2)
					{
						lastMatchVisible[entity] = entity.Visible;
						entity.Visible = false;
					}
					else
					{
						entity.Visible = lastMatchVisible[entity];
						lastMatchVisible.Remove(entity);
					}
				}
			}
		}
		lastAttachedState = currentState;
	}

	private bool TryAttach(bool first = false)
	{
		if (mover == null)
		{
			if (first || (attachMode != EntityContainer.ContainMode.RoomStart && attachMode != EntityContainer.ContainMode.DelayedRoomStart))
			{
				var closestDist = 0f;
				Entity closest = null;
				foreach (var entity in Scene.Entities)
				{
					if (string.IsNullOrEmpty(attachTo) ? (entity is JumpThru || entity is Solid) : entity.GetType().Name == attachTo)
					{
						if (node != null && entity.CollidePoint(Center + node.Value))
						{
							closest = entity;
							break;
						}
						if (node == null || !restrictToNode)
						{
							if (!string.IsNullOrEmpty(attachTo))
							{
								var dist = Vector2.Distance(node != null ? (Center + node.Value) : Center, entity.Center);
								if (closest == null || dist < closestDist)
								{
									closestDist = dist;
									closest = entity;
								}
							}
							else if (IsRiding(entity, false))
							{
								closest = entity;
							}
						}
					}
				}
				if (closest != null)
				{
					customAttached = closest;
					lastAttachedPos = EeveeUtils.GetPosition(customAttached);
					return true;
				}
			}
			else
			{
				customAttached = firstAttached;
				if (customAttached != null)
				{
					lastAttachedPos = EeveeUtils.GetPosition(customAttached);
				}

				return true;
			}
		}
		return false;
	}

	private bool IsValid(Entity entity)
	{
		if (mover != null)
		{
			return entity == mover.Platform;
		}
		else
		{
			return entity == customAttached;
		}
	}

	private bool IsRiding(Entity entity, bool checkNode)
	{
		if (checkNode && node != null)
		{
			return entity.CollidePoint(Center + node.Value);
		}
		else if (entity is JumpThru)
		{
			return CollideCheckOutside(entity, Position + Vector2.UnitY);
		}
		else
		{
			return CollideCheckOutside(entity, Position + Vector2.UnitY)
				|| CollideCheckOutside(entity, Position - Vector2.UnitY)
				|| CollideCheckOutside(entity, Position + Vector2.UnitX)
				|| CollideCheckOutside(entity, Position - Vector2.UnitX);
		}
	}

	private void OnMove(Vector2 amount)
	{
		if (onlyX) amount.Y = 0f;
		if (onlyY) amount.X = 0f;

		_Container.DoMoveAction(() => Position += amount, (h, move) => mover.Platform.LiftSpeed);
	}

	private void OnMoveSprites(Vector2 amount)
	{
		if (onlyX) amount.Y = 0f;
		if (onlyY) amount.X = 0f;

		foreach (var ieH in _Container.Contained)
		{
			foreach (var gc in ieH.Entity.Components.GetAll<GraphicsComponent>())
			{
				if (gc != null)
				{
					gc.RenderPosition += amount;
				}
			}
		}
	}

	private void OnEnable()
	{
		Active = Visible = Collidable = true;
		foreach (var entity in _Container.GetEntities())
		{
			var state = lastStates[entity];
			entity.Active = state.Item1;
			entity.Visible = state.Item2;
			entity.Collidable = state.Item3;
		}
		lastStates.Clear();
	}

	private void OnDisable()
	{
		Active = Visible = Collidable = false;
		foreach (var entity in _Container.GetEntities())
		{
			lastStates.Add(entity, Tuple.Create(entity.Active, entity.Visible, entity.Collidable));
			entity.Active = entity.Visible = entity.Collidable = false;
		}
	}
}
