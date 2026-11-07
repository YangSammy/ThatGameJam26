using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class WardDistanceRestoreChecks
    {
        const string Output="output/QA/WardColdBottomReveal_20261107";
        static void Require(bool pass,string message){if(!pass)throw new Exception(message);}
        static WBInput Keys(params KeyCode[] keys){var input=new WBInput();foreach(var key in keys)input.Keys.Add(key);return input;}
        static PaintingChapter Ward(int version,float peak,float actor,float time)
        {
            var p=new PaintingChapter();
            var culture=System.Globalization.CultureInfo.InvariantCulture;
            p.RestoreCheckpoint("{\"version\":"+version+",\"phase\":3,\"shapes\":[true,true,true,true,true],\"key\":true,\"ward\":"+
                peak.ToString(culture)+",\"wardActor\":"+actor.ToString(culture)+",\"wardTime\":"+time.ToString(culture)+"}");
            return p;
        }
        public static string RunRules()
        {
            var host=WBMain.Instance;bool qa=WBMain.IsQA;var results=new List<string>();
            try
            {
                WBMain.Instance=null;WBMain.IsQA=true;
                var p=Ward(5,0,0,0);p.Tick(60,Keys());
                Require(p.WardProgress==0 && !p.Completed,"Idle revealed or completed ward");results.Add("PASS idle freezes");
                p.Tick(2,Keys(KeyCode.D));float peak=p.WardDistance,progress=p.WardProgress;
                Require(peak>0 && progress>0,"Forward did not reveal");
                foreach(var key in new[]{KeyCode.A,KeyCode.S,KeyCode.W,KeyCode.LeftArrow,KeyCode.RightArrow})
                {
                    p.Tick(2,Keys(key));
                    Require(p.Actor.x==peak && p.WardProgress==progress,"Non-D key moved ward actor: "+key);
                }
                p.Tick(60,Keys());Require(p.Actor.x==peak && p.WardProgress==progress,"Idle advanced ward");
                p.Tick(.2f,Keys(KeyCode.D,KeyCode.A));Require(p.Actor.x>peak,"A cancelled D forward input");
                peak=p.WardDistance;progress=p.WardProgress;
                Require(Mathf.Abs(progress-p.Actor.x/p.WardWalkDistance)<.0001f,"Reveal not tied to position");
                results.Add("PASS D-only forward, no reverse, idle freeze, position-driven reveal");
                var saved=p.CaptureCheckpoint();var copy=new PaintingChapter();copy.RestoreCheckpoint(saved);
                Require(copy.Actor==p.Actor && copy.WardDistance==peak && copy.WardProgress==progress,"v5 roundtrip changed reveal/actor");results.Add("PASS v5 roundtrip");
                p=Ward(4,0,0,6);Require(Mathf.Abs(p.WardProgress-.5f)<.0001f && p.Actor.x==0,"v4 idle progress migration");
                p.Tick(3,Keys());Require(p.WardProgress==.5f && !p.Completed,"v4 migrated save auto advanced");results.Add("PASS v4 idle progress preserved");
                p=Ward(4,8,8,3);Require(Mathf.Abs(p.WardProgress-.25f)<.0001f && p.Actor.x==2,"v4 actor ahead of picture not rebased");
                p.Tick(.1f,Keys(KeyCode.D));Require(p.WardProgress>.25f && !p.Completed,"Migrated actor cannot continue safely");results.Add("PASS v4 ahead actor migration");
                p=Ward(3,2,1,0);Require(p.WardDistance==2 && p.Actor.x==1,"v3 distance changed");results.Add("PASS legacy v3 distance");
                p.PendantHintPlayed=true;copy=new PaintingChapter();copy.RestoreCheckpoint(p.CaptureCheckpoint());Require(copy.PendantHintPlayed,"Discovery cue replay flag lost");results.Add("PASS one-time discovery persists");
                int finishes=0; // Completion must remain latched after repeated movement ticks.
                p=Ward(5,0,0,0);p.Tick(p.WardWalkDistance/p.WardWalkSpeed+.01f,Keys(KeyCode.D));
                if(p.Completed)finishes++;float end=p.WardProgress;p.Tick(100,Keys(KeyCode.A));
                Require(finishes==1 && p.Completed && p.Phase==4 && p.WardProgress==end && end==1,"Completion did not latch");results.Add("PASS same-frame distance completion");
                foreach(string bad in new[]{saved.Replace("\"version\":5","\"version\":99"),"{\"version\":5,\"phase\":3,\"shapes\":[true,true,true,true,true],\"key\":true,\"ward\":-1}"})
                {bool rejected=false;try{new PaintingChapter().RestoreCheckpoint(bad);}catch(InvalidDataException){rejected=true;}Require(rejected,"Invalid checkpoint accepted");}
                results.Add("PASS malformed/unsupported save rejection");
            }
            finally{WBMain.Instance=host;WBMain.IsQA=qa;}
            Directory.CreateDirectory(Output);File.WriteAllLines(Output+"/ward-distance-rules.txt",results);
            return string.Join("\n",results);
        }

        static Texture2D Render(Material material,int width,int height,float progress)
        {
            var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32);
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                material.SetFloat("_ViewportAspect",(float)width/height);material.SetFloat("_Progress",progress);
                Graphics.Blit(material.mainTexture,rt,material);RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();return image;
            }
            catch{UnityEngine.Object.DestroyImmediate(image);throw;}
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
        }
        public static string RunGraphics()
        {
            Directory.CreateDirectory(Output);
            var template=Resources.Load<Material>("WardRuntimeReveal");Require(template!=null,"Missing runtime ward material");
            var mat=new Material(template);WardImageReveal.ConfigureRevealMaterial(mat);
            mat.SetFloat("_SceneBlend",1);mat.SetFloat("_UsePageInk",0);
            var white=mat.GetColor("_WhiteColor");
            // White source makes reveal invisible, so use a dark probe through the actual
            // production shader to measure alpha independently of picture colors.
            var original=mat.mainTexture;mat.mainTexture=Texture2D.blackTexture;
            // The base snow stays monotonic. The requested rolling veil may locally
            // brighten/darken, so validate it separately from this erosion invariant.
            float smoke=mat.GetFloat("_SmokeStrength");mat.SetFloat("_SmokeStrength",0);
            Color[] before=null;int frames=0;
            try
            {
                for(int i=0;i<=40;i++)
                {
                    var im=Render(mat,320,180,i/40f);var pixels=im.GetPixels();
                    if(before!=null)for(int j=0;j<pixels.Length;j++)Require(pixels[j].r<=before[j].r+.012f,"Reveal erased a previously visible pixel");
                    // Sample the v2 image eye region.
                    foreach(var uv in new[]{new Vector2(.494f,.703f),new Vector2(.481f,.702f),new Vector2(.506f,.704f)})
                        Require(im.GetPixel((int)(uv.x*320),(int)(uv.y*180)).r>.90f,"Protected face leaked");
                    // Every pixel above the nose must remain covered, regardless of noise.
                    for(int y=Mathf.CeilToInt(.688f*180);y<180;y++)
                        for(int x=0;x<320;x++)
                            Require(im.GetPixel(x,y).r>.90f,"Nose cutoff leaked");
                    before=pixels;UnityEngine.Object.DestroyImmediate(im);frames++;
                }
                mat.SetFloat("_SmokeStrength",smoke);
                mat.mainTexture=original;
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1280,576),new Vector2Int(960,720)})
                {
                    var im=Render(mat,size.x,size.y,.995f);File.WriteAllBytes(Output+"/ward-final-"+size.x+"x"+size.y+".png",im.EncodeToPNG());UnityEngine.Object.DestroyImmediate(im);
                }
                foreach(float u in new[]{0f,.2f,.38f,.56f,.74f,.83f,.98f})
                {var im=Render(mat,1280,720,u);File.WriteAllBytes(Output+"/ward-stage-"+Mathf.RoundToInt(u*100)+".png",im.EncodeToPNG());UnityEngine.Object.DestroyImmediate(im);}
                // Live ink must follow the same reveal as the paper.
                var opaqueInk=new Texture2D(1,1,TextureFormat.RGBA32,false);
                try
                {
                    opaqueInk.SetPixel(0,0,new Color(0,0,0,1));opaqueInk.Apply();
                    mat.SetFloat("_UsePageInk",1);mat.SetTexture("_PageTex",opaqueInk);
                    var paper=Render(mat,320,180,0);
                    try{Require(paper.GetPixel(160,23).r>.9f,"Live paper ink leaked at zero reveal");}
                    finally{UnityEngine.Object.DestroyImmediate(paper);}
                }
                finally{UnityEngine.Object.DestroyImmediate(opaqueInk);}
                string result="PASS unordered snow reveal: "+frames+" monotonic GPU frames, protected face, full-width nose cutoff, 3 aspect renders, paper/ink covered at zero progress";
                File.WriteAllText(Output+"/ward-graphics.txt",result);return result;
            }
            finally{UnityEngine.Object.DestroyImmediate(mat);}
        }
    }
}
