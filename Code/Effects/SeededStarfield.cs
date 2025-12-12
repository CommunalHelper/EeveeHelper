using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.EeveeHelper.Effects;

public class SeededStarfield : Backdrop
{
	private struct Star
	{
		public MTexture Texture;
		public Vector2 Position;
		public Color Color;
		public int NodeIndex;
		public float NodePercent;
		public float Distance;
		public float Sine;
	}

	private const int NodeDistance = 32;
	private const int NodeCount = 15;
	private const float MinDist = 4f;
	private const float MaxDist = 24f;

	private readonly float FlowSpeed;
	private readonly List<float> YNodes = [];
	private readonly Star[] Stars = new Star[128];

	public SeededStarfield(Color color, float speed = 1f, int seed = 0, string textureDir = "particles/starfield")
	{
		Color = color;
		FlowSpeed = speed;

		if (seed != 0)
		{
			Calc.PushRandom(seed);
		}

		float starY = Calc.Random.NextFloat(180f);
		for (int i = 0; i < NodeCount; i++)
		{
			YNodes.Add(starY);
			starY += Calc.Random.Choose(-1, 1) * (16f + Calc.Random.NextFloat(24f));
		}

		for (int i = 0; i < 4; i++)
		{
			YNodes[YNodes.Count - 1 - i] = Calc.LerpClamp(YNodes[YNodes.Count - 1 - i], YNodes[0], 1f - i / 4f);
		}

		var atlasSubtextures = GFX.Game.GetAtlasSubtextures(textureDir + "/");
		for (int i = 0; i < Stars.Length; i++)
		{
			float smallness = Calc.Random.NextFloat(1f);
			Stars[i].NodeIndex = Calc.Random.Next(YNodes.Count - 1);
			Stars[i].NodePercent = Calc.Random.NextFloat(1f);
			Stars[i].Distance = MinDist + smallness * (MaxDist - MinDist);
			Stars[i].Sine = Calc.Random.NextFloat((float)Math.PI * 2f);
			Stars[i].Position = GetTargetOfStar(ref Stars[i]);
			Stars[i].Color = Color.Lerp(Color, Color.Transparent, smallness * 0.5f);
			int textureIndex = (int)Calc.Clamp(Ease.CubeIn(1f - smallness) * atlasSubtextures.Count, 0f, atlasSubtextures.Count - 1);
			Stars[i].Texture = atlasSubtextures[textureIndex];
		}

		if (seed != 0)
		{
			Calc.PopRandom();
		}
	}

	public override void Update(Scene scene)
	{
		base.Update(scene);
		for (int i = 0; i < Stars.Length; i++)
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
				star.Position.X -= (NodeCount - 1) * NodeDistance;
			}
		}
		star.Position += (GetTargetOfStar(ref star) - star.Position) / 50f;
	}

	private Vector2 GetTargetOfStar(ref Star star)
	{
		var startNode = new Vector2(star.NodeIndex * NodeDistance, YNodes[star.NodeIndex]);
		var endNode = new Vector2((star.NodeIndex + 1) * NodeDistance, YNodes[star.NodeIndex + 1]);
		var lerpedNode = startNode + (endNode - startNode) * star.NodePercent;
		var direction = (endNode - startNode).SafeNormalize();
		var perpendicular = new Vector2(0f - direction.Y, direction.X);
		return lerpedNode + perpendicular * star.Distance * (float)Math.Sin(star.Sine);
	}

	public override void Render(Scene scene)
	{
		var position = (scene as Level).Camera.Position;
		for (int i = 0; i < Stars.Length; i++)
		{
			var renderPosition = new Vector2()
			{
				X = -64f + Mod(Stars[i].Position.X - position.X * Scroll.X, (NodeCount - 1) * NodeDistance),
				Y = -16f + Mod(Stars[i].Position.Y - position.Y * Scroll.Y, 180f + 32f)
			};

			Stars[i].Texture.DrawCentered(renderPosition, Stars[i].Color * FadeAlphaMultiplier);
		}
	}

	private static float Mod(float x, float m) => (x % m + m) % m;
}
