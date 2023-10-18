using Celeste.Mod.EeveeHelper.Components;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.EeveeHelper.Handlers.Impl {
    internal class DecalHandler : EntityHandler, IMoveable {

        public DecalHandler(Entity e) : base(e) {

        }

        public override bool IsInside(EntityContainer container) => container.CheckDecal(Entity as Decal);

        public bool Move(Vector2 move, Vector2? liftSpeed) {
            Entity.Position += move;
            return true;
        }
        public void PreMove() { }
    }
}
