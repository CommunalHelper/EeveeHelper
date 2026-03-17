using Celeste.Mod.EeveeHelper.Components;
using MonoMod.ModInterop;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.EeveeHelper;

[ModExportName("EeveeHelper")]
public static class Exports
{
	public static void RegisterBlacklistedAnchors(Type type, bool allowInherited, HashSet<string> fieldNames)
		=> EntityContainerMover.RegisterBlacklistedAnchors(type, allowInherited, fieldNames);
	public static void RegisterWhitelistedAnchors(Type type, bool allowInherited, HashSet<string> fieldNames)
		=> EntityContainerMover.RegisterWhitelistedAnchors(type, allowInherited, fieldNames);
}