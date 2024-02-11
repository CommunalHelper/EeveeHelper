using Celeste.Mod.Entities;
using Monocle;
using System.Linq;

namespace Celeste.Mod.EeveeHelper.Entities;

[Tracked]
[CustomEntity("EeveeHelper/NoDemoBindController")]
public sealed class NoDemoBindController : Entity
{
	public static bool Triggered { get; set; }

	public NoDemoBindController() : base() { }

	public override void Awake(Scene scene)
	{
		base.Awake(scene);
		Triggered = true;
	}

	public override void Removed(Scene scene)
	{
		base.Removed(scene);
		UpdateTriggered(this);
	}

	public static void UpdateTriggered(Entity ignore = null)
	{
		if (Engine.Scene is not Level level)
		{
			Triggered = false;
			return;
		}

		if (level.Tracker.GetEntities<NoDemoBindController>().Any(e => e != ignore))
		{
			Triggered = true;
			return;
		}

		foreach (NoDemoBindTrigger trigger in level.Tracker.GetEntities<NoDemoBindTrigger>())
		{
			if (trigger.PlayerIsInside && trigger != ignore)
			{
				Triggered = true;
				return;
			}
		}

		Triggered = false;
		return;
	}
}
