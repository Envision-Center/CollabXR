//using System;
//using System.Collections;
//using System.Collections.Generic;
//using CollabXR.Avatar;
//using CollabXR.Logistics;
//using CollabXR.Oculus;
//using CollabXR.VR;
//using Fusion;
//using Oculus.Platform.Models;
//using TMPro;
//using UnityEngine;

//namespace CollabXR.Networking
//{
//    public struct AutoStateAuthorityVariable<T> : INetworkStruct
//    {
//        public T Value {
//            get
//            {
//                return networkedValue;
//            }
//            set
//            {
//                queuedValue = value;

//                MarkValueAsDirty();
//            }
//        }

//        [Networked] private T networkedValue { get; set; }

//        private T queuedValue;

//        private bool valueDirty;

//        private void MarkValueAsDirty()
//        {
//            if (!valueDirty)
//            {
//                StartCoroutine(UpdateNetworkValueCoroutine());
//            }

//            valueDirty = true;
//        }

//        private IEnumerator UpdateNetworkValueCoroutine()
//        {
//            if (!HasStateAuthority)
//                Object.RequestStateAuthority();

//            while (!HasStateAuthority)
//            {
//                yield return null;
//            }

//            networkedValue = queuedValue;
//        }
//    }
//}
