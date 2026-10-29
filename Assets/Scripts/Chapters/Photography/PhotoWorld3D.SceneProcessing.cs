using System;
using UnityEngine;

namespace MemoryWhitebox
{
    public sealed partial class PhotoWorld3D
    {
        bool sceneProcessing;
        Vector3 bagHomeScale,tankLidHomePosition;
        Quaternion bagGripHomeRotation,clockHomeRotation;
        Quaternion BagGripRotation=>bagGripHomeRotation*(sceneProcessing?Quaternion.AngleAxis(Photo.BagTurns*360*printObjects.bagGripTurns,printObjects.bagGripAxis):Quaternion.Euler(Photo.BagTurns*720,0,0));

        Transform SceneProcessingProp(string id)
        {
            var prop=printObjects!=null?printObjects.Get(id):null;
            if(prop==null || prop.target==null)throw new InvalidOperationException("HouseScene darkroom reference missing: "+id);
            Nodes[id]=prop.target.gameObject;
            return prop.target;
        }
        void BindSceneProcessing()
        {
            sceneProcessing=true;
            bag=SceneProcessingProp("bag").gameObject;
            bagGrip=SceneProcessingProp("bag-grip");
            tankBody=SceneProcessingProp("tank");
            tankLid=SceneProcessingProp("tank-lid");
            clockHand=SceneProcessingProp("film-clock-hand");
            SceneProcessingProp("water");
            Remember("tank");
            for(int i=0;i<3;i++)
            {
                string id="chemical-"+i;
                chemistry.Add(SceneProcessingProp(id).gameObject);Remember(id);
            }
            // Capture the actual authored pose. Layout anchors never overwrite edited models.
        }
        void CaptureProcessingPose()
        {
            bagHomeScale=bag.transform.localScale;
            bagGripHomeRotation=bagGrip.localRotation;
            tankLidHomePosition=tankLid.localPosition;
            clockHomeRotation=clockHand.localRotation;
        }
        Vector3 ProcessingDragPoint(Ray ray)
        {
            bool authored=sceneProcessing && dragged!=null && (dragged=="tank" || dragged.StartsWith("chemical-"));
            return Point(ray,authored?dragOrigin.y-Origin.y:1.2f);
        }
        void RestoreSceneProcessing()
        {
            if(!sceneProcessing)return;
            foreach(var id in new[]{"tank","chemical-0","chemical-1","chemical-2"})
            {
                if(!Nodes.ContainsKey(id) || Nodes[id]==null)continue;
                var item=Nodes[id].transform;item.localPosition=homes[id];item.localRotation=homeRotations[id];SetColliders(item.gameObject,true);
            }
            if(bag!=null)bag.transform.localScale=bagHomeScale;
            if(bagGrip!=null)bagGrip.localRotation=bagGripHomeRotation;
            if(tankLid!=null)tankLid.localPosition=tankLidHomePosition;
            if(clockHand!=null)clockHand.localRotation=clockHomeRotation;
        }
    }
}
