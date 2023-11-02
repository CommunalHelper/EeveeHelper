using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.EeveeHelper.Effects;
public class SeededStarfield : Backdrop
{
	public struct Star
	{
		public MTexture Texture;
		public Vector2 Position;
		public Color Color;
		public int NodeIndex;
		public float NodePercent;
		public float Distance;
		public float Sine;
	}

	public const int StepSize = 32;
	public const int Steps = 15;
	public const float MinDist = 4f;
	public const float MaxDist = 24f;
	public float FlowSpeed;
	public List<float> YNodes = new();
	public Star[] Stars = new Star[128];

	public SeededStarfield(Color color, float speed = 1f, int seed = 0)
	{
		Color = color;
		FlowSpeed = speed;

		if (seed != 0)
		{
			Calc.PushRandom(seed);
		}

		var num = Calc.Random.NextFloat(180f);
		var num2 = 0;
		while (num2 < 15)
		{
			YNodes.Add(num);
			num2++;
			num += Calc.Random.Choose(-1, 1) * (16f + Calc.Random.NextFloat(24f));
		}
		for (var i = 0; i < 4; i++)
		{
			YNodes[YNodes.Count - 1 - i] = Calc.LerpClamp(YNodes[YNodes.Count - 1 - i], YNodes[0], 1f - i / 4f);
		}

		var atlasSubtextures = GFX.Game.GetAtlasSubtextures("particles/starfield/");
		for (var j = 0; j < Stars.Length; j++)
		{
			var num3 = Calc.Random.NextFloat(1f);
			Stars[j].NodeIndex = Calc.Random.Next(YNodes.Count - 1);
			Stars[j].NodePercent = Calc.Random.NextFloat(1f);
			Stars[j].Distance = 4f + num3 * 20f;
			Stars[j].Sine = Calc.Random.NextFloat((float)Math.PI * 2f);
			Stars[j].Position = GetTargetOfStar(ref Stars[j]);
			Stars[j].Color = Color.Lerp(Color, Color.Transparent, num3 * 0.5f);
			var index = (int)Calc.Clamp(Ease.CubeIn(1f - num3) * atlasSubtextures.Count, 0f, atlasSubtextures.Count - 1);
			Stars[j].Texture = atlasSubtextures[index];
		}

		if (seed != 0)
		{
			Calc.PopRandom();
		}
	}

	public override void Update(Scene scene)
	{
		base.Update(scene);
		for (var i = 0; i < Stars.Length; i++)
		{
			UpdateStar(ref Stars[i]);
		}
	}

	private void UpdateStar(ref Star star)
	{
		star.Sine += Engine.DeltaTime * FlowSpeed;
		star.NodePercent += Engine.DeltaTime * 0.25f * FlowSpeed;
		if (star.NodePercent >= 1f)
		{
			star.NodePercent -= 1f;
			star.NodeIndex++;
			if (star.NodeIndex >= YNodes.Count - 1)
			{
				star.NodeIndex = 0;
				star.Position.X -= 448f;
			}
		}
		star.Position += (GetTargetOfStar(ref star) - star.Position) / 50f;
	}

	private Vector2 GetTargetOfStar(ref Star star)
	{
		var vector = new Vector2(star.NodeIndex * 32, YNodes[star.NodeIndex]);
		var value = new Vector2((star.NodeIndex + 1) * 32, YNodes[star.NodeIndex + 1]);
		var value2 = vector + (value - vector) * star.NodePercent;
		var vector2 = (value - vector).SafeNormalize();
		var value3 = new Vector2(0f - vector2.Y, vector2.X);
		return value2 + value3 * star.Distance * (float)Math.Sin(star.Sine);
	}

	public override void Render(Scene scene)
	{
		var position = (scene as Level).Camera.Position;
		for (var i = 0; i < Stars.Length; i++)
		{
			var vector = new Vector2();
			vector.X = -64f + Mod(Stars[i].Position.X - position.X * Scroll.X, 448f);
			vector.Y = -16f + Mod(Stars[i].Position.Y - position.Y * Scroll.Y, 212f);
			var position2 = vector;
			Stars[i].Texture.DrawCentered(position2, Stars[i].Color * FadeAlphaMultiplier);
		}
	}

	private float Mod(float x, float m)
	{
		return (x % m + m) % m;
	}
}
