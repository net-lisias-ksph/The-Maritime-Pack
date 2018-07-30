using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MPHover
{
    public class MPHoverEngine : PartModule
    {
        [KSPField]
        public int liftThrust = 0;
        public static bool liftEngineActive = false;

        [KSPEvent(guiActive = true, guiName = "Activate Lift Engine", isPersistent = true)]
        public void HEngineActivate()
        {
            Events["HEngineActivate"].active = false;
            Events["HEngineDeactivate"].active = true;
            liftEngineActive = true;
            FSHoverDoLift.totalThrust += liftThrust;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Lift Engine", isDefault = true, active = false, isPersistent = true)]
        public void HEngineDeactivate()
        {
            Events["HEngineActivate"].active = true;
            Events["HEngineDeactivate"].active = false;
            liftEngineActive = false;
            FSHoverDoLift.totalThrust -= liftThrust;
        }

        public override void OnStart(StartState state)
        {
            liftEngineActive = false;
            FSHoverDoLift.totalThrust = 0;
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FSHoverDoLift : MonoBehaviour
    {
        public static int totalThrust = 0;
        public static int skirtCount = 0;
        public List<Transform> transformList = new List<Transform>();
        public List<Rigidbody> rbList = new List<Rigidbody>();
        public List<float> thrustList = new List<float>();
        public List<float> heightList = new List<float>();
        private int layermask = 1 << 15;
        private float maxHoverDist = 1.10f;
        private float minHoverDist = 1.0f;
        private float hoverHeight = 0f;
        private bool engineFound = false;
        private int partcount = 0;
        public AnimationCurve thrustCurve;
        public AnimationCurve thrustToNegate;
        private float lastTotalThrust = 0;

        Rigidbody rb;

        private float CalculateHoverHeight(Vector3 LiftPos)
        {
            float Height = 10, GD = 10, SD = 10;
            RaycastHit HoverRay;

            Vector3 geeDir = FlightGlobals.getGeeForceAtPosition(LiftPos).normalized;

            if (Physics.Raycast(LiftPos, geeDir, out HoverRay, maxHoverDist+10, layermask))
            {
                GD = HoverRay.distance;
            }

            if (FlightGlobals.currentMainBody.ocean)
            {
                float altitude = (float)FlightGlobals.getAltitudeAtPos(LiftPos);
                Plane Hover = new Plane(-geeDir, LiftPos + altitude * geeDir);

                Ray R;
                R = new Ray(LiftPos, geeDir);

                Hover.Raycast(R, out SD);
            }

            Height = rayclamp(GD, SD);

            //airConsumed = base.part.RequestResource("CushionPressure", AirConsumption * Height);
            return Height;
        }

        private float rayclamp(float distP, float distO)
        {
            float dist;

            dist = Math.Min(distP, distO);

            //if (dist < minHoverDist)
            //    return minHoverDist;
            //else
            return dist;
        }

        public void onStart()
        {
            partcount = 0;
        }


        public void FixedUpdate()
        {
            //waitTime -= Time.deltaTime;
            //if (waitTime > 0)
            //{
            //    return;
            //}
            if (!HighLogic.LoadedSceneIsFlight || totalThrust == 0) return;

            var sCount = 0;
 
            Vector3 thisWayUp = (FlightGlobals.ActiveVessel.rootPart.transform.position - FlightGlobals.ActiveVessel.mainBody.position).normalized;
            //Vector3 thisWayUp = rt.position + rt.up;
            //count the skirts to make sure something didn't blow up
            if (FlightGlobals.ActiveVessel.parts.Count != partcount)
            {
                partcount = FlightGlobals.ActiveVessel.parts.Count;
                transformList.Clear();
                rbList.Clear();
                engineFound = false;
                for (int p = FlightGlobals.ActiveVessel.Parts.Count - 1; p >= 0; --p)
                {

                    for (int i = FlightGlobals.ActiveVessel.Parts[p].Modules.Count - 1; i >= 0; --i)
                    {
                        PartModule m = FlightGlobals.ActiveVessel.Parts[p].Modules[i];
                        if (m is MPHoverEngine)
                        {
                            engineFound = true;
                        }
                        if (m is MPHoverSkirt)
                        {
                            rb = FlightGlobals.ActiveVessel.Parts[p].GetComponent<Rigidbody>();
                            if (rb == null)
                            {
                                Rigidbody rb = FlightGlobals.ActiveVessel.Parts[p].gameObject.AddComponent<Rigidbody>();
                            }
                            rb.drag = 0.12f;
                            sCount++;
                            rb = FlightGlobals.ActiveVessel.Parts[p].GetComponent<Rigidbody>();
                            if (FlightGlobals.ActiveVessel.parts[p].FindModelTransform("portLift"))
                            {
                                transformList.Add(FlightGlobals.ActiveVessel.Parts[p].FindModelTransform("portLift"));
                                rbList.Add(rb);
                                thrustList.Add(0);
                                heightList.Add(0);
                            }
                            if (FlightGlobals.ActiveVessel.parts[p].FindModelTransform("starboardLift"))
                            {
                                transformList.Add(FlightGlobals.ActiveVessel.Parts[p].FindModelTransform("starboardLift"));
                                rbList.Add(rb);
                                thrustList.Add(0);
                                heightList.Add(0);
                            }
                            if (FlightGlobals.ActiveVessel.parts[p].FindModelTransform("centerLift"))
                            {
                                transformList.Add(FlightGlobals.ActiveVessel.Parts[p].FindModelTransform("centerLift"));
                                rbList.Add(rb);
                                thrustList.Add(0);
                                heightList.Add(0);
                            }
                        }
                    }
                }
                if (!engineFound)
                {
                    totalThrust = 0;
                    //need to remove all thrust if there's no engine.  Sploded maybe?
                }
            }
            //let's do some lifting
            float thrustToAdd = 0f;
            float totalHeight = 0f;
            for (int i = 0; i < transformList.Count; i++)
            {
                totalHeight += CalculateHoverHeight(transformList[i].position);
            }
            float aveHeight = totalHeight / transformList.Count;
            for (int i = 0; i < transformList.Count; i++)
            {
                rb = rbList[i];
                hoverHeight = CalculateHoverHeight(transformList[i].position);
                //Debug.Log("HH :" + hoverHeight);
                ////how much height did we gain or lose?
                var heightChange = Math.Round(hoverHeight - heightList[i], 6);
                //Debug.Log("HC :" + heightChange);
                ////how much height are we gaining or losing per second?
                //var hps = Math.Round(heightChange * Time.deltaTime, 6);
                //Debug.Log("HPS :" + hps);
                ////where do we need to be?
                var optimalHeight = Math.Round(((maxHoverDist - minHoverDist) / 2) + minHoverDist, 6);
                //Debug.Log("OH :" + optimalHeight);
                ////how are we getting there?
                var heightNeeded = Math.Round(optimalHeight - hoverHeight, 6);
                //Debug.Log("HN :" + heightNeeded);
                ////thrust needed
                heightList[i] = hoverHeight;

                thrustCurve = AnimationCurve.EaseInOut(0f, 1f, maxHoverDist, 0.0f);
                thrustCurve.AddKey(new Keyframe(maxHoverDist * 0.8f, 0.9f));
                thrustToAdd = thrustCurve.Evaluate(hoverHeight) * (totalThrust/transformList.Count);
                //Debug.Log("Eval :" + thrustCurve.Evaluate(hoverHeight));

                thrustToNegate = AnimationCurve.EaseInOut(-0.001f, -1, 0.001f, 1);
                var reverseThrust = thrustToNegate.Evaluate((float) heightChange);
                if (hoverHeight < maxHoverDist)
                {
                    rb.AddForceAtPosition((thisWayUp.normalized) * (reverseThrust * -1), transformList[i].position, ForceMode.Impulse);
                }
                //Debug.Log("Adding Reverse Thrust: " + reverseThrust + ":" + transformList[i].position + ":" + transformList[i].name);

                //thrustList[i] = thrustToAdd;

                rb.AddForceAtPosition((thisWayUp.normalized) * thrustToAdd, transformList[i].position, ForceMode.Impulse);
                //Debug.Log("Adding Thrust: " + thrustToAdd + " Position :" + transformList[i].position + " Transform :" +transformList[i].name + " Height :" +hoverHeight);
            }
        }
    }

    public class MPHoverSkirt : PartModule
    {

    }
}
