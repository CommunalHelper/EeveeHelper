using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.EeveeHelper.Entities.Modifiers;

[CustomEntity(ID)]
public class GlobalModifier : Entity
{
	public const string ID = "EeveeHelper/GlobalModifier";

	public GlobalModifier(EntityData data, Vector2 offset) : base() { }
}
