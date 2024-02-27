using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Linq;

namespace Celeste.Mod.EeveeHelper.Entities;

[CustomEntity("EeveeHelper/InputToggleBlock")]
public class InputToggleBlock : Solid
{
	public enum Types
	{
		Grab,
		Jump,
		Dash,
		Custom
	}

	private string texture;
	private Types type;
	private float time;
	private bool cancellable;
	private string tutorialFlag;

	private InputListener listener;
	private PathRenderer path;
	private BirdTutorialGui gui;
	private Vector2 start;
	private Vector2 end;
	private float lerp;
	private bool moving;
	private bool toggled;

	public InputToggleBlock(EntityData data, Vector2 offset) : base(data.Position + offset, data.Width, data.Height, false)
	{
		Depth = Depths.FGTerrain + 1;

		texture = data.Attr("texture", "EeveeHelper/inputToggleBlock");
		type = data.Enum("type", Types.Grab);
		time = data.Float("time", 0.5f);
		cancellable = data.Bool("cancellable", true);
		tutorialFlag = data.Attr("tutorialFlag");

		start = Position;
		end = data.NodesOffset(offset).FirstOrDefault();
	}

	public override void Added(Scene scene)
	{
		base.Added(scene);

		scene.Add(listener = new InputListener(type, Depth + 1));

		var color = Color.White;
		switch (type)
		{
			case Types.Grab: color = new Color(1f, 0f, 1f); break;
			case Types.Jump: color = new Color(1f, 1f, 0f); break;
			case Types.Dash: color = new Color(0f, 1f, 1f); break;
			case Types.Custom: color = new Color(0f, 0f, 1f); break;
		}

		var typeName = type.ToString().ToLower();

		var offset = new Vector2(Width, Height) / 2f;
		scene.Add(path = new PathRenderer(start + offset, end + offset, texture, typeName, color));

		var tex = GFX.Game[$"objects/{texture}/block_{typeName}"];

		var slices = new MTexture[3, 3];
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				slices[i, j] = tex.GetSubtexture(i * 8, j * 8, 8, 8);
			}
		}

		var inner_columns = (int)Math.Ceiling(Width / 8) - 2;
		var inner_rows = (int)Math.Ceiling(Height / 8) - 2;
		for (int i = 0; i < inner_columns; i++)
		{
			Add(new Image(slices[1, 0]) { Position = new Vector2(i * 8 + 8, 0) });
			Add(new Image(slices[1, 2]) { Position = new Vector2(i * 8 + 8, Height - 8) });
			for (int j = 0; j < inner_rows; j++)
			{
				Add(new Image(slices[1, 1]) { Position = new Vector2(i * 8 + 8, j * 8 + 8) });
			}
		}
		for (int i = 0; i < inner_rows; i++)
		{
			Add(new Image(slices[0, 1]) { Position = new Vector2(0, i * 8 + 8) });
			Add(new Image(slices[2, 1]) { Position = new Vector2(Width - 8, i * 8 + 8) });
		}
		Add(new Image(slices[0, 0]) { Position = new Vector2(0, 0) });
		Add(new Image(slices[2, 0]) { Position = new Vector2(Width - 8, 0) });
		Add(new Image(slices[0, 2]) { Position = new Vector2(0, Height - 8) });
		Add(new Image(slices[2, 2]) { Position = new Vector2(Width - 8, Height - 8) });

		if (!string.IsNullOrEmpty(tutorialFlag))
		{
			object[] inputs = null;
			switch (type)
			{
				case Types.Grab:   inputs = [ Input.Grab ]; break;
				case Types.Jump:   inputs = [ Input.Jump ]; break;
				case Types.Dash:   inputs = [ Input.Dash ]; break;
				case Types.Custom: inputs = [ EeveeHelperModule.Settings.ActivateCustomInputBlock.Button, "(Rebindable)" ]; break;
			}

			gui = new BirdTutorialGui(this, new Vector2(Width / 2f, 0), Dialog.Clean($"EeveeHelper_InputToggleBlock_{typeName}"), inputs);
		}
	}

	public override void Removed(Scene scene)
	{
		base.Removed(scene);
		listener?.RemoveSelf();
		path?.RemoveSelf();
		gui?.RemoveSelf();
	}

	public override void Update()
	{
		base.Update();

		if (listener.ConsumePress(cancellable) && (cancellable || !moving))
		{
			Audio.Play(SFX.game_05_swapblock_move, Center);
			toggled = !toggled;
			moving = true;
		}

		if (moving)
		{
			lerp = Calc.Approach(lerp, toggled ? 1f : 0f, time > 0f ? Engine.DeltaTime / time : 1f);
			if (lerp == (toggled ? 1f : 0f))
			{
				Audio.Play(SFX.game_05_swapblock_move_end, Center);
				moving = false;
			}
			MoveTo(Vector2.Lerp(start, end, lerp));
		}

		if (!string.IsNullOrEmpty(tutorialFlag))
		{
			var open = SceneAs<Level>().Session.GetFlag(tutorialFlag);
			if (open && !gui.Open)
				Add(new Coroutine(ShowTutorialRoutine()));
			else if (!open && gui.Open)
				Add(new Coroutine(HideTutorialRoutine()));
		}

		path.Visible = Visible;
	}

	private IEnumerator ShowTutorialRoutine()
	{
		gui.Open = true;
		Scene.Add(gui);
		while (gui.Open && gui.Scale < 1f)
			yield return null;
		yield break;
	}

	private IEnumerator HideTutorialRoutine()
	{
		gui.Open = false;
		while (!gui.Open && gui.Scale > 0f)
			yield return null;
		if (!gui.Open)
			Scene.Remove(gui);
		yield break;
	}


	private class PathRenderer : Entity
	{
		private Vector2 endpoint;
		private MTexture texture;
		private Color color;

		public PathRenderer(Vector2 start, Vector2 end, string texturePath, string textureType, Color pathColor) : base(start)
		{
			Depth = Depths.BGDecals - 1;
			endpoint = end;
			color = pathColor;

			texture = GFX.Game[$"objects/{texturePath}/node_{textureType}"];
		}

		public override void Render()
		{
			base.Render();
			Draw.Line(Position, endpoint, color * 0.2f, 4f);
			texture.DrawCentered(Position);
			texture.DrawCentered(endpoint);
		}
	}

	private class InputListener : Entity
	{
		private Types inputType;
		private int presses = 0;
		private bool wasPressed;

		public InputListener(Types type, int depth)
		{
			inputType = type;

			Depth = depth;
			AddTag(Tags.FrozenUpdate);
		}

		public override void Update()
		{
			base.Update();

			var pressed = false;

			switch (inputType)
			{
				case Types.Grab: pressed = Input.Grab.Pressed; wasPressed = false; break;
				case Types.Jump: pressed = Input.Jump.Pressed; break;
				case Types.Dash: pressed = Input.Dash.Pressed || Input.CrouchDash.Pressed; break;
				case Types.Custom: pressed = EeveeHelperModule.Settings.ActivateCustomInputBlock.Pressed; wasPressed = false; break;
			}

			if (pressed && !wasPressed)
			{
				presses++;
			}

			wasPressed = pressed;
		}

		public bool ConsumePress(bool cancellable)
		{
			var pressed = cancellable ? presses % 2 == 1 : presses > 0;

			presses = 0;

			return pressed;
		}
	}
}