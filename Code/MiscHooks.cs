using Celeste.Mod.EeveeHelper.Components;
using Celeste.Mod.EeveeHelper.Entities;
using Celeste.Mod.EeveeHelper.Entities.Modifiers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.EeveeHelper;

public static class MiscHooks
{
	private delegate bool orig_Input_get_CrouchDashPressed();

	private static Hook inputGetCrouchDashPressedHook;
	private static ILHook levelLoadHook;
	private static ILHook zipMoverSequenceHook;

	public static void Load()
	{
		IL.Monocle.Collide.Check_Entity_Entity += Collide_Check_Entity_Entity;
		On.Celeste.Actor.MoveHExact += Actor_MoveHExact;
		On.Celeste.Actor.MoveVExact += Actor_MoveVExact;
		On.Celeste.Actor.OnGround_int += Actor_OnGround_int;
		IL.Celeste.Holdable.Release += Holdable_Release;
		IL.Celeste.MapData.ParseBackdrop += MapData_ParseBackdrop;
		IL.Monocle.EntityList.UpdateLists += EntityList_UpdateLists;
		On.Celeste.Player.Update += Player_Update;
		On.Celeste.Player.ClimbBegin += Player_ClimbBegin;
		On.Celeste.Player.ClimbUpdate += Player_ClimbUpdate;
		On.Celeste.Player.IsRiding_Solid += Player_IsRiding_Solid;
		On.Celeste.Level.LoadLevel += Level_LoadLevel;

		IL.Celeste.Solid.MoveHExact += Solid_MoveHExact;
		IL.Celeste.Solid.MoveVExact += Solid_MoveVExact;

		On.Celeste.StaticMover.Move += StaticMover_Move;

		IL.Celeste.SwapBlock.Update += SwapBlock_Update;

		inputGetCrouchDashPressedHook = new Hook(typeof(Input).GetProperty("CrouchDashPressed", BindingFlags.Public | BindingFlags.Static).GetMethod, Input_get_CrouchDashPressed);
		inputGetCrouchDashPressedHook.Apply();

		levelLoadHook = new ILHook(typeof(Level).GetMethod("orig_LoadLevel", BindingFlags.Public | BindingFlags.Instance), Level_orig_LoadLevel);
		zipMoverSequenceHook = new ILHook(typeof(ZipMover).GetMethod("Sequence", BindingFlags.Instance | BindingFlags.NonPublic).GetStateMachineTarget(), ZipMover_Sequence);
	}

	public static void Unload()
	{
		IL.Monocle.Collide.Check_Entity_Entity -= Collide_Check_Entity_Entity;
		On.Celeste.Actor.MoveHExact -= Actor_MoveHExact;
		On.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
		On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;
		IL.Celeste.Holdable.Release -= Holdable_Release;
		IL.Celeste.MapData.ParseBackdrop -= MapData_ParseBackdrop;
		IL.Monocle.EntityList.UpdateLists -= EntityList_UpdateLists;
		On.Celeste.Player.Update -= Player_Update;
		On.Celeste.Player.ClimbBegin -= Player_ClimbBegin;
		On.Celeste.Player.ClimbUpdate -= Player_ClimbUpdate;
		On.Celeste.Player.IsRiding_Solid -= Player_IsRiding_Solid;
		On.Celeste.Level.LoadLevel -= Level_LoadLevel;

		IL.Celeste.Solid.MoveHExact -= Solid_MoveHExact;
		IL.Celeste.Solid.MoveVExact -= Solid_MoveVExact;

		IL.Celeste.SwapBlock.Update -= SwapBlock_Update;
		On.Celeste.StaticMover.Move -= StaticMover_Move;

		inputGetCrouchDashPressedHook?.Dispose();

		levelLoadHook?.Dispose();
		zipMoverSequenceHook?.Dispose();
	}

	private static HashSet<EntityData> GlobalModifiedData = new();
	private static int LastLoadedGlobalModified;
	private static bool LoadingGlobalModified;
	private static int LoadingGlobalTags;

	private static void EntityList_UpdateLists(ILContext il)
	{
		var cursor = new ILCursor(il);

		var addedEntityLoc = -1;
		if (cursor.TryGotoNext(MoveType.Before,
			instr => instr.MatchLdloc(out addedEntityLoc),
			instr => true,
			instr => true,
			instr => instr.MatchCallvirt<Entity>("Added")))
		{
			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Ldloc, addedEntityLoc);
			cursor.EmitDelegate<Action<EntityList, Entity>>((entityList, entity) =>
			{
				var component = entity.Get<TagAdderComponent>();
				if (component != null)
				{
					LoadingGlobalModified = true;
					LoadingGlobalTags = component.Tags;
					LastLoadedGlobalModified = entityList.ToAdd.Count;
				}
			});

			cursor.Index += 4;

			cursor.Emit(OpCodes.Ldarg_0);
			cursor.EmitDelegate<Action<EntityList>>(entityList =>
			{
				if (LoadingGlobalModified)
				{
					foreach (var entity in entityList.ToAdd.Skip(LastLoadedGlobalModified))
					{
						entity.Add(new TagAdderComponent(LoadingGlobalTags));
					}

					LastLoadedGlobalModified = 0;
					LoadingGlobalModified = false;
				}
			});

			Logger.Log("EeveeHelper", "Added IL Hook for EntityList.UpdateLists (Added)");
		}

		var awakeEntityLoc = -1;
		if (cursor.TryGotoNext(MoveType.Before,
			instr => instr.MatchLdloc(out awakeEntityLoc),
			instr => true,
			instr => true,
			instr => instr.MatchCallvirt<Entity>("Awake")))
		{
			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Ldloc, addedEntityLoc);
			cursor.EmitDelegate<Action<EntityList, Entity>>((entityList, entity) =>
			{
				var component = entity.Get<TagAdderComponent>();
				if (component != null)
				{
					LoadingGlobalModified = true;
					LoadingGlobalTags = component.Tags;
					LastLoadedGlobalModified = entityList.ToAdd.Count;
				}
			});

			cursor.Index += 4; //yuck

			cursor.Emit(OpCodes.Ldarg_0);
			cursor.EmitDelegate<Action<EntityList>>(entityList =>
			{
				if (LoadingGlobalModified)
				{
					foreach (var entity in entityList.ToAdd.Skip(LastLoadedGlobalModified))
					{
						entity.Add(new TagAdderComponent(LoadingGlobalTags));
					}

					LastLoadedGlobalModified = 0;
					LoadingGlobalModified = false;
				}
			});

			Logger.Log("EeveeHelper", "Added IL Hook for EntityList.UpdateLists (Awake)");
		}
	}

	private static void Level_orig_LoadLevel(ILContext il)
	{
		var cursor = new ILCursor(il);

		HookLevelEntityLoading(cursor, "Entities");
		HookLevelEntityLoading(cursor, "Triggers");
	}

	private static void HookLevelEntityLoading(ILCursor cursor, string type)
	{
		if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<LevelData>(type)))
		{
			return;
		}

		cursor.Emit(OpCodes.Ldarg_0);
		cursor.Emit(OpCodes.Ldarg_2);
		cursor.EmitDelegate<Func<List<EntityData>, Level, bool, List<EntityData>>>((list, self, isFromLoader) =>
		{
			if (isFromLoader)
			{
				if (type == "Entities")
				{
					GlobalModifiedData.Clear();
				}
				var newList = new List<EntityData>(list);
				foreach (var level in self.Session.MapData.Levels)
				{
					foreach (var modifier in level.Entities.Where(data => data.Name == GlobalModifier.ID))
					{
						var whitelist = modifier.Attr("whitelist").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
						var contain = new Rectangle((int)modifier.Position.X, (int)modifier.Position.Y, modifier.Width, modifier.Height);
						int tags = Tags.Global;
						if (modifier.Bool("frozenUpdate")) tags |= Tags.FrozenUpdate;
						if (modifier.Bool("pauseUpdate")) tags |= Tags.PauseUpdate;
						if (modifier.Bool("transitionUpdate")) tags |= Tags.TransitionUpdate;

						foreach (var entity in (type == "Entities" ? level.Entities : level.Triggers))
						{
							if (entity != modifier && (whitelist.Length == 0 || whitelist.Any(str => entity.Name == str || entity.ID.ToString() == str)))
							{
								var matched = false;
								if (entity.Width != 0 && entity.Height != 0)
								{
									var rect = new Rectangle((int)entity.Position.X, (int)entity.Position.Y, entity.Width, entity.Height);
									matched = contain.Intersects(rect);
								}
								else
								{
									matched = contain.Contains((int)entity.Position.X, (int)entity.Position.Y);
								}

								if (matched)
								{
									if (newList.Contains(entity))
									{
										newList.Remove(entity);
									}

									GlobalModifiedData.Add(entity);

									var data = EeveeUtils.CloneEntityData(entity, self.Session.LevelData);
									data.Values["globalModifierSpawned"] = tags;

									newList.Add(data);
								}
							}
						}
					}
				}
				return newList;
			}
			else if (GlobalModifiedData.Count > 0)
			{
				return list.Except(GlobalModifiedData).ToList();
			}
			else
			{
				return list;
			}
		});
		Logger.Log("EeveeHelper", $"Added IL Hook for Level.orig_LoadLevel ({type} - 1)");

		if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdfld<EntityData>("ID")))
		{
			return;
		}

		cursor.Emit(OpCodes.Ldarg_0);
		cursor.EmitDelegate<Func<EntityData, Level, EntityData>>((entityData, self) =>
		{
			var tags = 0;
			if ((tags = entityData.Int("globalModifierSpawned", -1)) != -1)
			{
				LoadingGlobalModified = true;
				LoadingGlobalTags = tags;
				LastLoadedGlobalModified = self.Entities.ToAdd.Count;
			}
			return entityData;
		});
		Logger.Log("EeveeHelper", $"Added IL Hook for Level.orig_LoadLevel ({type} - 2)");

		if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchBrtrue(out _), instr => instr.MatchLeaveS(out _)))
		{
			return;
		}

		cursor.Emit(OpCodes.Ldarg_0);
		cursor.EmitDelegate<Action<Level>>(self =>
		{
			if (LoadingGlobalModified && self.Entities?.ToAdd != null)
			{
				foreach (var entity in self.Entities.ToAdd.Skip(LastLoadedGlobalModified))
				{
					entity?.Add(new TagAdderComponent(LoadingGlobalTags));
				}

				LastLoadedGlobalModified = 0;
				LoadingGlobalModified = false;
			}
		});
		Logger.Log("EeveeHelper", $"Added IL Hook for Level.orig_LoadLevel ({type} - 3)");
	}

	private static void Collide_Check_Entity_Entity(ILContext il)
	{
		ILCursor cursor = new ILCursor(il);
		ILLabel label = null;
		if(cursor.TryGotoNext(MoveType.After, i => i.MatchBeq(out label)))
		{
			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Ldarg_1);
			cursor.Emit(OpCodes.Call, typeof(MiscHooks).GetMethod(nameof(CheckContainers), BindingFlags.NonPublic | BindingFlags.Static));
			cursor.Emit(OpCodes.Brtrue, label);
		}
	}
	// If this statement is *true*, Collide Check returns *false*
	private static bool CheckContainers(Entity a, Entity b) =>
		(a is IContainer iA && iA.Container is { } aContainer && aContainer != null && !aContainer.CollideWithContained && aContainer.GetEntities().Contains(b)) ||
		(b is IContainer iB && iB.Container is { } bContainer && bContainer != null && !bContainer.CollideWithContained && bContainer.GetEntities().Contains(a)) ||
		(a is CollidableModifier.Solidifier aSolid && aSolid.Entity == b) ||
		(b is CollidableModifier.Solidifier bSolid && bSolid.Entity == a); 

	// Update: Prevents new DynamicData being created for every entity, since we don't want a DynamicData object created here we only want to see if one exists.
	private static FieldInfo DynamicData__DynamicDataMap = typeof(DynamicData).GetField("_DynamicDataMap", BindingFlags.Static | BindingFlags.NonPublic);

	private static bool Actor_MoveHExact(On.Celeste.Actor.orig_MoveHExact orig, Actor self, int moveH, Collision onCollide, Solid pusher)
	{
		CollidableModifier.Solidifier solidified = null;
		bool SolidifierExists = ((ConditionalWeakTable<object, DynamicData>)DynamicData__DynamicDataMap.GetValue(null)).TryGetValue(self, out var selfData) &&
			selfData.TryGet("solidModifierSolidifier", out solidified);

		var solidifierCollidable = true;
		if (SolidifierExists)
		{
			solidifierCollidable = solidified.Collidable;
			solidified.Collidable = false;
		}

		var result = true;
		if (self is HoldableContainer container)
		{
			var collidable = self.Collidable;
			container._Container.DoMoveAction(() => result = orig(self, moveH, onCollide, pusher),
				(entity, move) => (entity is Platform platform) ? new Vector2(platform.LiftSpeed.X + container.Speed.X, platform.LiftSpeed.Y) : move);
		}
		else
		{
			result = orig(self, moveH, onCollide, pusher);
		}
		if (SolidifierExists)
		{
			if (solidified != null)
			{
				solidified.Collidable = solidifierCollidable;
				solidified.MoveTo(self.ExactPosition);
			}
		}

		return result;
	}

	private static bool Actor_MoveVExact(On.Celeste.Actor.orig_MoveVExact orig, Actor self, int moveV, Collision onCollide, Solid pusher)
	{
		CollidableModifier.Solidifier solidified = null;
		bool SolidifierExists = ((ConditionalWeakTable<object, DynamicData>)DynamicData__DynamicDataMap.GetValue(null)).TryGetValue(self, out var selfData) &&
			selfData.TryGet<CollidableModifier.Solidifier>("solidModifierSolidifier", out solidified);

		var solidifierCollidable = true;
		if (SolidifierExists)
		{
			solidifierCollidable = solidified.Collidable;
			solidified.Collidable = false;
		}

		var result = true;
		if (self is HoldableContainer container)
		{
			var collidable = self.Collidable;
			container._Container.DoMoveAction(() => result = orig(self, moveV, onCollide, pusher),
				(entity, move) => (entity is Platform platform) ? new Vector2(platform.LiftSpeed.X, platform.LiftSpeed.Y + container.Speed.Y) : move);
		}
		else
		{
			result = orig(self, moveV, onCollide, pusher);
		}

		if (SolidifierExists)
		{
			solidified.Collidable = solidifierCollidable;
			var collidable = self.Collidable;
			var pushable = self.AllowPushing;
			self.Collidable = false;
			self.AllowPushing = false;
			solidified.MoveTo(self.ExactPosition);
			self.Collidable = collidable;
			self.AllowPushing = pushable;
		}

		return result;
	}

	private static bool Actor_OnGround_int(On.Celeste.Actor.orig_OnGround_int orig, Actor self, int downCheck)
	{
		CollidableModifier.Solidifier solidified = null;
		bool SolidifierExists = ((ConditionalWeakTable<object, DynamicData>)DynamicData__DynamicDataMap.GetValue(null)).TryGetValue(self, out var selfData) &&
			selfData.TryGet<CollidableModifier.Solidifier>("solidModifierSolidifier", out solidified);

		var solidifierCollidable = true;
		if (SolidifierExists)
		{
			solidifierCollidable = solidified.Collidable;
			solidified.Collidable = false;
		}

		var result = true;
		if (self is HoldableContainer container)
		{
			container._Container.DoIgnoreCollision(() => result = orig(self, downCheck));
		}
		else
		{
			result = orig(self, downCheck);
		}

		if (SolidifierExists)
		{
			solidified.Collidable = solidifierCollidable;
		}

		return result;
	}

	private static void Holdable_Release(ILContext il)
	{
		var cursor = new ILCursor(il);

		ILLabel endLabel = null;
		if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchBrfalse(out endLabel)))
		{
			Logger.Log("EeveeHelper", "Added IL hook at Holdable.Release");

			cursor.Emit(OpCodes.Ldarg_0);
			cursor.EmitDelegate<Func<Holdable, bool>>(self => self.Entity is HoldableContainer || self.Entity is HoldableTiles);
			cursor.Emit(OpCodes.Brtrue_S, endLabel);
		}
	}


	private static BlendState MultiplyBlend = new()
	{
		ColorBlendFunction = BlendFunction.Add,
		ColorSourceBlend = Blend.DestinationColor,
		ColorDestinationBlend = Blend.Zero
	};

	private static BlendState ReverseSubtractBlend = new()
	{
		ColorSourceBlend = Blend.One,
		ColorDestinationBlend = Blend.One,
		ColorBlendFunction = BlendFunction.Subtract,
		AlphaSourceBlend = Blend.One,
		AlphaDestinationBlend = Blend.One,
		AlphaBlendFunction = BlendFunction.Add
	};

	private static ConstructorInfo m_ParallaxCtor
		= typeof(Parallax).GetConstructor(new Type[] { typeof(MTexture) });

	private static void MapData_ParseBackdrop(ILContext il)
	{
		var cursor = new ILCursor(il);

		var parallaxLoc = -1;
		if (cursor.TryGotoNext(instr => instr.MatchNewobj(m_ParallaxCtor)))
		{
			if (cursor.TryGotoNext(instr => instr.MatchStloc(out parallaxLoc)))
			{
				Logger.Log("EeveeHelper", "Passed first for MapData.ParseBackdrop");

				var textLoc = 0;
				if (cursor.TryGotoNext(MoveType.AfterLabel, instr => instr.MatchLdloc(out textLoc), instr => instr.MatchLdstr("additive")))
				{
					Logger.Log("EeveeHelper", "Added IL hook at MapData.ParseBackdrop");

					cursor.Emit(OpCodes.Ldloc, parallaxLoc);
					cursor.Emit(OpCodes.Ldloc, textLoc);
					cursor.EmitDelegate<Action<Parallax, string>>((parallax, text) =>
					{
						if (text == "multiply")
						{
							parallax.BlendState = MultiplyBlend;
						}
						else if (text == "subtract")
						{
							parallax.BlendState = GFX.Subtract;
						}
						else if (text == "reversesubtract")
						{
							parallax.BlendState = ReverseSubtractBlend;
						}
					});
				}
			}
		}
	}

	private static void ZipMover_Sequence(ILContext il)
	{
		var cursor = new ILCursor(il);

		var thisLoc = -1;
		FieldReference fieldRef = null;

		if (!cursor.TryGotoNext(MoveType.After,
			instr => instr.MatchLdloc(out thisLoc),
			instr => instr.MatchLdfld<Entity>("Position"),
			instr => instr.MatchStfld(out fieldRef)))
		{
			Logger.Log("EeveeHelper", $"Failed zip mover hook");
			return;
		}

		while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld(fieldRef)))
		{
			Logger.Log("EeveeHelper", $"Hooking zip mover start field at position {cursor.Index}");

			cursor.Emit(OpCodes.Ldloc, thisLoc);
			cursor.EmitDelegate<Func<Vector2, ZipMover, Vector2>>((start, entity) =>
			{
				var data = DynamicData.For(entity);
				if (data.Get<bool?>("zipMoverNodeHandled") == true)
				{
					return entity.start;
				}
				return start;
			});
		}
	}

	private static void Solid_MoveHExact(ILContext il)
	{
		var cursor = new ILCursor(il);
		var entityIndex = il.Body.Variables.FirstOrDefault(v => v.VariableType.FullName == "Celeste.Actor").Index;
		while (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdarg(0), i2 => i2.MatchLdcI4(1), i3 => i3.MatchStfld<Entity>("Collidable")))
		{
			var label = cursor.MarkLabel();
			var clone = cursor.Clone();
			if (clone.TryGotoPrev(MoveType.Before, instr => instr.MatchLdloc(entityIndex), i2 => i2.MatchLdarg(0), i3 => i3.MatchLdfld<Platform>("LiftSpeed")))
			{
				clone.Emit(OpCodes.Ldsfld, typeof(EntityContainerMover).GetField(nameof(EntityContainerMover.LiftSpeedFix), BindingFlags.Static | BindingFlags.Public));
				clone.Emit(OpCodes.Brtrue, label);
			}
		}
	}
	private static void Solid_MoveVExact(ILContext il)
	{
		var cursor = new ILCursor(il);
		var entityIndex = il.Body.Variables.FirstOrDefault(v => v.VariableType.FullName == "Celeste.Actor").Index;
		while (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdarg(0), i2 => i2.MatchLdcI4(1), i3 => i3.MatchStfld<Entity>("Collidable")))
		{
			var label = cursor.MarkLabel();
			var clone = cursor.Clone();
			if (clone.TryGotoPrev(MoveType.Before, instr => instr.MatchLdloc(entityIndex), i2 => i2.MatchLdarg(0), i3 => i3.MatchLdfld<Platform>("LiftSpeed")))
			{
				clone.Emit(OpCodes.Ldsfld, typeof(EntityContainerMover).GetField(nameof(EntityContainerMover.LiftSpeedFix), BindingFlags.Static | BindingFlags.Public));
				clone.Emit(OpCodes.Brtrue, label);
			}
		}
	}

	private static void SwapBlock_Update(ILContext il)
	{
		var cursor = new ILCursor(il);
		if (cursor.TryGotoNext(MoveType.After, i => i.MatchLdarg(0), i2 => i2.MatchLdfld<Entity>("Position"), i3 => i3.MatchCall<Vector2>("op_Inequality")))
		{
			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Call, typeof(MiscHooks).GetMethod("ModifiedSwapBlockCheckHandler", BindingFlags.Static | BindingFlags.NonPublic));
		}
	}

	private static bool ModifiedSwapBlockCheckHandler(bool @in, SwapBlock swap)
	{
		var dyn = DynamicData.For(swap);
		if (!dyn.Data.ContainsKey(Handlers.Impl.SwapBlockHandler.HandledString))
		{
			return @in;
		}

		Audio.Position(swap.moveSfx, swap.Center);
		Audio.Position(swap.returnSfx, swap.Center);

		if (swap.lerp == swap.target)
		{
			if (swap.target == 0)
			{
				Audio.SetParameter(swap.returnSfx, "end", 1f);
				Audio.Play("event:/game/05_mirror_temple/swapblock_return_end", swap.Center);
			}
			else
			{
				Audio.Play("event:/game/05_mirror_temple/swapblock_move_end", swap.Center);
			}
		}

		return false;
	}

	private static void StaticMover_Move(On.Celeste.StaticMover.orig_Move orig, StaticMover self, Vector2 amount)
	{
		if (self.Platform is Solid && self.Entity is Decal && EntityContainerMover.DecalStaticMoverFix)
		{
			return;
		}
		orig(self, amount);
	}

	private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
	{
		orig(self);

		if (self.StateMachine.State == Player.StClimb && self.Speed.Y >= 0f && TryGetCeilingPopLeniency(self, out var leniency))
		{
			var data = DynamicData.For(self);

			if (data.TryGet<float>("EeveeHelper_cPopDelay", out var cPopDelay) && cPopDelay > 0)
			{
				return;
			}

			if (!self.CollideCheck<Solid>(self.Position + Vector2.UnitX * (float)self.Facing))
			{
				data.Set("EeveeHelper_cPopDelay", leniency);

				for (var i = 1; i <= 2; i++)
				{
					if (self.CollideCheck<Solid>(self.Position + new Vector2((float)self.Facing, -i)))
					{
						self.MoveVExact(-i + 1);
						break;
					}
				}

				self.Speed = Vector2.Zero;
			}
			else if (!self.CollideCheck<Solid>(self.Position + new Vector2((float)self.Facing, 1f)))
			{
				self.movementCounter.Y = 0.49f;
			}
		}
	}

	private static void Player_ClimbBegin(On.Celeste.Player.orig_ClimbBegin orig, Player self)
	{
		orig(self);

		DynamicData.For(self).Set("EeveeHelper_cPopDelay", 0f);
	}

	private static int Player_ClimbUpdate(On.Celeste.Player.orig_ClimbUpdate orig, Player self)
	{
		var data = DynamicData.For(self);

		if (!data.TryGet<float>("EeveeHelper_cPopDelay", out var cPopDelay) || cPopDelay <= 0f)
		{
			return orig(self);
		}

		cPopDelay -= Engine.DeltaTime;

		data.Set("EeveeHelper_cPopDelay", 0f);

		self.movementCounter.X = self.Facing == Facings.Right ? 0.49f : -0.49f;
		self.movementCounter.Y = 0.49f;

		if (Input.Jump.Pressed && (!self.Ducking || self.CanUnDuck))
		{
			if (self.moveX == -(int)self.Facing)
			{
				self.WallJump(-(int)self.Facing);
			}
			else
			{
				self.ClimbJump();
			}

			return Player.StNormal;
		}

		if (self.CanDash)
		{
			self.Speed += self.LiftBoost;

			return self.StartDash();
		}

		if (!Input.GrabCheck)
		{
			self.Speed += self.LiftBoost;
			self.Play("event:/char/madeline/grab_letgo", null, 0f);

			return Player.StNormal;
		}

		if (cPopDelay <= 0f)
		{
			self.StateMachine.State = Player.StNormal;

			// Instant regrabbing (for following moving solids)
			if (Input.GrabCheck && !self.IsTired && !self.Ducking && !SaveData.Instance.Assists.NoGrabbing && Input.MoveY < 1f && self.level.Wind.Y <= 0f)
			{
				for (var i = 1; i <= 2; i++)
				{
					if (!self.CollideCheck<Solid>(self.Position + Vector2.UnitY * -i) && self.ClimbCheck((int)self.Facing, -i))
					{
						self.MoveVExact(-i);

						self.StateMachine.State = Player.StClimb;

						return Player.StClimb;
					}
				}
			}

			return Player.StNormal;
		}

		data.Set("EeveeHelper_cPopDelay", cPopDelay);

		return Player.StClimb;
	}

	private static bool Player_IsRiding_Solid(On.Celeste.Player.orig_IsRiding_Solid orig, Player self, Solid solid)
	{
		if (self.StateMachine.State == Player.StClimb)
		{
			var data = DynamicData.For(self);

			if (data.TryGet<float>("EeveeHelper_cPopDelay", out var cPopDelay) && cPopDelay > 0f)
			{
				return self.CollideCheck(solid, self.Position + new Vector2((float)self.Facing, -1f));
			}
		}

		return orig(self, solid);
	}

	private static bool TryGetCeilingPopLeniency(Player player, out float leniency)
	{
		foreach (LenientCeilingPopTrigger trigger in player.Scene.Tracker.GetEntities<LenientCeilingPopTrigger>())
		{
			if (trigger.PlayerIsInside)
			{
				leniency = trigger.LeniencyFrames / 60f;
				return true;
			}
		}

		var controller = player.Scene.Tracker.GetEntity<LenientCeilingPopController>();

		if (controller != null)
		{
			leniency = controller.LeniencyFrames / 60f;
			return true;
		}

		leniency = 0f;
		return false;
	}

	private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader)
	{
		orig(self, playerIntro, isFromLoader);

		NoDemoBindController.UpdateTriggered();
	}

	private static bool Input_get_CrouchDashPressed(orig_Input_get_CrouchDashPressed orig)
	{
		if (NoDemoBindController.Triggered)
		{
			return false;
		}

		return orig();
	}
}
