using System;
using System.Reflection;
using System.Text;
using UnityEngine;
namespace MemoryWhitebox.EditorTools
{
    // Explicit Play-mode controller regression. This is NOT native mouse acceptance.
    public static class PhotographyDarkroomAssetChecks
    {
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static Ray RayTo(PhotoWorld3D world,string id,Vector3 origin)=>new Ray(origin,world.Nodes[id].transform.position-origin);
        static void Click(PhotoWorld3D world,string id){var ray=RayTo(world,id,new Vector3(2,2,3));world.Press(id,ray,0);world.Release(id,ray);}
        static void Drop(PhotoWorld3D world,string id,string destination,Vector3 origin)
        {Physics.SyncTransforms();var a=RayTo(world,id,origin);var b=RayTo(world,destination,origin);world.Press(id,a,0);world.Move(b);world.Release(destination,b);}
        static void Reload(PhotoWorld3D world)
        {Require(world.SaveProgress(),"Save failed");typeof(PhotoWorld3D).GetMethod("LoadProgress",Private).Invoke(world,null);}
        public static string ProcessCurrentRewoundRoll()
        {
            Require(Application.isPlaying && GameSavePaths.ResetOnEditorPlay,"Use isolated Editor Play saves.");
            var host=WBMain.Instance;var world=host.DarkroomWorld;
            Require(world.Photo.Phase==PhotoChapter.FilmPhase.Rewound,"Expected a rewound, open camera from the QA save.");
            var curtain=UnityEngine.Object.FindObjectOfType<StudyCurtain>();if(!curtain.IsOpen)curtain.Toggle();curtain.Advance(1);
            world.SetDirectDarkroom(true);host.InputContext.Select(InputContextId.Darkroom,PlayerControlState.Operation);
            // The synchronous fixture starts after the handoff; real input release is
            // checked separately with native clicks, not simulated by zero elapsed time.
            host.InputContext.ContinueGesture();
            Drop(world,"exposed-film","bag",new Vector3(.8f,1.8f,2.1f));
            Require(world.Photo.Phase==PhotoChapter.FilmPhase.DarkBag,"Film rejected by bag: "+world.ProgressHint);
            var bagRay=RayTo(world,"bag",new Vector3(1.6f,1.85f,2.3f));
            world.Press("bag",bagRay,0);world.Scroll("bag",4,bagRay);world.CancelInteraction();Reload(world);
            Require(Mathf.Abs(world.Photo.BagTurns-.4f)<.001f,"Bag progress lost");
            world.Press("bag",bagRay,0);world.Scroll("bag",6,bagRay);world.Release("bag",bagRay);Click(world,"tank-lid");
            var output=new StringBuilder("Controller/receiver tests, not native mouse tests\nBag transfer and 40% recovery passed\n");
            var origin=new Vector3(2.4f,2.2f,2.1f);
            for(int i=0;i<3;i++)
            {
                Drop(world,"chemical-"+i,"tank",origin);Require(world.Photo.Phase==PhotoChapter.FilmPhase.Processing,"Chemical "+i+" rejected");
                var ray=RayTo(world,"tank",origin);world.Press("tank",ray,0);world.Move(new Ray(origin,world.Nodes["tank"].transform.position+Vector3.right*.2f-origin));world.Release("tank",ray);
                Reload(world);world.TickDirectDarkroom(4);Require(world.Photo.Chemistry==i+1,"Chemistry did not advance");
                Drop(world,"tank","film-drain-basin",origin);Require(!(bool)typeof(PhotoWorld3D).GetField("needsDrain",Private).GetValue(world),"Drain rejected");
                output.AppendLine("Chemical "+i+": pour/agitate/save/load/drain passed");
            }
            Click(world,"water");world.TickDirectDarkroom(2);Click(world,"tank-lid");
            Drop(world,"print-negative","print-negative-clip",new Vector3(2.1f,2.1f,3));world.TickDirectDarkroom(10);
            Require(world.CurrentPrintStage==ParkPrintStage.ContactReady,"Drying did not finish");
            Drop(world,"print-paper","print-frame",new Vector3(2.1f,2.1f,3));Drop(world,"print-negative","print-frame",new Vector3(2.1f,2.1f,3));Click(world,"print-frame-lid");
            var exposure=RayTo(world,"print-exposure",new Vector3(2.1f,2.1f,3));world.Press("print-exposure",exposure,0);world.TickDirectDarkroom(.6f);world.Release("print-exposure",exposure);
            float remaining=(float)typeof(PhotoWorld3D).GetField("printRemaining",Private).GetValue(world);Reload(world);
            Require(Mathf.Abs(remaining-(float)typeof(PhotoWorld3D).GetField("printRemaining",Private).GetValue(world))<.001f,"Exposure remainder lost");
            var binding=UnityEngine.Object.FindObjectOfType<DarkroomPrintSceneBinding>();Require(!binding.exposureLight.activeSelf,"Exposure lamp stayed on after release");
            world.Press("print-exposure",exposure,0);Require(binding.exposureLight.activeSelf,"Exposure lamp did not turn on");world.TickDirectDarkroom(10);world.Release("print-exposure",exposure);Click(world,"print-frame-lid");
            output.AppendLine("Negative drying / frame / interrupted exposure recovery passed");
            var stages=new[]{ParkPrintStage.PaperStop,ParkPrintStage.PaperFixer,ParkPrintStage.PaperWash,ParkPrintStage.PaperDrying,ParkPrintStage.Complete};
            var receivers=new[]{"print-developer","print-stop","print-fixer","print-wash","print-photo-clip"};
            for(int i=0;i<receivers.Length;i++)
            {
                Drop(world,"print-paper",receivers[i],new Vector3(2.2f,2.1f,3.5f));if(i==3)Click(world,"print-tap");world.TickDirectDarkroom(.4f);Reload(world);world.TickDirectDarkroom(10);
                Require(world.CurrentPrintStage==stages[i],receivers[i]+" failed: "+world.ProgressHint);output.AppendLine(receivers[i]+": process/save/load passed");
            }
            var v=binding.GetComponent<DarkroomAssetVisuals>();var scale=v.ModelRoot("print-paper").lossyScale;
            Require(Vector3.Distance(scale,Vector3.one*v.materialDisplayScale)<.01f,"Paper stretched at hanger");
            foreach(var r in v.ModelRoot("print-paper").GetComponentsInChildren<SkinnedMeshRenderer>())Require(r.bounds.size.sqrMagnitude>.001f,"Paper culled by zero bounds");
            Require(ParkFilmStore.LoadActive().state==ParkFilmState.Developed,"Final print not persisted");
            output.AppendLine("Final print saved, uniform size and valid bounds passed");
            System.IO.File.WriteAllText("QA/BC-Integration/controller-regression.txt",output.ToString());return output.ToString();
        }
    }
}
