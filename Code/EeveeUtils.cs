using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.EeveeHelper;

public static class EeveeUtils
{
	internal static MethodInfo m_SpringBounceAnimate = typeof(Spring).GetMethod("BounceAnimate", BindingFlags.NonPublic | BindingFlags.Instance);
	internal static MethodInfo m_BounceBlockCheckModeChange = typeof(BounceBlock).GetMethod("CheckModeChange", BindingFlags.NonPublic | BindingFlags.Instance);

	public static Vector2 GetPosition(Entity entity)
	{
		return entity is Platform platform ? platform.ExactPosition : entity.Position;
	}

	public static Vector2 GetTrackBoost(Vector2 move, bool disableBoost)
	{
		return move * new Vector2(disableBoost ? 0f : 1f, 1f) + (move.X != 0f && move.Y == 0f && disableBoost ? Vector2.UnitY * 0.01f : Vector2.Zero);
	}

	public static Tuple<string, bool> ParseFlagAttr(string flag)
	{
		return flag.StartsWith("!") ? Tuple.Create(flag.Substring(1), true) : Tuple.Create(flag, false);
	}

	public static void ParseFlagAttr(string attr, out string flag, out bool notFlag)
	{
		var parsed = ParseFlagAttr(attr);
		flag = parsed.Item1;
		notFlag = parsed.Item2;
	}

	public static EntityData CloneEntityData(EntityData data, LevelData levelData = null)
	{
		var level = levelData ?? data.Level;

		return new EntityData
		{
			Name = data.Name,
			Level = levelData ?? data.Level,
			ID = data.ID,
			Position = data.Position + data.Level.Position - level.Position,
			Width = data.Width,
			Height = data.Height,
			Origin = data.Origin,
			Nodes = (Vector2[])data.Nodes.Clone(),
			Values = data.Values == null ? new Dictionary<string, object>() : new Dictionary<string, object>(data.Values)
		};
	}

	public static T GetValueOfType<T>(Dictionary<Type, T> dict, Type type)
	{
		var success = dict.TryGetValue(type, out var value);

		if (success)
		{
			return value;
		}

		if (type.BaseType == null)
		{
			return default;
		}

		return GetValueOfType(dict, type.BaseType);
	}
}
