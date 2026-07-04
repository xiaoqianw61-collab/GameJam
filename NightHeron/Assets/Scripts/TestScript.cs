using System.Linq;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class TestScript : MonoBehaviour
{
    public float angle;
    public float dis;
    
    [Button]
    public void TestMethod()
    {
        var spline = GetComponent<SplineContainer>().Spline;
        var knots = spline.Knots.ToList();
        var knot = knots[1];
        knot.Rotation = Quaternion.Euler(0, 90, 0) * Quaternion.Euler(0, 0, -90) * Quaternion.Euler(0, angle, 0);
        knot.TangentIn = new float3(0, 0, -dis);
        knot.TangentOut = new float3(0, 0, dis);
        knots[1] = knot;
        spline.Knots = knots;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}