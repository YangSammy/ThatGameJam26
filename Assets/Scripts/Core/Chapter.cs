using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed class WBInput
    {
        public readonly HashSet<KeyCode> Keys = new HashSet<KeyCode>();
        public Vector2 Pointer;
        public bool Down;
        public float Time;
    }

    public abstract class Chapter
    {
        public string Id {get;private set;}
        public ChapterDefinition Definition {get;private set;}
        public void BindDefinition(string id,ChapterDefinition definition=null)
        {
            ChapterIds.Index(id);
            if(definition!=null && definition.chapterId!=id)throw new ArgumentException("Chapter definition mismatch.");
            if(!string.IsNullOrEmpty(Id) && Id!=id)throw new InvalidOperationException("Chapter identity cannot change.");
            Id=id;Definition=definition;
        }
        public ChapterSaveRecord CaptureRecord()=>new ChapterSaveRecord{chapterId=Id,completed=Completed,payload=CaptureCheckpoint()};
        public bool Completed { get; protected set; }
        public virtual string Help => "";
        public virtual string DebugState => JsonUtility.ToJson(new State { completed = Completed });
        [Serializable] class State { public bool completed; }
        public virtual void Tick(float dt, WBInput input) { }
        public abstract void Draw();
        public virtual void PointerDown(Vector2 p, int button) { }
        public virtual void PointerMove(Vector2 p) { }
        public virtual void PointerUp(Vector2 p) { }
        public virtual void Wheel(float delta, Vector2 p) { }
        // Return durable nodes only; held controls, animation and drag poses never resume.
        public virtual string CaptureCheckpoint() => "";
        public virtual void RestoreCheckpoint(string json) { }
        public void RestoreStoryCompletion() { Completed=true; }
        protected void Finish()
        {
            if (Completed) return;
            Completed = true;
            if (WBMain.Instance != null) WBMain.Instance.ChapterComplete(this);
        }
    }

    public static class G
    {
        static Texture2D circle;
        static GUIStyle style;
        public static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out Color c); return c; }
        public static bool Hit(Vector2 p, Rect r) => r.Contains(p);
        public static void Box(Rect r, Color fill, Color? stroke = null, float width = 1)
        {
            Color before = GUI.color; GUI.color = fill; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = before;
            if (!stroke.HasValue) return;
            Line(new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),stroke.Value,width);
            Line(new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax),stroke.Value,width);
            Line(new Vector2(r.xMax,r.yMax),new Vector2(r.xMin,r.yMax),stroke.Value,width);
            Line(new Vector2(r.xMin,r.yMax),new Vector2(r.xMin,r.yMin),stroke.Value,width);
        }
        public static void Line(Vector2 a, Vector2 b, Color c, float width = 2)
        {
            var m = GUI.matrix; Color before = GUI.color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);
            GUI.color = c; GUI.DrawTexture(new Rect(a.x,a.y-width*.5f,Vector2.Distance(a,b),width),Texture2D.whiteTexture);
            GUI.matrix = m; GUI.color = before;
        }
        public static void Circle(Vector2 p, float radius, Color c)
        {
            if(circle == null)
            {
                circle = new Texture2D(64,64,TextureFormat.RGBA32,false); circle.hideFlags = HideFlags.HideAndDontSave;
                var pixels = new Color[4096];
                for(int y=0;y<64;y++)for(int x=0;x<64;x++) pixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(32f-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32))));
                circle.SetPixels(pixels); circle.Apply();
            }
            Color old = GUI.color; GUI.color = c; GUI.DrawTexture(new Rect(p.x-radius,p.y-radius,radius*2,radius*2),circle); GUI.color = old;
        }
        public static void Ring(Vector2 p, float radius, float progress, Color c, float width = 3)
        {
            int n=Mathf.Max(1,Mathf.CeilToInt(Mathf.Clamp01(progress)*60));
            for(int i=0;i<n;i++){float a=-Mathf.PI/2+i/60f*Mathf.PI*2,b=-Mathf.PI/2+Mathf.Min((i+1)/60f,progress)*Mathf.PI*2;Line(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,p+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,c,width);}
        }
        public static void Text(string s, Rect r, int size=16, Color? color=null, TextAnchor align=TextAnchor.MiddleCenter)
        {
            if(style==null)style=new GUIStyle(GUI.skin.label);
            style.font=WBMain.UIFont; style.fontSize=size; style.alignment=align; style.wordWrap=true;
            style.normal.textColor=color??C("#435151");GUI.Label(r,s,style);
        }
        public static void Arrow(Vector2 a, Vector2 b, Color c, float width=3)
        {
            Line(a,b,c,width);Vector2 u=(a-b).normalized;Vector2 v=new Vector2(-u.y,u.x);Line(b,b+u*12+v*7,c,width);Line(b,b+u*12-v*7,c,width);
        }
        public static void Person(Vector2 p, float scale, Color c)
        {
            Circle(p+new Vector2(0,-24)*scale,11*scale,c);Line(p+Vector2.down*-12*scale,p+Vector2.down*18*scale,c,5*scale);
            Line(p+Vector2.left*15*scale,p+Vector2.right*15*scale,c,4*scale);Line(p+Vector2.down*18*scale,p+new Vector2(-11,40)*scale,c,4*scale);Line(p+Vector2.down*18*scale,p+new Vector2(11,40)*scale,c,4*scale);
        }
        public static void Icon(int id, Vector2 p, float s, Color c)
        {
            if(id==0){Box(new Rect(p.x-s,p.y-s*.6f,s*2,s*1.2f),Color.clear,c,2);Ring(p+Vector2.left*s*.45f,s*.25f,1,c,2);Ring(p+Vector2.right*s*.45f,s*.25f,1,c,2);}
            else if(id==1){Box(new Rect(p.x-s,p.y-s*.6f,s*2,s*1.3f),Color.clear,c,2);Ring(p,s*.4f,1,c,2);Box(new Rect(p.x-s*.6f,p.y-s*.9f,s*.6f,s*.3f),c);}
            else if(id==2){Line(p+new Vector2(-s*.6f,s*.7f),p+new Vector2(s*.6f,-s*.7f),c,8);Line(p+new Vector2(-s*.6f,s*.7f),p+new Vector2(-s*.8f,s),c,2);}
            else if(id==3){Box(new Rect(p.x-s,p.y-s*.8f,s*2,s*1.6f),Color.clear,c,2);Circle(p+new Vector2(s*.4f,-s*.25f),s*.15f,c);Line(p+new Vector2(-s,s*.6f),p+new Vector2(-s*.1f,-s*.2f),c,2);Line(p+new Vector2(-s*.1f,-s*.2f),p+new Vector2(s*.55f,s*.6f),c,2);}
            else {Line(p+new Vector2(-s*.6f,-s*.8f),p+new Vector2(s*.5f,s*.8f),c,7);Line(p+new Vector2(s*.5f,-s*.4f),p+new Vector2(-s*.4f,s*.9f),c,7);}
        }
        public static void Play(Vector2 p,float s,Color c){Line(p+new Vector2(-s*.4f,-s*.6f),p+new Vector2(s*.6f,0),c,2);Line(p+new Vector2(s*.6f,0),p+new Vector2(-s*.4f,s*.6f),c,2);Line(p+new Vector2(-s*.4f,s*.6f),p+new Vector2(-s*.4f,-s*.6f),c,2);}
        public static void Check(Vector2 p,float s){Line(p+new Vector2(-s*.7f,0),p+new Vector2(-s*.15f,s*.5f),C("#62836d"),4);Line(p+new Vector2(-s*.15f,s*.5f),p+new Vector2(s*.8f,-s*.6f),C("#62836d"),4);}
    }
}
