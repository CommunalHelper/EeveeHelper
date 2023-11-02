using Celeste.Mod.EeveeHelper.Components;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.EeveeHelper.Entities;

[CustomEntity("EeveeHelper/FloatyContainer")]
public class FloatyContainer : Entity, IContainer
{
	public EntityContainer Container => _Container;

	public EntityContainerMover _Container { get; set; }
	private bool disablePush;
	private float floatSpeed;
	private float floatMove;
	private float pushSpeed;
	private float pushMove;
	private float sinkSpeed;
	private float sinkMove;

	private Vector2 anchorPosition;
	private float yLerp;
	private float sinkTimer;
	private float sineWave;
	private float dashEase;
	private Vector2 dashDirection;
	private bool liftSpeedFix;

	public FloatyContainer(EntityData data, Vector2 offset) : base(data.Position + offset)
	{
		Collider = new Hitbox(data.Width, data.Height);
		Depth = Depths.Top - 10;

		disablePush = data.Bool("disablePush");

		floatSpeed = data.Float("floatSpeed", 1f);
		floatMove = data.Float("floatMove", 4f);
		pushSpeed = data.Float("pushSpeed", 1f);
		pushMove = data.Float("pushMove", 8f);
		sinkSpeed = data.Float("sinkSpeed", 1f);
		sinkMove = data.Float("sinkMove", 12f);

		if (!data.Bool("disableSpawnOffset"))
		{
			sineWave = Calc.Random.NextFloat((float)Math.PI * 2f);
		}

		Add(_Container = new EntityContainerMover(data, fitContained: false)
		{
			DefaultIgnored = e => e is FloatyContainer
		});
		liftSpeedFix = data.Bool("liftSpeedFix", false);

		anchorPosition = Position;
	}

	public override void Awake(Scene scene)
	{
		base.Awake(scene);
		if (disablePush)
		{
			return;
		}

		foreach (var contained in _Container.Contained)
		{
			if (contained.Entity is Platform platform)
			{
				var prevCollision = platform.OnDashCollide;
				platform.OnDashCollide = null;
				platform.OnDashCollide = (player, direction) =>
				{
					var result = prevCollision?.Invoke(player, direction) ?? DashCollisionResults.NormalCollision;
					if (this.dashEase <= 0.2f)
					{
						this.dashEase = 1f;
						this.dashDirection = direction;
					}
					if (result == DashCollisionResults.NormalCollision)
					{
						return DashCollisionResults.NormalOverride;
					}
					else
					{
						return result;
					}
				};
			}
		}
	}

	public override void Update()
	{
		base.Update();

		if (!_Container.Attached)
		{
			return;
		}

		if (HasRider())
		{
			sinkTimer = 0.3f;
		}
		else if (sinkTimer > 0f)
		{
			sinkTimer -= Engine.DeltaTime;
		}

		if (sinkTimer > 0f)
		{
			yLerp = Calc.Approach(yLerp, 1f, sinkSpeed * Engine.DeltaTime);
		}
		else
		{
			yLerp = Calc.Approach(yLerp, 0f, sinkSpeed * Engine.DeltaTime);
		}

		sineWave += Engine.DeltaTime;
		dashEase = Calc.Approach(dashEase, 0f, Engine.DeltaTime * 1.5f * pushSpeed);

		var lastPos = Position;
		_Container.DoMoveAction(MoveToTarget, null, liftSpeedFix);
	}

	private void MoveToTarget()
	{
		var sine = (float)Math.Sin(sineWave * floatSpeed) * floatMove;
		var push = Calc.YoYo(Ease.QuadIn(dashEase)) * dashDirection * pushMove;

		var targetY = MathHelper.Lerp(anchorPosition.Y, anchorPosition.Y + sinkMove * (HasRider() ? 3 : 1), Ease.SineInOut(yLerp)) + sine;
		Position = new Vector2(anchorPosition.X + push.X, targetY + push.Y);
	}

	private bool HasRider()
	{
		foreach (var contained in _Container.Contained)
		{
			if ((contained.Entity is Solid solid && solid.HasPlayerRider()) || (contained.Entity is JumpThru jumpThru && jumpThru.HasPlayerRider()))
			{
				return true;
			}
		}

		return false;
	}
}
