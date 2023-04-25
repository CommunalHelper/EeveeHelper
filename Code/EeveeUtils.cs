using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.EeveeHelper {
    public static class EeveeUtils {
        internal static MethodInfo m_SpringBounceAnimate = typeof(Spring).GetMethod("BounceAnimate", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static MethodInfo m_BounceBlockCheckModeChange = typeof(BounceBlock).GetMethod("CheckModeChange", BindingFlags.NonPublic | BindingFlags.Instance);

        public static Vector2 GetPosition(Entity entity) =>
            entity is Platform platform ? platform.ExactPosition : entity.Position;

        public static Vector2 GetTrackBoost(Vector2 move, bool disableBoost) {
            return move * new Vector2(disableBoost ? 0f : 1f, 1f) + (move.X != 0f && move.Y == 0f && disableBoost ? Vector2.UnitY * 0.01f : Vector2.Zero);
        }

        public static Tuple<string, bool> ParseFlagAttr(string flag)
            => flag.StartsWith("!") ? Tuple.Create(flag.Substring(1), true) : Tuple.Create(flag, false);

        public static void ParseFlagAttr(string attr, out string flag, out bool notFlag) {
            var parsed = ParseFlagAttr(attr);
            flag = parsed.Item1;
            notFlag = parsed.Item2;
        }

        public static EntityData CloneEntityData(EntityData data, LevelData levelData = null) {
            var newData = new EntityData();
            newData.Name = data.Name;
            newData.Level = levelData ?? data.Level;
            newData.ID = data.ID;
            newData.Position = data.Position + data.Level.Position - newData.Level.Position;
            newData.Width = data.Width;
            newData.Height = data.Height;
            newData.Origin = data.Origin;
            newData.Nodes = (Vector2[])data.Nodes.Clone();
            if (data.Values == null)
                newData.Values = new Dictionary<string, object>();
            else
                newData.Values = new Dictionary<string, object>(data.Values);
            return newData;
        }
    }
}
