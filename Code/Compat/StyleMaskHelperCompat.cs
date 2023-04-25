using Celeste.Mod.EeveeHelper.Entities;
using Celeste.Mod.StyleMaskHelper.Entities;
using System.Collections.ObjectModel;

namespace Celeste.Mod.EeveeHelper.Compat {
    public class StyleMaskHelperCompat {

        public static void CreateCoreZoneMasks(CoreZone coreZone, bool addColorGrade, bool addStyleground) {
            if (addColorGrade) {
                coreZone.Scene.Add(coreZone.ColorGradeMask = new ColorGradeMask(coreZone.Position, coreZone.Width, coreZone.Height) {
                    ColorGradeTo = coreZone.GetColorGrade()
                });
            }
            if (addStyleground) {
                coreZone.Scene.Add(coreZone.StylegroundMask = new StylegroundMask(coreZone.Position, coreZone.Width, coreZone.Height, coreZone.GetRenderTags()));
            }
        }
        
        public static void UpdateCoreZoneMasks(CoreZone coreZone) {
            if (coreZone.ColorGradeMask is ColorGradeMask colorGradeMask) {
                colorGradeMask.ColorGradeTo = coreZone.GetColorGrade();
            }
            if (coreZone.StylegroundMask is StylegroundMask stylegroundMask) {
                stylegroundMask.RenderTags = new ObservableCollection<string>(coreZone.GetRenderTags());
            }
        }
    }
}
