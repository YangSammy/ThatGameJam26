using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MemoryWhitebox.V3
{
    // Returns passed-check descriptions; a failed contract throws. Root owns the Unity run.
    public static class PhotoV3Checks
    {
        public static Action<string,WorldChapter3D> CaptureStage;
        static readonly WBInput input=new WBInput();
        static void Need(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
        static void Test(List<string> failures,string name,Action action)
        {try{action();}catch(Exception e){failures.Add(name+": "+e.Message);}}
        static PhotoChapter NewModel()=>new PhotoChapter{SuppressCompletionNotification=true};
        static void Load(PhotoChapter p,int iso=100)
        {
            Need(p.OpenBack(),"back must open");Need(p.InsertFilm(iso),"insert cartridge");Need(p.PullLeader(),"pull leader");
            Need(p.SeatPerforations(),"engage perforations");Need(p.Advance(),"check take-up");Need(p.CloseBack(),"close back");Need(p.Advance(),"advance first frame");
        }
        public static void Align(PhotoChapter p)
        {
            var target=p.Target;var s=p.Settings;
            s.X=target.X;s.Z=target.Z;s.Yaw=target.Yaw;s.Pitch=target.Pitch;s.Focus=target.Focus;s.Focal=50;
            s.Aperture=target.Aperture;s.Shutter=target.Shutter*(200f/s.ISO);
            p.InViewfinder=true;
            p.Tick(2.2f,input);
        }
        static void ThreeExposures(PhotoChapter p)
        {
            for(int i=0;i<3;i++)
            {
                Align(p);if(i==2)Need(p.RespondToHand(),"answer offered hand");Need(p.Expose(),"expose frame "+i);
                if(i<2)Need(p.Advance(),"advance between exposures");
            }
        }
        public static void Process(PhotoChapter p,Action<string> stage=null)
        {
            Need(p.Rewind(1),"rewind roll");Need(p.PutInBag(),"put cartridge in bag");Need(p.SealBag(),"seal bag");stage?.Invoke("photo_dark_bag");
            Need(p.WindReel(1),"load reel in sealed bag");stage?.Invoke("photo_tank_ready_to_seal");Need(p.SealTank(),"seal daylight tank");
            Need(p.Pour(0),"developer");p.Agitated=true;stage?.Invoke("photo_film_developer");p.Tick(4,input);stage?.Invoke("photo_developer_drain");Need(p.Drain(),"drain developer");
            Need(p.Pour(1),"stop");p.Tick(2,input);Need(p.Drain(),"drain stop");
            Need(p.Pour(2),"fixer");p.Agitated=true;p.Tick(3,input);Need(p.Drain(),"drain fixer");
            Need(p.Pour(3),"wash");p.Tick(3,input);Need(p.Drain(),"drain wash");Need(p.OpenTank(),"open fixed washed tank");
            Need(p.HangNegative(),"hang negative");stage?.Invoke("photo_negative_drying");p.Tick(3,input);
            Need(p.BeginContact(),"contact expose");p.Tick(2,input);Need(p.PaperBath(0),"paper developer");stage?.Invoke("photo_paper_developer");p.Tick(3,input);
            Need(p.PaperBath(1),"paper stop");p.Tick(2,input);Need(p.PaperBath(2),"paper fixer");p.Tick(3,input);
            Need(p.PaperBath(3),"paper wash");p.Tick(3,input);Need(p.HangPaper(),"dry prints");p.Tick(4,input);
        }
        public static string[] Run()
        {
            var failures=new List<string>();
            Test(failures,"photography physical loading gates",()=>
            {
                var p=NewModel();Need(!p.InsertFilm(100),"cannot insert through closed back");Need(!p.Expose(),"unloaded shutter blocked");
                Need(p.OpenBack(),"open latch");Need(!p.InsertFilm(300),"unknown ISO blocked");Need(p.InsertFilm(800),"ISO800 available");
                Need(!p.CloseBack(),"cannot close before leader engaged and advance checked");Need(!p.Advance(),"cannot advance loose leader");
                Need(p.PullLeader()&&p.SeatPerforations()&&p.Advance()&&p.CloseBack()&&p.Advance(),"physical load sequence");
                Need(p.Settings.ISO==800 && !p.InsertFilm(100),"one ISO for entire roll");
                float focal=p.Settings.Focal;p.AdjustFocus(1);Need(p.Settings.Focal==focal && p.Settings.Focus>5,"focus wheel must not zoom");
                foreach(int iso in PhotoChapter.FilmSpeeds){var q=NewModel();Load(q,iso);Need(q.Settings.ISO==iso,"all four ISO cartridges");}
            });
            Test(failures,"photography equivalent exposures and action",()=>
            {
                foreach(int iso in PhotoChapter.FilmSpeeds)
                {
                    var p=NewModel();Load(p,iso);Align(p);Need(p.CompositionQualified,"equivalent ISO exposure "+iso);
                    p.Settings.Aperture=5.6f;p.Settings.Shutter*=.5f;p.Tick(2.2f,input);Need(p.CompositionQualified,"equivalent reciprocal aperture/shutter pair");
                    p.AdjustFocus(3);Need(!p.CompositionQualified,"parameter edit invalidates qualification");
                }
                var q=NewModel();Load(q);ThreeExposures(q);Need(q.Exposures.Count==3 && !q.Completed,"whole roll must be developed");
                var snapshot=q.Exposures[0].Settings;float focus=snapshot.Focus;q.Settings.Focus=12;q.Settings.X=90;
                Need(snapshot.Focus==focus && snapshot.X==0,"shutter settings copied by value");
                Need(q.Exposures[0].GirlAction>.65f && q.Exposures[1].GirlAction>.65f && q.Exposures[2].HandResponse,"three action differences retained");
            });
            Test(failures,"photography hand and per-frame advance gates",()=>
            {
                var p=NewModel();Load(p);Align(p);Need(p.Expose(),"first shutter");Need(!p.Expose(),"no double exposure without advance");
                Need(p.Advance(),"second frame advance");Align(p);Need(p.Expose()&&p.Advance(),"second shutter and advance");Align(p);
                Need(!p.Expose(),"third requires hand response");Need(p.RespondToHand()&&p.Expose(),"third hand response then shutter");Need(!p.Advance(),"roll stops at three frames");
            });
            Test(failures,"photography daylight tank safety and drains",()=>
            {
                var p=NewModel();Load(p);ThreeExposures(p);Need(!p.WindReel(1),"cannot load outside sealed bag");Need(!p.SealTank(),"no empty tank sealing");
                Need(p.Rewind(1)&&p.PutInBag(),"rewind then bag");Need(!p.WindReel(1),"open dark bag blocked");
                Need(p.SealBag()&&p.WindReel(1)&&p.SealTank(),"sealed dark chain");Need(!p.OpenTank(),"unfixed tank cannot open");Need(!p.Pour(2),"wrong chemical blocked");
                Need(p.Pour(0),"developer fill");p.Tick(10,input);Need(p.TimerRunning,"agitation required");p.Agitated=true;p.Tick(4,input);
                Need(p.NeedsDrain && !p.Pour(1),"drain before next liquid");Need(p.Drain()&&p.Pour(1),"drain unlocks stop");Need(!p.OpenTank(),"stop is not fixation");
            });
            Test(failures,"photography complete three-print development",()=>
            {
                var p=NewModel();Load(p);ThreeExposures(p);Process(p);Need(p.Completed && p.Phase==PhotoChapter.FilmPhase.Developed,"only dried prints complete chapter");
                Need(p.Exposures.Count==3 && p.Darkroom==PhotoChapter.DarkStep.Complete,"three references remain associated");
            });
            Test(failures,"photography cancel contact and wrong bath recovery",()=>
            {
                var p=NewModel();p.Darkroom=PhotoChapter.DarkStep.ContactReady;Need(p.BeginContact(),"start contact");p.Tick(.4f,input);p.CancelContact();
                Need(p.Darkroom==PhotoChapter.DarkStep.ContactReady && !p.TimerRunning,"released exposure retries locally");
                p.Darkroom=PhotoChapter.DarkStep.PaperReady;Need(!p.PaperBath(2)&&p.Darkroom==PhotoChapter.DarkStep.PaperReady,"wrong paper bath leaves recoverable state");
            });
            if(CaptureStage!=null)Test(failures,"photography 3D shutter and visual stages",CaptureWorldStages);
            if(failures.Count>0)throw new InvalidOperationException(string.Join("\n",failures.ToArray()));
            var passed=new List<string>{
                "Photography: four ISO speeds and gated physical loading",
                "Photography: fixed focal length, focus wheel and equivalent exposure pairs",
                "Photography: three immutable shutter settings and changed girl actions",
                "Photography: hand response and advance required between frames",
                "Photography: sealed bag/tank, agitation and mandatory drains",
                "Photography: negative/contact/paper baths and dry three-print completion",
                "Photography: canceled contact exposure and wrong-bath local recovery"
            };
            if(CaptureStage!=null)passed.Add("Photography: physical 3D shutter textures, edge navigation and rendered inspection stages");
            return passed.ToArray();
        }
        static void CaptureWorldStages()
        {
            Camera temporary=null;PhotoWorld3D world=null;var p=NewModel();
            try
            {
                if(Camera.main==null){var go=new GameObject("Photo V3 QA main camera");go.tag="MainCamera";temporary=go.AddComponent<Camera>();}
                world=new PhotoWorld3D(null,p);world.Activate();world.QAEnter(PhotoWorld3D.Inspection.Body);
                foreach(int iso in PhotoChapter.FilmSpeeds)
                {
                    string id="film-"+iso;Need(world.QAInView(id) && world.QAPickFromView(id)==id,"ISO cartridge must be visible and pickable from body view: "+id+"; hit "+world.QAPickFromView(id));
                }
                world.ApplyPose();CaptureStage("photo_camera_empty",world);
                Need(p.OpenBack(),"open back for physical pointer check");world.Tick(0,input);
                Need(world.QAInView("chamber") && world.QAPickFromView("chamber")=="chamber","visible camera view ray must hit empty film chamber, got "+world.QAPickFromView("chamber"));
                Need(p.InsertFilm(400),"insert film for take-up check");world.Tick(0,input);
                Need(world.QAInView("spool") && world.QAPickFromView("spool")=="spool","visible camera view ray must hit take-up spool, got "+world.QAPickFromView("spool"));
                Need(p.PullLeader(),"prepare open-back state");world.Tick(0,input);
                Need(world.QAInView("perforations") && world.QAPickFromView("perforations")=="perforations","visible camera view ray must hit perforations, got "+world.QAPickFromView("perforations"));
                world.Tick(0,input);world.ApplyPose();CaptureStage("photo_loading",world);
                Need(p.SeatPerforations()&&p.Advance()&&p.CloseBack()&&p.Advance(),"finish load");
                world.QAEnter(PhotoWorld3D.Inspection.Walk);world.QAEdge(new Vector2(20,735));Need(world.Mode==PhotoWorld3D.Inspection.Meter,"lower left meter");
                world.ApplyPose();CaptureStage("photo_meter",world);world.QAEdge(new Vector2(640,10));Need(world.Mode==PhotoWorld3D.Inspection.Walk,"top returns to world");
                world.QAEdge(new Vector2(1260,735));Need(world.Mode==PhotoWorld3D.Inspection.Body,"lower right camera");
                for(int i=0;i<3;i++)
                {
                    Align(p);if(i==2)p.RespondToHand();world.QAEnter(PhotoWorld3D.Inspection.Finder);world.Tick(0,input);world.ApplyPose();CaptureStage("photo_finder_"+i,world);
                    if(i==0)
                    {
                        world.QAEdge(new Vector2(10,380));Need(world.Mode==PhotoWorld3D.Inspection.Aperture,"left aperture window");world.ApplyPose();CaptureStage("photo_aperture",world);
                        world.QAEdge(new Vector2(640,380));world.QAEdge(new Vector2(1270,380));Need(world.Mode==PhotoWorld3D.Inspection.Shutter,"right shoulder shutter dial");world.ApplyPose();CaptureStage("photo_shutter",world);
                        world.QAEnter(PhotoWorld3D.Inspection.Finder);
                    }
                    Need(world.QAShoot(),"physical 3D exposure "+i);if(i<2)Need(p.Advance(),"advance frame");
                }
                Need(world.NativeCaptureCount==3,"one actual render per shutter");
                Texture2D frozen=world.ShotTextures[0];Color before=default(Color);
                if(frozen!=null)before=frozen.GetPixel(frozen.width/2,frozen.height/2);
                world.QAEnter(PhotoWorld3D.Inspection.Darkroom);
                Process(p,name=>{
                    world.Tick(0,input);world.ApplyPose();
                    if(name=="photo_tank_ready_to_seal")
                    {
                        Need(world.QAInView("tank-lid") && world.QAPickFromView("tank-lid")=="tank-lid","darkroom camera ray must reach tank lid, got "+world.QAPickFromView("tank-lid"));
                        Need(world.QAInView("pour-port") && world.QAPickFromView("pour-port")=="pour-port","darkroom camera ray must reach pour port, got "+world.QAPickFromView("pour-port"));
                    }
                    if(name=="photo_developer_drain")Need(world.QAInView("drain") && world.QAPickFromView("drain")=="drain","darkroom camera ray must reach drain, got "+world.QAPickFromView("drain"));
                    CaptureStage(name,world);
                });
                world.Tick(0,input);world.ApplyPose();CaptureStage("photo_final_three_prints",world);
                Need(world.CompletionImages!=null && world.CompletionImages.Length==3,"three completion images");
                if(SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Null)
                {
                    Need(world.NativePhotoReady,"three real developed textures");Need(frozen.GetPixel(frozen.width/2,frozen.height/2)==before,"processing cannot mutate shutter frame");
                    Need(world.CompletionImages[0]!=frozen,"developed image is a separate immutable output");
                }
            }
            finally
            {
                world?.Dispose();
                if(temporary!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(temporary.gameObject);else UnityEngine.Object.DestroyImmediate(temporary.gameObject);}
            }
        }
    }
}
