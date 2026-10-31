using System;
using System.IO;
using System.Linq;
using UnityEngine;
public static class BedroomGarmentEvidence {
 [Serializable] public class Part {public string name;public bool enabled,convex,trigger,positiveBounds,hasUv,penetratesController;public Vector3 min,max;public float penetrationMetres;public int vertices;}
 [Serializable] public class Evidence {public int version=17;public bool passed;public string season,scope="Actual stationary garment MeshColliders and finite same-size capsule pose penetration query (disabled measurement probe only; actual player unchanged). Native inspection cameras are not route evidence; density file is actual arrived Coatrack pose.";public float summerSunIntensity,controllerRadius,controllerHeight,controllerSkin;public Vector3 controllerPosition;public Part[] parts;}
 public static void Write(string folder,CharacterController body) {
  var all=UnityEngine.Object.FindObjectsOfType<MeshCollider>().Where(c=>c.name.StartsWith("Clothing_")).ToArray();
  var probeObject=new GameObject("V17_measurement_capsule_only");probeObject.transform.SetPositionAndRotation(body.transform.position,body.transform.rotation);var probe=probeObject.AddComponent<CapsuleCollider>();probe.enabled=false;probe.radius=body.radius;probe.height=body.height;probe.center=body.center;
  var parts=all.Select(c=>{Vector3 dir;float depth;bool penetrates=Physics.ComputePenetration(probe,body.transform.position,body.transform.rotation,c,c.transform.position,c.transform.rotation,out dir,out depth);return new Part{name=c.name,enabled=c.enabled,convex=c.convex,trigger=c.isTrigger,min=c.bounds.min,max=c.bounds.max,positiveBounds=c.bounds.size.x>0&&c.bounds.size.y>0&&c.bounds.size.z>0,hasUv=c.sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0),vertices=c.sharedMesh.vertexCount,penetratesController=penetrates,penetrationMetres=penetrates?depth:0};}).ToArray();
  string season=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;var sun=GameObject.Find("Window_sun_Summer");
  var e=new Evidence{season=season,parts=parts,summerSunIntensity=sun?sun.GetComponent<Light>().intensity:-1,controllerPosition=body.transform.position,controllerRadius=body.radius,controllerHeight=body.height,controllerSkin=body.skinWidth};
  e.passed=parts.Length==3&&parts.All(p=>p.enabled&&!p.trigger&&p.positiveBounds&&p.hasUv&&p.penetrationMetres<=body.skinWidth+.002f)&&(sun==null||Mathf.Abs(e.summerSunIntensity-1.9f)<.001f);
  UnityEngine.Object.Destroy(probeObject);
  File.WriteAllText(Path.Combine(folder,folder.EndsWith("native")?"garment-"+season.Replace("Bedroom_","")+".json":"garment-colliders.json"),JsonUtility.ToJson(e,true));if(!e.passed){Debug.LogError("V17_GARMENT_COLLIDER_OR_DIRECT_LIGHT_FAIL");Application.Quit(2);}
 }
}
