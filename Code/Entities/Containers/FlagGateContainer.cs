using Celeste.Mod.EeveeHelper.Components;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Linq;

namespace Celeste.Mod.EeveeHelper.Entities.Containers;

[CustomEntity("EeveeHelper/FlagGateContainer")]
public class FlagGateContainer : Entity, IContainer
{
	// Originally "im so hecking gay".GetHashCode(), which desynced TASes on different platforms
	// Hardcoded to keep compatibility with older TASes but sync across future ones
	private const int RandomGay = -1569374549;

	public EntityContainer Container
	{
		get => _Container;
		set
		{
			if (value is EntityContainerMover mover)
			{
				_Container = mover;
			}
		}
	}

	public EntityContainerMover _Container;

	private Vector2[] nodes;
	private string flag; // replace
	private bool notFlag; // replace
						  //private bool resetFlags;
	private bool canReturn;
	private float shakeTime;
	private float moveTime;
	//private bool progression;
	private Ease.Easer easer;
	private Color inactiveColor;
	private Color activeColor;
	private Color finishColor;
	private bool iconVisible;
	private bool staticFit;
	private bool playSounds;

	private Vector2 startPos;
	private Vector2 anchorPos;
	private Vector2 offset;
	private Sprite icon;
	private Vector2 iconOffset;
	private Wiggler wiggler;
	private SoundSource openSfx;
	private bool moving;
	private bool cancelMoving;
	private bool activated; // replace
	//private bool[] wasEnabled;
	private bool vanilla;

	private bool shaking;
	private float shakeTimer;
	private Vector2 shakeAmount;
	private Random shakeRand;

	public FlagGateContainer(EntityData data, Vector2 offset) : base(data.Position + offset)
	{
		Collider = new Hitbox(data.Width, data.Height);
		Depth = Depths.BGDecals - 1;

		var rawNodes = data.NodesOffset(offset);
		if (rawNodes.Length > 0)
		{
			iconOffset = rawNodes[0] - Center;
		}

		nodes = rawNodes.Skip(1).ToArray();
		for (var i = 0; i < nodes.Length; i++)
		{
			nodes[i] += new Vector2(Width / 2f, Height / 2f);
		}

		EeveeUtils.ParseFlagAttr(data.Attr("moveFlag"), out flag, out notFlag);
		//resetFlags = data.Bool("resetFlags", true);
		canReturn = data.Bool("canReturn", true);
		shakeTime = data.Float("shakeTime", 0.5f);
		moveTime = data.Float("moveTime", 2f);
		//progression = data.Bool("progression");
		easer = EeveeHelperModule.EaseTypes[data.Attr("easing", "CubeOut")];
		inactiveColor = data.HexColor("inactiveColor", Calc.HexToColor("5fcde4"));
		activeColor = data.HexColor("activeColor", Color.White);
		finishColor = data.HexColor("finishColor", Calc.HexToColor("f141df"));
		iconVisible = data.Bool("iconVisible");
		staticFit = data.Bool("staticFit");
		playSounds = data.Bool("playSounds");
		var iconName = data.Attr("icon", "objects/switchgate/icon");

		shakeRand = new Random(RandomGay);

		startPos = anchorPos = Center;
		//wasEnabled = new bool[flags.Length];

		//if (flags.Length == 0 && nodes.Length > 0)
		if (string.IsNullOrEmpty(flag))
		{
			vanilla = true;
		}

		Add(icon = new Sprite(GFX.Game, iconName));
		icon.Add("spin", "", 0.1f, "spin");
		icon.Play("spin");
		icon.Rate = 0f;
		icon.Color = inactiveColor;
		icon.Position = new Vector2(Width / 2f, Height / 2f) + iconOffset;
		icon.Visible = iconVisible;
		icon.CenterOrigin();

		Add(wiggler = Wiggler.Create(0.5f, 4f, f => icon.Scale = Vector2.One * (1f + f)));
		Add(openSfx = new SoundSource());

		Add(_Container = new EntityContainerMover(data, true)
		{
			OnFit = OnFit,
			DefaultIgnored = e => e is FlagGateContainer,
			OnAttach = h => Depth = Math.Min(Depth, h.Entity.Depth - 1)
		});
	}

	public override void Awake(Scene scene)
	{
		base.Awake(scene);

		activated = vanilla ?
			Switch.CheckLevelFlag(scene as Level) :
			((scene as Level).Session.GetFlag(flag) != notFlag);

		if (activated)
		{
			MoveTo(nodes[0]);
			icon.Rate = 0f;
			icon.SetAnimationFrame(0);
			icon.Color = finishColor;
		}
	}

	public override void Update()
	{
		base.Update();

		var newActivated = vanilla ?
			Switch.Check(Scene) :
			(SceneAs<Level>().Session.GetFlag(flag) != notFlag);

		if (newActivated != activated && (!activated || canReturn))
		{
			activated = newActivated;
			Add(new Coroutine(Sequence(activated ? nodes[0] : startPos)));
		}

		var newDepth = Depths.BGDecals - 1;
		foreach (var entity in _Container.GetEntities())
		{
			newDepth = Math.Min(newDepth, entity.Depth - 1);
		}

		Depth = newDepth;

		if (shaking)
		{
			if (Scene.OnInterval(0.04f))
			{
				shakeAmount = shakeRand.ShakeVector();
			}
			if (shakeTimer > 0f)
			{
				shakeTimer -= Engine.DeltaTime;
				if (shakeTimer <= 0f)
				{
					shaking = false;
					StopShaking();
				}
			}
		}

		MoveTo(anchorPos);
	}

	public override void Render()
	{
		if (iconVisible)
		{
			icon.DrawOutline();
		}

		base.Render();
	}

	private void OnFit(Vector2 pos, float width, float height)
	{
		var lastCenter = Center;
		Position = pos;
		Collider.Width = width;
		Collider.Height = height;
		icon.Position = new Vector2(Width / 2f, Height / 2f) + iconOffset;
		if (staticFit)
		{
			_Container.DoMoveAction(() => Center = lastCenter);
		}
		else
		{
			offset += Center - lastCenter;
		}
	}

	private void MoveTo(Vector2 center)
	{
		anchorPos = center;
		if (Center != center + offset + shakeAmount)
		{
			_Container.DoMoveAction(() => Center = center + offset + shakeAmount);
		}
	}

	public void StartShaking(float time = 0f)
	{
		shaking = true;
		shakeTimer = time;
	}

	public void StopShaking()
	{
		shaking = false;
		shakeAmount = Vector2.Zero;
	}

	private IEnumerator Sequence(Vector2 node)
	{
		while (moving)
		{
			cancelMoving = true;
			yield return null;
		}
		cancelMoving = false;
		moving = true;
		var start = anchorPos;
		wiggler.Stop();
		icon.Scale = Vector2.One;
		if (node != start)
		{
			if (shakeTime > 0f)
			{
				yield return 0.1f;
				if (playSounds)
				{
					openSfx.Play("event:/game/general/touchswitch_gate_open");
				}

				StartShaking(shakeTime);
				while (icon.Rate < 1f)
				{
					var lastColor = icon.Color;
					icon.Color = Color.Lerp(lastColor, activeColor, icon.Rate);
					icon.Rate += Engine.DeltaTime / shakeTime;
					yield return null;
				}
				yield return 0.1f;
			}
			else
			{
				if (playSounds)
				{
					openSfx.Play("event:/game/general/touchswitch_gate_open");
				}

				icon.Rate = 1f;
				icon.Color = activeColor;
			}
			if (cancelMoving)
			{
				moving = false;
				yield break;
			}
			var tween = Tween.Create(Tween.TweenMode.Oneshot, easer, moveTime, start: true);
			var waiting = true;
			tween.OnUpdate = (t) => MoveTo(Vector2.Lerp(start, node, t.Eased));
			tween.OnComplete = (t) => waiting = false;
			Add(tween);
			while (waiting)
			{
				if (cancelMoving)
				{
					tween.Stop();
					Remove(tween);
					moving = false;
					yield break;
				}
				yield return null;
			}
			Remove(tween);
			MoveTo(node);
			if (playSounds)
			{
				Audio.Play("event:/game/general/touchswitch_gate_finish", Position);
			}

			if (shakeTime > 0f)
			{
				StartShaking(0.2f);
			}

			while (icon.Rate > 0f && !cancelMoving)
			{
				icon.Color = Color.Lerp(activeColor, activated ? finishColor : inactiveColor, 1f - icon.Rate);
				icon.Rate -= Engine.DeltaTime * 4f;
				yield return null;
			}
			if (cancelMoving)
			{
				moving = false;
				yield break;
			}
			icon.Rate = 0f;
			icon.SetAnimationFrame(0);
			wiggler.Start();
			if (iconVisible)
			{
				var was = Collidable;
				Collidable = false;
				for (var i = 0; i < 32; i++)
				{
					var angle = Calc.Random.NextFloat((float)Math.PI * 2f);
					SceneAs<Level>().ParticlesFG.Emit(TouchSwitch.P_Fire, Center + iconOffset + Calc.AngleToVector(angle, 4f), angle);
				}
				Collidable = was;
			}
		}
		else
		{
			icon.Rate = 1f;
			while (icon.Rate > 0f && !cancelMoving)
			{
				var lastColor = icon.Color;
				icon.Color = Color.Lerp(lastColor, activated ? finishColor : inactiveColor, 1f - icon.Rate);
				icon.Rate -= Engine.DeltaTime * 4f;
				yield return null;
			}
			icon.Color = finishColor;
			icon.Rate = 0f;
			icon.SetAnimationFrame(0);
		}
		moving = false;
	}
}
