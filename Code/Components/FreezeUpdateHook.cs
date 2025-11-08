using Monocle;
using MonoMod.Cil;
using System;
using System.Linq;

namespace Celeste.Mod.EeveeHelper.Components;

// based on https://github.com/CommunalHelper/CommunalHelper/blob/dev/src/Entities/Misc/AbstractInputController.cs

[Tracked]
public class FreezeUpdateHook(Action onFreezeUpdate) : Component(true, false)
{
	public Action OnFreezeUpdate = onFreezeUpdate;

    internal static void Load()
    {
        IL.Monocle.Engine.Update += Engine_Update;
    }

    internal static void Unload()
    {
        IL.Monocle.Engine.Update -= Engine_Update;
    }
	
	private static void Engine_Update(ILContext il)
	{
		var cursor = new ILCursor(il);

		if (cursor.TryGotoNext(instr => instr.MatchLdsfld<Engine>("FreezeTimer"),
			instr => instr.MatchCall<Engine>("get_RawDeltaTime")))
		{
			cursor.EmitDelegate(UpdateFreezeUpdateHooks);
		}
	}

    private static void UpdateFreezeUpdateHooks()
    {
        foreach (FreezeUpdateHook freezeUpdateHook in Engine.Scene.Tracker.GetComponents<FreezeUpdateHook>().Cast<FreezeUpdateHook>())
			if (freezeUpdateHook.Active)
            	freezeUpdateHook.OnFreezeUpdate();
    }
}