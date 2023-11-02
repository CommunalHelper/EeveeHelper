using Celeste.Mod.EeveeHelper.Handlers;
using Celeste.Mod.EeveeHelper.Handlers.Impl.Compat;
using Celeste.Mod.Helpers;
using System;

namespace Celeste.Mod.EeveeHelper.Compat;

public class AdventureHelperCompat
{
	public static Type multiNodeTrackSpinnerType;

	public static void Initialize()
	{
		multiNodeTrackSpinnerType = FakeAssembly.GetFakeEntryAssembly().GetType("Celeste.Mod.AdventureHelper.Entities.MultipleNodeTrackSpinner");

		EntityHandler.RegisterInherited(multiNodeTrackSpinnerType, (entity, container) => new MultipleNodeTrackSpinnerHandler(entity));
	}
}
