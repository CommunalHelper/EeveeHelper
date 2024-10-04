using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.EeveeHelper.Entities;

[CustomEntity("EeveeHelper/RefillShard")]
public class RefillShardController : Entity
{
	public const float RespawnTime = 3600f;

	public List<RefillShard> Shards;
	public Refill Refill;

	private bool spawnRefill;
	private bool twoDashes;
	private bool resetOnGround;
	private bool oneUse;
	private int collectAmount;
	private Vector2[] nodes;

	private bool finished;

	public RefillShardController(EntityData data, Vector2 offset)
		: base(data.Position + offset)
	{
		spawnRefill = data.Bool("spawnRefill");
		twoDashes = data.Bool("twoDashes");
		resetOnGround = data.Bool("resetOnGround");
		oneUse = data.Bool("oneUse");
		collectAmount = data.Int("collectAmount");

		nodes = data.NodesOffset(offset);
	}

	public override void Added(Scene scene)
	{
		base.Added(scene);

		Shards = [];

		for (var i = 0; i < nodes.Length; i++)
		{
			var shard = new RefillShard(this, nodes[i], i, twoDashes, resetOnGround, oneUse, spawnRefill);
			Shards.Add(shard);
			scene.Add(shard);
		}

		if (spawnRefill)
		{
			Refill = new Refill(Position, twoDashes, oneUse);
			scene.Add(Refill);

			Refill.Collidable = false;
			Refill.Depth = Depths.Pickups;
			Refill.sprite.Visible = Refill.flash.Visible = false;
			Refill.outline.Visible = true;
			Refill.respawnTimer = RespawnTime;
		}
	}

	public override void Update()
	{
		base.Update();
		if (!finished && spawnRefill)
		{
			Refill.respawnTimer = RespawnTime;
		}
	}

	public void CheckCollection()
	{
		var collectedShards = Shards.Count(shard => shard.Follower.HasLeader);
		if (!finished && collectedShards >= (collectAmount > 0 ? collectAmount : Shards.Count))
		{
			if (spawnRefill || (oneUse && collectedShards == Shards.Count))
			{
				finished = true;
			}

			if (!spawnRefill)
			{
				List<RefillShard> toRemove = [];
				foreach (var shard in Shards)
				{
					if (shard.Follower.HasLeader)
					{
						shard.Collect(!oneUse);
						if (oneUse)
						{
							toRemove.Add(shard);
						}
					}
				}
				foreach (var shard in toRemove)
				{
					Shards.Remove(shard);
				}

				var player = Scene.Tracker.GetEntity<Player>();
				Audio.Play(twoDashes ? SFX.game_10_pinkdiamond_touch : SFX.game_gen_diamond_touch, player.Position);
				player.UseRefill(twoDashes);
				Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
				Celeste.Freeze(0.05f);
				SceneAs<Level>().Shake();
			}
			else
			{
				var maxDist = 0f;

				foreach (var shard in Shards)
				{
					shard.Finished = true;
					if (shard.Follower.HasLeader)
					{
						shard.Follower.Leader.LoseFollower(shard.Follower);
					}

					shard.OnCollectCutscene();

					var startPos = shard.Position;
					var targetPos = Refill.Position;

					var dist = (targetPos - startPos).Length();
					maxDist = Math.Max(dist, maxDist);

					var tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, dist / 200f, true);
					tween.OnUpdate = (t) =>
					{
						shard.Position = Vector2.Lerp(startPos, targetPos, t.Eased);
						shard.Whiten = t.Eased;
					};
					shard.Add(tween);
				}

				Add(Alarm.Create(Alarm.AlarmMode.Oneshot, () =>
				{
					Scene.Remove(Shards);
					Shards.Clear();
					SpawnRefill();
				}, maxDist / 200f, true));
			}
		}
	}

	public void SpawnRefill()
	{
		Refill.respawnTimer = RespawnTime;
		Refill.Respawn();
	}
}