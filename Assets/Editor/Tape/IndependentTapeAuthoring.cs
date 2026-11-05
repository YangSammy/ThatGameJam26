using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MemoryWhitebox.Editor
{
    public static class IndependentTapeAuthoring
    {
        const int OriginalCount = 3;
        const int OldWorkCount = 3;
        const int WorkCount = 5;
        const string UndoLabel = "Author independent work tapes";
        const string BackupPath = "output/QA/IndependentWorkTapes_20261105/Bedroom_Spring.before.unity";

        // A scene layout pass: cassette geometry and inventory identities stay unchanged.
        public static string ResizeStorage()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Stop Play before resizing storage.");
            var scene=SceneManager.GetSceneByPath(SpringTapeRoom.ScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(SpringTapeRoom.ScenePath,OpenSceneMode.Additive);
            Require(!scene.isDirty,"Spring has unsaved edits.");
            var room=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SpringTapeRoom>(true)).Single();
            var d=room.deck;var w=d.workbenchVisuals;var storage=w.storage.transform;
            Validate(room);
            const string backup="output/QA/RoomierTapeStorage_20261105/Bedroom_Spring.before.unity";
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if(!File.Exists(backup))File.Copy(SpringTapeRoom.ScenePath,backup);
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Resize and separate tape storage");
            try
            {
                Undo.RecordObject(storage,UndoLabel);storage.localScale=Vector3.one*1.3f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(storage);
                var liner=w.storage.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="A01_Liner");
                foreach(var child in storage.GetComponentsInChildren<Transform>(true))
                    if(child.name.StartsWith("A01_RowDivider",StringComparison.Ordinal) || child.name.StartsWith("A01_SlotMarker",StringComparison.Ordinal))
                    {Undo.RecordObject(child.gameObject,UndoLabel);child.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);}
                // Source A and work B occupy opposite sides of the existing centre divider.
                for(int i=0;i<OriginalCount;i++)
                {
                    PositionStoredTape(d.originals[i].tape,w.sources[i],storage.position+storage.right*.068f+storage.forward*((1-i)*.055f),liner.bounds.max.y);
                    SetPose(d.originals[i].home,d.originals[i].tape);
                    SetPose(w.slots[i],d.originals[i].tape);
                }
                for(int i=0;i<WorkCount;i++)
                {
                    PositionStoredTape(d.tapes[i].root,w.intact[i],storage.position-storage.right*.068f+storage.forward*((2-i)*.030f),liner.bounds.max.y);
                    SetPose(w.slots[OriginalCount+i],d.tapes[i].root);
                }
                StorageLabel(storage,"Source lane label","SOURCE A",storage.position+storage.right*.068f+storage.forward*.077f,liner.bounds.max.y);
                StorageLabel(storage,"Work lane label","WORK B",storage.position-storage.right*.068f+storage.forward*.077f,liner.bounds.max.y);
                Undo.RecordObject(d.storageCloseView.transform,UndoLabel);Undo.RecordObject(d.storageCloseView,UndoLabel);
                var eye=storage.position+new Vector3(.12f,.65f,.01f);
                d.storageCloseView.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(storage.position+Vector3.up*.025f-eye));
                d.storageCloseView.fieldOfView=40;
                ValidateStorageClearance(d);
                EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"Could not save storage layout.");
                Undo.CollapseUndoOperations(group);
                return "Storage enlarged 30%; SOURCE A / WORK B separated. Closed-lid, wall, divider and cassette clearance passed.";
            }
            catch {Undo.FlushUndoRecordObjects();Undo.RevertAllDownToGroup(group);throw;}
        }

        static Bounds ShellBounds(WorkbenchAssetView view)
        {
            var shells=view.ShellHover;Require(shells.Length>0,"Cassette shell renderer missing.");
            var bounds=shells[0].bounds;foreach(var shell in shells.Skip(1))bounds.Encapsulate(shell.bounds);return bounds;
        }
        static void PositionStoredTape(Transform tape,WorkbenchAssetView view,Vector3 center,float floor)
        {
            var bounds=ShellBounds(view);center.y=floor+.003f+bounds.extents.y;
            Undo.RecordObject(tape,UndoLabel);tape.position+=center-bounds.center;
            PrefabUtility.RecordPrefabInstancePropertyModifications(tape);
        }
        static void SetPose(Transform anchor,Transform tape)
        {Undo.RecordObject(anchor,UndoLabel);anchor.SetPositionAndRotation(tape.position,tape.rotation);PrefabUtility.RecordPrefabInstancePropertyModifications(anchor);}
        static void StorageLabel(Transform storage,string name,string text,Vector3 position,float floor)
        {
            var child=storage.Find(name);
            if(child==null){child=new GameObject(name).transform;child.SetParent(storage,false);Undo.RegisterCreatedObjectUndo(child.gameObject,UndoLabel);}
            Undo.RecordObject(child,UndoLabel);position.y=floor+.001f;
            child.SetPositionAndRotation(position,Quaternion.LookRotation(Vector3.down,Vector3.left));
            var scale=storage.lossyScale;child.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
            var label=child.GetComponent<TextMesh>();if(label==null)label=Undo.AddComponent<TextMesh>(child.gameObject);
            Undo.RecordObject(label,UndoLabel);label.text=text;label.fontSize=64;label.characterSize=.002f;label.anchor=TextAnchor.MiddleCenter;
            label.color=new Color(.95f,.88f,.66f);label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            child.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
        }

        public static string ValidateStorageClearance(HouseRadioDeck d)
        {
            var w=d.workbenchVisuals;var shells=w.sources.Select(ShellBounds).Concat(w.intact.Select(ShellBounds)).ToArray();
            var structure=w.storage.GetComponentsInChildren<Renderer>(true).Where(r=>r.gameObject.activeInHierarchy &&
                (r.name=="A01_Bottom" || r.name=="A01_Liner" || r.name.StartsWith("A01_Side",StringComparison.Ordinal) ||
                 r.name.StartsWith("A01_End",StringComparison.Ordinal) || r.name=="A01_CenterDivider" || r.name=="A01_Lid" || r.name=="A01_LidInset")).ToArray();
            float minGap=float.MaxValue;
            for(int i=0;i<shells.Length;i++)
            {
                foreach(var part in structure)Require(!shells[i].Intersects(part.bounds),"Cassette "+i+" intersects "+part.name);
                for(int j=i+1;j<shells.Length;j++)Require(!shells[i].Intersects(shells[j]),"Cassette shells overlap.");
                var lid=structure.Single(r=>r.name=="A01_Lid");minGap=Mathf.Min(minGap,lid.bounds.min.y-shells[i].max.y);
                Require(i<OriginalCount?shells[i].max.z<w.storage.transform.position.z:shells[i].min.z>w.storage.transform.position.z,"Cassette crossed its assigned lane.");
            }
            Require(minGap>.005f,"Insufficient closed-lid headroom.");
            return "PASS All eight shells clear box surfaces and each other; separate lanes; minimum closed-lid gap "+(minGap*1000).ToString("F1")+" mm.";
        }

        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play before authoring Spring tapes.");

            var scene = SceneManager.GetSceneByPath(SpringTapeRoom.ScenePath);
            if (!scene.isLoaded)
                scene = EditorSceneManager.OpenScene(SpringTapeRoom.ScenePath, OpenSceneMode.Additive);
            Require(!scene.isDirty, "Spring has unsaved edits. Save or discard them before authoring tapes.");

            var room = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SpringTapeRoom>(true)).Single();
            var deck = room.deck;
            var visuals = deck.workbenchVisuals;
            if (deck.tapes.Length == WorkCount)
            {
                Validate(room);
                return "Spring already has five independent work tapes. Authored positions retained.";
            }

            Require(deck.tapes.Length == OldWorkCount &&
                    deck.originals.Length == OriginalCount &&
                    deck.storageDeskLandings.Length == OriginalCount + OldWorkCount &&
                    visuals.intact.Length == OldWorkCount && visuals.broken.Length == OldWorkCount &&
                    visuals.candidateGrips.Length == OldWorkCount && visuals.slots.Length == 6 &&
                    deck.completedCopies.Length == OriginalCount,
                "Spring does not match the three-work-tape scene expected by this command.");
            Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
            if (!File.Exists(BackupPath))
                File.Copy(SpringTapeRoom.ScenePath, BackupPath);

            var chapter = deck.GetComponent<ChapterSceneBinding>();
            var template = deck.tapes[OldWorkCount - 1];
            var templateSubject = room.inspectionSubjects.Single(subject => subject.source == template.root);
            var templateGrip = visuals.candidateGrips[OldWorkCount - 1];

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            Undo.RegisterCompleteObjectUndo(new Object[] { room, room.workArea, deck, visuals, chapter }, UndoLabel);
            try
            {
                Array.Resize(ref deck.tapes, WorkCount);
                Array.Resize(ref visuals.intact, WorkCount);
                Array.Resize(ref visuals.broken, WorkCount);
                Array.Resize(ref visuals.candidateGrips, WorkCount);
                Array.Resize(ref deck.storageDeskLandings, OriginalCount + WorkCount);
                var subjects = new List<SafeInspectionSubject>(room.inspectionSubjects);
                var members = new List<OperationArea.Member>(room.workArea.members);
                var chapterItems = new List<ItemSceneBinding>(chapter.items);
                for (var i = OldWorkCount; i < WorkCount; i++)
                {
                    var number = i + 1;
                    var tapeRoot = Duplicate(template.root, "Tape " + number);
                    var links = new Dictionary<Object, Object>();
                    MapHierarchy(template.root, tapeRoot, links);
                    var tape = deck.tapes[i] = new HouseRadioDeck.Cassette
                    {
                        root = tapeRoot,
                        model = Mapped(template.model, links),
                        leftReel = Mapped(template.leftReel, links),
                        rightReel = Mapped(template.rightReel, links),
                        hit = Mapped(template.hit, links),
                        audio = template.audio,
                        label = "Tape " + number,
                        leftSpindleHit = Mapped(template.leftSpindleHit, links),
                        rightSpindleHit = Mapped(template.rightSpindleHit, links),
                        damageMarker = Mapped(template.damageMarker, links),
                        preparation = Mapped(template.preparation, links)
                    };
                    visuals.intact[i] = Mapped(visuals.intact[OldWorkCount - 1], links);
                    visuals.broken[i] = Mapped(visuals.broken[OldWorkCount - 1], links);
                    visuals.candidateGrips[i] = Mapped(templateGrip, links);
                    visuals.legacyRenderers = visuals.legacyRenderers.Concat(
                        visuals.legacyRenderers.Where(renderer => renderer != null && links.ContainsKey(renderer))
                            .Select(renderer => Mapped(renderer, links))).ToArray();
                    visuals.legacyColliders = visuals.legacyColliders.Concat(
                        visuals.legacyColliders.Where(collider => collider != null && links.ContainsKey(collider))
                            .Select(collider => Mapped(collider, links))).ToArray();

                    var subjectRoot = Duplicate(templateSubject.transform, "Inspect - Tape " + number);
                    var subject = subjectRoot.GetComponent<SafeInspectionSubject>();
                    RemapReferences(subjectRoot, links);
                    subject.title = tape.label;
                    var entry = tapeRoot.GetComponent<InspectionEntry>();
                    if (entry != null)
                        entry.subject = subject;
                    subjects.Add(subject);

                    var binding = tapeRoot.GetComponent<ItemSceneBinding>();
                    binding.instanceId = "tape.work." + number;
                    binding.hitSurfaces = new[] { tape.hit, tape.leftSpindleHit, tape.rightSpindleHit, visuals.candidateGrips[i] };
                    members.Add(new OperationArea.Member { item = tapeRoot, itemId = binding.instanceId });
                    chapterItems.Add(binding);
                }

                room.inspectionSubjects = subjects.ToArray();
                room.workArea.members = members.ToArray();
                chapter.items = chapterItems.ToArray();
                LayoutStorage(deck);
                LayoutDesk(room);
                Validate(room);
                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene), "Unity could not save the Spring scene.");
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }

            return "Spring authored with five independent work tapes, eight desk landings and three archive slots. Backup " +
                Path.GetFullPath(BackupPath);
        }

        static void LayoutStorage(HouseRadioDeck deck)
        {
            var visuals = deck.workbenchVisuals;
            var first = deck.tapes[0].root.position;
            var last = deck.completedCopies[0].transform.position;
            last.y = first.y;
            var storage = visuals.storage.transform;
            var slots = Anchor(storage, "IndependentWorkTapeSlots", first, deck.tapes[0].root.rotation);

            // The two fixed dividers in the work lane cross the new positions.
            // The centre divider and the entire source lane stay where they were.
            var sourceLane = deck.originals[0].tape.position;
            foreach (var divider in storage.GetComponentsInChildren<Transform>(true)
                         .Where(child => child.name.StartsWith("A01_RowDivider", StringComparison.Ordinal)))
            {
                if (Mathf.Abs(divider.position.z - first.z) >= Mathf.Abs(divider.position.z - sourceLane.z))
                    continue;
                Undo.RecordObject(divider.gameObject, UndoLabel);
                divider.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(divider.gameObject);
            }

            var workSlots = new Transform[WorkCount];
            for (var i = 0; i < WorkCount; i++)
            {
                var tape = deck.tapes[i].root;
                var position = Vector3.Lerp(first, last, (float)i / (WorkCount - 1));
                position.y = tape.position.y;
                workSlots[i] = Anchor(slots, "WorkTapeSlot_" + (i + 1), position, tape.rotation);
                Undo.RecordObject(tape, UndoLabel);
                tape.position = position;
                var binding = tape.GetComponent<ItemSceneBinding>();
                Undo.RecordObject(binding, UndoLabel);
                binding.homeAnchor = workSlots[i];
            }
            visuals.slots = visuals.slots.Take(OriginalCount).Concat(workSlots).ToArray();

            // A steeper view keeps the low grip on an intact cassette visible
            // beyond the higher grip of the adjacent suspect cassette.
            var close = deck.storageCloseView.transform;
            var focus = storage.position + Vector3.up * .06f;
            var offset = close.position - focus;
            var vertical = Vector3.Project(offset, Vector3.up);
            var aboveBox = focus + vertical + (offset - vertical) * .36f;
            Undo.RecordObject(close, UndoLabel);
            close.SetPositionAndRotation(aboveBox, Quaternion.LookRotation(focus - aboveBox, Vector3.up));
        }

        static void LayoutDesk(SpringTapeRoom room)
        {
            var deck = room.deck;
            var desk = deck.storageDeskLandings;
            var left = desk[OriginalCount];
            var right = desk[OriginalCount + OldWorkCount - 1];
            var row = (right.position - left.position).normalized;
            var awayFromEquipment = Vector3.Cross(row, room.workArea.Normal).normalized;
            var sourceToWork = left.position - desk[0].position;
            if (Vector3.Dot(awayFromEquipment, sourceToWork) < 0)
                awayFromEquipment = -awayFromEquipment;

            var center = Vector3.Lerp(left.position, right.position, .5f);
            desk[6] = Anchor(left.parent, "WorkTapeDesk_4", center + awayFromEquipment * .13f, right.rotation);
            desk[7] = Anchor(left.parent, "WorkTapeDesk_5", right.position + awayFromEquipment * .13f, right.rotation);

            var archivePosition = center + awayFromEquipment * .27f;
            archivePosition.y = room.workArea.Surface.y;
            var archive = Anchor(left.parent, "Finished Tape Rack", archivePosition, Quaternion.LookRotation(awayFromEquipment, room.workArea.Normal));
            var visuals = deck.workbenchVisuals;
            var oldTrigger = visuals.finishedHit;
            Undo.RecordObject(oldTrigger, UndoLabel);
            oldTrigger.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(oldTrigger);
            var basePlate = RackPiece(archive, "Submit finished copy", .3f, .115f, .008f,
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Props/TapeWorkbenchV1/Materials/TW_Edge.mat"));
            basePlate.layer = oldTrigger.gameObject.layer;
            visuals.finishedHit = basePlate.GetComponent<BoxCollider>();
            visuals.finishedHit.isTrigger = true;
            var label = Undo.AddComponent<WorldTooltipLabel>(basePlate);
            label.text = "Finished copies · Submit recording";
            label.target = basePlate.transform;

            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Props/TapeWorkbenchV1/Materials/TW_Charcoal.mat");
            deck.finishedTapeAnchors = new Transform[OriginalCount];
            var template = deck.tapes[OldWorkCount - 1].root;
            var home = template.position;
            var homeRotation = template.rotation;
            var spacing = Vector3.Distance(left.position, right.position) * .5f;
            try
            {
                template.SetPositionAndRotation(archivePosition, right.rotation);
                var renderers = visuals.intact[OldWorkCount - 1].GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer.enabled).ToArray();
                var lift = archivePosition.y - renderers.Min(renderer => renderer.bounds.min.y);
                for (var i = 0; i < OriginalCount; i++)
                {
                    var slot = RackPiece(archive, "Archive slot " + (i + 1), .087f, .101f, .002f, mat);
                    slot.transform.position += row * ((i - 1) * spacing) + Vector3.up * .008f;
                    Undo.DestroyObjectImmediate(slot.GetComponent<Collider>());
                    var position = slot.transform.position + Vector3.up * (.003f + lift);
                    deck.finishedTapeAnchors[i] = Anchor(archive, "FinishedTape_" + (i + 1), position, right.rotation);
                    Undo.RecordObject(deck.completedCopies[i], UndoLabel);
                    deck.completedCopies[i].SetActive(false);
                }
            }
            finally
            {
                template.SetPositionAndRotation(home, homeRotation);
            }
        }

        static GameObject RackPiece(Transform parent, string name, float width, float depth, float height, Material mat)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.position = parent.position + Vector3.up * (height * .5f);
            var scale = parent.lossyScale;
            piece.transform.localScale = new Vector3(width / scale.x, height / scale.y, depth / scale.z);
            piece.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Undo.RegisterCreatedObjectUndo(piece, UndoLabel);
            return piece;
        }

        static Transform Anchor(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.SetPositionAndRotation(position, rotation);
            Undo.RegisterCreatedObjectUndo(anchor.gameObject, UndoLabel);
            return anchor;
        }

        static Transform Duplicate(Transform template, string name)
        {
            var clone = Object.Instantiate(template.gameObject, template.parent, false);
            clone.name = name;
            Undo.RegisterCreatedObjectUndo(clone, UndoLabel);
            return clone.transform;
        }

        static void MapHierarchy(Transform source, Transform clone, IDictionary<Object, Object> links)
        {
            links.Add(source.gameObject, clone.gameObject);
            var sourceComponents = source.GetComponents<Component>();
            var cloneComponents = clone.GetComponents<Component>();
            Require(source.childCount == clone.childCount && sourceComponents.Length == cloneComponents.Length,
                "Tape clone differs from its template hierarchy.");
            for (var i = 0; i < sourceComponents.Length; i++)
            {
                if (sourceComponents[i] == null)
                    continue;
                Require(cloneComponents[i] != null && sourceComponents[i].GetType() == cloneComponents[i].GetType(),
                    "Tape clone component order differs from its template.");
                links.Add(sourceComponents[i], cloneComponents[i]);
            }
            for (var i = 0; i < source.childCount; i++)
                MapHierarchy(source.GetChild(i), clone.GetChild(i), links);
        }

        static T Mapped<T>(T reference, IDictionary<Object, Object> links) where T : Object =>
            reference == null ? null : (T)links[reference];

        static void RemapReferences(Transform root, IDictionary<Object, Object> links)
        {
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null)
                    continue;
                using var serialized = new SerializedObject(component);
                using var property = serialized.GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null &&
                        links.TryGetValue(property.objectReferenceValue, out var replacement))
                        property.objectReferenceValue = replacement;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void Validate(SpringTapeRoom room)
        {
            var deck = room.deck;
            var visuals = deck.workbenchVisuals;
            Require(deck.tapes.Length == WorkCount && visuals.intact.Length == WorkCount && visuals.broken.Length == WorkCount &&
                    visuals.candidateGrips.Length == WorkCount && visuals.slots.Length == OriginalCount + WorkCount &&
                    deck.storageDeskLandings.Length == OriginalCount + WorkCount && deck.storageDeskLandings.All(landing => landing != null) &&
                    deck.finishedTapeAnchors.Length == OriginalCount && deck.finishedTapeAnchors.All(anchor => anchor != null) &&
                    deck.finishedTapeAnchors.Distinct().Count() == OriginalCount && visuals.finishedHit != null && visuals.finishedHit.enabled,
                "Spring tape array or archive references are incomplete.");
            Require(deck.tapes.Select(tape => tape.root).Distinct().Count() == WorkCount, "Work tapes must have separate roots.");
            for (var i = 0; i < WorkCount; i++)
            {
                var tape = deck.tapes[i];
                var binding = tape.root.GetComponent<ItemSceneBinding>();
                var subject = room.inspectionSubjects.Single(candidate => candidate.source == tape.root);
                Require(binding != null && binding.instanceId == "tape.work." + (i + 1) &&
                        tape.root.IsChildOf(room.workspace.transform) &&
                        room.workArea.members.Count(member => member.item == tape.root && member.itemId == binding.instanceId) == 1,
                    "Missing stable work-tape registration for " + tape.root.name);
                Require(new Component[] { tape.model, tape.leftReel, tape.rightReel, tape.hit, tape.leftSpindleHit, tape.rightSpindleHit,
                            tape.preparation, visuals.intact[i], visuals.broken[i], visuals.candidateGrips[i] }
                        .All(component => component != null && component.transform.IsChildOf(tape.root)) &&
                        subject.mainFaceFrame.IsChildOf(tape.root) &&
                        subject.poses.All(pair => pair.source != null && pair.source.IsChildOf(tape.root)) &&
                        subject.renderers.All(pair => pair.source != null && pair.source.transform.IsChildOf(tape.root)),
                    "Inspection references must resolve inside " + tape.root.name);
            }
        }

        static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
