using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MPUtils
{
    class MPGui
    {
        [KSPAddon(KSPAddon.Startup.Flight, false)]
        public class MPdrawGUI : MonoBehaviour
        {
            private Rect _windowPosition = new Rect();


            private void Start()
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    RenderingManager.AddToPostDrawQueue(0, OnDraw);
                }
            }

            private void OnDraw()
            {
                if (FlightGlobals.ActiveVessel)
                {
                    _windowPosition = GUILayout.Window(10, _windowPosition, OnWindow, "Sail Info");
                }
            }

            private void OnWindow(int windowID)
            {

                GUILayout.BeginHorizontal(GUILayout.Width(500));                
                GUILayout.Label("Wind " + MPFunctions.MStoKTS(MPSailRoutines.windForce)+" kts.");
                GUILayout.Label("Lat  " + MPFunctions.GetLat(FlightGlobals.ActiveVessel,1));
                GUILayout.Label("Long " + MPFunctions.GetLong(FlightGlobals.ActiveVessel,1));
                GUILayout.Label("Heading " + MPFunctions.GetHeading(MPSailRoutines.TWR1Vessel));
                GUILayout.Label("Body " + MPFunctions.GetBody());
                GUILayout.EndHorizontal();
                GUI.DragWindow();
            }
        }
    }
}
/*
Start()
{
    GameObject windArrowInstance = new GameObject();

    try
    {
        windArrowInstance = GameDatabase.Instance.GetModel("yourModDirectoryTree/model");
    }
    catch
    {
        print("Problem locating the arrow model");
    }
}


FixedUpdate()
{
    if (!loadedsceneisflight) return;
    if (debugMode)
    {
        if windArrowInstance != null)
        {
            windArrowInstance.transform.position = this.transform.position; //the sail transform position or thrust position
            windArrowInstance.transform.LookAt(thrustDirection); //the vector3 of the thrusttransform.forward.normalized * a distance 10f or whatever
            windArrowInstance.SetActive(true);
        }
    }
}
*/