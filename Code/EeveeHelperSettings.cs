using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.EeveeHelper;

public class EeveeHelperSettings : EverestModuleSettings
{
	[DefaultButtonBinding(Buttons.LeftTrigger | Buttons.RightTrigger, Keys.LeftShift)]
	public ButtonBinding ActivateCustomInputBlock { get; set; }
}
