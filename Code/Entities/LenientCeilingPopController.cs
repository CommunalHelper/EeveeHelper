using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.EeveeHelper.Entities;

[Tracked]
[CustomEntity("EeveeHelper/LenientCeilingPopController")]
public class LenientCeilingPopController : Entity
{
	public int LeniencyFrames;

	public LenientCeilingPopController(EntityData data, Vector2 offset) : base()
	{
		LeniencyFrames = data.Int("leniencyFrames", 3);
	}
}
