using Microsoft.Xna.Framework;

namespace Celeste.Mod.EeveeHelper.Handlers;

public interface IMoveable
{
	void PreMove();

	bool Move(Vector2 move, Vector2? liftSpeed);
}
