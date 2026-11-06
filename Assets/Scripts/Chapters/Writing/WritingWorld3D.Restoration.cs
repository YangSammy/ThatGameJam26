using UnityEngine;
using MemoryWhitebox.Art;

namespace MemoryWhitebox
{
    public sealed partial class WritingWorld3D
    {
        AlbumWinterVisual memoryActor;
        Transform memoryVase, memoryVeil;
        Material memoryVeilMaterial;
        float memoryFade, actorStep;
        Vector3 actorPrevious;

        GameObject MemoryFurniture(GameObject source,string name,Vector3 size,Vector3 center,Vector3 euler)
        {
            var root=new GameObject(name).transform;root.SetParent(memory,false);
            SetLayer(root,MemoryLayer);
            var visual=CreateVisual(source,root);
            FitVisual(visual,size,center,euler);
            return visual;
        }

        void BuildMemoryRestoration()
        {
            collectionProps[0].localPosition=MemoryPosition(State.ItemPositions[0])+Vector3.up*.84f;
            collectionProps[0].localRotation=Quaternion.identity;
            collectionProps[3].localPosition=MemoryPosition(State.ItemPositions[3])+Vector3.up*.84f;
            collectionProps[1].localPosition=MemoryPosition(State.ItemPositions[1])+Vector3.up*1.10f;
            memoryVase=new GameObject("Empty vase remains after flowers").transform;memoryVase.SetParent(memory,false);
            memoryVase.localPosition=MemoryPosition(State.ItemPositions[1])+Vector3.up*.92f;
            var vase=CreateVisual(visualAssets.memoryVase,memoryVase);
            FitVisual(vase,new Vector3(.25f,.32f,.25f),Vector3.zero,Vector3.zero);
            SetLayer(memoryVase,MemoryLayer);
            // A camera-local veil affects only memory, leaving the paper and mechanical timer legible.
            memoryVeil=GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
            memoryVeil.name="Timeout memory fade";memoryVeil.SetParent(MemoryCamera.transform,false);
            memoryVeil.localPosition=new Vector3(0,0,.12f);
            memoryVeil.GetComponent<Collider>().enabled=false;SetLayer(memoryVeil,MemoryLayer);
            memoryVeilMaterial=Material("#dad8ce");
            memoryVeilMaterial.shader=visualAssets.memoryFadeShader;
            memoryVeil.GetComponent<Renderer>().sharedMaterial=memoryVeilMaterial;
            actorPrevious=player.localPosition;
            RestoreMemoryPresentation();
        }

        void RestoreMemoryPresentation()
        {
            memoryFade=State.TimedOut?1:0;
            actorPrevious=player.localPosition;
            ApplyMemoryFade();
        }

        void ApplyMemoryFade()
        {
            if(memoryVeil==null)return;
            // Overfill handles letterboxed windows and any aspect of the right viewport.
            float height=2*.12f*Mathf.Tan(MemoryCamera.fieldOfView*Mathf.Deg2Rad*.5f);
            memoryVeil.localScale=new Vector3(height*Mathf.Max(.2f,MemoryCamera.aspect)*1.08f,height*1.08f,1);
            memoryVeilMaterial.color=new Color(.855f,.847f,.808f,Mathf.SmoothStep(0,1,memoryFade));
            memoryVeil.gameObject.SetActive(memoryFade>0);
        }

        void UpdateMemoryRestoration(float dt)
        {
            if((State.Phase=="tutorial" || State.Phase=="collect") && !State.TimedOut)
                for(int i=0;i<4;i++)if(!State.HasItem(i))
                    TaskOutlineCue.Show(collectionProps[i],MemoryCamera,collectionProps[i].GetComponentsInChildren<Renderer>(true));
            float travel=Vector3.Distance(actorPrevious,player.localPosition);
            actorPrevious=player.localPosition;
            if(travel>.001f && dt>0)actorStep=Mathf.Repeat(actorStep+travel*1.8f,1);
            memoryActor.SetActorPose(travel>.001f && dt>0?"Walk":"Idle",actorStep);
            // A little draft through the open doorway moves petals and the envelope edge;
            // these are object cues, without outline rings or recoloring the materials.
            collectionProps[1].localRotation=Quaternion.Euler(Mathf.Sin(time*1.35f)*2,0,Mathf.Cos(time*1.2f)*1.4f);
            collectionProps[3].localRotation=Quaternion.Euler(0,0,Mathf.Sin(time*1.7f)*1.5f);
            memoryFade=State.TimedOut?Mathf.MoveTowards(memoryFade,1,dt/1.1f):0;
            ApplyMemoryFade();
        }
    }
}
