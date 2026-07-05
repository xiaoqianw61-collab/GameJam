using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.U2D;

[DefaultExecutionOrder(-100)]
public class AnchorManager : MonoBehaviour
{
    public static AnchorManager Instance;
    
    [SerializeField]
    private SplineContainer splineContainer;
    [SerializeField]
    private LineRenderer lineRenderer;
    [SerializeField]
    private Transform startPos;
    [SerializeField]
    private Transform endPos;

    /// <summary>
    /// 所有锚点信息
    /// </summary>
    public List<BezierKnot> AllAnchor => _allAnchor;
    /// <summary>
    /// 剩余锚点数量
    /// </summary>
    public int RemainingAnchorCount => GameState.Instance.config.anchorCount - _usedAnchorCount;
    
    private List<BezierKnot> _allAnchor;
    private List<float> _allAnchorAngle;

    private int _usedAnchorCount;
    
    private bool _isDirty;
    public event Action<int> OnAnchorNumChanged;
    public event Action OnAnchorInfoChanged;

    private static readonly int s_mainTex = Shader.PropertyToID("_MainTex");
    
    private NativeArray<Vector3> _posArr;
    private void Awake()
    {
        Instance = this;
        
        _posArr = new NativeArray<Vector3>(1024, Allocator.Persistent);
        _usedAnchorCount = 0;
        
        _allAnchor = splineContainer.Spline.Knots.ToList();
        _allAnchorAngle = new List<float>();
        for (var i = 0; i < _allAnchor.Count; i++)
        {
            _allAnchorAngle.Add(0);
        }
        var startAnchor = _allAnchor[0];
        var endAnchor = _allAnchor[1];
        startAnchor.Position = startPos.position;
        endAnchor.Position = endPos.position;
        _allAnchor[0] = startAnchor;
        _allAnchor[1] = endAnchor;
        splineContainer.Spline.Knots = _allAnchor;
        splineContainer.Spline.SetTangentMode(0, TangentMode.AutoSmooth);
        splineContainer.Spline.SetTangentMode(1, TangentMode.AutoSmooth);
        
        _isDirty = true;
    }
    private void OnDestroy()
    {
        _posArr.Dispose();
    }
    private void Update()
    {
        lineRenderer.sharedMaterial.SetTextureOffset(s_mainTex, new Vector2(-Time.time, 0));
        if (CanAddAnchor() && Input.GetMouseButtonDown(0) && !UIUtil.IsOverlapUI(Input.mousePosition))
        {
            var template = _allAnchor[_allAnchor.Count - 2];
            template.Position = CameraManager.MouseWorldPos;
            var angle = 0;
            template.Rotation = CalculateAngle(angle);
            template.TangentIn = new float3(0, 0, -1);
            template.TangentOut = new float3(0, 0, 1);
            var insertIndex = _allAnchor.Count - 1;
            _allAnchor.Insert(insertIndex, template);
            _allAnchorAngle.Insert(insertIndex, angle);
            splineContainer.Spline.Knots = _allAnchor;
            _usedAnchorCount++;
            OnAnchorNumChanged?.Invoke(insertIndex);
            SetDirty();
            SoundManager.Instance?.PlayAnchorPlace();
        }

        if (_isDirty)
        {
            _isDirty = false;
            // 更新曲线
            var length = splineContainer.CalculateLength();
            var pointCount = (int) (length / 0.2f) + 1;
            if (_posArr.Length < pointCount)
            {
                _posArr.Dispose();
                _posArr = new NativeArray<Vector3>(pointCount, Allocator.Persistent);
            }
            for (int i = 0; i < pointCount - 1; i++)
            {
                var pos = splineContainer.EvaluatePosition((float) i / (pointCount - 1));
                _posArr[i] = pos;
            }
            _posArr[pointCount - 1] = splineContainer.EvaluatePosition(1);
            lineRenderer.positionCount = pointCount;
            lineRenderer.SetPositions(_posArr.Slice(0, pointCount));
        }
    }

    public BezierKnot GetAnchorInfo(int index, out float angle)
    {
        angle = -_allAnchorAngle[index];
        return _allAnchor[index];
    }
    public void SetAnchorPos(int index, Vector3 pos)
    {
        var knot = _allAnchor[index];
        knot.Position = pos;
        _allAnchor[index] = knot;
        splineContainer.Spline.Knots = _allAnchor;
        OnAnchorInfoChanged?.Invoke();
        SetDirty();
    }
    public void SetAnchorTangent(int index, Vector3 pos, bool isHandle0)
    {
        var knot = _allAnchor[index];
        var delta = pos - (Vector3) knot.Position;
        delta = VectorUtil.VectorRotate(delta, 90);
        var angle = Vector2.SignedAngle(delta, Vector2.up);
        if (!isHandle0) angle += 180;   
        var dis = delta.magnitude / 1.5f;
        knot.Rotation = CalculateAngle(angle);
        knot.TangentIn = new float3(0, 0, -dis);
        knot.TangentOut = new float3(0, 0, dis);
        _allAnchor[index] = knot;
        _allAnchorAngle[index] = angle;
        splineContainer.Spline.Knots = _allAnchor;
        OnAnchorInfoChanged?.Invoke();
        SetDirty();
    }

    public bool CanAddAnchor()
    {
        return _usedAnchorCount < GameState.Instance.config.anchorCount;
    }

    private void SetDirty()
    {
        _isDirty = true;
    }

    private quaternion CalculateAngle(float angle)
    {
        return Quaternion.Euler(0, 90, 0) * Quaternion.Euler(0, 0, -90) * Quaternion.Euler(0, angle, 0);
    }
}
