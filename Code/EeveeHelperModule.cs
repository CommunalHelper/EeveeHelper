using Celeste.Mod.EeveeHelper.Compat;
using Celeste.Mod.EeveeHelper.Components;
using Celeste.Mod.EeveeHelper.Effects;
using Celeste.Mod.EeveeHelper.Entities;
using Celeste.Mod.EeveeHelper.Handlers;
using Celeste.Mod.EeveeHelper.Handlers.Impl;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.EeveeHelper {
    public class EeveeHelperModule : EverestModule {
        public static EeveeHelperModule Instance { get; set; }

        public EeveeHelperModule() {
            Instance = this;
        }

        public override Type SessionType => typeof(EeveeHelperSession);
        public static EeveeHelperSession Session => (EeveeHelperSession)Instance._Session;

        public static bool AdventureHelperLoaded { get; set; }
        public static bool StyleMaskHelperLoaded { get; set; }

        public override void Load() {
            MiscHooks.Load();
            RoomChest.Load();
            HoldableTiles.Load();
            PatientBooster.Load();
            CoreZone.Load();

            Everest.Events.Level.OnLoadBackdrop += this.OnLoadBackdrop;

            EntityHandler.RegisterInherited<Water>((entity, container) => new WaterHandler(entity));
            EntityHandler.RegisterInherited<TrackSpinner>((entity, container) => new TrackSpinnerHandler(entity));
            EntityHandler.RegisterInherited<RotateSpinner>((entity, container) => new RotateSpinnerHandler(entity));

            EntityHandler.RegisterInherited<DashSwitch>((entity, container) => new AxisMoverHandler(entity, new Tuple<string, bool>("startY", true)));

            EntityHandler.RegisterInherited<ZipMover>((entity, container) => new ZipMoverNodeHandler(entity, true),
                (entity, container) => ZipMoverNodeHandler.InsideCheck(container, true, DynamicData.For(entity as ZipMover)));
            EntityHandler.RegisterInherited<ZipMover>((entity, container) => new ZipMoverNodeHandler(entity, false),
                (entity, container) => ZipMoverNodeHandler.InsideCheck(container, false, DynamicData.For(entity as ZipMover)));
            EntityHandler.RegisterInherited<SwapBlock>((entity, container) => new SwapBlockHandler(entity, true),
                (entity, container) => SwapBlockHandler.InsideCheck(container, true, DynamicData.For(entity as SwapBlock)));
            EntityHandler.RegisterInherited<SwapBlock>((entity, container) => new SwapBlockHandler(entity, false),
                (entity, container) => SwapBlockHandler.InsideCheck(container, false, DynamicData.For(entity as SwapBlock)));
            EntityHandler.RegisterInherited<Decal>((entity, container) => new DecalHandler(entity),
                (entity, container) => container.CheckDecal(entity as Decal));
        }

        public override void Unload() {
            MiscHooks.Unload();
            RoomChest.Unload();
            HoldableTiles.Unload();
            PatientBooster.Unload();
            CoreZone.Unload();
        }

        public override void Initialize() {
            AdventureHelperLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata {
                Name = "AdventureHelper",
                VersionString = "1.5.1"
            });
            StyleMaskHelperLoaded = Everest.Loader.DependencyLoaded(new EverestModuleMetadata {
                Name = "StyleMaskHelper",
                VersionString = "1.2.0"
            });

            if (AdventureHelperLoaded) {
                AdventureHelperCompat.Initialize();
            }
        }

        private Backdrop OnLoadBackdrop(MapData map, BinaryPacker.Element child, BinaryPacker.Element above) {
            if (child.Name.Equals("EeveeHelper/SeededStarfield", StringComparison.OrdinalIgnoreCase))
                return new SeededStarfield(Calc.HexToColor(child.Attr("color")), child.AttrFloat("speed", 1f), child.AttrInt("seed"));
            return null;
        }

        public static Dictionary<string, Ease.Easer> EaseTypes = new Dictionary<string, Ease.Easer> {
            { "Linear", Ease.Linear },
            { "SineIn", Ease.SineIn },
            { "SineOut", Ease.SineOut },
            { "SineInOut", Ease.SineInOut },
            { "QuadIn", Ease.QuadIn },
            { "QuadOut", Ease.QuadOut },
            { "QuadInOut", Ease.QuadInOut },
            { "CubeIn", Ease.CubeIn },
            { "CubeOut", Ease.CubeOut },
            { "CubeInOut", Ease.CubeInOut },
            { "QuintIn", Ease.QuintIn },
            { "QuintOut", Ease.QuintOut },
            { "QuintInOut", Ease.QuintInOut },
            { "BackIn", Ease.BackIn },
            { "BackOut", Ease.BackOut },
            { "BackInOut", Ease.BackInOut },
            { "ExpoIn", Ease.ExpoIn },
            { "ExpoOut", Ease.ExpoOut },
            { "ExpoInOut", Ease.ExpoInOut },
            { "BigBackIn", Ease.BigBackIn },
            { "BigBackOut", Ease.BigBackOut },
            { "BigBackInOut", Ease.BigBackInOut },
            { "ElasticIn", Ease.ElasticIn },
            { "ElasticOut", Ease.ElasticOut },
            { "ElasticInOut", Ease.ElasticInOut },
            { "BounceIn", Ease.BounceIn },
            { "BounceOut", Ease.BounceOut },
            { "BounceInOut", Ease.BounceInOut }
        };
    }
}
