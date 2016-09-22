using System;
using System.Linq;
using UnityEngine;
using KSP.IO;
using System.Collections.Generic;

namespace CVFlightDeck
{
    public class FSArrestingGear : PartModule
    {
        public LineRenderer line = null;
        public static bool KLOLSActive;
        double boxdeflection = 5; //how many degrees from the ships aft end the landing box is
        float messagedelay = 0.0f; //timer to keep from spamming screen messages
        public Part FSTailHook; //the actual hook
        public static bool hookdeployed = false; //checks the animation state to see if hook is lowered.
        float distancecheck = 0.0f; //timer to keep from spamming the log
        public static bool arrestingGearActive = false;
        double AGactivationRange = 2000;
        public bool showstats = false;
        public string ArrestingSound = "Maritime Pack/Sounds/ArrestingGear";
        public FXGroup SoundGroup = null;
        public static bool playOnce = false;

        public override void OnStart(StartState state)
        {
            MPLog.Writelog("[CVFlightDeck] Arresting Gear ready. Clear the flight deck.");
            if (KLOLSActive == true)
            {
                ActivateKLOLS();
            }
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }
            distancecheck -= Time.deltaTime;
            messagedelay -= Time.deltaTime;
            double distanceFromCarrier = AGactivationRange;
            bool tailHookFound = false;
            bool inthebox = false;

            foreach (Part aPart in FlightGlobals.ActiveVessel.Parts)
            {
                foreach (PartModule aModule in aPart.Modules)
                {
                    if (aModule is FSTailHook)
                    {
                        tailHookFound= true;
                        FSTailHook = aPart;
                        break;
                    }
                }
                if (tailHookFound == true) 
                {
                    distanceFromCarrier = (aPart.transform.position - this.part.transform.position).magnitude;
                    break;
                }
            }
            if (tailHookFound == true && distanceFromCarrier > 500 && distanceFromCarrier <= AGactivationRange && arrestingGearActive == false)
            {
                arrestingGearActive = true;
                MPLog.Writelog("[CVFlightDeck] Arresting gear standing by.");
            }
            if (arrestingGearActive == true && distanceFromCarrier < 5f && FlightGlobals.ActiveVessel.srf_velocity.magnitude > 10)
            {
                // turn assist system on.
                if (hookdeployed)
                {
                    doLandAssist();
                }
                else
                {
                    MPLog.Writelog("[CVFlightDeck] Hook not deployed. Landlubber incoming!");
                }
            }
            if (FlightGlobals.ActiveVessel.srf_velocity.magnitude < 1 && arrestingGearActive == true) //when the plane is completely stopped the system is disabled.
            {
                arrestingGearActive = false;
                playOnce = false;
                if (line != null)
                {
                    line.SetColors(Color.red, Color.red);
                }
                inthebox = false;
                if (FlightGlobals.ActiveVessel.Splashed)
                {
                    MPLog.Writelog("[CVFlightDeck] Man Overboard! Pilot in the drink!");
                }
                else if (FlightGlobals.ActiveVessel.Landed)
                {
                    MPLog.Writelog("[CVFlightDeck] Aircraft landed. Welcome aboard!");
                }
            }
            if (arrestingGearActive == true)
            {
                // do some math
                double CVHeading = MPFunctions.GetHeading(this.vessel);
                double RevCVHeading = 0.0f;
                if (CVHeading >= 180)
                {
                    RevCVHeading = CVHeading - 180;
                }
                else
                {
                    RevCVHeading = CVHeading + 180;
                }
                //double portdeg = MPFunctions.NormalizeAngle(RevCVHeading, -boxdeflection);
                //double stbddeg = MPFunctions.NormalizeAngle(RevCVHeading, boxdeflection);
                double aircraftheading = MPFunctions.GetHeading(FlightGlobals.ActiveVessel);
                Vector3 UpVect = (this.vessel.transform.position - this.vessel.mainBody.position).normalized;
                //                Vector3 EastVect = this.vessel.mainBody.getRFrmVel(this.vessel.findWorldCenterOfMass()).normalized;
                Vector3 EastVect = this.vessel.mainBody.getRFrmVel(this.vessel.CurrentCoM.normalized);

                Vector3 NorthVect = Vector3.Cross(EastVect, UpVect).normalized;
                Vector3 TargetVect = FlightGlobals.ActiveVessel.transform.position - this.vessel.transform.position;
                Vector3 SurfTargetVect = TargetVect - Vector3.Dot(UpVect, TargetVect) * UpVect; // removing the vertical component
                float bearing = Vector3.Angle(SurfTargetVect, NorthVect);
                if (Math.Sign(Vector3.Dot(SurfTargetVect, EastVect)) < 0)
                {
                    bearing = 360 - bearing; // westward headings become angles greater than 180
                }
                Vector3 ToFlyingVect = (FlightGlobals.ActiveVessel.transform.position - this.vessel.transform.position).normalized; // A vector pointing toward the flying vessel with length 1
                UpVect = (this.vessel.transform.position - this.vessel.mainBody.transform.position).normalized; // A vector from the center of the current planet through the root part of your vessel with length 1
                float elevation = 90 - Vector3.Angle(UpVect, ToFlyingVect); // 90 (vertical) less angle from vertical
                double revBearing = MPFunctions.NormalizeAngle(bearing, 180);
                double aircraftPortdeg = MPFunctions.NormalizeAngle(revBearing, -45);
                double aircraftStbddeg = MPFunctions.NormalizeAngle(revBearing, 45);

                if (distancecheck < 0.0f && showstats)
                {
                    distancecheck = 1f;
                    MPLog.Writelog("[CVFlightDeck] ----------- ");
                    MPLog.Writelog("[CVFlightDeck] CV Heading " + CVHeading);
                    MPLog.Writelog("[CVFlightDeck] CV Rev Heading " + RevCVHeading);
                    //MPLog.Writelog("[CVFlightDeck] Port Deg: " + portdeg);
                    //MPLog.Writelog("[CVFlightDeck] Starboard Deg: " + stbddeg);
                    MPLog.Writelog("[CVFlightDeck] Aircraft Heading: " + aircraftheading);
                    MPLog.Writelog("[CVFlightDeck] Bearing: " + bearing);
                    MPLog.Writelog("[CVFlightDeck] Rev Bearing: " + revBearing);
                    MPLog.Writelog("[CVFlightDeck] Aircraft Stbd Deg: " + aircraftStbddeg);
                    MPLog.Writelog("[CVFlightDeck] Aircraft Port Deg: " + aircraftPortdeg);
                    MPLog.Writelog("[CVFlightDeck] Aircraft Stbd Angle Diff: " + MPFunctions.AngleDiff(aircraftheading, aircraftStbddeg));
                    MPLog.Writelog("[CVFlightDeck] Aircraft Port Angle Diff: " + MPFunctions.AngleDiff(aircraftheading, aircraftPortdeg));
                    MPLog.Writelog("[CVFlightDeck] Aircraft Deg off CV Rev Heading: " + MPFunctions.AngleDiff(RevCVHeading, bearing));

                    MPLog.Writelog("[CVFlightDeck] Incoming aircraft distance: " + distanceFromCarrier);
                    MPLog.Writelog("[CVFlightDeck] Aircraft Alt: " + FlightGlobals.ActiveVessel.altitude);
                    MPLog.Writelog("[CVFlightDeck] Elevation: " + elevation);
                }

                if (MPFunctions.AngleDiff(RevCVHeading, bearing) < boxdeflection)
                {
                    if (elevation < 10 && elevation > 0)
                    {
                        if (MPFunctions.AngleDiff(aircraftheading, aircraftPortdeg) < 45 || MPFunctions.AngleDiff(aircraftheading, aircraftStbddeg) < 45)
                        {
                            inthebox = true;
                        }
                    }
                }
                if (inthebox == true)
                {
                    //is aircraft in the box?

                    if (messagedelay <= 0)
                    {
                        if (inthebox)
                        {
                            ScreenMessages.PostScreenMessage("In the box", 2);
                            if (line != null)
                            {
                                line.SetColors(Color.green, Color.green);
                            }
                        }
                        else
                        {
                            ScreenMessages.PostScreenMessage("Out of the box", 2);
                            if (line != null)
                            {
                                line.SetColors(Color.red, Color.red);
                            }
                        }
                        messagedelay = 2f;
                    }
                 }
            }
        }


        private void doLandAssist()
        {
            // Stop the plane!
            if (playOnce == false)
            {
                SoundGroup.audio = gameObject.AddComponent<AudioSource>();
                SoundGroup.audio.clip = GameDatabase.Instance.GetAudioClip(ArrestingSound);
                SoundGroup.audio.Play();
                SoundGroup.audio.loop = false;
                playOnce = true;
            }
            MPLog.Writelog("[CVFlightDeck] ----------- Landing Assist! -----------");
            float distanceFromCarrier = (FlightGlobals.ActiveVessel.transform.position - this.part.transform.position).magnitude;
            MPLog.Writelog("[CVFlightDeck] Distance from carrier: " + distanceFromCarrier);
            FlightInputHandler.state.mainThrottle = 0f;
            FlightGlobals.ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true); //temporary to see the effect.                
            MPLog.Writelog("[CVFlightDeck] Setting aircraft brakes.");
            double aircraftSpeed = FlightGlobals.ActiveVessel.srf_velocity.magnitude;
            MPLog.Writelog("[CVFlightDeck] Aircraft Speed:" + aircraftSpeed);
            Vector3 headingDirection = FlightGlobals.ActiveVessel.GetComponent<Rigidbody>().transform.position - this.transform.position;
            Vector3 forward;
            forward = FlightGlobals.ActiveVessel.transform.up;
            if (Vector3.Dot(forward, headingDirection.normalized) > 0f)
            {
                //if (FSTailHook != null)
                //{
                //    FSTailHook.GetComponent<Rigidbody>().AddForce(-forward * ((float)aircraftSpeed * 25));
                //    MPLog.Writelog("[CVFlightDeck] Braking force added to tailhook: " + (float)aircraftSpeed * 25);
                //}
                //else
                //{
                    FSTailHook.vessel.GetComponent<Rigidbody>().AddForce(-forward * ((float)aircraftSpeed * 25));
                    MPLog.Writelog("[CVFlightDeck] Braking force added to vessel: " + (float)aircraftSpeed * 25);
                //}
            }
        }


        [KSPEvent(guiActive = true, guiName = "Activate KLOLS")]
        public void ActivateKLOLS()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }
            GameObject obj = new GameObject("Line");
            // Then create renderer itself...
            line = obj.AddComponent<LineRenderer>();
            line.transform.parent = part.transform; //this got screwed in 1.2  It needs to be centered on the part or the vessel or crete a transform to center it.
            line.useWorldSpace = false;
            line.transform.localPosition = Vector3.zero;
            line.transform.localEulerAngles = Vector3.zero;
            line.material = new Material(Shader.Find("Particles/Additive"));
            line.SetColors(Color.red, Color.red);
            line.SetWidth(1, 0);
            line.SetVertexCount(2);
            line.SetPosition(0, Vector3.down);
            line.SetPosition(1, Vector3.down * 4000 + Vector3.back * 100);

            ScreenMessages.PostScreenMessage("KLOLS On", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            Events["ActivateKLOLS"].active = false;
            Events["DeactivateKLOLS"].active = true;
            KLOLSActive = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate KLOLS", active = false)]
        public void DeactivateKLOLS()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }
            Destroy(line);
            line = null;
            ScreenMessages.PostScreenMessage("KLOLS Off", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            Events["ActivateKLOLS"].active = true;
            Events["DeactivateKLOLS"].active = false;
            KLOLSActive = false;
        }

    }

    //iTween.ShakePosition(Camera.mainCamera.gameObject, new Vector3(0.6f, 0.6f, 0.6f), 1.15f);
    //foreach (Camera c in FlightCamera.fetch.cameras)
    //{
    //    if (!c.enabled) continue;
    //    iTween.ShakePosition(c.gameObject, new Vector3(0.6f, 0.6f, 0.6f), 1.15f);
    //}


    public class FSCatapult : PartModule
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Catapult Power"), UI_FloatRange(minValue = 1f, maxValue = 200f, stepIncrement = 1f)]
        public float power_modifier = 100;

        [KSPField]
        public string model = "C1";


        public Part landingGear; //where we attach the catapult
        public bool catapultActive = false;
        public double catapultLength = 10f;
        public bool waitforpilot = false;
        public double preflighttimer = 3.0;
        public float MaxEMCTimer = 100; //maximum number of times thrust is applied
        public float EMCtakeoffTimer = 0f; //counts the number of times thrust is applied.
        public string ArrestingSound = "Maritime Pack/Sounds/CatLaunch";
        public FXGroup SoundGroup = null;
        public static bool playOnce = false;

        public LineRenderer line = null;

        public override void OnStart(StartState state)
        {
            MPLog.Writelog("[CVFlightDeck] Catapult ready. Clear the flight deck.");
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel.isActiveVessel) return; //if the scene isn't a flight or the carrier is the active vessel... don't bother with the code. added to get rid of errors in sph.

            if (waitforpilot)
            {
                preflighttimer -= Time.fixedDeltaTime;
                if (preflighttimer <= 0f)
                {
                    waitforpilot = false;
                    preflighttimer = 3.0f;
                }
            }
            if (Input.GetKey(KeyCode.Tab) && (FlightGlobals.ActiveVessel.transform.position - this.part.transform.position).magnitude < 50)
            {
                if (catapultActive == false)
                {
                    if ((FlightGlobals.ActiveVessel.transform.position - this.part.transform.position).magnitude >= (5 + catapultLength))
                    {
                        if (waitforpilot == false)
                        {
                            float distancetocat = (FlightGlobals.ActiveVessel.transform.position - this.part.transform.position).magnitude;

                            MPLog.Writelog("[CVFlightDeck] Launch requested. Waiting for pilot.");
                            ScreenMessages.PostScreenMessage("You are " + distancetocat.ToString("n2") + " meters away from the catapult.");
                            ScreenMessages.PostScreenMessage("Move within 5 meters of the catapult.", 5);
                            MPLog.Writelog("[CVFlightDeck] Catapult distance > 5m.");
                            waitforpilot = true;
                            preflighttimer = 3.0f;
                        }
                    }
                    else if (FlightGlobals.ActiveVessel.ctrlState.mainThrottle > 0f)
                    {
                        if (waitforpilot == false)
                        {
                            MPLog.Writelog("[CVFlightDeck] Launch requested. Waiting for pilot.");
                            ScreenMessages.PostScreenMessage("Pilot, set your throttle to 0.", 5);
                            MPLog.Writelog("[CVFlightDeck] Launch Throttle > 0.");
                            waitforpilot = true;
                            preflighttimer = 3.0f;
                        }
                    }
                    else if (FlightGlobals.ActiveVessel.srf_velocity.magnitude > 0.1)
                    {
                        if (waitforpilot == false)
                        {
                            MPLog.Writelog("[CVFlightDeck] Launch requested. Waiting for pilot.");
                            ScreenMessages.PostScreenMessage("Pilot, set your brakes and bring your aircraft to a complete stop.", 5);
                            MPLog.Writelog("[CVFlightDeck] Launch Speed > 1.");
                            waitforpilot = true;
                            preflighttimer = 3.0f;
                        }
                    }
                    else
                    {
                        MPLog.Writelog("[CVFlightDeck] Launch request accepted.");
                        ScreenMessages.PostScreenMessage("Launch requested.  Stand by...", 5);
                        waitforpilot = false;
                        preflighttimer = 3.0f;
                        catapultActive = true;
                    }
                }
            }

        }


 
        public void FixedUpdate()
        {

        //if the scene isn't a flight or the carrier is the active vessel... don't bother with the code. added to get rid of errors in sph.
            if (!HighLogic.LoadedSceneIsFlight || vessel.isActiveVessel) return;
            {
                bool lgfound = false;
                if (catapultActive == true)// is a launch requested?
                {
                    foreach (Part aPart in FlightGlobals.ActiveVessel.Parts)
                    {
                         if (model == "C1" && aPart.Modules.Contains("ModuleWheelBase")) //see if there's landing gear attached so we know what to launch
                         {
                             landingGear = aPart;
                             lgfound = true;
                             break;
                        }
                        if (model == "C2" && aPart.Modules.Contains("FSCatapultShuttle")) //rotating catapult, look for the shuttle
                        {
                            landingGear = aPart;
                            lgfound = true;
                            break;
                        }
                    }
                    if (lgfound == true)
                    {
                        MPLog.Writelog("[CVFlightDeck] Landing gear or shuttle found. Proceeding to launch.");
                        doEMCtakeoff();
                    }
                    else
                    {
                        if (model == "C1")
                        {
                            ScreenMessages.PostScreenMessage("Unable to launch. No valid landing gear found!", 5);
                        }
                        else if (model == "C2")
                        {
                            ScreenMessages.PostScreenMessage("Catapult shuttle not found on aircraft.", 5);
                        }
                        catapultActive = false;
                        EMCtakeoffTimer = 0f;
                    }
                }

            }
        }


        private void doEMCtakeoff()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }

            float distanceFromCarrier = (FlightGlobals.ActiveVessel.transform.position - this.part.transform.position).magnitude;

            if (FlightGlobals.ActiveVessel.srf_velocity.magnitude <= 100f && EMCtakeoffTimer < MaxEMCTimer && distanceFromCarrier <= 15f)
            {
                if (playOnce == false)
                {
                    SoundGroup.audio = gameObject.AddComponent<AudioSource>();
                    SoundGroup.audio.clip = GameDatabase.Instance.GetAudioClip(ArrestingSound);
                    SoundGroup.audio.Play();
                    SoundGroup.audio.loop = false;
                    playOnce = true;
                }
                MPLog.Writelog("[CVFlightDeck] Catapult launching.");
                ScreenMessages.PostScreenMessage("Launch!", 5);
                EMCtakeoffTimer += 1f;
                // Aircraft needs to be stopped and throttle at zero.
                FlightInputHandler.state.mainThrottle = 100f;

                FlightGlobals.ActiveVessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false); //remove brakes              
                                                                                                // apply 100% throttle and remove brakes...

                Vector3 forward = FlightGlobals.ActiveVessel.transform.up;

                if (FlightGlobals.ActiveVessel.GetComponent<Rigidbody>() != null)
                {
                    FlightGlobals.ActiveVessel.GetComponent<Rigidbody>().AddForce(forward * (power_modifier * 50), ForceMode.Acceleration);
                    MPLog.Writelog("[CVFlightDeck] LAUNCH! T+" + EMCtakeoffTimer);
                }
                else
                {
                    MPLog.Writelog("[CVFlightDeck] No rigidbody found on aircraft!  Unable to launch.");
                    ScreenMessages.PostScreenMessage("Unable to launch. No valid rigidbody found!!!", 5);
                }
            }
            else
            {
                string abortreason = "[CVFlightDeck] Catapult deactivated. Reason:";
                if (distanceFromCarrier > 15f)
                {
                    abortreason += " Distance > 15 meters: " + distanceFromCarrier + " meters";
                }
                else if (FlightGlobals.ActiveVessel.srf_velocity.magnitude > 100)
                {
                    abortreason += " Velocity > 100: " + FlightGlobals.ActiveVessel.srf_velocity.magnitude + " m/s";
                }
                else if (EMCtakeoffTimer >= MaxEMCTimer)
                {
                    abortreason += " Launch count > " + MaxEMCTimer;
                }
                MPLog.Writelog(abortreason);
                catapultActive = false;
                playOnce = false;
                EMCtakeoffTimer = 0f;
                MPLog.Writelog("[CVFlightDeck] Aircraft away: V=" + FlightGlobals.ActiveVessel.srf_velocity.magnitude + " D=" + distanceFromCarrier + " T=" + EMCtakeoffTimer);
            }
        }
    }

    class FSCatapultShuttle : PartModule
    {
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
        }
    }

    class FSTailHook : PartModule
    {

        private ModuleAnimateGeneric deployAnimation = new ModuleAnimateGeneric();

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            deployAnimation = part.Modules.OfType<ModuleAnimateGeneric>().FirstOrDefault();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight || !vessel.isActiveVessel) return;

            if (deployAnimation.animTime > 0)
            {
                FSArrestingGear.hookdeployed = true;
            }
            else
            {
                FSArrestingGear.hookdeployed = false;
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class MPStartup : MonoBehaviour
    {
        public void Awake() { }
        public void Start()
        {
            CVConfig.Getconfig();
            MPLog.NewLog();
            MPLog.Writelog("[CVFlightDeck] DLL Active!");
            FSArrestingGear.KLOLSActive = false;
        }
    }


    class MPLog
    {
        //TODO: Change this when moved to a gamedata directory
        private const string MPPath = "GameData/MPUtils/Plugins/PluginData/";
        private const string MPDebugLog = "MPLog.dat";

        public static void NewLog()
        {
            string path = KSPUtil.ApplicationRootPath + MPPath;
            string filepath = System.IO.Path.Combine(path, MPDebugLog);
            System.IO.File.Delete(filepath);
        }

        public static void Writelog(string thisline)
        {
            string path = KSPUtil.ApplicationRootPath + MPPath;
            string filepath = System.IO.Path.Combine(path, MPDebugLog);
            if (CVConfig.MPWLog)
            {
                System.IO.File.AppendAllText(filepath, DateTime.Now.ToString() + ": " + thisline + Environment.NewLine);
            }
            if (CVConfig.KSPWLog)
            {
                Debug.Log(thisline);
            }
        }
    }

    class CVConfig
    {

        public static bool MPWLog;
        public static bool KSPWLog;

        public static void Getconfig()
        {
            PluginConfiguration cfg = PluginConfiguration.CreateForType<CVConfig>();
            cfg.load();
            MPWLog = cfg.GetValue<bool>("MPWLog", true);
            KSPWLog = cfg.GetValue<bool>("KSPWLog", false);
        }
    }


    public static class MPFunctions
    {
        public static float GetHeading(Vessel vessel)
        {
            var up = vessel.upAxis;
            var north = GetNorthVector(vessel);
            var myheading =
                Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.GetTransform().rotation) *
                                   Quaternion.LookRotation(north, up));

            return myheading.eulerAngles.y;
        }

        public static Vector3d GetNorthVector(Vessel vessel)
        {
            return Vector3d.Exclude(vessel.upAxis, vessel.mainBody.transform.up);
        }

        public static double NormalizeAngle(double angle, double deviation)
        {
            return (angle + deviation + 3600) % 360;
        }

        public static double AngleDiff(double angle1, double angle2)
        {
            return 180 - Math.Abs(Math.Abs(angle1 - angle2) - 180);
        }

        public static double Radians(double angle)
        {
            return (Math.PI / 180) * angle;
        }
    }
}


