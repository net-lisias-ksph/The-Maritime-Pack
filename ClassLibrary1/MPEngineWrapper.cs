/*******************************************************************************************************************
* This code has been adapted from an original work by snjo under the following license: 
*
* License: You may reuse code and textures from this mod, as long as you give credit 
* in the download file and on the download post/page. Reuse of models with permission.
* No reselling. No redistribution of the whole pack without permission. UV map texture 
* guides are included so you can re-skin to your liking.
*
* For reuse of the plugin, please either direct people to download the dll from my official
* release, OR recompile the wanted partmodule/class with a new class name to avoid conflicts.
*/

using System.Linq;
using UnityEngine;

namespace MaritimePack.engine
{
    class MPengineWrapper
    {
        public enum EngineType
        {
            NONE,
            ModuleEngine,
            ModuleEngineFX,
            FSengine,
        }
        public EngineType type = EngineType.NONE;
        public ModuleEngines engine;
        public ModuleEnginesFX engineFX;

        public MPengineWrapper(Part part)
        {
            engine = part.Modules.OfType<ModuleEngines>().FirstOrDefault();
            if (engine != null)
            {
                type = EngineType.ModuleEngine;
            }
            else
            {
                engineFX = part.Modules.OfType<ModuleEnginesFX>().FirstOrDefault();
                type = EngineType.ModuleEngineFX;
            }
            //Debug.Log("FSengineWrapper: engine type is " + type.ToString());
        }

        public MPengineWrapper(Part part, string name)
        {
            engineFX = part.Modules.OfType<ModuleEnginesFX>().Where(p => p.engineID == name).FirstOrDefault();
            if (engineFX != null)
                type = EngineType.ModuleEngineFX;
            //Debug.Log("FSengineWrapper: engine type is " + type.ToString());

        }

        public float maxThrust
        {
            get
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        return engine.maxThrust;
                    case EngineType.ModuleEngineFX:
                        return engineFX.maxThrust;
                    default:
                        return 0f;
                }
            }
            set
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        engine.maxThrust = value;
                        break;
                    case EngineType.ModuleEngineFX:
                        engineFX.maxThrust = value;
                        break;
                }
            }
        }

        public float minThrust
        {
            get
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        return engine.minThrust;
                    case EngineType.ModuleEngineFX:
                        return engineFX.minThrust;
                    //case EngineType.FSengine:
                    //    return fsengine.minThrust;
                    default:
                        return 0f;
                }
            }
            set
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        engine.minThrust = value;
                        break;
                    case EngineType.ModuleEngineFX:
                        engineFX.minThrust = value;
                        break;
                        //case EngineType.FSengine:
                        //    fsengine.minThrust = value;
                        //    break;
                }
            }
        }

        public bool EngineIgnited
        {
            get
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        return engine.EngineIgnited;
                    case EngineType.ModuleEngineFX:
                        return engineFX.EngineIgnited;
                    default:
                        return false;
                }
            }
            set
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        engine.EngineIgnited = value;
                        break;
                    case EngineType.ModuleEngineFX:
                        engineFX.EngineIgnited = value;
                        break;
                }
            }
        }

        public bool flameout
        {
            get
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        return engine.flameout;
                    case EngineType.ModuleEngineFX:
                        return engineFX.flameout;
                    default:
                        return false;
                }
            }
            set
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        engine.flameout = value;
                        break;
                    case EngineType.ModuleEngineFX:
                        engineFX.flameout = value;
                        break;
                }
            }
        }

        public bool getIgnitionState
        {
            get
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        return engine.getIgnitionState;
                    case EngineType.ModuleEngineFX:
                        return engineFX.getIgnitionState;
                    default:
                        return false;
                }
            }
        }

        public bool getFlameoutState
        {
            get
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        return engine.getFlameoutState;
                    case EngineType.ModuleEngineFX:
                        return engineFX.getFlameoutState;
                    default:
                        return false;
                }
            }
        }

        public float engineAccelerationSpeed
        {
            get
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        return engine.engineAccelerationSpeed;
                    case EngineType.ModuleEngineFX:
                        return engineFX.engineAccelerationSpeed;
                    default:
                        return 0f;
                }
            }
            set
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        engine.engineAccelerationSpeed = value;
                        break;
                    case EngineType.ModuleEngineFX:
                        engineFX.engineAccelerationSpeed = value;
                        break;
                }
            }
        }

        public float engineDecelerationSpeed
        {
            get
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        return engine.engineDecelerationSpeed;
                    case EngineType.ModuleEngineFX:
                        return engineFX.engineDecelerationSpeed;
                    default:
                        return 0f;
                }
            }
            set
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        engine.engineDecelerationSpeed = value;
                        break;
                    case EngineType.ModuleEngineFX:
                        engineFX.engineDecelerationSpeed = value;
                        break;
                }
            }
        }

        public float finalThrust
        {
            get
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        return engine.finalThrust;
                    case EngineType.ModuleEngineFX:
                        return engineFX.finalThrust;
                    default:
                        return 0f;
                }
            }
            set
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        engine.finalThrust = value;
                        break;
                    case EngineType.ModuleEngineFX:
                        engineFX.finalThrust = value;
                        break;
                }
            }
        }

        public float finalThrustNormalized
        {
            get
            {
                return finalThrust / maxThrust;
            }
        }

        public bool throttleLocked
        {
            get
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        return engine.throttleLocked;
                    case EngineType.ModuleEngineFX:
                        return engineFX.throttleLocked;
                    //case EngineType.FSengine:
                    //    return fsengine.throttleLocked;
                    default:
                        return false;
                }
            }
            set
            {
                switch (type)
                {
                    case EngineType.ModuleEngine:
                        engine.throttleLocked = value;
                        break;
                    case EngineType.ModuleEngineFX:
                        engineFX.throttleLocked = value;
                        break;
                        //case EngineType.FSengine:
                        //    fsengine.flameout = value;
                        //    break;
                }
            }
        }
    }
}