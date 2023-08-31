using Celeste.Mod.EeveeHelper.Components;
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
    [CustomEntity("EeveeHelper/CollidableModifier")]
    public class CollidableModifier : Entity, IContainer {
        private Mode mode;
        private bool keepBaseCollision;

        public EntityContainer Container { get; set; }

        private Dictionary<Entity, bool> wasCollidable = new Dictionary<Entity, bool>();
        private Dictionary<Entity, Solid> solids = new Dictionary<Entity, Solid>();
        private Dictionary<Entity, Hazard> hazards = new Dictionary<Entity, Hazard>();
        private Dictionary<Entity, Component> hazardComponents = new Dictionary<Entity, Component>();

        public CollidableModifier(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Collider = new Hitbox(data.Width, data.Height);

            if (data.Has("collisionMode")) {
                mode = data.Enum("collisionMode", Mode.NoCollide);
            } else {
                mode = data.Bool("solidify") ? Mode.Solid : Mode.NoCollide;
            }
            keepBaseCollision = data.Bool("keepBaseCollision");

            Add(Container = new EntityContainer(data) {
                IsValid = e => !(e is Solidifier solidifier && Container.GetEntities().Contains(solidifier.Entity)) && !(e is Hazard),
                DefaultIgnored = e => e.Get<EntityContainer>() != null,
                OnAttach = h => OnAttach(h.Entity),
                OnDetach = h => OnDetach(h.Entity)
            });
        }

        private void OnAttach(Entity entity) {
            if (mode == Mode.Solid && !solids.ContainsKey(entity) && entity.Collider != null && (!keepBaseCollision || !(entity is Solid))) {
                var solid = new Solidifier(entity);
                DynamicData.For(entity).Set("solidModifierSolidifier", solid);
                solids.Add(entity, solid);
                Scene.Add(solid);
            }
            if (mode == Mode.Hazardous && entity.Collider != null) {
                if (keepBaseCollision && !hazardComponents.ContainsKey(entity)) {
                    var hazardComponent = new PlayerCollider(player => player.Die((player.Center - entity.Center).SafeNormalize()));
                    hazardComponents.Add(entity, hazardComponent);
                    entity.Add(hazardComponent);
                } else if (!keepBaseCollision && !hazards.ContainsKey(entity)) {
                    var hazard = new Hazard(entity);
                    DynamicData.For(entity).Set("collidableModifierHazard", hazard);
                    hazards.Add(entity, hazard);
                    Scene.Add(hazard);
                }
            }
            if (mode == Mode.NoCollide || !keepBaseCollision) {
                if (!wasCollidable.ContainsKey(entity))
                    wasCollidable.Add(entity, entity.Collidable);
                entity.Collidable = false;
            }
        }

        private void OnDetach(Entity entity) {
            if (mode == Mode.Solid && solids.ContainsKey(entity)) {
                var solid = solids[entity];
                DynamicData.For(entity).Set("solidModifierSolidifier", null);
                solids.Remove(solid);
                if (solid.Scene != null)
                    solid.RemoveSelf();
            }
            if (mode == Mode.Hazardous) {
                if (hazardComponents.ContainsKey(entity)) {
                    entity.Remove(hazardComponents[entity]);
                    hazardComponents.Remove(entity);
                } else if (hazards.ContainsKey(entity)) {
                    var hazard = hazards[entity];
                    DynamicData.For(entity).Set("collidableModifierHazard", null);
                    hazards.Remove(entity);
                    if (hazard.Scene != null)
                        hazard.RemoveSelf();
                }
            }
            if (mode == Mode.NoCollide || !keepBaseCollision) {
                if (wasCollidable.ContainsKey(entity))
                    entity.Collidable = wasCollidable[entity];
                else
                    entity.Collidable = true;
            }
        }

        public override void Update() {
            base.Update();

            if (mode == Mode.NoCollide || !keepBaseCollision) {
                foreach (var entity in Container.GetEntities())
                    entity.Collidable = false;
            }
        }


        public enum Mode {
            NoCollide,
            Solid,
            Hazardous
        }

        public sealed class Solidifier : Solid {
            public Entity Entity;

            public Solidifier(Entity entity) : base(EeveeUtils.GetPosition(entity), 1f, 1f, false) {
                Collider = entity.Collider.Clone();
                Depth = entity.Depth + 1;
                Entity = entity;
            }

            public override void Update() {
                base.Update();

                if (Entity == null || Entity.Scene == null) {
                    RemoveSelf();
                    return;
                }

                Depth = Entity.Depth + 1;

                if (Entity.Collider.Size != Collider.Size)
                    Collider = Entity.Collider.Clone();

                if (ExactPosition != EeveeUtils.GetPosition(Entity))
                    MoveTo(EeveeUtils.GetPosition(Entity));
            }

            public override void MoveHExact(int move) {
                var collidable = Entity.Collidable;
                Entity.Collidable = false;
                var pushable = true;
                var actor = Entity as Actor;
                if (actor != null) {
                    pushable = actor.AllowPushing;
                    actor.AllowPushing = false;
                }
                base.MoveHExact(move);
                Entity.Collidable = collidable;
                if (actor != null)
                    actor.AllowPushing = pushable;
            }

            public override void MoveVExact(int move) {
                var collidable = Entity.Collidable;
                Entity.Collidable = false;
                var pushable = true;
                var actor = Entity as Actor;
                if (actor != null) {
                    pushable = actor.AllowPushing;
                    actor.AllowPushing = false;
                }
                base.MoveVExact(move);
                Entity.Collidable = collidable;
                if (actor != null)
                    actor.AllowPushing = pushable;
            }
        }

        public class Hazard : Entity {
            public Entity Entity;

            public Hazard(Entity entity) : base(entity.Position) {
                Collider = entity.Collider.Clone();
                Depth = entity.Depth + 1;
                Entity = entity;

                Add(new PlayerCollider(OnPlayer));
            }

            public override void Update() {
                base.Update();

                if (Entity == null || Entity.Scene == null) {
                    RemoveSelf();
                    return;
                }

                Depth = Entity.Depth + 1;

                if (Entity.Collider.Size != Collider.Size)
                    Collider = Entity.Collider.Clone();

                Position = Entity.Position;
            }

            private void OnPlayer(Player player) {
                player.Die((player.Center - Center).SafeNormalize());
            }
        }
    }
}
