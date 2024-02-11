using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.EeveeHelper.Entities;

[Tracked]
[CustomEntity("EeveeHelper/NoDemoBindTrigger")]
public sealed class NoDemoBindTrigger : Trigger
{
	public NoDemoBindTrigger(EntityData data, Vector2 offset) : base(data, offset) { }

	public override void OnEnter(Player player)
	{
		base.OnEnter(player);
		NoDemoBindController.Triggered = true;
	}

	public override void OnLeave(Player player)
	{
		base.OnLeave(player);
		NoDemoBindController.UpdateTriggered(this);
	}

	public override void Removed(Scene scene)
	{
		base.Removed(scene);
		NoDemoBindController.UpdateTriggered(this);
	}
}
