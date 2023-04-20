﻿using Celeste.Mod.EeveeHelper.Compat;
using Celeste.Mod.EeveeHelper.Components;
using Celeste.Mod.EeveeHelper.Handlers;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.EeveeHelper.Entities.Modifiers {
    [CustomEntity("EeveeHelper/FlagToggleModifier")]
    public class FlagToggleModifier : Entity, IContainer {
        public bool Toggled => string.IsNullOrEmpty(flag) ? !notFlag : SceneAs<Level>().Session.GetFlag(flag) != notFlag;

        private string flag;
        private bool notFlag;

        private bool toggleActive;
        private bool toggleVisible;
        private bool toggleCollidable;

        public EntityContainer Container { get; set; }
        private Dictionary<IEntityHandler, object> entityStates = new Dictionary<IEntityHandler, object>();

        public FlagToggleModifier(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Collider = new Hitbox(data.Width, data.Height);
            Depth = Depths.Top - 9;

            var parsedFlag = EeveeUtils.ParseFlagAttr(data.Attr("flag"));
            flag = parsedFlag.Item1;
            notFlag = parsedFlag.Item2;

            if (data.Bool("notFlag"))
                notFlag = !notFlag;

            toggleActive = data.Bool("toggleActive", true);
            toggleVisible = data.Bool("toggleVisible", true);
            toggleCollidable = data.Bool("toggleCollidable", true);

            Add(Container = new EntityContainer(data) {
                DefaultIgnored = e => e.Get<EntityContainer>() != null,
                OnDetach = EnableEntity
            });

            Add(new TransitionListener {
                OnIn = (f) => CheckToggled()
            });
        }

        public override void Update() {
            base.Update();
            CheckToggled();
        }

        private void CheckToggled() {
            if (Toggled)
                EnableEntities();
            else
                DisableEntities();
        }

        private void DisableEntities() {
            foreach (var handler in Container.Contained) {
                if (!entityStates.ContainsKey(handler)) {
                    var state = HandlerUtils.GetAs<IToggleable, object>(handler, t => t.SaveState(), e => new EntityState(e));
                    if (state != null) {
                        entityStates.Add(handler, state);
                    }
                }
                HandlerUtils.DoAs<IToggleable>(handler,
                    t => t.Disable(toggleActive, toggleVisible, toggleCollidable),
                    e => EntityState.Disable(e, toggleActive, toggleVisible, toggleCollidable));
            }
        }

        private void EnableEntities() {
            foreach (var handler in Container.Contained)
                EnableEntity(handler);

            entityStates.Clear();
        }

        private void EnableEntity(IEntityHandler handler) {
            if (entityStates.ContainsKey(handler)) {
                var state = entityStates[handler];
                HandlerUtils.DoAs<IToggleable>(handler,
                    t => t.ReadState(state, toggleActive, toggleVisible, toggleCollidable),
                    e => ((EntityState)state).Apply(e, toggleActive, toggleVisible, toggleCollidable));
            }
        }

        public struct EntityState {
            bool Active;
            bool Visible;
            bool Collidable;

            bool TalkComponentEnabled;

            public EntityState(Entity entity) {
                Active = entity.Active;
                Visible = entity.Visible;
                Collidable = entity.Collidable;

                var talkComponent = entity.Get<TalkComponent>();
                if (talkComponent != null)
                    TalkComponentEnabled = talkComponent.Enabled;
                else
                    TalkComponentEnabled = false;
            }

            public void Apply(Entity entity, bool toggleActive, bool toggleVisible, bool toggleCollidable) {
                if (toggleActive)  entity.Active = Active;
                if (toggleVisible) entity.Visible = Visible;

                if (toggleCollidable) {
                    entity.Collidable = Collidable;

                    var talkComponent = entity.Get<TalkComponent>();
                    if (talkComponent != null)
                        talkComponent.Enabled = TalkComponentEnabled;
                }
            }

            public static void Disable(Entity entity, bool toggleActive, bool toggleVisible, bool toggleCollidable) {
                if (toggleActive)  entity.Active = false;
                if (toggleVisible) entity.Visible = false;

                if (toggleCollidable) {
                    entity.Collidable = false;

                    var talkComponent = entity.Get<TalkComponent>();
                    if (talkComponent != null)
                        talkComponent.Enabled = false;
                }
            }
        }
    }
}
