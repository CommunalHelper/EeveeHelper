using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.EeveeHelper.Entities;

[Tracked]
[CustomEntity("EeveeHelper/HoldableTiles")]
public class HoldableTiles : Actor
{
	public EntityID ID;
	public Vector2 Speed;
	public Holdable Hold;
	public HoldableTilesSolid Solid;
	private char tiletype;
	private TileGrid tiles;
	private bool holdable;
	private bool destroyable;
	private bool noDuplicate;
	private float noGravityTimer;
	private Vector2 prevLiftSpeed;
	private Vector2 previousPosition;

	public HoldableTiles(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset + new Vector2(data.Width / 2f, data.Height))
	{
		tiletype = data.Char("tiletype");
		holdable = data.Bool("holdable", true);
		noDuplicate = data.Bool("noDuplicate");
		destroyable = data.Bool("destroyable", true);

		ID = id;
		Collider = new Hitbox(data.Width, data.Height);
		Collider.Position = new Vector2(-Width / 2f, -Height);
		Depth = Depths.FGTerrain + 2;

		Add(new LightOcclude(1f));
		Add(tiles = GFX.FGAutotiler.GenerateBox(tiletype, (int)(Width / 8), (int)(Height / 8)).TileGrid);
		tiles.Position = new Vector2(-Width / 2f, -Height);
		if (holdable)
		{
			Add(Hold = new Holdable()
			{
				PickupCollider = new Hitbox(Width + 16f, Height + 16f, (-Width / 2f) - 8f, -Height - 8f),
				SlowFall = false,
				SlowRun = true,
				OnPickup = OnPickup,
				OnRelease = OnRelease,
				OnHitSpring = HitSpring,
				SpeedGetter = () => Speed,
				OnCarry = OnCarry
			});
		}
		SquishCallback = OnSquishNew;
	}

	public override void Added(Scene scene)
	{
		base.Added(scene);
		foreach (HoldableTiles tiles in scene.Tracker.GetEntities<HoldableTiles>())
		{
			if (noDuplicate && tiles != this && tiles.ID.Key == ID.Key && tiles.holdable && tiles.Hold.IsHeld)
			{
				RemoveSelf();
				return;
			}
		}
		scene.Add(Solid = new HoldableTilesSolid(Position, (int)Width, (int)Height, this));
	}

	private void OnPickup()
	{
		Speed = Vector2.Zero;
		Collidable = false;
		Solid.Collidable = false;
		AddTag(Tags.Persistent);
		Solid.AddTag(Tags.Persistent);
	}

	private void OnRelease(Vector2 force)
	{
		if (CollideCheck<Solid>())
		{
			ReleasedSquishWiggle();
			force = Vector2.Zero;
		}
		Collidable = true;
		Solid.Collidable = true;
		RemoveTag(Tags.Persistent);
		Solid.RemoveTag(Tags.Persistent);
		if (force.X != 0f && force.Y == 0f)
		{
			force.Y = -0.4f;
		}

		Speed = force * 200f;
		if (Speed != Vector2.Zero)
		{
			noGravityTimer = 0.1f;
		}
	}

	private void OnCarry(Vector2 target)
	{
		Position = target;
		Solid.MoveTo(Position);
	}

	private void OnSquishNew(CollisionData data)
	{
		if (Collidable)
		{
			TryBigSquishWiggle(data);
		}

		return;
	}

	private bool TryBigSquishWiggle(CollisionData data)
	{
		var collidable = Collidable;
		var solidCollidable = Solid.Collidable;
		Collidable = false;
		Solid.Collidable = false;
		data.Pusher.Collidable = true;
		for (var i = 0; i <= Math.Max(3, (int)Width / 2); i++)
		{
			for (var j = 0; j <= Math.Max(3, (int)Height / 2); j++)
			{
				if (i != 0 || j != 0)
				{
					for (var k = 1; k >= -1; k -= 2)
					{
						for (var l = 1; l >= -1; l -= 2)
						{
							var value = new Vector2(i * k, j * l);
							if (!CollideCheck<Solid>(Position + value))
							{
								Position += value;
								Solid.MoveTo(Position);
								data.Pusher.Collidable = false;
								Collidable = collidable;
								Solid.Collidable = solidCollidable;
								return true;
							}
						}
					}
				}
			}
		}
		data.Pusher.Collidable = false;
		Collidable = collidable;
		Solid.Collidable = solidCollidable;
		return false;
	}

	private bool ReleasedSquishWiggle()
	{
		var collidable = Collidable;
		var solidCollidable = Solid.Collidable;
		Collidable = false;
		Solid.Collidable = false;
		for (var i = 0; i <= Math.Max(3, (int)Width); i++)
		{
			for (var j = 0; j <= Math.Max(3, (int)Height); j++)
			{
				if (i != 0 || j != 0)
				{
					for (var k = 1; k >= -1; k -= 2)
					{
						for (var l = 1; l >= -1; l -= 2)
						{
							var value = new Vector2(i * k, j * l);
							if (!CollideCheck<Solid>(Position + value))
							{
								Position += value;
								Solid.MoveTo(Position);
								Collidable = collidable;
								Solid.Collidable = solidCollidable;
								return true;
							}
						}
					}
				}
			}
		}
		Collidable = collidable;
		Solid.Collidable = solidCollidable;
		return false;
	}

	public override void Update()
	{
		base.Update();

		if (destroyable)
		{
			foreach (SeekerBarrier barrier in Scene.Tracker.GetEntities<SeekerBarrier>())
			{
				barrier.Collidable = true;
				var collided = CollideCheck(barrier);
				barrier.Collidable = false;

				if (collided)
				{
					Audio.Play(SFX.game_10_glider_emancipate, Position);
					var speed = Speed;
					if (holdable && Hold.IsHeld)
					{
						speed = Hold.Holder.Speed * 0.5f;
						Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
					}
					Destroy(speed.SafeNormalize());
					return;
				}
			}
		}

		if (holdable && Hold.IsHeld)
		{
			prevLiftSpeed = Vector2.Zero;
		}
		else
		{
			var level = SceneAs<Level>();
			Spring spring = null;
			if (!holdable)
			{
				foreach (var entity in Scene.Entities)
				{
					if (entity is Spring currentSpring && CollideCheck(entity))
					{
						spring = currentSpring;
					}
				}
			}
			if (spring != null)
			{
				HitSpring(spring);
				spring.BounceAnimate();
			}
			else if (OnGround(1))
			{
				float target;
				if (!OnGround(Position + Vector2.UnitX * 3f, 1))
				{
					target = 20f;
				}
				else if (!OnGround(Position - Vector2.UnitX * 3f, 1))
				{
					target = -20f;
				}
				else
				{
					target = 0f;
				}
				Speed.X = Calc.Approach(Speed.X, target, 800f * Engine.DeltaTime);
				var liftSpeed = LiftSpeed;
				if (liftSpeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero)
				{
					Speed = prevLiftSpeed;
					prevLiftSpeed = Vector2.Zero;
					Speed.Y = Math.Min(Speed.Y * 0.6f, 0f);
					if (Speed.X != 0f && Speed.Y == 0f)
					{
						Speed.Y = -60f;
					}
					if (Speed.Y < 0f)
					{
						noGravityTimer = 0.15f;
					}
				}
				else
				{
					prevLiftSpeed = liftSpeed;
					if (liftSpeed.Y < 0f && Speed.Y < 0f)
					{
						Speed.Y = 0f;
					}
				}
			}
			else if (!holdable || Hold.ShouldHaveGravity)
			{
				var num = 800f;
				if (Math.Abs(Speed.Y) <= 30f)
				{
					num *= 0.5f;
				}
				var num2 = 350f;
				if (Speed.Y < 0f)
				{
					num2 *= 0.5f;
				}
				Speed.X = Calc.Approach(Speed.X, 0f, num2 * Engine.DeltaTime);
				if (noGravityTimer > 0f)
				{
					noGravityTimer -= Engine.DeltaTime;
				}
				else
				{
					Speed.Y = Calc.Approach(Speed.Y, 200f, num * Engine.DeltaTime);
				}
			}
			previousPosition = ExactPosition;
			MoveH(Speed.X * Engine.DeltaTime, OnCollideH, null);
			MoveV(Speed.Y * Engine.DeltaTime, OnCollideV, null);
			if (Left < level.Bounds.Left)
			{
				Left = level.Bounds.Left;
				Solid.MoveToX(Position.X);
				Speed.X *= -0.4f;
			}
			else if (Top < (level.Bounds.Top - 4))
			{
				Top = level.Bounds.Top + 4;
				Solid.MoveToY(Position.Y);
				Speed.Y = 0f;
			}
			else if (Top > level.Bounds.Bottom)
			{
				Die();
				return;
			}
		}
		Hold?.CheckAgainstColliders();
	}

	public override bool IsRiding(Solid solid)
	{
		return solid is not HoldableTilesSolid && base.IsRiding(solid);
	}

	private void Destroy(Vector2 direction)
	{
		if (tiletype == '1')
		{
			Audio.Play("event:/game/general/wall_break_dirt", this.Position);
		}
		else if (tiletype == '3')
		{
			Audio.Play("event:/game/general/wall_break_ice", this.Position);
		}
		else if (tiletype == '9')
		{
			Audio.Play("event:/game/general/wall_break_wood", this.Position);
		}
		else
		{
			Audio.Play("event:/game/general/wall_break_stone", this.Position);
		}
		var num = 0;
		while (num < Width / 8f)
		{
			var num2 = 0;
			while (num2 < Height / 8f)
			{
				Scene.Add(Engine.Pooler.Create<Debris>().Init(this.TopLeft + new Vector2(4 + num * 8, 4 + num2 * 8), tiletype, true).BlastFrom(Center - direction * new Vector2(Width, Height)));
				num2++;
			}
			num++;
		}
		Die();
	}

	private void Die()
	{
		Solid.DestroyStaticMovers();
		Solid.RemoveSelf();
		RemoveSelf();
	}


	private void OnCollideH(CollisionData data)
	{
		if (data.Hit is DashSwitch)
		{
			(data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
		}
		//Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);
		Speed.X *= -0.4f;
	}

	private void OnCollideV(CollisionData data)
	{
		if (data.Hit is DashSwitch)
		{
			(data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
		}
		if (Speed.Y > 0f)
		{
			/*if (hardVerticalHitSoundCooldown <= 0f) {
				Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", Calc.ClampedMap(Speed.Y, 0f, 200f, 0f, 1f));
				hardVerticalHitSoundCooldown = 0.5f;
			} else {
				Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", 0f);
			}*/
		}
		if (Speed.Y > 160f)
		{
			ImpactSfx();
			LandParticles();
		}
		Speed.Y = 0f;
	}

	private void ImpactSfx()
	{
		if (tiletype == '3')
		{
			Audio.Play("event:/game/01_forsaken_city/fallblock_ice_impact", BottomCenter);
			return;
		}
		if (tiletype == '9')
		{
			Audio.Play("event:/game/03_resort/fallblock_wood_impact", BottomCenter);
			return;
		}
		if (tiletype == 'g')
		{
			Audio.Play("event:/game/06_reflection/fallblock_boss_impact", BottomCenter);
			return;
		}
		Audio.Play("event:/game/general/fallblock_impact", BottomCenter);
	}

	private void LandParticles()
	{
		var num = 2;
		while (num <= Width)
		{
			if (Scene.CollideCheck<Solid>(BottomLeft + new Vector2(num, 3f)))
			{
				SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_FallDustA, 1, new Vector2(Left + num, Bottom), Vector2.One * 4f, -1.57079637f);
				float direction;
				if (num < Width / 2f)
				{
					direction = (float)Math.PI;
				}
				else
				{
					direction = 0f;
				}
				SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_LandDust, 1, new Vector2(Left + num, Bottom), Vector2.One * 4f, direction);
			}
			num += 4;
		}
	}

	public bool HitSpring(Spring spring)
	{
		if (!holdable || !Hold.IsHeld)
		{
			if (spring.Orientation == Spring.Orientations.Floor && Speed.Y >= 0f)
			{
				Speed.X *= 0.5f;
				Speed.Y = -160f;
				noGravityTimer = 0.15f;
				return true;
			}
			if (spring.Orientation == Spring.Orientations.WallLeft && Speed.X <= 0f)
			{
				MoveTowardsY(spring.CenterY + 5f, 4f, null);
				Speed.X = 220f;
				Speed.Y = -80f;
				noGravityTimer = 0.1f;
				return true;
			}
			if (spring.Orientation == Spring.Orientations.WallRight && Speed.X >= 0f)
			{
				MoveTowardsY(spring.CenterY + 5f, 4f, null);
				Speed.X = -220f;
				Speed.Y = -80f;
				noGravityTimer = 0.1f;
				return true;
			}
		}
		return false;
	}


	public static void Load()
	{
		On.Celeste.Actor.MoveHExact += Actor_MoveHExact;
		On.Celeste.Actor.MoveVExact += Actor_MoveVExact;
		On.Celeste.Actor.OnGround_int += Actor_OnGround_int;
	}

	public static void Unload()
	{
		On.Celeste.Actor.MoveHExact -= Actor_MoveHExact;
		On.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
		On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;
	}

	private static bool Actor_MoveHExact(On.Celeste.Actor.orig_MoveHExact orig, Actor self, int moveH, Collision onCollide, Solid pusher)
	{
		if (self is HoldableTiles holdTiles)
		{
			var wasCollidable = holdTiles.Solid.Collidable;
			holdTiles.Solid.Collidable = false;

			var lastX = self.Position.X;
			var result = orig(self, moveH, onCollide, pusher);
			var deltaX = self.Position.X - lastX;

			holdTiles.Solid.Collidable = wasCollidable;
			holdTiles.Solid.MoveH(deltaX, holdTiles.Speed.X);

			return result;
		}
		return orig(self, moveH, onCollide, pusher);
	}

	private static bool Actor_MoveVExact(On.Celeste.Actor.orig_MoveVExact orig, Actor self, int moveV, Collision onCollide, Solid pusher)
	{
		if (self is HoldableTiles holdTiles)
		{
			var wasCollidable = holdTiles.Solid.Collidable;
			holdTiles.Solid.Collidable = false;

			var lastY = self.Position.Y;
			var result = orig(self, moveV, onCollide, pusher);
			var deltaY = self.Position.Y - lastY;

			holdTiles.Solid.Collidable = wasCollidable;
			holdTiles.Solid.MoveV(deltaY, holdTiles.Speed.Y);

			return result;
		}
		return orig(self, moveV, onCollide, pusher);
	}

	private static bool Actor_OnGround_int(On.Celeste.Actor.orig_OnGround_int orig, Actor self, int downCheck)
	{
		if (self is HoldableTiles holdTiles)
		{
			var wasCollidable = holdTiles.Solid.Collidable;
			holdTiles.Solid.Collidable = false;
			var result = orig(self, downCheck);
			holdTiles.Solid.Collidable = wasCollidable;
			return result;
		}
		return orig(self, downCheck);
	}


	public class HoldableTilesSolid : Solid
	{
		public HoldableTiles Parent;

		public HoldableTilesSolid(Vector2 position, int width, int height, HoldableTiles parent) : base(position, width, height, true)
		{
			Parent = parent;
			Collider.Position = new Vector2(-Width / 2f, -Height);
		}

		public override void MoveHExact(int move)
		{
			Parent.AllowPushing = false;
			base.MoveHExact(move);
			Parent.AllowPushing = true;
		}

		public override void MoveVExact(int move)
		{
			Parent.AllowPushing = false;
			base.MoveVExact(move);
			Parent.AllowPushing = true;
		}
	}
}
