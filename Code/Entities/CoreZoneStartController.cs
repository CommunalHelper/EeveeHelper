using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.EeveeHelper.Entities {
    [CustomEntity("EeveeHelper/CoreZoneStartController")]
    public class CoreZoneStartController : Entity {
        private string zoneId;
        private Session.CoreModes coreMode;
        private bool startOnly;
        private bool persistent;

        public CoreZoneStartController(EntityData data, Vector2 offset) : base() {
            zoneId = data.Attr("zoneId");
            coreMode = data.Enum("coreMode", Session.CoreModes.None);
            startOnly = data.Bool("startOnly");
            persistent = data.Bool("persistent");

            if (startOnly && CoreZone.ZoneGroups.ContainsKey(zoneId))
                return;

            CoreZone.SetZoneMode(zoneId, coreMode, persistent);
        }
    }
}
