using UnityEngine;
namespace MemoryWhitebox
{
    // Authored visual bindings; gameplay and saves remain on HouseRadioDeck/RadioTapeChapter.
    public sealed class TapeWorkbenchVisuals:MonoBehaviour
    {
        public WorkbenchAssetView storage,dispenser,strip;
        public WorkbenchAssetView[] sources,intact,broken,sourceProxies;
        public WorkbenchAssetView targetProxy;
        public Collider lidHit,finishedHit;
        [Tooltip("Box interior footprint; nearby desk tapes must not count as stored.")]
        public Renderer storageFloor;
        public Collider[] candidateGrips=new Collider[RadioTapeChapter.WorkTapeCount];
        public Transform roll,head,cutLine;
        public SkinnedMeshRenderer loosePiece;
        public Transform[] slots;
        public Renderer[] legacyRenderers;
        public Collider[] legacyColliders;
        public Transform[] dormantRoots;
        public bool LidOpen {get;private set;}
        public bool LidBusy=>Mathf.Abs(lid-(LidOpen?1:0))>.001f;
        public float UsedLength {get;private set;}
        public float PullLength {get;private set;}
        float lid;
        float recoil;
        readonly float[] reelL=new float[RadioTapeChapter.WorkTapeCount],reelR=new float[RadioTapeChapter.WorkTapeCount];
        public void ToggleLid(){if(!LidBusy)LidOpen=!LidOpen;}
        public void Tick(float dt)
        {
            lid=Mathf.MoveTowards(lid,LidOpen?1:0,dt*1.8f);storage.Sample("A01_Lid_Open",lid);
            recoil=Mathf.MoveTowards(recoil,0,dt*2.5f);
            if(loosePiece!=null)loosePiece.SetBlendShapeWeight(0,recoil*100);
            foreach(var r in legacyRenderers)if(r!=null)r.enabled=false;
            foreach(var c in legacyColliders)if(c!=null)c.enabled=false;
            foreach(var t in dormantRoots)if(t!=null && t.gameObject.activeSelf)t.gameObject.SetActive(false);
        }
        public void Inspect(int index,TapeInspectionState.Candidate data)
        {
            intact[index].gameObject.SetActive(true);broken[index].gameObject.SetActive(false);
            var v=intact[index];v.Winding(data.Position/TapeInspectionState.Duration);
            v.Reel(0,reelL[index]);v.Reel(1,reelR[index]);
            v.Cover(0,data.HoleA?1:0);v.Cover(1,data.HoleB?1:0);
        }
        public void Pull(float length)
        {
            PullLength=Mathf.Clamp(length,0,.12f);dispenser.Sample("A05_CutState",0);dispenser.Sample("A05_Pull_Sample",PullLength/.12f);
            strip.gameObject.SetActive(false);
            UpdateRoll();
        }
        public void Cut()
        {UsedLength+=PullLength;PullLength=0;dispenser.Sample("A05_Pull_Sample",0);dispenser.Sample("A05_CutState",1);UpdateRoll();strip.gameObject.SetActive(true);recoil=1;}
        public void CancelPull()
        {PullLength=0;dispenser.Sample("A05_Pull_Sample",0);dispenser.Sample("A05_CutState",0);UpdateRoll();strip.gameObject.SetActive(true);}
        public void RestorePull(float usedLength){UsedLength=Mathf.Max(0,usedLength);recoil=0;CancelPull();}
        void UpdateRoll(){roll.localRotation=Quaternion.AngleAxis(-(UsedLength+PullLength)/.026f*Mathf.Rad2Deg,Vector3.right);}
        public bool InStorage(Transform tape)
        {
            if(storage==null || tape==null)return false;
            if(storageFloor==null)return Vector3.Distance(tape.position,storage.transform.position)<.23f;
            var bounds=storageFloor.bounds;var p=tape.position;
            return p.x>=bounds.min.x-.005f && p.x<=bounds.max.x+.005f &&
                p.z>=bounds.min.z-.005f && p.z<=bounds.max.z+.005f && Mathf.Abs(p.y-bounds.center.y)<.12f;
        }
        public string Tooltip(Collider hit)
        {
            if(hit==lidHit)return LidBusy?"Wait for the lid":LidOpen?"Close storage box":"Open storage box · Choose a tape inside";
            if(hit==finishedHit)return "Finished compartment · Drop the ejected B tape here";
            return null;
        }
    }
}
