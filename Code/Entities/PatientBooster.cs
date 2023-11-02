using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Collections;

namespace Celeste.Mod.EeveeHelper.Entities;

[CustomEntity("EeveeHelper/PatientBooster")]
public class PatientBooster : Booster
{
	private DynamicData baseData;
	private Vector2? lastSpritePos;

	public Sprite Sprite => baseData.Get<Sprite>("sprite");

	public PatientBooster(EntityData data, Vector2 offset)
		: base(data.Position + offset, data.Bool("red"))
	{
		var red = data.Bool("red");

		baseData = new DynamicData(typeof(Booster), this);
		Remove(Sprite);
		var sprite = GFX.SpriteBank.Create($"EeveeHelper_patientBooster{(red ? "Red" : "")}");
		baseData.Set("sprite", sprite);
		Add(sprite);
	}

	public override void Update()
	{
		base.Update();
		var player = Scene.Tracker.GetEntity<Player>();
		if (player != null && player.CurrentBooster == this)
		{
			baseData.Set("BoostingPlayer", true);
			new DynData<Player>(player).Set("boostTarget", Center);
			var targetPos = Center - player.Collider.Center + (Input.Aim.Value * 3f);
			player.MoveToX(targetPos.X);
			player.MoveToY(targetPos.Y);
		}
		var sprite = Sprite;
		if (sprite.CurrentAnimationID == "pop")
		{
			if (lastSpritePos == null)
			{
				lastSpritePos = sprite.RenderPosition;
			}

			sprite.RenderPosition = lastSpritePos.Value;
		}
		else
		{
			lastSpritePos = null;
		}
	}

	public static void Load()
	{
		On.Celeste.Player.BoostCoroutine += Player_BoostCoroutine;
	}

	public static void Unload()
	{
		On.Celeste.Player.BoostCoroutine -= Player_BoostCoroutine;
	}

	private static IEnumerator Player_BoostCoroutine(On.Celeste.Player.orig_BoostCoroutine orig, Player self)
	{
		if (self.CurrentBooster is PatientBooster)
		{
			yield break;
		}

		var orig_enum = orig(self);
		while (orig_enum.MoveNext())
		{
			yield return orig_enum.Current;
		}
	}
}
