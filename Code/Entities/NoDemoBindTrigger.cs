using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.EeveeHelper.Entities;

[Tracked]
[CustomEntity("EeveeHelper/NoDemoBindTrigger")]
public class NoDemoBindTrigger : Trigger
{
	public NoDemoBindTrigger(EntityData data, Vector2 offset) : base(data, offset) { }
}
