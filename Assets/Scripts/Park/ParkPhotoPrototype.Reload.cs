using System;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class ParkPhotoPrototype : ISerializationCallbackReceiver
    {
        [Serializable] struct InstrumentBinding
        {
            public string id;
            public Transform target;
            public Vector3 home;
        }
        // Unity retains runtime objects across script reload but not dictionaries.
        // This is component serialization only; it is not part of the film save format.
        [SerializeField,HideInInspector] InstrumentBinding[] reloadBindings;

        public void OnBeforeSerialize()
        {
            reloadBindings=new InstrumentBinding[instrumentParts.Count];
            int i=0;
            foreach(var pair in instrumentParts)
                reloadBindings[i++]=new InstrumentBinding{id=pair.Key,target=pair.Value,home=partHomes[pair.Key]};
        }
        public void OnAfterDeserialize()
        {
            // Serialization callbacks may run off the main thread: no Unity API,
            // object lookup, instantiation, Transform access or save IO here.
            instrumentParts.Clear();partHomes.Clear();
            if(reloadBindings==null)return;
            foreach(var binding in reloadBindings)
            {
                instrumentParts[binding.id]=binding.target;
                partHomes[binding.id]=binding.home;
            }
        }
        void OnEnable()
        {
            if(instruments==null)return; // First creation belongs to Start, once.
            // A null string may deserialize as empty. Never resume a stale gesture
            // or treat the first post-compile mouse release as a new action.
            dragPart=null;bodyPointer.Clear();assetPull=0;
            moveVelocity=Vector3.zero;inputGate=true;handoff.Arm();
            Aiming=photoMode==PhotographyMode.Viewfinder;
            SyncParkInput();InputContext.Handoff();
            SyncInstruments();SyncCameraPreview();
        }
    }
}
