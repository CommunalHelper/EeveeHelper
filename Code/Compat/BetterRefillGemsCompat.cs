using Celeste.Mod.Helpers;
using On.Celeste.Mod;
using System;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.EeveeHelper.Compat;

public class BetterRefillGemsCompat
{
	public static bool Enabled => p_Enabled != null && (bool)p_Enabled.GetValue(module._Settings);

	private static EverestModule module;
	private static PropertyInfo p_Enabled;

	public static void Initialize(EverestModule _module)
	{
		module = _module;

		p_Enabled = module.SettingsType.GetProperty("Enabled", BindingFlags.Public | BindingFlags.Instance);
	}
}
