using Celeste.Mod.EeveeHelper.Entities;
using MonoMod.ModInterop;
using System;

namespace Celeste.Mod.EeveeHelper.Compat {
    public class SpeedrunToolCompat {

        public static void Initialize() {
            typeof(SaveLoadImports).ModInterop();

            SaveLoadImports.RegisterStaticTypes(typeof(RoomChest), new string[] { "LastEntities", "LastChests", "LastRooms", "LastSpawnPoints", "OriginalSession", "OriginalModSessions" });
        }

        [ModImportName("SpeedrunTool.SaveLoad")]
        private static class SaveLoadImports {
            public static Func<Type, string[], object> RegisterStaticTypes;
        }
    }
}
