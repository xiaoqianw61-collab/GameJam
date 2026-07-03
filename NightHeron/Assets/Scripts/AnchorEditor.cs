using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 锚点编辑器 - 设计模式
/// 左键空白：放置锚点（需向 GameManager 申请库存）
/// 左键锚点：选中并拖拽移动
/// 左键蓝色手柄：拖拽调整曲线（镜像对称）
/// 退格：撤销最后一个锚点（归还库存给 GameManager）
/// 每段使用独立的 Cubic Bezier 曲线
/// </summary>
public class AnchorEditor : MonoBehaviour
{
    [System.Serializable]
    public class AnchorData
    {
        public Vector3 position;
        public Vector3 handleIn;  // 相对锚点位置，控制进入切线
        public Vector3 handleOut; // 相对锚点位置，控制离开切线

        public Vector3 HandleInWorld => position + handleIn;
        public Vector3 HandleOutWorld => position + handleOut;
    }

    [Header("锚点设置")]
    public float anchorRadius = 0.35f;
    public Color anchorColor = new Color(1f, 0.85f, 0.2f);
    public Color selectedAnchorColor = new Color(1f, 0.55f, 0.2f);
    public Color pathColor = new Color(1f, 1f, 1f, 0.5f);
    public int curveSegments = 20;

    [Header("手柄")]
    public Color handleColor = new Color(0.35f, 0.65f, 1f);
    public Color handleOutlineColor = new Color(0f, 0f, 0f, 0.8f); // 描边颜色（深色）
    public float handleLineWidth = 0.04f;
    public float handleOutlineWidth = 0.08f; // 描边宽度
    public float handleHitRadius = 0.5f;
    public float defaultHandleRatio = 0.3f; // 默认手柄长度占段长的比例

    [Header("起点/终点")]
    public Vector3 startPos = new Vector3(-10f, 4f, 0f);
    public Vector3 endPos = new Vector3(20f, 4f, 0f);

    [Header("UI")]
    public Text anchorCountText;
    public List<Image> anchorStockImages;

    /// <summary>是否处于编辑模式（false = 游戏开始，锁定编辑）</summary>
    public bool isEditing = true;

    private List<AnchorData> anchors = new List<AnchorData>();
    private List<GameObject> anchorGizmos = new List<GameObject>();
    private List<SpriteRenderer> anchorRenderers = new List<SpriteRenderer>();

    /// <summary>最新生成的曲线路径点（供 PlayerBird 飞行用）</summary>
    private List<Vector3> cachedCurvePath = new List<Vector3>();

    private LineRenderer pathLine;
    private Camera mainCam;

    // 手柄可视化
    private GameObject handleInGizmo;
    private GameObject handleOutGizmo;
    private LineRenderer handleInLine;
    private LineRenderer handleOutLine;
    private LineRenderer handleInLineOutline;
    private LineRenderer handleOutLineOutline;

    // 选中与拖拽
    private int selectedAnchorIndex = -1;
    private enum DragMode { None, Anchor, HandleIn, HandleOut }
    private DragMode dragMode = DragMode.None;
    private int dragAnchorIndex = -1;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null) return;

        CreatePathLine();
        CreateHandleVisuals();
        UpdatePathLine();
        UpdateInstructionText();
        UpdateAnchorStockUI();
    }

    void Update()
    {
        HandleInput();
    }

    /// <summary>获取当前贝塞尔曲线路径点列表</summary>
    public List<Vector3> GetCurvePath()
    {
        return new List<Vector3>(cachedCurvePath);
    }

    void HandleInput()
    {
        if (!isEditing) return;
        Vector3 worldPos = GetMouseWorldPos();

        if (Input.GetMouseButtonDown(0))
        {
            // 优先级1：当前选中锚点的手柄（只有已激活手柄时才能拖拽）
            if (selectedAnchorIndex >= 0)
            {
                var selAnchor = anchors[selectedAnchorIndex];
                if (selAnchor.handleIn.magnitude > 0.01f && IsNearHandle(worldPos, selectedAnchorIndex, true))
                {
                    dragMode = DragMode.HandleIn;
                    dragAnchorIndex = selectedAnchorIndex;
                    return;
                }
                if (selAnchor.handleOut.magnitude > 0.01f && IsNearHandle(worldPos, selectedAnchorIndex, false))
                {
                    dragMode = DragMode.HandleOut;
                    dragAnchorIndex = selectedAnchorIndex;
                    return;
                }
            }

            // 优先级2：锚点本体
            int hit = FindAnchorAt(worldPos);
            if (hit >= 0)
            {
                var anchor = anchors[hit];
                bool handlesActive = anchor.handleIn.magnitude > 0.01f || anchor.handleOut.magnitude > 0.01f;

                SelectAnchor(hit);

                if (handlesActive)
                {
                    // 已激活手柄 → 点击锚点本体 = 移动锚点
                    dragMode = DragMode.Anchor;
                    dragAnchorIndex = hit;
                }
                else
                {
                    // 首次点击 → 自动激活默认手柄，不进入拖拽模式
                    ActivateDefaultHandles(hit);
                    // 不设置 dragMode，用户接下来可以拖蓝色圆点调整曲线
                }
                return;
            }

            // 优先级3：空白区域（向 GameManager 申请锚点库存）
            if (GameManager.Instance != null && GameManager.Instance.CanPlaceAnchor())
            {
                AddAnchor(worldPos);
            }
            else
            {
                SelectAnchor(-1); // 取消选中
            }
        }

        if (Input.GetMouseButton(0) && dragMode != DragMode.None && dragAnchorIndex >= 0)
        {
            var anchor = anchors[dragAnchorIndex];

            switch (dragMode)
            {
                case DragMode.Anchor:
                    anchor.position = worldPos;
                    UpdateAnchorGizmo(dragAnchorIndex);
                    break;

                case DragMode.HandleOut:
                    anchor.handleOut = worldPos - anchor.position;
                    // 镜像：进入手柄与离开手柄共线反向，保持平滑
                    anchor.handleIn = -anchor.handleOut.normalized * anchor.handleOut.magnitude;
                    break;

                case DragMode.HandleIn:
                    anchor.handleIn = worldPos - anchor.position;
                    anchor.handleOut = -anchor.handleIn.normalized * anchor.handleIn.magnitude;
                    break;
            }

            UpdateHandleVisuals();
            UpdatePathLine();
            UpdateInstructionText();
        }

        if (Input.GetMouseButtonUp(0))
        {
            dragMode = DragMode.None;
            dragAnchorIndex = -1;
        }

        // Backspace 撤销最后一个锚点
        if (Input.GetKeyDown(KeyCode.Backspace) && anchors.Count > 0)
        {
            if (selectedAnchorIndex == anchors.Count - 1) SelectAnchor(-1);
            RemoveLastAnchor();
        }
    }

    void SelectAnchor(int index)
    {
        // 恢复旧选中锚点颜色
        if (selectedAnchorIndex >= 0 && selectedAnchorIndex < anchorRenderers.Count)
            anchorRenderers[selectedAnchorIndex].color = anchorColor;

        selectedAnchorIndex = index;

        // 高亮新选中锚点
        if (selectedAnchorIndex >= 0 && selectedAnchorIndex < anchorRenderers.Count)
            anchorRenderers[selectedAnchorIndex].color = selectedAnchorColor;

        UpdateHandleVisuals();
    }

    /// <summary>
    /// 为指定锚点激活默认手柄（计算朝向下一锚点的方向）
    /// </summary>
    void ActivateDefaultHandles(int index)
    {
        if (index < 0 || index >= anchors.Count) return;
        var anchor = anchors[index];

        Vector3 prevPoint = index == 0 ? startPos : anchors[index - 1].position;
        Vector3 nextPoint = index == anchors.Count - 1 ? endPos : anchors[index + 1].position;
        Vector3 dir = (nextPoint - prevPoint).normalized;
        if (dir.magnitude < 0.001f) dir = Vector3.right;

        float segLen = Vector3.Distance(prevPoint, nextPoint);
        float handleLen = segLen * defaultHandleRatio;
        anchor.handleOut = dir * handleLen;
        anchor.handleIn = -dir * handleLen;

        UpdateHandleVisuals();
        UpdatePathLine();
        UpdateAnchorStockUI();
    }

    /// <summary>
    /// 更新底部锚点库存方块（通过 GameManager 获取剩余数量）
    /// </summary>
    void UpdateAnchorStockUI()
    {
        if (anchorStockImages == null || GameManager.Instance == null) return;
        int remaining = GameManager.Instance.RemainingAnchors;
        for (int i = 0; i < anchorStockImages.Count; i++)
        {
            bool visible = i < remaining;
            anchorStockImages[i].gameObject.SetActive(visible);
        }
    }

    bool IsNearHandle(Vector3 pos, int anchorIdx, bool isHandleIn)
    {
        if (anchorIdx < 0 || anchorIdx >= anchors.Count) return false;
        var anchor = anchors[anchorIdx];
        Vector3 handlePos = isHandleIn ? anchor.HandleInWorld : anchor.HandleOutWorld;
        return Vector3.Distance(pos, handlePos) < handleHitRadius;
    }

    void AddAnchor(Vector3 pos)
    {
        // 向 GameManager 消耗一个锚点库存
        if (GameManager.Instance != null && !GameManager.Instance.TryUseAnchor())
            return;

        var anchor = new AnchorData();
        anchor.position = pos;

        anchor.handleOut = Vector3.zero;
        anchor.handleIn = Vector3.zero;

        anchors.Add(anchor);
        CreateAnchorGizmo(pos, anchors.Count - 1);
        SelectAnchor(anchors.Count - 1);
        UpdatePathLine();
        UpdateInstructionText();
        UpdateAnchorStockUI();
    }

    void RemoveLastAnchor()
    {
        int idx = anchors.Count - 1;
        if (idx < anchorGizmos.Count && anchorGizmos[idx] != null)
            Destroy(anchorGizmos[idx]);
        if (idx < anchorGizmos.Count) anchorGizmos.RemoveAt(idx);
        if (idx < anchorRenderers.Count) anchorRenderers.RemoveAt(idx);
        anchors.RemoveAt(idx);

        // 归还锚点库存给 GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnAnchor();

        UpdatePathLine();
        UpdateInstructionText();
        UpdateAnchorStockUI();
    }

    int FindAnchorAt(Vector3 pos)
    {
        for (int i = 0; i < anchors.Count; i++)
        {
            if (Vector3.Distance(pos, anchors[i].position) < 0.7f)
                return i;
        }
        return -1;
    }

    void CreateAnchorGizmo(Vector3 pos, int index)
    {
        var go = new GameObject("Anchor_" + index);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(24, Color.white);
        sr.color = anchorColor;
        sr.sortingOrder = 10;
        anchorRenderers.Add(sr);

        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(1.2f, 1.2f);
        bc.isTrigger = true;

        // 外圈光环
        var ring = new GameObject("Ring");
        ring.transform.SetParent(go.transform);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localScale = Vector3.one * 1.5f;
        var ringSr = ring.AddComponent<SpriteRenderer>();
        ringSr.sprite = CreateCircleSprite(24, Color.white);
        ringSr.color = new Color(1f, 0.85f, 0.2f, 0.3f);
        ringSr.sortingOrder = 9;

        // 序号标签
        var label = new GameObject("Label");
        label.transform.SetParent(go.transform);
        label.transform.localPosition = new Vector3(0, 0.5f, 0);
        var labelSr = label.AddComponent<SpriteRenderer>();
        labelSr.sprite = CreateNumberSprite(index + 1);
        labelSr.color = Color.white;
        labelSr.sortingOrder = 11;

        anchorGizmos.Add(go);
    }

    void UpdateAnchorGizmo(int idx)
    {
        if (idx >= 0 && idx < anchorGizmos.Count && anchorGizmos[idx] != null)
            anchorGizmos[idx].transform.position = anchors[idx].position;
    }

    void CreatePathLine()
    {
        var go = new GameObject("PathLine");
        pathLine = go.AddComponent<LineRenderer>();
        pathLine.startWidth = 0.08f;
        pathLine.endWidth = 0.08f;
        pathLine.material = new Material(Shader.Find("Sprites/Default"));
        pathLine.startColor = pathColor;
        pathLine.endColor = pathColor;
        pathLine.sortingOrder = 5;
        pathLine.positionCount = 0;
    }

    void CreateHandleVisuals()
    {
        handleInGizmo = CreateHandleGizmo("HandleIn");
        handleOutGizmo = CreateHandleGizmo("HandleOut");
        handleInLine = CreateHandleLine("HandleInLine", handleColor, handleLineWidth, 11);
        handleOutLine = CreateHandleLine("HandleOutLine", handleColor, handleLineWidth, 11);
        // 描边层：深色粗线，在蓝色线下面
        handleInLineOutline = CreateHandleLine("HandleInLineOutline", handleOutlineColor, handleOutlineWidth, 10);
        handleOutLineOutline = CreateHandleLine("HandleOutLineOutline", handleOutlineColor, handleOutlineWidth, 10);
        SetHandlesVisible(false);
    }

    GameObject CreateHandleGizmo(string name)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(8, Color.white);
        sr.color = handleColor;
        sr.sortingOrder = 12;

        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(0.6f, 0.6f);
        bc.isTrigger = true;
        return go;
    }

    LineRenderer CreateHandleLine(string name, Color color, float width, int sortingOrder)
    {
        var go = new GameObject(name);
        var lr = go.AddComponent<LineRenderer>();
        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = sortingOrder;
        lr.positionCount = 2;
        return lr;
    }

    void SetHandlesVisible(bool visible)
    {
        if (handleInGizmo != null) handleInGizmo.SetActive(visible);
        if (handleOutGizmo != null) handleOutGizmo.SetActive(visible);
        if (handleInLine != null) handleInLine.gameObject.SetActive(visible);
        if (handleOutLine != null) handleOutLine.gameObject.SetActive(visible);
        if (handleInLineOutline != null) handleInLineOutline.gameObject.SetActive(visible);
        if (handleOutLineOutline != null) handleOutLineOutline.gameObject.SetActive(visible);
    }

    void UpdateHandleVisuals()
    {
        if (selectedAnchorIndex < 0 || selectedAnchorIndex >= anchors.Count)
        {
            SetHandlesVisible(false);
            return;
        }

        var anchor = anchors[selectedAnchorIndex];
        Vector3 hIn = anchor.HandleInWorld;
        Vector3 hOut = anchor.HandleOutWorld;
        bool inActive = anchor.handleIn.magnitude > 0.01f;
        bool outActive = anchor.handleOut.magnitude > 0.01f;

        // 只显示非零长度的手柄
        handleInGizmo.SetActive(inActive);
        handleInLine.gameObject.SetActive(inActive);
        handleInLineOutline.gameObject.SetActive(inActive);

        handleOutGizmo.SetActive(outActive);
        handleOutLine.gameObject.SetActive(outActive);
        handleOutLineOutline.gameObject.SetActive(outActive);

        if (inActive)
        {
            handleInGizmo.transform.position = hIn;
            handleInLine.SetPosition(0, anchor.position);
            handleInLine.SetPosition(1, hIn);
            handleInLineOutline.SetPosition(0, anchor.position);
            handleInLineOutline.SetPosition(1, hIn);
        }

        if (outActive)
        {
            handleOutGizmo.transform.position = hOut;
            handleOutLine.SetPosition(0, anchor.position);
            handleOutLine.SetPosition(1, hOut);
            handleOutLineOutline.SetPosition(0, anchor.position);
            handleOutLineOutline.SetPosition(1, hOut);
        }
    }

    void UpdatePathLine()
    {
        List<Vector3> allPoints = new List<Vector3> { startPos };
        foreach (var a in anchors) allPoints.Add(a.position);
        allPoints.Add(endPos);

        int segCount = allPoints.Count - 1;

        List<Vector3> curvePoints = new List<Vector3>();

        for (int i = 0; i < segCount; i++)
        {
            Vector3 p0 = allPoints[i];
            Vector3 p1 = allPoints[i + 1];
            Vector3 c0, c1;

            if (i == 0)
            {
                // 起点 → 第一个锚点（无锚点时直接连到终点）
                c0 = p0; // 起点无手柄
                c1 = anchors.Count > 0 ? anchors[0].HandleInWorld : p1;
            }
            else if (i == segCount - 1)
            {
                // 最后一个锚点 → 终点
                c0 = anchors.Count > 0 ? anchors[anchors.Count - 1].HandleOutWorld : p0;
                c1 = p1; // 终点无手柄
            }
            else
            {
                // 中间段
                c0 = anchors[i - 1].HandleOutWorld;
                c1 = anchors[i].HandleInWorld;
            }

            var pts = GenerateCubicBezier(p0, p1, c0, c1, curveSegments);
            curvePoints.AddRange(pts);
        }

        pathLine.positionCount = curvePoints.Count;
        pathLine.SetPositions(curvePoints.ToArray());
        cachedCurvePath = curvePoints;
    }

    /// <summary>
    /// Cubic Bezier: p0 → p1，控制点 c0, c1
    /// </summary>
    List<Vector3> GenerateCubicBezier(Vector3 p0, Vector3 p1, Vector3 c0, Vector3 c1, int segments)
    {
        var result = new List<Vector3>();
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float u = 1f - t;
            result.Add(
                u * u * u * p0
              + 3f * u * u * t * c0
              + 3f * u * t * t * c1
              + t * t * t * p1
            );
        }
        return result;
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCam.transform.position.z;
        return mainCam.ScreenToWorldPoint(mousePos);
    }

    void UpdateInstructionText()
    {
        if (anchorCountText != null && GameManager.Instance != null)
        {
            anchorCountText.text = $"锚点: {GameManager.Instance.UsedAnchors}/{GameManager.Instance.maxAnchors} (选中标橙后拖蓝色手柄调曲线)";
        }
    }

    #region 绘图工具

    Sprite CreateCircleSprite(int radius, Color color)
    {
        int size = radius * 2;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius + 0.5f;
                float dy = y - radius + 0.5f;
                pixels[y * size + x] = (dx * dx + dy * dy <= radius * radius)
                    ? Color.white : Color.clear;
            }
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 48);
    }

    Sprite CreateNumberSprite(int num)
    {
        int size = 24;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        int[,] patterns = new int[,] {
            {1,1,1, 1,1,1, 1,1,1, 1,1,1, 1,0,1, 1,1,1, 1,1,1, 1,1,1, 1,1,1, 1,1,1},
            {1,0,1, 0,1,0, 0,0,1, 0,0,1, 1,0,1, 1,0,0, 1,0,0, 0,0,1, 1,0,1, 1,0,1},
            {1,0,1, 1,1,0, 1,1,1, 0,1,1, 1,1,1, 1,1,1, 1,1,1, 0,1,0, 1,1,1, 1,1,1},
            {1,0,1, 0,1,0, 1,0,0, 0,0,1, 0,0,1, 0,0,1, 1,0,1, 1,0,0, 1,0,1, 0,0,1},
            {1,1,1, 1,1,1, 1,1,1, 1,1,1, 0,0,1, 1,1,1, 1,1,1, 1,0,0, 1,1,1, 1,1,1},
        };

        if (num < 0 || num > 9) num = 1;
        int cellW = 3, cellH = 5;
        int padX = 3, padY = 3;
        int dotW = 5, dotH = 3;

        for (int row = 0; row < cellH; row++)
            for (int col = 0; col < cellW; col++)
                if (patterns[row, num * cellW + col] == 1)
                {
                    int baseX = padX + col * dotW;
                    int baseY = padY + row * dotH;
                    for (int dy = 0; dy < dotH; dy++)
                        for (int dx = 0; dx < dotW; dx++)
                        {
                            int px = baseX + dx, py = baseY + dy;
                            if (px < size && py < size) pixels[py * size + px] = Color.white;
                        }
                }

        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 48);
    }

    #endregion
}
