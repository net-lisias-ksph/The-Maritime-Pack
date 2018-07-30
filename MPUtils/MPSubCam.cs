
/*********************************************************************************************
* The Maritime Pack MPUtil plugin is copyright 2015 Fengist, all rights reserved.
* For full license information please visit http://www.kerbaltopia.com
*
* SubWaterModel code originally copyright by InfiniteDice, used with permission
* Underwater Camera code based on code originally by Hooligan Labs
**********************************************************************************************/

using System;
using UnityEngine;


namespace MPUtils
{
    public class MPUnderwaterCamera : PartModule
    {
        public static bool active = false;
        public override void OnStart(StartState state)
        {
            MPLog.Writelog("[Maritime Pack] Sub Camera Found on "+vessel.name);
            // Add stuff to the log
            MPSubRoutines.SubFound = true;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Underwater Camera")]
        public void UWCamActivate()
        {
            Events["UWCamActivate"].active = false;
            Events["UWCamDeactivate"].active = true;
            active = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Underwater Camera", active = false)]
        public void UWCamDeactivate()
        {
            Events["UWCamActivate"].active = true;
            Events["UWCamDeactivate"].active = false;
            active = false;
        }
    }


    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class MPSubRoutines : MonoBehaviour
    {
        [KSPField]
        public static bool SubFound = false;
        public bool doItOnce = true;
        public GameObject subWaterModel;
        public float invWaterOffset = 0f;
        public float CameraDistance = 50.0f;
        private float _cameraX;
        private float _cameraY;
        public float cameraAltitude;
        private float color1;
        private float color2;
        private float color3;
        private float density;
        private float enddistance;
        public int msgcooldown = 0;
        public float depthUnderShip; // this variable contains the depth under the ship.
        public int depthTimer = 0;
        public float impactDTimer;
        public float impactDCoolDown = 1.2f;
        public float densitytimer = 0;

        private void Start()
        {
            try
            {
                this.subWaterModel = GameDatabase.Instance.GetModel("Maritime Pack/Dome/Sub/model");
            }
            catch
            {
                MPLog.Writelog("[Maritime Pack] Problem locating SubWaterLayer!");
            }
            if (this.subWaterModel != null )
            {
                this.subWaterModel.SetActive(false);
            }
            else
            {
                MPLog.Writelog("[Maritime Pack ] One of the underwater environment models are not loading!");
            }
            if (UnderwaterCamera.ManualControl)
            {
                try
                {
                    UnderwaterCamera.RestoreCameraParent();

                }
                catch (Exception)
                {
                    MPLog.Writelog("[Maritime Pack ] Startup no active camera parent");
                }
                UnderwaterCamera.ManualControl = false;    
            }
        }

        public void Update()
        {
            if (!SubFound || FlightGlobals.ActiveVessel == null)
            {
                return;
            }
            //Camera Switch
            if (FlightGlobals.ActiveVessel.altitude <= -2.0 && MPUnderwaterCamera.active)
            {
                try
                {
                    if (UnderwaterCamera.ManualControl && (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA || CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Internal)) // Exit when IVA too
                    {
                        UnderwaterCamera.RestoreCameraParent();
                        return;
                    }
                }
                catch (Exception)
                {
                    MPLog.Writelog("[Maritime Pack] RestoreCameraParent Exception!");
                    return;
                }
                if (!UnderwaterCamera.ManualControl)
                {
                    try
                    {
                        UnderwaterCamera.SetCameraParent();
                        MPLog.Writelog("[Maritime Pack] Setting Camera Parent");

                    }
                    catch (Exception ex)
                    {
                        print("[Maritime Pack] Set Camera Exception!"); print(ex.Message);
                    }
                    try
                    {
                        UnderwaterCamera.ManualControl = true;
                        cameraManualControl();
                        MPLog.Writelog("[Maritime Pack] Camera Manual Control Activated");
                    }
                    catch (Exception ex)
                    {
                        MPLog.Writelog("[Maritime Pack] Camera Manual Control Exception!"); print(ex.Message);
                    }
                }
                else
                {
                    if (FlightCamera.fetch.enabled) 
                    {
                        new WaitForEndOfFrame();
                        cameraManualControl();
                    }
                }
            }
            else
            {
                if (UnderwaterCamera.ManualControl)
                {
                    try
                    {
                        UnderwaterCamera.RestoreCameraParent();
                    }
                    catch (Exception ex)
                    {
                        MPLog.Writelog("[Maritime Pack] Restore Camera Parent Exception!"); print(ex.Message);
                    }
                }
            }
            //End Camera Switch    
        }

        public void OnLoad(ConfigNode node)
        {
        }

        public void FixedUpdate()
        {
            if (!SubFound || FlightGlobals.ActiveVessel == null)
            {
                return;
            }

            //end calculate depth
            double num = (double)Vector3.Distance(Camera.main.transform.position, FlightGlobals.ActiveVessel.mainBody.position) - FlightGlobals.ActiveVessel.mainBody.Radius;
            Rigidbody rb = FlightGlobals.ActiveVessel.GetComponent<Rigidbody>();
            Vector3d normalized = (rb.position - FlightGlobals.ActiveVessel.mainBody.position).normalized;
            float num2 = ((float)FlightGlobals.ActiveVessel.altitude + this.invWaterOffset) * -1f;
            //Set Dome
            if (num < 0.0) // 
            {

                Vector3 b = normalized * (double)num2;
                this.subWaterModel.transform.position = FlightGlobals.ActiveVessel.transform.position + b;
                this.subWaterModel.transform.LookAt(FlightGlobals.ActiveVessel.mainBody.position);
                this.subWaterModel.SetActive(true);
                RenderSettings.fog = true;

                color1 = (float)(0.1 - ((num * -1) * 0.000167));
                color2 = (float)(0.25 - ((num * -1) * 0.0004167));
                color3 = (float)(0.75 - ((num * -1) * 0.00125));
                enddistance = (float)(2500 - (3.5f * (num * -1)));  //was 2500 
                double densitymultiplier = 0;  //keep that silly camera split fog density from being blatantly obvious
                if (num < -600)
                {
                    densitymultiplier = -500;
                }
                else
                {
                    densitymultiplier = num;
                }
                density = (float)((0.000001337 * (densitymultiplier * -1)));  //0.0003f originally totals .004 @ 600m

                RenderSettings.fogColor = new Color(0f, color1, color2, color3);
                RenderSettings.fogDensity = density;// density;
                RenderSettings.fogStartDistance = 1000f;
                RenderSettings.fogEndDistance = 2500;// enddistance;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                Camera.main.clearFlags = CameraClearFlags.Depth;
                msgcooldown++;
                if (msgcooldown >= 200 && MPConfig.MPDepthWarn && num < 550)
                {
                    ScreenMessages.PostScreenMessage(new ScreenMessage("Depth Warning", 1.25f, ScreenMessageStyle.UPPER_CENTER));
                    msgcooldown = 0;
                }
            }
            else
            {
                this.subWaterModel.SetActive(false);
                RenderSettings.fog = false;
                Camera.main.clearFlags = CameraClearFlags.Depth;
            }
            //End Set Dome
 
        }

        private void cameraManualControl()
        {
            if (!UnderwaterCamera.ManualControl)
                return;
            _cameraX = 0;
            _cameraY = 0;

            if (Input.GetMouseButton(1))    // RMB
            {
                _cameraX = Input.GetAxis("Mouse X") * UnderwaterCamera.CameraSpeed;  // Horizontal
                _cameraY = Input.GetAxis("Mouse Y") * UnderwaterCamera.CameraSpeed;  // Vertical
            }

            if (GameSettings.AXIS_MOUSEWHEEL.GetAxis() != 0f)   // MMB
            {
                CameraDistance =
                    Mathf.Clamp(
                        CameraDistance *
                        (1f - (GameSettings.AXIS_MOUSEWHEEL.GetAxis() * UnderwaterCamera.ActiveFlightCamera.zoomScaleFactor)),
                        UnderwaterCamera.ActiveFlightCamera.minDistance, UnderwaterCamera.ActiveFlightCamera.maxDistance);
            }

            UnderwaterCamera.ActiveCameraPivot.transform.RotateAround(UnderwaterCamera.ActiveCameraPivot.transform.position, -1 * FlightGlobals.getGeeForceAtPosition(UnderwaterCamera.ActiveCameraPivot.transform.position).normalized, _cameraX);
            UnderwaterCamera.ActiveCameraPivot.transform.RotateAround(UnderwaterCamera.ActiveCameraPivot.transform.position, -1 * UnderwaterCamera.ActiveFlightCamera.transform.right, _cameraY);
            UnderwaterCamera.ActiveCameraPivot.transform.position = FlightGlobals.ActiveVessel.CoM; //.transform.position;
            UnderwaterCamera.ActiveFlightCamera.transform.LookAt(UnderwaterCamera.ActiveCameraPivot.transform.position, -1 * FlightGlobals.getGeeForceAtPosition(UnderwaterCamera.ActiveFlightCamera.transform.position).normalized);
        }

    }

    public static class UnderwaterCamera
    {
        private static Transform _originalParentTransform;
        private static bool _manualControl;

        public static FlightCamera ActiveFlightCamera;
        public static GameObject ActiveCameraPivot;

        public static float CameraSpeed = 0f;
        public static float CameraSpeedMulti = 20f;

        public static bool ManualControl
        {
            set
            {
                if (value && ActiveFlightCamera == null)
                {
                    _manualControl = false;
                    MPLog.Writelog("[Maritime Pack] Tried to set manual camera control while FlightCamera.fetch was null.");
                    return;
                }
                _manualControl = value;
            }
            get { return _manualControl; }
        }

        public static void SetCameraParent()
        {
            // Assign FlightCamera instance to public var.
            ActiveFlightCamera = FlightCamera.fetch;

            // For replacing the camera when done editing.
            if (_originalParentTransform == null)
                _originalParentTransform = ActiveFlightCamera.transform.parent;

            // For translating the camera
            if (ActiveCameraPivot != null) GameObject.Destroy(ActiveCameraPivot);
            ActiveCameraPivot = new GameObject("FSCamPivot");
            ActiveCameraPivot.transform.position = FlightGlobals.ActiveVessel.transform.position;

            ActiveFlightCamera.transform.position = FlightCamera.fetch.transform.position;
            ActiveCameraPivot.transform.LookAt(ActiveFlightCamera.transform.position, -1 * FlightGlobals.getGeeForceAtPosition(UnderwaterCamera.ActiveFlightCamera.transform.position).normalized);
            ActiveFlightCamera.transform.LookAt(ActiveCameraPivot.transform.position, -1 * FlightGlobals.getGeeForceAtPosition(UnderwaterCamera.ActiveFlightCamera.transform.position).normalized);
                       
            // Switch to active object.
            ActiveFlightCamera.transform.parent = ActiveCameraPivot.transform;

            ActiveFlightCamera.maxDistance = 4900.00f; // dome is 5km

             // Use the FlightCamera sensitivity for the speed.
            CameraSpeed = ActiveFlightCamera.orbitSensitivity * CameraSpeedMulti;

            // Instruct LateUpdate that we're controlling the camera manually now.
            ManualControl = true;

            // Say something.
            MPLog.Writelog("[Maritime Pack] FlightCamera switched to: " + FlightGlobals.ActiveVessel.name);
        }

        public static void RestoreCameraParent()
        {
            // Restore camera control to vessel.
            FlightCamera.fetch.transform.parent = _originalParentTransform;
            _originalParentTransform = null;


            ManualControl = false;

            MPLog.Writelog("[Maritime Pack] FlightCamera restored to vessel.");
        }
    }




}
