using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Kipc2
{
    using global::KRPC.SpaceCenter.Services;
    using global::KRPC.SpaceCenter.Services.Parts;
    using KIPC2;
    using KRPC.SpaceCenter.ExtensionMethods;
    using UnityEngine;
    using Tuple3 = System.Tuple<double, double, double>;

    /// <summary>
    /// Service for controlling Kerbals on EVA
    /// </summary>
    [KRPCService(GameScene = GameScene.All, Name = "EVA")]
    public static class EvaService
    {
        
        [KRPCProperty]
        public static bool Active
        {
            get => XEvaController.Instance.active;
            set { XEvaController.Instance.active = value; }
        }

        [KRPCProperty]
        public static bool Jetpack
        {
            get => XEvaController.Instance.rcs;
            set { XEvaController.Instance.rcs = value; }
        }


        [KRPCProperty]
        public static Tuple3 Look
        {
            get { return XEvaController.Instance.look.ToTuple(); }
            set { XEvaController.Instance.look = value.ToVector(); }
        }

        [KRPCProperty]
        public static Tuple3 Up
        {
            get { return XEvaController.Instance.up.ToTuple(); }
            set { XEvaController.Instance.up = value.ToVector(); }
        }

        [KRPCProcedure]
        public static void SetLookUp(Tuple3 look, Tuple3 up, ReferenceFrame referenceFrame)
        {
            var _l = referenceFrame.DirectionToWorldSpace(look.ToVector());
            var _u = referenceFrame.DirectionToWorldSpace(up.ToVector());
            XEvaController.Instance.setLookUp(_l, _u);
        }

        [KRPCProperty]
        public static Tuple3 MovementThrottle
        {
            get => XEvaController.Instance.MovementThrottle.ToTuple();
            set
            {
                Vector3 v = value.ToVector();
                if(v.magnitude>1)
                {
                    v.Normalize();
                }
                XEvaController.Instance.MovementThrottle = v;
            }
        }

        [KRPCProcedure]
        public static void BoardPart(Part part)
        {
            XEvaController.Instance.BoardPart(part.InternalPart);
        }

        [KRPCProcedure]
        public static void BoardSeat(Part part)
        {
            XEvaController.Instance.BoardSeat(part.InternalPart);
        }

        [KRPCProcedure]
        public static IList<string> ListEvents()
        {
            return XEvaController.Instance.ListEvents();
        }


        [KRPCProcedure]
        public static IList<string> ListEventsPart(Part part)
        {
            return XEvaController.Instance.ListEventsPart(part.InternalPart);
        }

        [KRPCProcedure]
        public static void DoEventPart(Part part, string eventName)
        {
            XEvaController.Instance.DoEventPart(part.InternalPart, eventName);
        }


    }
}
