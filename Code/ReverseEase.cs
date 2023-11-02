using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.EeveeHelper;

public static class ReverseEase
{
	public static Ease.Easer Linear = (float t) => t;

	public static Ease.Easer SineIn = (float t) => 2 * (float)Math.Acos(1 - t) / (float)Math.PI;
	public static Ease.Easer SineOut = (float t) => 2 * (float)Math.Asin(t) / (float)Math.PI;
	public static Ease.Easer SineInOut = (float t) => 2 * (float)Math.Asin(Math.Sqrt(t)) / (float)Math.PI;

	public static Ease.Easer QuadIn = (float t) => (float)Math.Sqrt(t);
	public static Ease.Easer QuadOut = Ease.Invert(QuadIn);
	public static Ease.Easer QuadInOut = Ease.Follow(QuadIn, QuadOut);

	public static Ease.Easer CubeIn = (float t) => (float)Math.Pow(t, 1f / 3f);
	public static Ease.Easer CubeOut = Ease.Invert(CubeIn);
	public static Ease.Easer CubeInOut = Ease.Follow(CubeIn, CubeOut);

	public static Ease.Easer QuintIn = (float t) => (float)Math.Pow(t, 1f / 5f);
	public static Ease.Easer QuintOut = Ease.Invert(QuintIn);
	public static Ease.Easer QuintInOut = Ease.Follow(QuintIn, QuintOut);

	public static Ease.Easer ExpoIn = (float t) => (float)Math.Log(1024f * t) / (10f * (float)Math.Log(2f));
	public static Ease.Easer ExpoOut = Ease.Invert(ExpoIn);
	public static Ease.Easer ExpoInOut = Ease.Follow(ExpoIn, ExpoOut);

	// there are no reverse easings for Back, BigBack, Elastic and Bounce

	private static Dictionary<Ease.Easer, Ease.Easer> reverseEasings = new()
	{
		{ Ease.Linear, Linear },
		{ Ease.SineIn, SineIn },
		{ Ease.SineOut, SineOut },
		{ Ease.SineInOut, SineInOut },
		{ Ease.QuadIn, QuadIn },
		{ Ease.QuadOut, QuadOut },
		{ Ease.QuadInOut, QuadInOut },
		{ Ease.CubeIn, CubeIn },
		{ Ease.CubeOut, CubeOut },
		{ Ease.CubeInOut, CubeInOut },
		{ Ease.QuintIn, QuintIn },
		{ Ease.QuintOut, QuintOut },
		{ Ease.QuintInOut, QuintInOut },
		{ Ease.ExpoIn, ExpoIn },
		{ Ease.ExpoOut, ExpoOut },
		{ Ease.ExpoInOut, ExpoInOut }
	};

	public static Ease.Easer GetReverse(Ease.Easer ease)
	{
		if (reverseEasings.TryGetValue(ease, out var result))
		{
			return result;
		}
		return Linear;
	}
}
