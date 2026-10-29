using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

namespace MemoryWhitebox.EditorTools
{
    public sealed class InspectionAuthoringWindow : EditorWindow
    {
        [Serializable] sealed class PartRow
        {
            public Transform target;
            public string path,label="部件";
            public SafeInspectionPart.Motion motion;
            public Vector3 axis=Vector3.up,pivot;
            public float range=100,travel=.012f,touchRadius;
            public AudioClip contact;
            public bool remove;
        }
        [SerializeField] SafeInspection viewer;
        [SerializeField] Transform source;
        [SerializeField] SafeInspectionSubject existing;
        [SerializeField] string displayTitle;
        [SerializeField] List<PartRow> parts=new List<PartRow>();
        Vector2 scroll;
        string status;

        [MenuItem("Tools/检视/接入或更新选中物品")]
        public static void Open()
        {
            var window=GetWindow<InspectionAuthoringWindow>("接入／更新检视物品");
            window.minSize=new Vector2(460,520);window.UseSelection();window.Show();
        }
        void UseSelection()
        {
            if(Selection.activeTransform==null){status="请先在 Hierarchy 中选择物品根节点。";return;}
            source=Selection.activeTransform;
            var display=source.GetComponentInParent<SafeInspectionSubject>();
            if(display!=null && display.source!=null)source=display.source;
            viewer=Object.FindObjectsOfType<SafeInspection>(true).FirstOrDefault(v=>v.gameObject.scene==source.gameObject.scene);
            existing=viewer!=null && viewer.subjects!=null?viewer.subjects.FirstOrDefault(s=>s!=null && s.source==source):null;
            if(existing==null && viewer!=null && viewer.subjects!=null)
            {
                existing=viewer.subjects.Where(s=>s!=null && s.source!=null && source.IsChildOf(s.source))
                    .OrderByDescending(s=>Depth(s.source)).FirstOrDefault();
                if(existing!=null)source=existing.source;
            }
            LoadExisting();status=null;
        }
        static int Depth(Transform t){int n=0;while(t!=null){n++;t=t.parent;}return n;}
        static string Path(Transform root,Transform child)
        {
            var names=new List<string>();
            while(child!=null && child!=root){names.Add(child.name);child=child.parent;}
            if(child!=root)return null;names.Reverse();return string.Join("/",names.ToArray());
        }
        static Transform Resolve(Transform root,string path)
        {
            if(root==null || path==null)return null;if(path.Length==0)return root;
            foreach(string name in path.Split('/'))
            {
                Transform match=null;int count=0;
                foreach(Transform child in root)if(child.name==name){match=child;count++;}
                if(count!=1)return null;root=match;
            }
            return root;
        }
        void LoadExisting()
        {
            parts.Clear();displayTitle=existing!=null?existing.title:source!=null?source.name:"";
            if(existing==null || existing.parts==null)return;
            foreach(var p in existing.parts)
            {
                if(p==null)continue;
                var pose=existing.poses!=null?existing.poses.FirstOrDefault(x=>x.display==p.transform):null;
                string path=Path(existing.content,p.transform);
                Transform mapped=pose!=null && pose.source!=null && source!=null && pose.source.IsChildOf(source)?pose.source:Resolve(source,path);
                var touch=p.GetComponent<SphereCollider>();
                parts.Add(new PartRow{target=mapped,path=path,label=p.label,motion=p.motion,axis=p.axis,pivot=p.pivotOffset,
                    range=p.range,travel=p.travel,contact=p.contact,touchRadius=touch!=null?touch.radius*MaxScale(p.transform):0});
            }
        }
        static float MaxScale(Transform t)
        {var s=t.lossyScale;return Mathf.Max(.0001f,Mathf.Max(Mathf.Abs(s.x),Mathf.Max(Mathf.Abs(s.y),Mathf.Abs(s.z))));}

        void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("接入／更新检视物品",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("1. 选择场景中的物品根节点。\n2. 新增时直接接入；替换模型时选择原检视记录。\n3. 检查部件对应关系后点击底部按钮，最后保存场景。\n只复制外观和检视部件，不复制玩法脚本，也不调整关卡操作镜头。",MessageType.Info);
            if(GUILayout.Button("使用 Hierarchy 当前选择"))UseSelection();
            EditorGUI.BeginChangeCheck();
            source=(Transform)EditorGUILayout.ObjectField(new GUIContent("物品根节点","场景中的真实物件。建议保留外层根节点，只替换内部美术模型。"),source,typeof(Transform),true);
            if(EditorGUI.EndChangeCheck())
            {
                if(source!=null)viewer=Object.FindObjectsOfType<SafeInspection>(true).FirstOrDefault(v=>v.gameObject.scene==source.gameObject.scene);
                existing=viewer!=null && viewer.subjects!=null?viewer.subjects.FirstOrDefault(s=>s!=null && s.source==source):null;LoadExisting();
            }
            viewer=(SafeInspection)EditorGUILayout.ObjectField("统一检视组件",viewer,typeof(SafeInspection),true);
            EditorGUI.BeginChangeCheck();
            existing=(SafeInspectionSubject)EditorGUILayout.ObjectField(new GUIContent("原检视记录（可选）","更新时指定原记录；整个旧根节点已替换时，将原展示库中的 Inspect 物体拖到这里。留空表示新增。"),existing,typeof(SafeInspectionSubject),true);
            if(EditorGUI.EndChangeCheck())LoadExisting();
            displayTitle=EditorGUILayout.TextField("检视显示名称",displayTitle);
            EditorGUILayout.HelpBox("自动生成展示副本、绑定原模型、注册列表、计算拾取区域，并接入主房间 E 检视。原物品已有交互碰撞体时优先复用。新摆件不会自动成为左键关卡入口。",MessageType.None);
            EditorGUILayout.Space();EditorGUILayout.LabelField("可操作部件（仅观察外观可留空）",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("更新时按原对象引用或同名层级路径匹配部件。模型改名、层级变化、同名歧义需手动指定；红色缺失项必须重新指定或勾选移除。轴向和铰链位置由模型决定，工具不会猜测。",MessageType.None);
            for(int i=0;i<parts.Count;i++)
            {
                var row=parts[i];EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                row.remove=EditorGUILayout.ToggleLeft("移除此部件",row.remove);
                using(new EditorGUI.DisabledScope(row.remove))
                {
                    if(row.target==null)EditorGUILayout.HelpBox("未匹配部件："+(row.path??row.label),MessageType.Error);
                    row.target=(Transform)EditorGUILayout.ObjectField("对应原模型部件",row.target,typeof(Transform),true);
                    row.label=EditorGUILayout.TextField("玩家看到的名称",row.label);
                    row.motion=(SafeInspectionPart.Motion)EditorGUILayout.Popup("操作方式",(int)row.motion,new[]{"翻盖／铰链","旋钮／轮轴","按压／回弹按钮"});
                    row.axis=EditorGUILayout.Vector3Field("运动轴（父节点坐标）",row.axis);
                    if(row.motion==SafeInspectionPart.Motion.Button)row.travel=EditorGUILayout.FloatField("按压行程（局部单位）",row.travel);
                    else
                    {
                        row.range=EditorGUILayout.FloatField("转动范围（度，可为负）",row.range);
                        row.pivot=EditorGUILayout.Vector3Field("轴心偏移（父节点坐标）",row.pivot);
                    }
                    row.touchRadius=Mathf.Max(0,EditorGUILayout.FloatField(new GUIContent("额外命中半径（米）","0 表示只使用模型网格命中；小按钮可增加少量容错。"),row.touchRadius));
                    row.contact=(AudioClip)EditorGUILayout.ObjectField("触碰音效",row.contact,typeof(AudioClip),false);
                }
                EditorGUILayout.EndVertical();
            }
            if(GUILayout.Button("添加可操作部件"))parts.Add(new PartRow());
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("当前支持 MeshRenderer 网格和 UI 悬停提示。骨骼蒙皮、粒子、线条等渲染组件会明确提示暂不支持。新模型统一 -Z 为正面、+Y 向上；现有类型自动应用观察面规则。",MessageType.None);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
                if(GUILayout.Button(existing!=null?"更新检视副本并重新绑定":"接入检视并自动注册",GUILayout.Height(36)))Apply();
            if(!string.IsNullOrEmpty(status))EditorGUILayout.HelpBox(status,MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        string Preflight()
        {
            if(source==null || viewer==null)return "请选择真实物品和当前场景的统一检视组件。";
            if(EditorUtility.IsPersistent(source) || !source.gameObject.scene.IsValid() || source.gameObject.scene!=viewer.gameObject.scene)return "请使用与统一检视组件同一场景中的物品实例，不要直接选择 Project 中的模型或 Prefab 资产。";
            if(source.GetComponentInParent<SafeInspectionSubject>()!=null)return "请选择真实物品，不要选择展示副本。";
            if(source.GetComponentInChildren<Camera>(true)!=null || source.GetComponentInChildren<SafeInspection>(true)!=null)return "所选根节点包含摄像机或检视系统，请缩小到独立物品根节点。";
            if(source.GetComponentsInChildren<InspectionEntry>(true).Any(e=>e.transform!=source))return "根节点包含另一个已接入物品，请选择独立物品，避免嵌套拾取入口。";
            if(existing!=null && existing.gameObject.scene!=source.gameObject.scene)return "原检视记录必须属于同一场景。";
            var duplicate=viewer.subjects!=null?viewer.subjects.FirstOrDefault(s=>s!=null && s.source==source && s!=existing):null;
            if(duplicate!=null)return "该物品已经注册，请将原检视记录设为 "+duplicate.name+" 后更新，避免重复注册。";
            var renderers=source.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0)return "物品下没有可复制的渲染模型。";
            foreach(var r in renderers)
            {
                if(!(r is MeshRenderer))return "暂不支持 "+r.GetType().Name+"："+r.name+"。请先转换为独立静态网格。";
                var mesh=r.GetComponent<MeshFilter>();
                if(r.GetComponent<TextMesh>()==null && (mesh==null || mesh.sharedMesh==null))return "渲染物体缺少网格："+r.name;
                var scale=r.transform.lossyScale;
                if(Mathf.Abs(scale.x*scale.y*scale.z)<.000000001f)return "模型含零缩放："+r.name;
            }
            var assigned=new HashSet<Transform>();
            foreach(var row in parts.Where(p=>!p.remove))
            {
                if(row.target==null || !row.target.IsChildOf(source))return "请重新指定部件“"+row.label+"”到当前物品内部，或勾选移除。";
                if(!assigned.Add(row.target))return "同一节点只能配置一个检视操作部件："+row.target.name;
                if(row.axis.sqrMagnitude<.000001f)return "部件运动轴不能为零："+row.label;
            }
            return null;
        }

        void Apply()
        {
            string error=Preflight();if(error!=null){status=error;return;}
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("接入或更新检视物品");
            SafeInspectionSubject fresh=null;
            try
            {
                Transform library=existing!=null?existing.transform.parent:null;
                if(library==null && viewer.subjects!=null)library=viewer.subjects.Where(s=>s!=null).Select(s=>s.transform.parent).FirstOrDefault(t=>t!=null);
                if(library==null)
                {
                    var go=new GameObject("Safe inspection - authored display models");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,source.gameObject.scene);
                    Undo.RegisterCreatedObjectUndo(go,"创建检视展示库");library=go.transform;
                }
                fresh=MemoryWhitebox.Editor.SafeInspectionBuild.CreateAuthoredSubject(source,library,string.IsNullOrWhiteSpace(displayTitle)?source.name:displayTitle);
                Undo.RegisterCreatedObjectUndo(fresh.gameObject,"生成检视副本");
                foreach(var row in parts.Where(p=>!p.remove))
                {
                    var pair=fresh.poses.FirstOrDefault(p=>p.source==row.target);
                    if(pair==null)throw new InvalidOperationException("部件没有可用的展示节点："+row.label);
                    var part=Undo.AddComponent<SafeInspectionPart>(pair.display.gameObject);Undo.RecordObject(part,"配置检视部件");part.label=row.label;part.motion=row.motion;part.axis=row.axis;part.pivotOffset=row.pivot;part.range=row.range;part.travel=row.travel;part.contact=row.contact;
                    if(row.touchRadius>0){var touch=Undo.AddComponent<SphereCollider>(pair.display.gameObject);Undo.RecordObject(touch,"配置部件命中区域");touch.radius=row.touchRadius/MaxScale(pair.display);}
                }
                fresh.parts=fresh.GetComponentsInChildren<SafeInspectionPart>(true);fresh.surfaces=fresh.GetComponentsInChildren<Collider>(true);
                var subject=fresh;
                if(existing!=null)
                {
                    var oldSource=existing.source;
                    Undo.RecordObject(existing,"更新检视引用");
                    if(existing.content!=null)Undo.DestroyObjectImmediate(existing.content.gameObject);
                    Undo.SetTransformParent(fresh.content,existing.transform,"替换展示模型");
                    Undo.RecordObject(fresh.content,"设置展示根姿态");fresh.content.localPosition=Vector3.zero;fresh.content.localRotation=Quaternion.identity;fresh.content.localScale=source.lossyScale;
                    existing.title=fresh.title;existing.source=source;existing.displayRoot=existing.transform;existing.content=fresh.content;existing.fixedDevice=false;
                    existing.poses=fresh.poses;existing.renderers=fresh.renderers;existing.parts=fresh.parts;existing.surfaces=fresh.surfaces;existing.entry=fresh.entry;
                    existing.mainFaceFrame=fresh.mainFaceFrame;existing.mainFaceNormal=fresh.mainFaceNormal;existing.mainFaceUp=fresh.mainFaceUp;existing.mainFaceTilt=fresh.mainFaceTilt;
                    subject=existing;
                    if(oldSource!=null && oldSource!=source)
                    {
                        var oldEntry=oldSource.GetComponent<InspectionEntry>();
                        if(oldEntry!=null && oldEntry.subject==existing)
                        {if(oldEntry.pickup!=null && oldEntry.pickup.transform!=oldSource)Undo.DestroyObjectImmediate(oldEntry.pickup.gameObject);Undo.DestroyObjectImmediate(oldEntry);}
                    }
                    Undo.DestroyObjectImmediate(fresh.gameObject);fresh=null;
                }
                subject.RefreshFraming();
                Undo.RecordObject(viewer,"注册检视物品");
                var registered=viewer.subjects!=null?viewer.subjects.Where(s=>s!=null).Distinct().ToList():new List<SafeInspectionSubject>();
                if(!registered.Contains(subject))registered.Add(subject);viewer.subjects=registered.ToArray();
                BindEntry(subject);
                subject.gameObject.SetActive(false);
                EditorUtility.SetDirty(subject);EditorUtility.SetDirty(viewer);EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
                Undo.CollapseUndoOperations(group);existing=subject;LoadExisting();
                status="已接入／更新“"+subject.title+"”。展示副本和 E 检视入口已建立，部件 "+subject.parts.Length+" 个。请保存场景；本操作支持 Ctrl+Z。未运行 Play 或测试。";
                EditorGUIUtility.PingObject(source);
            }
            catch(Exception ex)
            {
                Undo.RevertAllDownToGroup(group);
                status="接入未完成，已撤回本次变更："+ex.Message;
            }
        }
        void BindEntry(SafeInspectionSubject subject)
        {
            var entry=source.GetComponent<InspectionEntry>();if(entry==null)entry=Undo.AddComponent<InspectionEntry>(source.gameObject);
            Undo.RecordObject(entry,"绑定物品检视入口");entry.subject=subject;
            bool hasOtherHit=source.GetComponentsInChildren<Collider>(true).Any(c=>c!=entry.pickup && c.enabled && c.gameObject.activeInHierarchy && c.gameObject.layer==25);
            if(entry.pickup==null && !hasOtherHit)
            {
                var go=new GameObject("Inspection pickup - generated");go.transform.SetParent(source,false);go.layer=25;
                Undo.RegisterCreatedObjectUndo(go,"创建 E 检视拾取区域");entry.pickup=Undo.AddComponent<BoxCollider>(go);entry.pickup.isTrigger=true;
            }
            if(entry.pickup!=null)
            {
                bool first=true;var bounds=new Bounds();
                foreach(var pair in subject.renderers)
                {
                    var r=pair.source;if(r==null || !r.enabled || !r.gameObject.activeInHierarchy)continue;var b=r.bounds;
                    for(int i=0;i<8;i++)
                    {
                        var p=entry.pickup.transform.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                        if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
                    }
                }
                if(first)throw new InvalidOperationException("物品当前没有启用的可见模型，无法生成拾取区域。");
                Undo.RecordObject(entry.pickup,"更新拾取区域");entry.pickup.center=bounds.center;var size=bounds.size;
                entry.pickup.size=new Vector3(Mathf.Max(.005f,size.x),Mathf.Max(.005f,size.y),Mathf.Max(.005f,size.z));EditorUtility.SetDirty(entry.pickup);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(entry);EditorUtility.SetDirty(entry);
        }
    }

    [CustomEditor(typeof(InspectionEntry))]
    public sealed class InspectionEntryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var entry=(InspectionEntry)target;
            EditorGUILayout.HelpBox("此物品已接入统一 E 安全检视。替换内部模型后，使用下方按钮重新生成展示副本和拾取范围；不会改动关卡操作镜头。",MessageType.Info);
            using(new EditorGUI.DisabledScope(true))EditorGUILayout.ObjectField("关联检视记录",entry.subject,typeof(SafeInspectionSubject),true);
            if(GUILayout.Button("接入／更新此物品")){Selection.activeGameObject=entry.gameObject;InspectionAuthoringWindow.Open();}
        }
    }
}
