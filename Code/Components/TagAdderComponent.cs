using Monocle;

namespace Celeste.Mod.EeveeHelper.Components;

public class TagAdderComponent : Component
{
	public int Tags;

	public TagAdderComponent(int tags) : base(true, true)
	{
		Tags = tags;
	}

	public override void Added(Entity entity)
	{
		base.Added(entity);
		entity.AddTag(Tags);
	}

	public override void Update()
	{
		base.Update();
		Entity.AddTag(Tags);
	}
}
