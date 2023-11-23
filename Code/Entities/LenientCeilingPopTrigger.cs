using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.EeveeHelper.Entities;

[Tracked]
[CustomEntity("EeveeHelper/LenientCeilingPopTrigger")]
public class LenientCeilingPopTrigger : Trigger
{
	public int LeniencyFrames;

	public LenientCeilingPopTrigger(EntityData data, Vector2 offset) : base(data, offset)
	{
		LeniencyFrames = data.Int("leniencyFrames", 3);
	}
}
