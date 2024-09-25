/* Adapted from https://github.com/AlexanderDzhoganov/ksp-advanced-flybywire/blob/master/EVAController.cs under terms of MIT license
* Original Copyright (c) 2014 Alexander Dzhoganov
* Modifications Copyright (c) 2015 Sean McDougall
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using HarmonyLib;
using KRPC.SpaceCenter.Services.Parts;
using Steamworks;

namespace KIPC2
{

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class EvaControllerPatch : MonoBehaviour
    {
        void Awake()
        {
            var harmony = new Harmony("KIPC2");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(KerbalEVA), "HandleMovementInput")]
        class KerbalEVA_HandleMovementInput_Patch
        {
            static void Prefix(KerbalEVA __instance)
            {
                //var evaController = __instance.part.FindModuleImplementing<XEvaController>();
                var evaController = XEvaController.Instance;
                if (evaController != null)
                {
                    evaController.HandleMovementInput_Prefix();
                }
            }

            static void Postfix(KerbalEVA __instance)
            {
                //var evaController = __instance.part.FindModuleImplementing<XEvaController>();
                var evaController = XEvaController.Instance;
                if (evaController != null)
                {
                    evaController.HandleMovementInput_Postfix();
                }
            }
        }

    }


    class XEvaController
    {
        private static XEvaController instance;
        private bool m_active = false;
        Vector3 m_movementThrottle = Vector3.zero;
        Vector3 m_lookDirection;
        Vector3 m_upDirection;

        public static XEvaController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new XEvaController();
                }
                return instance;
            }
        }


        private KerbalEVA getEva()
        {
            return FlightGlobals.ActiveVessel.GetComponent<KerbalEVA>();
        }

        private FieldInfo eva_tgtFwd;
        private FieldInfo eva_tgtUp;
        private FieldInfo eva_packTgtRPos;
        private FieldInfo eva_tgtRpos;
        public KerbalEVA eva;

        public XEvaController()
        {
            eva_tgtFwd = typeof(KerbalEVA).GetField("tgtFwd", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            eva_tgtUp = typeof(KerbalEVA).GetField("tgtUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            eva_packTgtRPos = typeof(KerbalEVA).GetField("packTgtRPos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            eva_tgtRpos = typeof(KerbalEVA).GetField("tgtRpos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            eva = getEva();
        }

        public bool active
        {
            get => m_active;
            set
            {
                m_active = value;
            }
        }


        public bool rcs
        {
            get => getEva().JetpackDeployed;
            set
            {
                KerbalEVA eva = getEva();
                if(eva.JetpackDeployed != value) {
                    eva.ToggleJetpack();
                }

            }
        }

        public void setLookUp(Vector3 look, Vector3 up)
        {
            m_lookDirection = look;
            m_upDirection = up;
        }

        public Vector3 up
        {
            get => m_lookDirection;
            set
            {
                m_upDirection = value;
                if (value != Vector3.zero)
                {
                    m_upDirection.Normalize();
                }
            }
        }


        public Vector3 look
        {
            get => m_lookDirection;
            set
            {
                m_lookDirection = value.normalized;
                m_upDirection = Vector3.zero;
            }
        }

        public Vector3 tgtRpos
        {
            get
            {
                return (Vector3)eva_tgtRpos.GetValue(getEva());
            }
            set { eva_tgtRpos.SetValue(getEva(), value); }
        }

        public Vector3 packTgtRPos
        {
            get
            {
                return (Vector3)eva_packTgtRPos.GetValue(getEva());
            }
            set { eva_packTgtRPos.SetValue(getEva(), value); }
        }

        public Vector3 MovementThrottle
        {
            get => m_movementThrottle;
            set
            {
                m_movementThrottle = value;
            }
        }

        public void BoardPart(Part part)
        {
            getEva().BoardPart(part);
        }

        public void BoardSeat(Part part)
        {
            PartModule pm = part.Modules.GetModule("KerbalSeat");
            getEva().BoardSeat((KerbalSeat)pm);
        }

        public IList<string> ListEvents()
        {
            var eva = getEva();
            var result = new List<string>();
            foreach (var evt in eva.fsm.CurrentState.StateEvents) {
                result.Add(evt.name);
            }
            return result;
        }

        public IList<string> ListEventsPart(Part part)
        {
            var eva = getEva();
            var result = new List<string>();


            PartModule[] allpartmodules = part.GetComponents<PartModule>();
            foreach (var pm in allpartmodules)
            {
                foreach (var evt in pm.Events)
                {
                    result.Add(evt.name);
                }
            }
            return result;
        }

        public void DoEventPart(Part part, string eventName)
        {
            var eva = getEva();
            PartModule[] allpartmodules = part.GetComponents<PartModule>();
            foreach (var pm in allpartmodules)
            {
                foreach (var evt in pm.Events)
                {
                    if (evt.name == eventName)
                    {
                        evt.Invoke();
                        return;
                    }
                }
            }

        }


        internal void HandleMovementInput_Prefix()
        {

            if (!m_active) return;

            var eva = getEva();

            Vector3 tgtRpos =
                MovementThrottle.z * eva.transform.forward +
                MovementThrottle.x * eva.transform.right;

            Vector3 packTgtRpos = tgtRpos + MovementThrottle.y * eva.transform.up;

            eva_tgtRpos.SetValue(eva, tgtRpos);
            eva_packTgtRPos.SetValue(eva, packTgtRpos);
        }

        internal void HandleMovementInput_Postfix()
        {
            if (!m_active) return;

            var eva = getEva();

            eva_tgtFwd.SetValue(eva, m_lookDirection);
            if(m_upDirection != Vector3.zero)
            {
                eva_tgtUp.SetValue(eva, m_upDirection);
            }

            // the movement code will not try to turn in place if tgtRPos is zero
            if (packTgtRPos == Vector3.zero && Vector3.Dot(look, eva.transform.forward) < 0.999f)
            {
                eva_tgtRpos.SetValue(eva, m_lookDirection * 0.0001f);
            }

        }

    }
}