/*********************************************************************************************
* The Maritime Pack MPUtil plugin is copyright 2015 Fengist, all rights reserved.
* For more information please visit http://www.kerbaltopia.com
**********************************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MPUtils
{

    /********************************************************
    * Layered Animations
    * public domain originally by Starwaster
    *********************************************************/
    public class ModuleLayeredAnimator : ModuleAnimateGeneric
    {
        [KSPField]
        public int layer = 1;

        public ModuleLayeredAnimator()
        {
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            this.anim[this.animationName].layer = layer;
        }
    }

    public class MPanimEngine : PartModule
    {
        //** Thanks to JPLRepo for helping make this class more efficient
        [KSPField]
        public string animationName = "null";  //the name of the animation stored in the .cfg

        [KSPField]
        public string partType = "null";  //the part type that it's connected to (Engine / Intake)

        [KSPField]
        public bool syncThrottle = false;  //is the animation synced to the throttle position

        [KSPField]
        public bool smoothThrottle = false; //should smoothing be used when the throttle position changes rapidly

        public Animation aniEngine = null; //the animation
        private ModuleEngines myEngine = new ModuleEngines();  //the first or default engine found in the part
        private ModuleResourceIntake myIntake = new ModuleResourceIntake();
        public float lastThrottle = 0;  //tracks the last throttle position. Always 0 or 1 for non sync'd engines
        public bool faultFound = false;  //set to true if no engine or animation is found on the part
        public string oldClip = "null";

        public List<Animation> getAllAnimations = new List<Animation>();

        public void Play_Anim(string aname, float aspeed, float atime)
        {
            try
            {
                //ScreenMessages.PostScreenMessage(new ScreenMessage("Name " + aname + " speed "+aspeed+" time "+atime, 5f, ScreenMessageStyle.UPPER_RIGHT));
                Animation anim;
                Animation[] animators = this.part.FindModelAnimators(aname);
                if (animators.Length > 0)
                {
                    anim = animators[0];
                    anim.clip = anim.GetClip(animationName);
                    anim[aname].speed = aspeed;
                    anim[aname].normalizedTime = atime;
                    anim.Play(aname);
                }
            }
            catch (Exception ex)
            {
                Debug.Log("[Maritime Pack]Exception in Play_Anim");
                Debug.Log("[Maritime Pack]Err: " + ex);
            }
        }

        public override void OnStart(StartState state)
        {
            foreach (var anim in part.FindModelAnimators(animationName))
            {
                aniEngine = anim;
                if (aniEngine.GetClipCount() > 1)
                {
                    aniEngine.Stop(); //stopping any animations loaded by ModuleAnimateGeneric
                }
                print("[Maritime Pack] (aniEngine) Found animation: " + animationName);
            }
            if (partType == "Engine")
            {
                myEngine = this.part.Modules.OfType<ModuleEngines>().FirstOrDefault();
            }
            if (partType == "Intake")
            {
                myIntake = this.part.Modules.OfType<ModuleResourceIntake>().FirstOrDefault();
            }
        }

        public void FixedUpdate()
        {
            if (faultFound || FlightGlobals.ActiveVessel == null) { return; }
            if (myEngine == null && myIntake == null || animationName == "null")
            {
                if (aniEngine == null)
                {
                    print("[Maritime Pack] (aniEngine) Animation not found.");
                }
                else
                {
                    print("[Maritime Pack] (aniEngine) "+partType+" not found.");
                }
                faultFound = true;
            }
            else
            {
                if ((partType == "Engine" && !myEngine.EngineIgnited) || (partType == "Intake" && !myIntake.intakeEnabled)) //Intake or Engine shut down, reverse animation
                {
                     if (lastThrottle != 0.0f)
                    {
                        if (syncThrottle)
                        {
                            if (smoothThrottle)
                            {
                                lastThrottle = lastThrottle - 0.02f;
                                if (lastThrottle < 0.0f)
                                {
                                    lastThrottle = 0.0f;
                                }
                            }
                            else
                            {
                                lastThrottle = 0.0f;
                            }
                            Play_Anim(animationName, 0.0f, lastThrottle);
                        }
                        else //not synced to play at full speed from the end to the beginning
                        {
                            if (!aniEngine.isPlaying)
                            {
                                Play_Anim(animationName, -1.0f, 1.0f);
                                lastThrottle = 0.0f;
                            }
                        }
                    }
                }
                else //engine ignited, run animation
                {
                    if (syncThrottle)
                    {
                        if (smoothThrottle)
                        {
                            if (FlightInputHandler.state.mainThrottle > lastThrottle)
                            {
                                if (FlightInputHandler.state.mainThrottle + lastThrottle > 0.02f)
                                {
                                    lastThrottle = lastThrottle + 0.02f;
                                    if (lastThrottle > 1.0f)
                                    {
                                        lastThrottle = 1.0f;
                                    }
                                }
                                else
                                {
                                    lastThrottle = FlightInputHandler.state.mainThrottle;
                                }
                            }
                            if (FlightInputHandler.state.mainThrottle < lastThrottle)
                            {
                                if (lastThrottle - FlightInputHandler.state.mainThrottle > 0.02f || FlightInputHandler.state.mainThrottle == 0 && lastThrottle != 0)
                                {
                                    lastThrottle = lastThrottle - 0.02f;
                                    if (lastThrottle < 0.0f)
                                    {
                                        lastThrottle = 0.0f;
                                    }
                                }
                                else
                                {
                                    lastThrottle = FlightInputHandler.state.mainThrottle;
                                }
                            }
                            Play_Anim(animationName, 0.0f, lastThrottle);
                        }
                        else
                        {
                            Play_Anim(animationName, 0.0f, FlightInputHandler.state.mainThrottle);
                        }
                    }
                    else //not synced so play at full speed from the beginning
                    {
                        if (!aniEngine.isPlaying && lastThrottle != 1.0f)
                        {
                            Play_Anim(animationName,1.0f,0.0f);
                            lastThrottle = 1.0f;
                        }
                    }
                }
            }
        }
    }


    public class MPSub : PartModule
    {
        [KSPField]
        public bool manageIntakes = false;

        [KSPField]
        public bool manageThrottle = false;

        public override void OnStart(StartState state)
        {
            Debug.Log("[Maritime Pack]Sub Found");
            FSSubRoutines.SubFound = true;
            FSSubRoutines.manageIntakes = manageIntakes;
            if (manageIntakes)
            {
                Debug.Log("[Maritime Pack]Intakes Enabled");
            }
            FSSubRoutines.manageThrottle = manageThrottle;
            if (manageThrottle)
            {
                Debug.Log("[Maritime Pack]Engines Enabled");
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FSSubRoutines : MonoBehaviour
    {
        public static bool SubFound = false;
        public static bool manageIntakes = false;
        public static bool manageThrottle = false;
        public bool intakesOpen = true;
        public bool hasSplashed = false;
        public bool checkOnce = false;

        public void FixedUpdate()
        {
            if (!checkOnce)
            {
                if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.SPLASHED)
                {
                    hasSplashed = true;
                    checkOnce = true;
                }
            }
            if (!hasSplashed && (!SubFound || FlightGlobals.ActiveVessel == null))
            {
                return;
            }
            //Close Intakes
            if (manageIntakes)
            {
                if (FlightGlobals.ActiveVessel.altitude <= -1.0)
                {
                    if (intakesOpen)
                    {
                        for (int p = FlightGlobals.ActiveVessel.Parts.Count - 1; p >= 0; --p)
                        {
                            for (int i = FlightGlobals.ActiveVessel.Parts[p].Modules.Count - 1; i >= 0; --i)
                            {
                                PartModule m = FlightGlobals.ActiveVessel.Parts[p].Modules[i];
                                if (m is ModuleResourceIntake)
                                {
                                    (m as ModuleResourceIntake).enabled = false;
                                    (m as ModuleResourceIntake).Deactivate();
                                    intakesOpen = false;
                                    Debug.Log("[Maritime Pack]Closing Intakes");
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (!intakesOpen)
                    {
                        for (int p = FlightGlobals.ActiveVessel.Parts.Count - 1; p >= 0; --p)
                        {
                            for (int i = FlightGlobals.ActiveVessel.Parts[p].Modules.Count - 1; i >= 0; --i)
                            {
                                PartModule m = FlightGlobals.ActiveVessel.Parts[p].Modules[i];
                                if (m is ModuleResourceIntake)
                                {
                                    (m as ModuleResourceIntake).enabled = true;
                                    (m as ModuleResourceIntake).Activate();
                                    intakesOpen = true;
                                    Debug.Log("[Maritime Pack]Opening Intakes");
                                }
                            }
                        }
                    }
                }
            }
            //end Close Intakes
            //Engine Shutdown
            if (manageThrottle)
            {
                if (FlightGlobals.ActiveVessel.altitude > 2.0)
                {
                    Debug.Log("[Maritime Pack]Shutting Down Engine");
                    FlightGlobals.ActiveVessel.ctrlState.mainThrottle = 0;
                }
                //end Engine Shutdown
            }
        }
    }
}

