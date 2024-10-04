using Celeste.Mod.EeveeHelper.Compat;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.EeveeHelper.Entities;

public class RefillShard : Entity
{
	public const float LoseTime = 0.15f;

	public bool Finished;
	public Follower Follower;
	public float Whiten;

	private int index;
	private bool twoDashes;
	private bool resetOnGround;
	private RefillShardController controller;

	private Vector2 start;
	private Platform attached;
	private float canLoseTimer;
	private float loseTimer;
	private bool losing;
	private bool transforming;
	private Color borderColor;

	private Sprite sprite;
	private Sprite flash;
	private Image dottedOutline;
	private Image border;
	private ParticleType p_shatter;
	private ParticleType p_regen;
	private ParticleType p_glow;
	private Wiggler wiggler;
	private BloomPoint bloom;
	private VertexLight light;
	private SineWave sine;

	private bool renderShard; //we can't use the standard Visible modifier without removing the dotted outline after grabbing a shard

	public RefillShard(RefillShardController controller, Vector2 position, int index, bool two, bool groundReset, bool oneUse, bool spawnRefill)
		: base(position)
	{
		this.index = index;
		this.controller = controller;

		twoDashes = two;
		resetOnGround = groundReset;

		start = Position;

		Add(Follower = new Follower(OnGainLeader, OnLoseLeader));
		Follower.FollowDelay = 0.2f;
		Follower.PersistentFollow = false;
		Add(new PlayerCollider(OnPlayer));
		Add(new StaticMover
		{
			SolidChecker = solid => solid.CollideCheck(this),
			OnAttach = platform =>
			{
				Depth = Depths.Top;
				Collider = new Hitbox(18f, 18f, -9f, -9f);
				attached = platform;
				start = Position - platform.Position;
			}
		});

		p_shatter = two ? Refill.P_ShatterTwo : Refill.P_Shatter;
		p_regen = two ? Refill.P_RegenTwo : Refill.P_Regen;
		p_glow = two ? Refill.P_GlowTwo : Refill.P_Glow;

		borderColor = Color.White;
		if (oneUse && EeveeHelperModule.BetterRefillGemsLoaded && BetterRefillGemsCompat.Enabled)
		{
			borderColor = new Color(255, 41, 41);
		}

		Add(dottedOutline = new Image(GFX.Game["objects/EeveeHelper/refillShard/outline"]));
		dottedOutline.CenterOrigin();
		dottedOutline.Visible = !oneUse && !spawnRefill;

		Add(border = new Image(GFX.Game["objects/EeveeHelper/refillShard/border"]));
		border.CenterOrigin();
		border.Color = borderColor;

		Add(sprite = new Sprite(GFX.Game, $"objects/EeveeHelper/refillShard/{(two ? "two" : "one")}"));
		sprite.AddLoop("idle", "", 0.1f);
		sprite.Play("idle");
		sprite.CenterOrigin();

		Add(flash = new Sprite(GFX.Game, "objects/EeveeHelper/refillShard/flash"));
		flash.Add("idle", "", 0.05f, [0]);
		flash.Add("flash", "", 0.05f, "idle");
		flash.Add("end", "", 0.05f);
		flash.Play("idle");
		flash.CenterOrigin();

		dottedOutline.Rotation = border.Rotation = sprite.Rotation = flash.Rotation = Calc.Random.Next(4) * ((float)Math.PI / 2f);

		Add(wiggler = Wiggler.Create(1f, 4f, value => sprite.Scale = border.Scale = dottedOutline.Scale = flash.Scale = Vector2.One * (1f + value * 0.2f)));

		Add(bloom = new BloomPoint(0.8f, 8f));
		Add(light = new VertexLight(Color.White, 1f, 8, 32));
		Add(sine = new SineWave(0.6f).Randomize());

		UpdateY();
		Depth = Depths.Pickups;
		Collider = new Hitbox(16f, 16f, -8f, -8f);

		renderShard = true;
	}

	public override void Update()
	{
		base.Update();

		if (!Finished && resetOnGround)
		{
			if (canLoseTimer > 0f)
			{
				canLoseTimer -= Engine.DeltaTime;
			}
			else if (Follower.HasLeader && (Follower.Leader.Entity as Player).LoseShards)
			{
				losing = true;
			}
			if (losing)
			{
				var player = Follower.Leader.Entity as Player;
				if (loseTimer <= 0f || player.Speed.Y < 0f)
				{
					player.Leader.LoseFollower(Follower);
					losing = false;
				}
				else if (player.LoseShards)
				{
					loseTimer -= Engine.DeltaTime;
				}
				else
				{
					loseTimer = LoseTime;
					losing = false;
				}
			}
		}

		if (renderShard && Scene.OnInterval(0.1f))
		{
			SceneAs<Level>().ParticlesFG.Emit(p_glow, 1, Position, Vector2.One * 4f);
		}

		if (renderShard && Scene.OnInterval(2f))
		{
			flash.Play("flash");
		}

		light.Alpha = Calc.Approach(light.Alpha, renderShard ? 1f : 0f, 4f * Engine.DeltaTime);
		bloom.Alpha = light.Alpha * 0.8f;
		UpdateY();
	}

	public override void Render()
	{
		dottedOutline.RenderPosition = start + new Vector2(0, sine.Value * 2f);

		if (!renderShard)
		{
			dottedOutline.Render();
			return;
		}

		var outlineColor = Color.Black;

		if (transforming)
		{
			outlineColor = Color.Lerp(outlineColor, Color.White, Whiten);
			border.Color = Color.Lerp(borderColor, Color.White, Whiten);
		}

		border.DrawOutline(outlineColor);

		base.Render();
	}

	public void OnCollectCutscene()
	{
		transforming = true;
		flash.Play("end");
	}

	public void Collect(bool respawn)
	{
		Finished = true;
		Follower.Leader.LoseFollower(Follower);
		SceneAs<Level>().ParticlesFG.Emit(p_shatter, 8, Position, Vector2.One * 3f, Calc.Random.NextFloat((float)Math.PI * 2f));
		if (!respawn)
		{
			RemoveSelf();
		}
		else
		{
			Collidable = false;
			renderShard = false;
			Add(Alarm.Create(Alarm.AlarmMode.Oneshot, Respawn, 3.6f + index * 0.1f, true));
		}
	}

	private void UpdateY()
	{
		dottedOutline.Y = flash.Y = border.Y = sprite.Y = bloom.Y = sine.Value * 2f;
	}

	private void OnGainLeader()
	{
		controller.CheckCollection();
		canLoseTimer = 0.25f;
		loseTimer = LoseTime;
	}

	private void OnLoseLeader()
	{
		if (!Finished)
		{
			Add(new Coroutine(ReturnRoutine()));
		}
	}

	private void OnPlayer(Player player)
	{
		Audio.Play(SFX.game_gen_seed_touch, Position, "count", index % 5);
		Collidable = false;
		Depth = Depths.Top;
		player.Leader.GainFollower(Follower);
	}

	private IEnumerator ReturnRoutine()
	{
		Audio.Play(SFX.game_gen_seed_poof, Position);
		Collidable = false;
		sprite.Scale = flash.Scale = Vector2.One * 2f;
		yield return 0.05f;

		Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
		for (var i = 0; i < 6; i++)
		{
			var dir = Calc.Random.NextFloat((float)Math.PI * 2f);
			SceneAs<Level>().ParticlesFG.Emit(StrawberrySeed.P_Burst, 1, Position + Calc.AngleToVector(dir, 4f), Vector2.Zero, dir);
		}
		renderShard = false;
		yield return 0.3f + index * 0.1f;

		Respawn();
		yield break;
	}

	private void Respawn()
	{
		Position = start;
		if (attached != null)
		{
			Position += attached.Position;
		}

		var sound = Audio.Play(twoDashes ? SFX.game_10_pinkdiamond_return : SFX.game_gen_diamond_return, Position);
		sound.setVolume(0.75f);
		sound.setPitch(2f);
		SceneAs<Level>().ParticlesFG.Emit(p_regen, 8, Position, Vector2.One * 2f);
		sprite.Scale = flash.Scale = Vector2.One;
		wiggler.Start();
		renderShard = true;
		Collidable = true;
		Finished = false;
	}
}