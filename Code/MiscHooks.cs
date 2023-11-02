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

namespace Celeste.Mod.EeveeHelper;

public static class MiscHooks
{
	private static ILHook levelLoadHook;
	private static ILHook zipMoverSequenceHook;

	public static void Load()
	{
		On.Monocle.Collide.Check_Entity_Entity += Collide_Check_Entity_Entity;
		On.Celeste.Actor.MoveHExact += Actor_MoveHExact;
		On.Celeste.Actor.MoveVExact += Actor_MoveVExact;
		On.Celeste.Actor.OnGround_int += Actor_OnGround_int;
		IL.Celeste.Holdable.Release += Holdable_Release;
		IL.Celeste.MapData.ParseBackdrop += MapData_ParseBackdrop;
		IL.Monocle.EntityList.UpdateLists += EntityList_UpdateLists;

		IL.Celeste.Solid.MoveHExact += Solid_MoveHExact;
		IL.Celeste.Solid.MoveVExact += Solid_MoveVExact;

		On.Celeste.StaticMover.Move += StaticMover_Move;

		IL.Celeste.SwapBlock.Update += SwapBlock_Update;

		levelLoadHook = new ILHook(typeof(Level).GetMethod("orig_LoadLevel", BindingFlags.Public | BindingFlags.Instance), Level_orig_LoadLevel);
		zipMoverSequenceHook = new ILHook(typeof(ZipMover).GetMethod("Sequence", BindingFlags.Instance | BindingFlags.NonPublic).GetStateMachineTarget(), ZipMover_Sequence);
	}

	public static void Unload()
	{
		On.Monocle.Collide.Check_Entity_Entity -= Collide_Check_Entity_Entity;
		On.Celeste.Actor.MoveHExact -= Actor_MoveHExact;
		On.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
		On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;
		IL.Celeste.Holdable.Release -= Holdable_Release;
		IL.Celeste.MapData.ParseBackdrop -= MapData_ParseBackdrop;
		IL.Monocle.EntityList.UpdateLists -= EntityList_UpdateLists;

		IL.Celeste.Solid.MoveHExact -= Solid_MoveHExact;
		IL.Celeste.Solid.MoveVExact -= Solid_MoveVExact;

		IL.Celeste.SwapBlock.Update -= SwapBlock_Update;
		On.Celeste.StaticMover.Move -= StaticMover_Move;

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

	//Small optimization here where if it isn't an EntityContainingSet or has an EntityContainer it doesn't worry about the messy code. This needs to be improved a ton already but whatever.
	private static bool Collide_Check_Entity_Entity(On.Monocle.Collide.orig_Check_Entity_Entity orig, Entity a, Entity b)
	{
		if (a == null || b == null || (a is CollidableModifier.Solidifier aSolid && aSolid.Entity == b) ||
			(b is CollidableModifier.Solidifier bSolid && bSolid.Entity == a))
		{
			return false;
		}
		EntityContainer aContainer = null;
		EntityContainer bContainer = null;
		if (a is IContainer iA) { aContainer = iA.Container; }
		if (b is IContainer iB) { bContainer = iB.Container; }
		if (aContainer == null && bContainer == null) { return orig(a, b); }
		if ((aContainer != null && !aContainer.CollideWithContained && aContainer.GetEntities().Contains(b)) ||
			(bContainer != null && !bContainer.CollideWithContained && bContainer.GetEntities().Contains(a)))
		{
			return false;
		}
		return orig(a, b);
	}

	private static bool Actor_MoveHExact(On.Celeste.Actor.orig_MoveHExact orig, Actor self, int moveH, Collision onCollide, Solid pusher)
	{
		var selfData = new DynamicData(typeof(Entity), self);
		var result = true;

		var solidified = selfData.Get<CollidableModifier.Solidifier>("solidModifierSolidifier");
		var solidifierCollidable = solidified?.Collidable ?? true;

		if (solidified != null)
		{
			solidified.Collidable = false;
		}

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

		if (solidified != null)
		{
			solidified.Collidable = solidifierCollidable;
			solidified.MoveTo(self.ExactPosition);
		}

		return result;
	}

	private static bool Actor_MoveVExact(On.Celeste.Actor.orig_MoveVExact orig, Actor self, int moveV, Collision onCollide, Solid pusher)
	{
		var selfData = new DynamicData(typeof(Entity), self);
		var result = true;

		var solidified = selfData.Get<CollidableModifier.Solidifier>("solidModifierSolidifier");
		var solidifierCollidable = solidified?.Collidable ?? true;

		if (solidified != null)
		{
			solidified.Collidable = false;
		}

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

		if (solidified != null)
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
		var selfData = DynamicData.For(self);
		var result = true;

		var solidified = selfData.Get<CollidableModifier.Solidifier>("solidModifierSolidifier");
		var solidifierCollidable = solidified?.Collidable ?? true;

		if (solidified != null)
		{
			solidified.Collidable = false;
		}

		if (self is HoldableContainer container)
		{
			container._Container.DoIgnoreCollision(() => result = orig(self, downCheck));
		}
		else
		{
			result = orig(self, downCheck);
		}

		if (solidified != null)
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
					return data.Get<Vector2>("start");
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

		var lerp = dyn.Get<float>("lerp");
		var target = dyn.Get<int>("target");
		Audio.Position(dyn.Get<FMOD.Studio.EventInstance>("moveSfx"), swap.Center);
		Audio.Position(dyn.Get<FMOD.Studio.EventInstance>("returnSfx"), swap.Center);
		if (lerp == target)
		{
			if (target == 0)
			{
				Audio.SetParameter(dyn.Get<FMOD.Studio.EventInstance>("returnSfx"), "end", 1f);
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
}
