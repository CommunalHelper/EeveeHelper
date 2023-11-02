using Celeste.Mod.EeveeHelper.Components;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.EeveeHelper.Handlers;

public interface IEntityHandler
{
	Entity Entity { get; }
	EntityContainer Container { get; set; }

	void OnAttach(EntityContainer container);

	void OnDetach(EntityContainer container);

	bool IsInside(EntityContainer container);

	Rectangle GetBounds();

	void Destroy();
}
