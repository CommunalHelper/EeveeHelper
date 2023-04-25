using Celeste.Mod.EeveeHelper.Compat;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.EeveeHelper.Entities {
    [Tracked]
    [CustomEntity("EeveeHelper/CoreZone")]
    public class CoreZone : Entity {
        public enum Mode {
            Current,
            Inverted,
            Hot,
            Cold,
            None
        }

        private static string[] hotTags = new string[] { "hot" };
        private static string[] coldTags = new string[] { "cold" };
        private static string[] noTags = new string[] { "nocore" };

        public static Dictionary<string, Session.CoreModes> ZoneGroups = new Dictionary<string, Session.CoreModes>();

        private Mode zoneMode;
        private bool addColorMask;
        private bool addStyleMask;

        private bool playerInside;

        public Entity ColorGradeMask;
        public Entity StylegroundMask;

        public string ZoneId;
        public Session.CoreModes CurrentMode;
        public List<Entity> Contained;

        public CoreZone(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Collider = new Hitbox(data.Width, data.Height);

            AddTag(Tags.TransitionUpdate);

            ZoneId = data.Attr("zoneId");
            zoneMode = data.Enum("coreMode", Mode.Current);
            addColorMask = data.Bool("colorGradeMask");
            addStyleMask = data.Bool("stylegroundMask");

            Contained = new List<Entity>();

            Add(new CoreModeListener(mode => {
                if (playerInside)
                    UpdateMusic(GetCoreMode());
            }));
        }

        public override void Added(Scene scene) {
            base.Added(scene);

            CurrentMode = GetCoreMode();

            if (addColorMask || addStyleMask) {
                if (!EeveeHelperModule.StyleMaskHelperLoaded)
                    throw new Exception("Attempt to load Core Zone style masks without StyleMaskHelper.");

                StyleMaskHelperCompat.CreateCoreZoneMasks(this, addColorMask, addStyleMask);
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            foreach (var listener in Scene.Tracker.GetComponents<CoreModeListener>()) {
                if (CollidePoint(listener.Entity.Center)) {
                    ContainEntity(listener.Entity);
                }
                if (listener.Entity is BounceBlock bounceBlock) {
                    if ((int)DynamicData.For(bounceBlock).Get("state") == 0) { // BounceBlock.States.Waiting
                        EeveeUtils.m_BounceBlockCheckModeChange.Invoke(bounceBlock, new object[] { });
                    }
                }
            }
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);

            var player = scene.Tracker.GetEntity<Player>();
            
            if (player != null) {
                var playerData = DynamicData.For(player);

                if (playerData.Get<CoreZone>("coreZoneContainer") == this) {
                    playerData.Set("coreZoneContainer", null);
                }
            }
        }

        public override void Update() {
            base.Update();

            playerInside = false;
            var player = Scene.Tracker.GetEntity<Player>();
            if (player != null) {
                var playerData = DynamicData.For(player);
                var coreZone = playerData.Get<CoreZone>("coreZoneContainer");
                playerInside = coreZone == this && CollidePoint(player.Center);
                if (coreZone == this && !playerInside) {
                    var newZone = GetCoreZoneAt(player.Center);
                    if (newZone != null) {
                        playerData.Set("coreZoneContainer", newZone);
                        UpdateMusic(newZone.CurrentMode);
                    } else {
                        playerData.Set("coreZoneContainer", null);
                        UpdateMusic(SceneAs<Level>().CoreMode);
                    }
                } else if (coreZone == null && CollidePoint(player.Center)) {
                    playerData.Set("coreZoneContainer", this);
                    UpdateMusic(CurrentMode);
                    playerInside = true;
                }
            }

            var coreMode = GetCoreMode();
            if (coreMode != CurrentMode) {
                CurrentMode = coreMode;
                foreach (var entity in Contained) {
                    var data = DynamicData.For(entity);
                    var action = data.Get<Action<Session.CoreModes>>("coreZoneOnChange");
                    action?.Invoke(coreMode);
                }
                if (addColorMask || addStyleMask)
                    StyleMaskHelperCompat.UpdateCoreZoneMasks(this);
                if (playerInside)
                    UpdateMusic(CurrentMode);
            }

            foreach (var listener in Scene.Tracker.GetComponents<CoreModeListener>()) {
                var entity = listener.Entity;
                if (Contained.Contains(entity)) {
                    if (!CollidePoint(entity.Center))
                        RemoveEntity(entity);
                } else if (CollidePoint(entity.Center)) {
                    ContainEntity(entity);
                }
            }
        }

        private void ContainEntity(Entity entity) {
            var data = DynamicData.For(entity);
            var container = data.Get<CoreZone>("coreZoneContainer");
            if (container != null)
                return;
            var listener = entity.Get<CoreModeListener>();
            data.Set("coreZoneOnChange", listener.OnChange);
            listener.OnChange?.Invoke(CurrentMode);
            listener.OnChange = mode => { };
            data.Set("coreZoneContainer", this);
            Contained.Add(entity);
        }

        private void RemoveEntity(Entity entity) {
            var data = DynamicData.For(entity);
            var listener = entity.Get<CoreModeListener>();
            listener.OnChange = data.Get<Action<Session.CoreModes>>("coreZoneOnChange");
            data.Set("coreZoneOnChange", null);
            data.Set("coreZoneContainer", null);
            Contained.Remove(entity);

            var newZone = GetCoreZoneAt(entity.Center);
            if (newZone != null) {
                newZone.ContainEntity(entity);
            } else {
                listener.OnChange?.Invoke(SceneAs<Level>().CoreMode);
            }
        }

        private CoreZone GetCoreZoneAt(Vector2 point)
            => Scene.CollideFirst<CoreZone>(point);

        private void UpdateMusic(Session.CoreModes coreMode) {
            Audio.SetParameter(Audio.CurrentAmbienceEventInstance, "room_state", (coreMode == Session.CoreModes.Hot) ? 0 : 1);
            if (Audio.CurrentMusic == "event:/music/lvl9/main") {
                var level = SceneAs<Level>();
                level.Session.Audio.Music.Layer(1, coreMode == Session.CoreModes.Hot);
                level.Session.Audio.Music.Layer(2, coreMode == Session.CoreModes.Cold);
                level.Session.Audio.Apply(false);
            }
        }

        public string GetColorGrade() {
            switch (CurrentMode) {
                case Session.CoreModes.Cold:
                    return "cold";
                case Session.CoreModes.Hot:
                    return "hot";
                default:
                    return "(current)";
            }
        }

        public string[] GetRenderTags() {
            switch (CurrentMode) {
                case Session.CoreModes.Cold:
                    return coldTags;
                case Session.CoreModes.Hot:
                    return hotTags;
                default:
                    return noTags;
            }
        }

        private Session.CoreModes GetCoreMode(Session.CoreModes? levelCoreMode = null) {
            var levelMode = string.IsNullOrEmpty(ZoneId) ? (levelCoreMode ?? SceneAs<Level>().CoreMode) : GetZoneMode(ZoneId);
            switch (zoneMode) {
                case Mode.Cold:
                    return Session.CoreModes.Cold;
                case Mode.Hot:
                    return Session.CoreModes.Hot;
                case Mode.None:
                    return Session.CoreModes.None;
                case Mode.Current:
                    return levelMode;
                case Mode.Inverted:
                    switch (levelMode) {
                        case Session.CoreModes.Cold:
                            return Session.CoreModes.Hot;
                        case Session.CoreModes.Hot:
                            return Session.CoreModes.Cold;
                        default:
                            return Session.CoreModes.None;
                    }
            }
            return Session.CoreModes.None;
        }


        public static Session.CoreModes GetZoneMode(string id)
            => ZoneGroups.ContainsKey(id) ? ZoneGroups[id] : Session.CoreModes.Hot;

        public static void SetZoneMode(string id, Session.CoreModes mode, bool persistent = false) {
            ZoneGroups[id] = mode;
            if (persistent)
                EeveeHelperModule.Session.CoreZones[id] = mode;
        }


        public static void Load() {
            IL.Celeste.Player.NormalUpdate += Player_NormalUpdate;
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            On.Celeste.Level.TransitionTo += Level_TransitionTo;
        }

        public static void Unload() {
            IL.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            On.Celeste.Level.TransitionTo -= Level_TransitionTo;
        }

        private static void Player_NormalUpdate(ILContext il) {
            var cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallOrCallvirt<Level>("get_CoreMode"))) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Session.CoreModes, Player, Session.CoreModes>>((coreMode, player) => {
                    var coreZone = DynamicData.For(player).Get<CoreZone>("coreZoneContainer");
                    return coreZone != null ? coreZone.CurrentMode : coreMode;
                });
            }
        }

        private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            ZoneGroups = new Dictionary<string, Session.CoreModes>(EeveeHelperModule.Session.CoreZones);
            orig(self, playerIntro, isFromLoader);
        }

        private static void Level_TransitionTo(On.Celeste.Level.orig_TransitionTo orig, Level self, LevelData next, Vector2 direction) {
            EeveeHelperModule.Session.CoreZones = new Dictionary<string, Session.CoreModes>(ZoneGroups);
            orig(self, next, direction);
        }
    }
}
