using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class HouseRadioDeck
    {
        Renderer[] hoveredSurfaces;
        readonly HoverOutline outline = new HoverOutline();
        LineRenderer holeOutline;
        Material holeInk;

        // Called with the exact collider selected for the workbench tooltip.
        public string PresentTapeHover(Collider hit,Vector2 point)
        {
            if(workbenchVisuals==null || Chapter==null || !focused || movingOriginal || draggingTape ||
                benchWaiting || preparationCameraMoving || radioCameraMoving || TapeUIAt(point) ||
                !(WorkbenchMode && house.InputContext.Allows(InputContextId.TapeWorkbench) || RecordingInputReady))
            {ClearTapeHover();return null;}

            if(benchGesture==BenchGesture.MoveStrip)
            {
                SetHoverSurfaces(null);
                int side=LooseStripDropTarget(point);
                if(side<0){HideHole();return PatchLockReason(BenchIndex)??"Aim at a visible, uncovered hole.";}
                ShowHole(tapes[BenchIndex].preparation.pieces[side].transform);
                return side==0?"Release to seal the left hole":"Release to seal the right hole";
            }

            HideHole();
            Renderer[] surfaces=null;
            bool isReel=false;
            if(benchGesture==BenchGesture.None && hit!=null)
            {
                int index=CandidateForHit(hit);
                if(index>=0 && index!=Selected && WorkTapeLockReason(index)==null)
                {
                    var tape=tapes[index];
                    var model=Chapter.Tape.Candidates[index].Broken?workbenchVisuals.broken[index]:workbenchVisuals.intact[index];
                    if(hit==tape.hit || hit==workbenchVisuals.candidateGrips[index])surfaces=model.ShellHover;
                    else if(OnPreparationMat(tape.root))
                    {
                        if(hit==tape.leftSpindleHit || hit==tape.rightSpindleHit)
                        {
                            isReel=true;
                            surfaces=model.ReelHover(hit==tape.leftSpindleHit?0:1);
                        }
                    }
                }
                else if(hit==originalHit && !Chapter.SourceLoaded && !Chapter.Completed && !DeckBusy(0) && !StorageClosed(originalTape))
                    surfaces=workbenchVisuals.sources[CurrentCopy].ShellHover;
            }
            if(surfaces==null && benchGesture==BenchGesture.None && hit)
            {
                if(dualDeck!=null && dualDeck.Command(hit)!=null)
                    surfaces=outline.Surfaces(hit.transform);
                else if(hit==workbenchVisuals.lidHit && !workbenchVisuals.LidBusy)
                    surfaces=outline.Surfaces(hit.transform);
                else if(hit==adhesiveTab && WorkbenchMode && Chapter.AuditionComplete && !Chapter.LooseStripReady)
                    surfaces=outline.Surfaces(workbenchVisuals.head.parent.Find("A05_PullableTail"));
                else if(WorkbenchMode && Chapter.AuditionComplete && Chapter.LooseStripReady && adhesiveStrip && hit.transform==adhesiveStrip && workbenchVisuals.loosePiece)
                    surfaces=outline.Surfaces(workbenchVisuals.loosePiece.transform);
                hoveredSurfaces=surfaces;
                outline.Show(view,surfaces);
            }
            else SetHoverSurfaces(surfaces,isReel);
            return null;
        }

        void SetHoverSurfaces(Renderer[] next,bool isReel=false)
        {
            hoveredSurfaces=next;
            outline.Show(view,next,true,isReel);
        }

        void ShowHole(Transform hole)
        {
            // The imported protection hole is an empty anchor. This rim is visual only.
            if(!holeOutline)
            {
                var go=new GameObject("Eligible protection hole");go.transform.SetParent(transform,false);
                holeOutline=go.AddComponent<LineRenderer>();holeOutline.useWorldSpace=true;holeOutline.loop=true;
                holeOutline.positionCount=4;holeOutline.numCornerVertices=3;
                holeOutline.startWidth=holeOutline.endWidth=.0012f;
                holeInk=new Material(Shader.Find("Unlit/Color")){color=Color.white};holeOutline.sharedMaterial=holeInk;
            }
            var c=hole.position+hole.up*.001f;
            var x=hole.right*.0035f;var z=hole.forward*.0025f;
            holeOutline.SetPosition(0,c-x-z);holeOutline.SetPosition(1,c-x+z);
            holeOutline.SetPosition(2,c+x+z);holeOutline.SetPosition(3,c+x-z);holeOutline.enabled=true;
        }
        void HideHole(){if(holeOutline)holeOutline.enabled=false;}
        public void ClearTapeHover(){SetHoverSurfaces(null);HideHole();}
        void OnDestroy()
        {
            ClearTapeHover();outline.Dispose();
            if(holeInk)Destroy(holeInk);
        }
    }
}
