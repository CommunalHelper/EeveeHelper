using Celeste.Mod.EeveeHelper.Components;
using Celeste.Mod.EeveeHelper.Handlers;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.EeveeHelper.Entities.Modifiers;

[CustomEntity("EeveeHelper/FlagToggleModifier")]
public class FlagToggleModifier : Entity, IContainer
{
	public bool Toggled => string.IsNullOrEmpty(flag) ? !notFlag : SceneAs<Level>().Session.GetFlag(flag) != notFlag;

	private string flag;
	private bool notFlag;

	private bool toggleActive;
	private bool toggleVisible;
	private bool toggleCollidable;

	private bool rememberInitialState;
	private bool delayedToggle;

	public EntityContainer Container { get; set; }
	private Dictionary<IEntityHandler, object> entityStates = new();

	public FlagToggleModifier(EntityData data, Vector2 offset) : base(data.Position + offset)
	{
		Collider = new Hitbox(data.Width, data.Height);
		Depth = Depths.Top - 9;

		var parsedFlag = EeveeUtils.ParseFlagAttr(data.Attr("flag"));
		flag = parsedFlag.Item1;
		notFlag = parsedFlag.Item2;

		if (data.Bool("notFlag"))
		{
			notFlag = !notFlag;
		}

		toggleActive = data.Bool("toggleActive", true);
		toggleVisible = data.Bool("toggleVisible", true);
		toggleCollidable = data.Bool("toggleCollidable", true);

		rememberInitialState = data.Bool("rememberInitialState", true);
		delayedToggle = data.Bool("delayedToggle", true);

		Add(Container = new EntityContainer(data)
		{
			DefaultIgnored = e => e.Get<EntityContainer>() != null,
			OnAttach = OnAttach,
			OnDetach = EnableEntity
		});

		Add(new TransitionListener
		{
			OnIn = (f) => CheckToggled()
		});
	}

	public override void Update()
	{
		base.Update();
		CheckToggled();
	}

	private void CheckToggled()
	{
		if (Toggled)
		{
			EnableEntities();
		}
		else
		{
			DisableEntities();
		}
	}

	private void OnAttach(IEntityHandler handler)
	{
		if (delayedToggle || Toggled)
		{
			return;
		}

		DisableEntity(handler);
	}

	private void DisableEntities()
	{
		foreach (var handler in Container.Contained)
		{
			DisableEntity(handler);
		}
	}

	private void DisableEntity(IEntityHandler handler)
	{
		if (!entityStates.ContainsKey(handler))
		{
			var state = HandlerUtils.GetAs<IToggleable, object>(handler,
				t => rememberInitialState ? t.SaveState() : t.GetDefaultState(),
				e => rememberInitialState ? new EntityState(e) : new EntityState(true));

			if (state != null)
			{
				entityStates.Add(handler, state);
			}
		}

		HandlerUtils.DoAs<IToggleable>(handler,
			t => t.Disable(toggleActive, toggleVisible, toggleCollidable),
			e => EntityState.Disable(e, toggleActive, toggleVisible, toggleCollidable));
	}

	private void EnableEntities()
	{
		foreach (var handler in Container.Contained)
		{
			EnableEntity(handler);
		}

		entityStates.Clear();
	}

	private void EnableEntity(IEntityHandler handler)
	{
		if (entityStates.ContainsKey(handler))
		{
			var state = entityStates[handler];
			HandlerUtils.DoAs<IToggleable>(handler,
				t => t.ReadState(state, toggleActive, toggleVisible, toggleCollidable),
				e => ((EntityState)state).Apply(e, toggleActive, toggleVisible, toggleCollidable));
		}
	}

	public struct EntityState
	{
		public bool Active;
		public bool Visible;
		public bool Collidable;

		public bool TalkComponentEnabled;

		public EntityState(bool value)
		{
			Active = value;
			Visible = value;
			Collidable = value;

			TalkComponentEnabled = value;
		}

		public EntityState(Entity entity)
		{
			Active = entity.Active;
			Visible = entity.Visible;
			Collidable = entity.Collidable;

			var talkComponent = entity.Get<TalkComponent>();
			if (talkComponent != null)
			{
				TalkComponentEnabled = talkComponent.Enabled;
			}
			else
			{
				TalkComponentEnabled = false;
			}
		}

		public void Apply(Entity entity, bool toggleActive, bool toggleVisible, bool toggleCollidable)
		{
			if (toggleActive)
			{
				entity.Active = Active;
			}

			if (toggleVisible)
			{
				entity.Visible = Visible;
			}

			if (toggleCollidable)
			{
				entity.Collidable = Collidable;

				var talkComponent = entity.Get<TalkComponent>();
				if (talkComponent != null)
				{
					talkComponent.Enabled = TalkComponentEnabled;
				}
			}
		}

		public static void Disable(Entity entity, bool toggleActive, bool toggleVisible, bool toggleCollidable)
		{
			if (toggleActive)
			{
				entity.Active = false;
			}

			if (toggleVisible)
			{
				entity.Visible = false;
			}

			if (toggleCollidable)
			{
				entity.Collidable = false;

				var talkComponent = entity.Get<TalkComponent>();
				if (talkComponent != null)
				{
					talkComponent.Enabled = false;
				}
			}
		}
	}
}
