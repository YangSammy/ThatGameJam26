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
        public Collider[] candidateGrips=new Collider[3];
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
        readonly float[] reelL=new float[3],reelR=new float[3];
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
            intact[index].gameObject.SetActive(!data.Broken);broken[index].gameObject.SetActive(data.Broken);
            var v=data.Broken?broken[index]:intact[index];v.Winding(data.Position/TapeInspectionState.Duration);
            v.Reel(0,reelL[index]);v.Reel(1,reelR[index]);
            if(data.Broken){v.Sample("A04_LoseTension",1);}
            else {v.Cover(0,data.HoleA?1:0);v.Cover(1,data.HoleB?1:0);}
        }
        public void Turn(int index,int side,float amount,bool damaged)
        {
            if(side==0)reelL[index]+=amount*85;else reelR[index]+=amount*85;
            if(damaged){var v=broken[index];v.Sample(side==0?"A04_EndFlutter_L":"A04_EndFlutter_R",Mathf.Repeat(Mathf.Abs(side==0?reelL[index]:reelR[index])/360,1));}
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
        void UpdateRoll(){roll.localRotation=Quaternion.AngleAxis(-(UsedLength+PullLength)/.026f*Mathf.Rad2Deg,Vector3.right);}
        public bool InStorage(Transform tape)=>storage!=null && Vector3.Distance(tape.position,storage.transform.position)<.23f;
        public string Tooltip(Collider hit)
        {
            if(hit==lidHit)return LidBusy?"Wait for the lid":LidOpen?"Close storage box":"Open storage box · Choose a tape inside";
            if(hit==finishedHit)return "Finished copies · SUBMIT COPY checks and saves your recording";
            return null;
        }
    }
}
