using UnityEngine;

// The imported rotor pivot retains all five authored blades and its hub as children.
// The guard, stand and controls remain stationary, independent editable objects.
public sealed class SeasonalFanSpin : MonoBehaviour
{
    public float DegreesPerSecond=245;
    public Vector3 WorldAxis=Vector3.forward;
    void Update(){transform.Rotate(WorldAxis,DegreesPerSecond*Time.deltaTime,Space.World);}
}
