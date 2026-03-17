using Celeste.Mod.EeveeHelper.Components;
using MonoMod.ModInterop;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.EeveeHelper;

[ModExportName("EeveeHelper")]
public static class Exports
{
	public static void RegisterIgnoredAnchors(Type forType, HashSet<string> fieldNames)
		=> EntityContainerMover.AddIgnoredAnchors(forType, fieldNames);
}