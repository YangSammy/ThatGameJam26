using System;
using UnityEngine;
namespace MemoryWhitebox
{
    [DisallowMultipleComponent]
    public sealed class ItemSceneBinding:MonoBehaviour
    {
        [Tooltip("此实例的稳定 ID。复制物品必须分配新 ID；改名不改 ID。")]
        public string instanceId;
        public ItemDefinition definition;
        public string ownerChapterId;
        public Transform model,homeAnchor;
        public Collider[] hitSurfaces=new Collider[0];
        public string DefinitionId=>definition!=null?definition.definitionId:"";
        public ItemRuntimeState CreateState(string locationId)=>new ItemRuntimeState(instanceId,DefinitionId,locationId);
        // Binding never restores a fixed object's transform. Placement belongs to its controller.
        public ItemSaveRecord Capture(string locationId,string payload)=>new ItemSaveRecord{instanceId=instanceId,definitionId=DefinitionId,ownerChapterId=ownerChapterId,locationId=locationId,payload=payload};
        public void RequireIdentity(ItemSaveRecord record)
        {
            if(record==null || record.instanceId!=instanceId || record.definitionId!=DefinitionId || record.ownerChapterId!=ownerChapterId)
                throw new System.IO.InvalidDataException("Item identity mismatch: "+instanceId);
        }
        [ContextMenu("Assign new instance ID")]
        void AssignNewId(){instanceId=Guid.NewGuid().ToString("N");}
    }
}
