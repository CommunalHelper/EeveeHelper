using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.EeveeHelper.Handlers {
    public interface IToggleable {
        object GetDefaultState();

        object SaveState();

        void ReadState(object state, bool toggleActive, bool toggleVisible, bool toggleCollidable);

        void Disable(bool toggleActive, bool toggleVisible, bool toggleCollidable);
    }
}
