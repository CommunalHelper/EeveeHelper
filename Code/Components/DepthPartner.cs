using Monocle;
using System;

namespace Celeste.Mod.EeveeHelper.Components;

public class DepthPartner : Component
{
	public PartnerEntity Partner;

	public int Depth;
	public Action OnUpdate;
	public Action OnRender;

	public DepthPartner(int depth) : base(true, true)
	{
		Depth = depth;
	}

	public override void EntityAdded(Scene scene)
	{
		base.EntityAdded(scene);
		scene.Add(Partner = new PartnerEntity(this));
	}

	public override void EntityRemoved(Scene scene)
	{
		base.EntityRemoved(scene);
		Partner.RemoveSelf();
	}

	public class PartnerEntity : Entity
	{
		private DepthPartner component;

		public PartnerEntity(DepthPartner component) : base()
		{
			this.component = component;
			Depth = component.Depth;
		}

		public override void Update()
		{
			base.Update();
			if (component.Entity != null && component.Entity.Active)
			{
				component.OnUpdate?.Invoke();
			}
		}

		public override void Render()
		{
			base.Render();
			if (component.Entity != null && component.Entity.Visible)
			{
				component.OnRender?.Invoke();
			}
		}
	}
}
