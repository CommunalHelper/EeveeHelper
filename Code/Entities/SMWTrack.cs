using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.EeveeHelper.Entities;

[Tracked]
[CustomEntity("EeveeHelper/SMWTrack")]
public class SMWTrack : Entity
{
	public Color Color;
	public bool StartOpen;
	public bool EndOpen;

	public List<Section> Sections;
	public float Length;
	public bool Enabled = true;

	protected List<Vector2> nodes;
	private Color inactiveColor;
	private string startOpenFlag;
	private string endOpenFlag;
	private string flag;
	private bool notFlag;
	private bool hidden;
	private bool hideInactive;

	private MTexture pointTexture;
	private MTexture endTexture;
	private bool notStartOpenFlag;
	private bool notEndOpenFlag;
	private Collider startCollider;
	private Collider endCollider;

	public SMWTrack(EntityData data, Vector2 offset) : base(data.Position + offset)
	{
		Depth = Depths.BGDecals - 10;

		pointTexture = GFX.Game.GetOrDefault("objects/EeveeHelper/smwTrack/point", GFX.Game.GetFallback());
		endTexture = GFX.Game.GetOrDefault("objects/EeveeHelper/smwTrack/end", GFX.Game.GetFallback());

		Color = data.HexColor("color", Color.White);
		inactiveColor = data.HexColor("inactiveColor", Color.Transparent);
		StartOpen = data.Bool("startOpen");
		EndOpen = data.Bool("endOpen");
		var parsedFlag = EeveeUtils.ParseFlagAttr(data.Attr("flag"));
		flag = parsedFlag.Item1;
		notFlag = data.Bool("notFlag", parsedFlag.Item2);
		startOpenFlag = data.Attr("startOpenFlag");
		endOpenFlag = data.Attr("endOpenFlag");
		notStartOpenFlag = !StartOpen;
		notEndOpenFlag = !EndOpen;
		hidden = data.Bool("hidden");
		hideInactive = data.Bool("hideInactive", true);

		nodes = new List<Vector2>();
		nodes.Add(Position);
		nodes.AddRange(data.NodesOffset(offset));

		startCollider = new Circle(4f, nodes.First().X - X, nodes.First().Y - Y);
		endCollider = new Circle(4f, nodes.Last().X - X, nodes.Last().Y - Y);

		if (hidden)
		{
			Visible = false;
		}

		GenerateSections();
	}

	public override void Added(Scene scene)
	{
		base.Added(scene);
		FlagCheck();
	}

	public override void Render()
	{
		base.Render();
		foreach (var section in Sections)
		{
			DrawSection(section);
		}
		for (var i = 0; i < nodes.Count; i++)
		{
			var node = nodes[i];
			if ((i == 0 && !StartOpen) || (i == nodes.Count - 1 && !EndOpen))
			{
				endTexture.DrawCentered(node.Floor(), GetColor());
			}
			else
			{
				pointTexture.DrawCentered(node.Floor(), GetColor());
			}
		}
	}

	public override void Update()
	{
		base.Update();
		FlagCheck();
	}

	protected virtual void FlagCheck()
	{
		var level = SceneAs<Level>();

		if (!string.IsNullOrEmpty(flag))
		{
			Enabled = Collidable = (level.Session.GetFlag(flag) != notFlag);
		}
		if (!string.IsNullOrEmpty(startOpenFlag))
		{
			StartOpen = level.Session.GetFlag(startOpenFlag) != notStartOpenFlag;
		}
		if (!string.IsNullOrEmpty(endOpenFlag))
		{
			EndOpen = level.Session.GetFlag(endOpenFlag) != notEndOpenFlag;
		}

		Visible = !hidden && (!hideInactive || Enabled);
	}

	protected virtual void GenerateSections()
	{
		Sections = new List<Section>();
		Length = 0f;
		for (var i = 0; i < nodes.Count - 1; i++)
		{
			var section = new Section(nodes[i], nodes[i + 1], Length);
			Sections.Add(section);
			Length += section.Length;
		}
	}

	protected virtual Color GetColor()
	{
		if (Enabled)
		{
			return Color;
		}
		else if (inactiveColor == Color.Transparent)
		{
			return Color.Lerp(Color, Color.Black, 0.5f);
		}
		else
		{
			return inactiveColor;
		}
	}

	protected virtual void DrawSection(Section section)
	{
		if (Enabled)
		{
			Draw.Line(section.Start, section.End, Color.Black, 4f);
			Draw.Line(section.Start, section.End, GetColor(), 2f);
		}
		else
		{
			var dist = Vector2.Distance(section.Start, section.End);
			var angle = Vector2.Normalize(section.End - section.Start);
			var segments = (int)Math.Floor(dist / 4f);

			if (segments > 0 && segments % 2 == 0)
			{
				segments--;
			}

			var offset = (dist - (segments * 4f)) / 2f;
			for (var i = 0; i < segments; i++)
			{
				if (i % 2 == 0)
				{
					continue;
				}

				var start = section.Start + angle * (offset + i * 4f);
				var end = start + angle * 4f;
				Draw.Line(start, end, Color.Black, 4f);
				Draw.Line(start + angle, end - angle, GetColor(), 2f);
			}
		}
	}

	public Section TryQuickAttach(Vector2 point, out Vector2 pos, Section ignore = null)
	{
		pos = Vector2.Zero;

		if (!Collidable)
		{
			return null;
		}

		if (Sections.First() != ignore)
		{
			Collider = startCollider;
			if (CollidePoint(point))
			{
				Collider = null;
				pos = Sections.First().Start;
				return Sections.First();
			}
		}
		if (Sections.Last() != ignore)
		{
			Collider = endCollider;
			if (CollidePoint(point))
			{
				Collider = null;
				pos = Sections.Last().End;
				return Sections.Last();
			}
		}

		return null;
	}

	public virtual Section GetIntersect(Vector2 lineFrom, Vector2 lineTo, out Vector2 intersect, float parallelTolerance = 0f)
	{
		intersect = Vector2.Zero;

		if (!Enabled)
		{
			return null;
		}

		var lineHorizontal = lineFrom.Y == lineTo.Y;
		var lineVertical = lineFrom.X == lineTo.X;

		var lineXFrom = lineTo - Vector2.UnitX * parallelTolerance;
		var lineYFrom = lineTo - Vector2.UnitY * parallelTolerance;
		var lineXTo = lineTo + Vector2.UnitX * parallelTolerance;
		var lineYTo = lineTo + Vector2.UnitY * parallelTolerance;

		foreach (var section in Sections)
		{
			if (Collide.LineCheck(section.Start, section.End, lineFrom, lineTo, out intersect))
			{
				return section;
			}

			// Check for parallel lines
			if (lineHorizontal && section.Start.Y == section.End.Y && parallelTolerance > 0f &&
				Collide.LineCheck(section.Start, section.End, lineYFrom, lineYTo, out intersect))
			{
				return section;
			}

			if (lineVertical && section.Start.X == section.End.X && parallelTolerance > 0f &&
				Collide.LineCheck(section.Start, section.End, lineXFrom, lineXTo, out intersect))
			{
				return section;
			}
		}

		return null;
	}

	public virtual Section GetSection(float progress)
	{
		return Sections.FirstOrDefault(s => progress >= s.Offset && progress < s.Offset + s.Length);
	}

	public Vector2 GetPos(float progress)
	{
		var section = GetSection(progress);
		if (section == null)
		{
			if (progress < 0f)
			{
				section = Sections.First();
			}
			else
			{
				section = Sections.Last();
			}
		}
		return section.GetPos(progress - section.Offset);
	}

	public class Section
	{
		public Vector2 Start;
		public Vector2 End;
		public float Offset;
		public float Length;

		public Section(Vector2 start, Vector2 end, float offset)
		{
			Start = start;
			End = end;
			Offset = offset;
			Length = Vector2.Distance(start, end);
		}

		public virtual Vector2 GetPos(float progress)
		{
			return Vector2.Lerp(Start, End, progress / Length);
		}

		public virtual float GetProgress(Vector2 pos)
		{
			return Vector2.Distance(pos, Start);
		}

		public virtual Vector2 GetAngle(float progress)
		{
			return Calc.SafeNormalize(End - Start);
		}
	}
}
