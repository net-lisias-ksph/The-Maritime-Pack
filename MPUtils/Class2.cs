using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPUtils
{
    namespace MPUtils
    {
        public class MPEngine : PartModule
        {
            public override void OnStart(StartState state)
            {
                MPLog.Writelog("[Maritime Pack] MPEngine Found on " + vessel.name);
            }

            public void FixedUpdate()
            {
                if (!HighLogic.LoadedSceneIsFlight)
                {
                    return;
                }

                MPLog.Writelog("[Maritime Pack] MPEngine submerged portion: " + part.submergedPortion);
                if (part.isActiveAndEnabled && part.submergedPortion < 80.0f)
                {
                    part.deactivate();
                }
            }
        }
    }
}
