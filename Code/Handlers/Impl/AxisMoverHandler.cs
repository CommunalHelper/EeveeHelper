using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.EeveeHelper.Handlers.Impl {
    internal class AxisMoverHandler : EntityHandler, IMoveable {
        private DynamicData entityData;
        private Tuple<string, bool>[] axesHandler;

        public AxisMoverHandler(Entity entity, params Tuple<string, bool>[] singleAxisHandler) : base(entity) {
            entityData = new DynamicData(entity);
            axesHandler = singleAxisHandler;
        }

        public bool Move(Vector2 move, Vector2? liftSpeed) {
            if (Entity is Platform p) {
                p.MoveH(move.X);
                p.MoveV(move.Y);
            } else {
                Entity.Position += move;
            }
            foreach (Tuple<string, bool> pair in axesHandler)
                entityData.Set(pair.Item1, (float)(entityData.Get<float>("startY") + (pair.Item2 ? move.Y : move.X)));
            return true;
        }
        public void PreMove() { }
    }
}
