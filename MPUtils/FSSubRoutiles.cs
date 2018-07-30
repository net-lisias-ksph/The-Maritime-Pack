using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MPSubPod : PartModule
{
    public override void OnStart(StartState state)
    {
        Debug.Log("[Maritime Pack]Sub Found");
        // Add stuff to the log
        FSSubRoutines.SubFound = true;
    }
}


[KSPAddon(KSPAddon.Startup.Flight, false)]
public class FSSubRoutines : MonoBehaviour
{
    [KSPField]
    public static bool SubFound = false;
    public float calibrateDepth = 0f; //set in cfg.
    public bool doItOnce = true;
    public GameObject subWaterModel;
    public GameObject deepWaterModel;
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
    public bool intakesOpen = true;
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
            Debug.Log("[Maritime Pack]Problem locating SubWaterLayer!");
        }
        try
        {
            this.deepWaterModel = GameDatabase.Instance.GetModel("Maritime Pack/Dome/Deep/model");
        }
        catch
        {
            Debug.Log("[Maritime Pack]Problem locating DeepWaterLayer!");
        }
        if (this.subWaterModel != null && this.deepWaterModel != null)
        {
            this.subWaterModel.SetActive(false);
            this.deepWaterModel.SetActive(false);
        }
        else
        {
            Debug.Log("[Maritime Pack]One of the underwater environment models are not loading!");
        }
        UnderwaterCamera.ManualControl = false;
    }

    public void Update()
    {
        if (!SubFound || FlightGlobals.ActiveVessel == null)
        {
            return;
        }
        //Camera Switch
        if (FlightGlobals.ActiveVessel.altitude <= 1.0)
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
                Debug.Log("[Maritime Pack]RestoreCameraParent Exception!");
                return;
            }
            if (!UnderwaterCamera.ManualControl)
            {
                try
                {
                    UnderwaterCamera.SetCameraParent();
                    Debug.Log("[Maritime Pack]Setting Camera Parent");

                }
                catch (Exception ex)
                {
                    print("[Maritime Pack]Set Camera Exception!"); print(ex.Message);
                }
                try
                {
                    UnderwaterCamera.ManualControl = true;
                    cameraManualControl();
                    Debug.Log("[Maritime Pack]Camera Manual Control Activated");
                }
                catch (Exception ex)
                {
                    print("[Maritime Pack]Camera Manual Control Exception!"); print(ex.Message);
                }
            }
            else
            {
                cameraManualControl();
            }
        }
        else
        {
            if (UnderwaterCamera.ManualControl)
            {
                //Debug.Log("[Maritime Pack]Vessel Above 5.0M");
                try
                {
                    UnderwaterCamera.RestoreCameraParent();
                }
                catch (Exception ex)
                {
                    Debug.Log("[Maritime Pack]Restore Camera Parent Exception!"); print(ex.Message);
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
        //calculate depth

        if (FlightGlobals.ActiveVessel.heightFromTerrain >= 0)
        {
            depthUnderShip = Mathf.Round(FlightGlobals.ActiveVessel.heightFromTerrain - calibrateDepth);
        }
        else if (FlightGlobals.ActiveVessel.heightFromTerrain < 0)
            depthUnderShip = 600f;

        depthTimer++;// this is a casual info popup
        if (depthTimer <= 0f)
        {
            depthTimer = 200;
            ScreenMessages.PostScreenMessage(new ScreenMessage("Depth Below Keel " + depthUnderShip + " m", 5f, ScreenMessageStyle.UPPER_RIGHT));
        }
        impactDTimer -= Time.deltaTime; // we check this more often as it's critical
        if (impactDTimer <= 0f)
        {
            impactDTimer = impactDCoolDown;
            if (depthUnderShip < 20f && depthUnderShip > 0f && FlightGlobals.ActiveVessel.horizontalSrfSpeed > 5.0d)//show constantly for now  - Will also need to play warning sound.
            {
                ScreenMessages.PostScreenMessage(new ScreenMessage("Impact Warning!!! " + depthUnderShip + " m", 5f, ScreenMessageStyle.UPPER_CENTER));
            }
        }


        //end calculate depth
        double num = (double)Vector3.Distance(Camera.main.transform.position, FlightGlobals.ActiveVessel.mainBody.position) - FlightGlobals.ActiveVessel.mainBody.Radius;
        //      Updated due to depreciation of FlightGlobals.ActiveVessel.rigidbody.position 
        Rigidbody rb = FlightGlobals.ActiveVessel.GetComponent<Rigidbody>();
        Vector3d normalized = (rb.position - FlightGlobals.ActiveVessel.mainBody.position).normalized;
        //        Vector3d normalized = (FlightGlobals.ActiveVessel.rigidbody.position - FlightGlobals.ActiveVessel.mainBody.position).normalized;
        float num2 = ((float)FlightGlobals.ActiveVessel.altitude + this.invWaterOffset) * -1f;
        //Set Dome
        if (num < 0.0 && num > -600.0) // 
        {

            Vector3 b = normalized * (double)num2;
            this.subWaterModel.transform.position = FlightGlobals.ActiveVessel.transform.position + b;
            this.subWaterModel.transform.LookAt(FlightGlobals.ActiveVessel.mainBody.position);
            this.subWaterModel.SetActive(true);
            this.deepWaterModel.SetActive(false);
            RenderSettings.fog = true;

            color1 = (float)(0.1 - ((num * -1) * 0.000167));
            color2 = (float)(0.25 - ((num * -1) * 0.0004167));
            color3 = (float)(0.75 - ((num * -1) * 0.00125));
            enddistance = (float)(2500 - (3.5f * (num * -1)));  //was 2500 
            density = (float)((0.000001337 * (num * -1)));  //0.0003f originally totals .004 @ 600m

            //this.subWaterModel.GetComponent<Renderer>().material.color = new Color(0f, color1, color2, color3);
            RenderSettings.fogColor = new Color(0f, color1, color2, color3);
            RenderSettings.fogDensity = density;// density;
            RenderSettings.fogStartDistance = 1000f;
            RenderSettings.fogEndDistance = 2500;// enddistance;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            Camera.main.clearFlags = CameraClearFlags.Depth;
        }
        else
        {
            if (num >= 0.0)
            {
                this.subWaterModel.SetActive(false);
                this.deepWaterModel.SetActive(false);
                RenderSettings.fog = false;
                Camera.main.clearFlags = CameraClearFlags.Depth;
            }
            else
            {
                if (num <= -600.0 && FlightGlobals.ActiveVessel.altitude <= -600.0)
                {
                    Vector3 b = normalized * (double)num2;
                    this.subWaterModel.transform.position = FlightGlobals.ActiveVessel.transform.position + b;
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = Color.black;
                    RenderSettings.fogDensity = 0.0008f;
                    RenderSettings.fogStartDistance = 1000f;
                    RenderSettings.fogEndDistance = 2500f;
                    RenderSettings.fogMode = FogMode.ExponentialSquared;
                    Camera.main.clearFlags = CameraClearFlags.Depth;
                    msgcooldown++;
                    if (msgcooldown >= 200)
                    {
                        ScreenMessages.PostScreenMessage(new ScreenMessage("Depth Warning", 1.25f, ScreenMessageStyle.UPPER_CENTER));
                        msgcooldown = 0;
                    }
                }
            }
        }
        //End Set Dome
    }

    private void cameraManualControl()
    {
        if (!UnderwaterCamera.ManualControl)
            return;
        //Debug.Log("[Maritime Pack]Manually Controlling");
        _cameraX = 0;
        _cameraY = 0;
        double cameraAltitude = 0;

        if (UnderwaterCamera.ActiveFlightCamera != null)
        {
            cameraAltitude = findAltitude(UnderwaterCamera.ActiveFlightCamera.transform);
        }
        else
        {
            cameraAltitude = findAltitude(FlightCamera.fetch.transform);
        }

        if (Input.GetMouseButton(1))    // RMB
        {
            //Debug.Log("[Maritime Pack]RMB Clicked");
            _cameraX = Input.GetAxis("Mouse X") * UnderwaterCamera.CameraSpeed;  // Horizontal
            _cameraY = Input.GetAxis("Mouse Y") * UnderwaterCamera.CameraSpeed;  // Vertical
        }

        if (GameSettings.AXIS_MOUSEWHEEL.GetAxis() != 0f)   // MMB
        {
            //Debug.Log("[Maritime Pack]Middle Mouse Wheel Scrolled");
            CameraDistance =
                Mathf.Clamp(
                    CameraDistance *
                    (1f - (GameSettings.AXIS_MOUSEWHEEL.GetAxis() * UnderwaterCamera.ActiveFlightCamera.zoomScaleFactor)),
                    UnderwaterCamera.ActiveFlightCamera.minDistance, UnderwaterCamera.ActiveFlightCamera.maxDistance);
        }

        Debug.DrawLine(UnderwaterCamera.ActiveCameraPivot.transform.position, new Vector3(1, 0, 0), Color.red, 1f, true);

        UnderwaterCamera.ActiveCameraPivot.transform.RotateAround(UnderwaterCamera.ActiveCameraPivot.transform.position, -1 * FlightGlobals.getGeeForceAtPosition(UnderwaterCamera.ActiveCameraPivot.transform.position).normalized, _cameraX);
        UnderwaterCamera.ActiveCameraPivot.transform.RotateAround(UnderwaterCamera.ActiveCameraPivot.transform.position, -1 * UnderwaterCamera.ActiveFlightCamera.transform.right, _cameraY);
        UnderwaterCamera.ActiveCameraPivot.transform.position = FlightGlobals.ActiveVessel.transform.position;
        UnderwaterCamera.ActiveFlightCamera.transform.LookAt(UnderwaterCamera.ActiveCameraPivot.transform.position, -1 * FlightGlobals.getGeeForceAtPosition(UnderwaterCamera.ActiveFlightCamera.transform.position).normalized);

    }

    private static double findAltitude(Transform aLocation)
    {
        if (FlightGlobals.ActiveVessel == null) return 0;
        return Vector3.Distance(aLocation.position, FlightGlobals.ActiveVessel.mainBody.position) - (FlightGlobals.ActiveVessel.mainBody.Radius);
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
                Debug.Log("[Maritime Pack]Tried to set manual camera control while FlightCamera.fetch was null.");
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

        // Use the FlightCamera sensitivity for the speed.
        CameraSpeed = ActiveFlightCamera.orbitSensitivity * CameraSpeedMulti;

        // Instruct LateUpdate that we're controlling the camera manually now.
        ManualControl = true;

        // Say something.
        Debug.Log("[Maritime Pack]FlightCamera switched to: " + FlightGlobals.ActiveVessel.name);
    }

    public static void RestoreCameraParent()
    {
        // Restore camera control to vessel.
        FlightCamera.fetch.transform.parent = _originalParentTransform;
        _originalParentTransform = null;


        ManualControl = false;

        Debug.Log("[Maritime Pack]FlightCamera restored to vessel.");
    }
}



