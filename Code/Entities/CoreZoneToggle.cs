using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;

namespace Celeste.Mod.EeveeHelper.Entities {
    [CustomEntity("EeveeHelper/CoreZoneToggle")]
    public class CoreZoneToggle : Entity {
        private const float Cooldown = 1f;

        private string zoneId;
        private bool onlyFire;
        private bool onlyIce;
        private bool persistent;

        private bool iceMode;
        private float cooldownTimer;
        private bool playSounds;
        private Sprite sprite;
        private CoreZone coreZone;

        private bool Usable {
            get {
                if (!onlyFire || iceMode) {
                    if (onlyIce) {
                        return !iceMode;
                    }
                    return true;
                }
                return false;
            }
        }

        public CoreZoneToggle(Vector2 position, bool onlyFire, bool onlyIce, bool persistent)
            : base(position) {
            this.onlyFire = onlyFire;
            this.onlyIce = onlyIce;
            this.persistent = persistent;
            Collider = new Hitbox(16f, 24f, -8f, -12f);
            Add(new PlayerCollider(OnPlayer));
            Add(sprite = GFX.SpriteBank.Create("coreFlipSwitch"));
            Depth = 2000;
        }

        public CoreZoneToggle(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Bool("onlyFire"), data.Bool("onlyIce"), data.Bool("persistent")) {

            zoneId = data.Attr("zoneId");
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            iceMode = CoreZone.GetZoneMode(zoneId) == Session.CoreModes.Cold;
            SetSprite(animate: false);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            coreZone = scene.Tracker.GetEntities<CoreZone>().FirstOrDefault(e => (e as CoreZone).ZoneId == zoneId && e.CollidePoint(Center)) as CoreZone;
            if (coreZone != null) {
                iceMode = coreZone.CurrentMode == Session.CoreModes.Cold;
                SetSprite(animate: false);
            }
        }

        private void SetSprite(bool animate) {
            if (animate) {
                if (playSounds) {
                    Audio.Play(iceMode ? "event:/game/09_core/switch_to_cold" : "event:/game/09_core/switch_to_hot", Position);
                }
                if (Usable) {
                    sprite.Play(iceMode ? "ice" : "hot");
                } else {
                    if (playSounds) {
                        Audio.Play("event:/game/09_core/switch_dies", Position);
                    }
                    sprite.Play(iceMode ? "iceOff" : "hotOff");
                }
            } else if (Usable) {
                sprite.Play(iceMode ? "iceLoop" : "hotLoop");
            } else {
                sprite.Play(iceMode ? "iceOffLoop" : "hotOffLoop");
            }
            playSounds = false;
        }

        private void OnPlayer(Player player) {
            if (Usable && cooldownTimer <= 0f) {
                playSounds = true;
                var level = SceneAs<Level>();
                if (CoreZone.GetZoneMode(zoneId) == Session.CoreModes.Cold) {
                    CoreZone.SetZoneMode(zoneId, Session.CoreModes.Hot, persistent);
                } else {
                    CoreZone.SetZoneMode(zoneId, Session.CoreModes.Cold, persistent);
                }
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                level.Flash(Color.White * 0.15f, drawPlayerOver: true);
                Celeste.Freeze(0.05f);
                cooldownTimer = 1f;
            }
        }

        public override void Update() {
            base.Update();

            if (cooldownTimer > 0f) {
                cooldownTimer -= Engine.DeltaTime;
            }

            var newIceMode = (coreZone != null ? coreZone.CurrentMode : CoreZone.GetZoneMode(zoneId)) == Session.CoreModes.Cold;
            if (newIceMode != iceMode) {
                iceMode = newIceMode;
                SetSprite(animate: true);
            }
        }
    }
}
