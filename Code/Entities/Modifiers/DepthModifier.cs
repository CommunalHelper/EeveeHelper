using Celeste.Mod.EeveeHelper.Components;
using Celeste.Mod.EeveeHelper.Handlers;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.EeveeHelper.Entities.Modifiers {
    [CustomEntity("EeveeHelper/DepthModifier")]
    public class DepthModifier : Entity, IContainer {
        public int TargetDepth;

        public EntityContainer Container { get; set; }
        private Dictionary<Entity, int> lastDepths = new Dictionary<Entity, int>();

        public DepthModifier(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Collider = new Hitbox(data.Width, data.Height);
            Depth = Depths.Top - 9;

            TargetDepth = data.Int("depth");

            Add(Container = new EntityContainer(data) {
                DefaultIgnored = e => e.Get<EntityContainer>() != null,
                OnAttach = ModifyDepth,
                OnDetach = RestoreDepth
            });
        }

        private void ModifyDepth(IEntityHandler handler) {
            var entity = handler.Entity;

            if (!lastDepths.ContainsKey(entity))
                lastDepths.Add(entity, entity.Depth);

            entity.Depth = TargetDepth;
        }

        private void RestoreDepth(IEntityHandler handler) {
            var entity = handler.Entity;

            if (!lastDepths.TryGetValue(entity, out var depth))
                return;

            entity.Depth = depth;

            lastDepths.Remove(entity);
        }
    }
}
