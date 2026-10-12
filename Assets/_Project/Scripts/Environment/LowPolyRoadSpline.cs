using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chế độ tính cao độ đường mòn:
/// Giải quyết triệt để vấn đề đường tự động nhô lên khi đi qua Cube / Model / Nhà xưởng.
/// </summary>
public enum RoadHeightMode
{
    [Tooltip("Chỉ bám theo mặt đất tự nhiên (Băng qua bên dưới mọi Cube, Model, Nhà xưởng)")]
    PureGroundOnly = 0,

    [Tooltip("Giữ nguyên độ cao Y do bạn tự tay kéo chỉnh trong Scene View (Không tự ý đổi Y)")]
    ManualWaypointY = 1,

    [Tooltip("Chỉ bám Collider của LowPoly_Terrain (Bỏ qua tất cả các Collider của Cube/Model)")]
    TerrainColliderOnly = 2
}

/// <summary>
/// Kiểu dáng đường và cách xử lý góc rẽ:
/// Cho phép tạo góc vuông 90° sắc nét, bo nhẹ góc 30°-45°, hoặc uốn lượn mượt mà.
/// </summary>
public enum RoadCurveStyle
{
    [Tooltip("Góc rẽ kiểm soát (Controlled Fillet): Chỉnh được góc vuông 90° hoặc bo nhẹ 30°-45°")]
    ControlledCorner = 0,

    [Tooltip("Đường cong Catmull-Rom uốn lượn tự nhiên mềm mại")]
    SmoothCatmullRom = 1,

    [Tooltip("Đường thẳng tuyệt đối nối giữa các mốc tọa độ")]
    StraightSegments = 2
}

/// <summary>
/// Công cụ Tạo & Chỉnh Sửa Đường Mòn Uốn Lượn Low-Poly (Low-Poly Curved Road Spline).
/// Thiết kế mặt cắt chuẩn theo ảnh mẫu tham khảo:
/// - Lòng đường vát phẳng đa giác Low-Poly bắt sáng từng mặt phẳng.
/// - Gờ đất nổi hai bên mép (curb embankment) sắc cạnh chuẩn phong cách Low-Poly 3D.
/// - Thành ngoài thẳng đứng dày dặn như tấm đúc 3D nguyên khối.
/// - Đóng nắp 2 đầu phẳng phiu (End Caps) khi không khép kín.
/// - Hỗ trợ khép kín vòng lặp (Closed Loop) tạo thành 1 con đường ô vuông duy nhất bao quanh xưởng!
/// - Cho phép con đường nằm bên dưới mọi Model / Cube mà không bị tự ý nhô lên.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LowPolyRoadSpline : MonoBehaviour
{
    [Header("--- CHẾ ĐỘ ĐỘ CAO (CHỐNG NHÔ LÊN MODEL / CUBE) ---")]
    [Tooltip("Chế độ tính cao độ: PureGroundOnly giúp đường luôn nằm dưới mọi Cube/Model!")]
    public RoadHeightMode heightMode = RoadHeightMode.PureGroundOnly;

    [Tooltip("Độ nâng mặt đường trên nền cỏ (mét)")]
    [Range(0.01f, 0.20f)]
    public float groundOffset = 0.06f;

    [Header("--- HÌNH DÁNG & GÓC RẼ (CORNER & LOOP) ---")]
    [Tooltip("Khép kín thành 1 vòng tròn / ô vuông duy nhất bao quanh toàn bộ khu xưởng")]
    public bool isClosedLoop = false;

    [Tooltip("Kiểu dáng đường và cách bo góc ngã rẽ")]
    public RoadCurveStyle curveStyle = RoadCurveStyle.ControlledCorner;

    [Tooltip("Bán kính bo góc cua (mét). Kéo về 0 = Góc vuông 90° sắc lẹm! Kéo 0.5 - 2.0 = Bo góc cua 30°-45° gọn gàng")]
    [Range(0f, 6f)]
    public float cornerFilletRadius = 1.2f;

    [Header("--- THIẾT LẬP KÍCH THƯỚC ĐƯỜNG CHUẨN ẢNH MẪU ---")]
    [Tooltip("Độ rộng lòng đường (mét)")]
    [Range(1.5f, 8.0f)]
    public float roadWidth = 2.8f;

    [Tooltip("Độ cao gờ viền đất hai bên")]
    [Range(0.02f, 0.40f)]
    public float curbHeight = 0.12f;

    [Tooltip("Độ rộng gờ viền đất")]
    [Range(0.10f, 0.60f)]
    public float curbWidth = 0.25f;

    [Tooltip("Độ võng/lõm của lòng đường")]
    [Range(0.0f, 0.15f)]
    public float bedDip = 0.035f;

    [Tooltip("Độ sâu chân thành hông thẳng đứng cắm vào đất")]
    [Range(0.10f, 0.80f)]
    public float skirtDepth = 0.35f;

    [Tooltip("Mật độ chia đoạn uốn lượn (càng cao càng cong mượt)")]
    [Range(4, 30)]
    public int segmentsPerCurve = 10;

    [Tooltip("Tạo lưới Low-Poly Faceted (mỗi mặt tam giác đổ bóng riêng biệt như ảnh mẫu)")]
    public bool lowPolyFaceted = true;

    [Header("--- TRANG TRÍ SỎI ĐÁ TRÊN ĐƯỜNG (ROAD PEBBLES) ---")]
    [Tooltip("Tự động rải các viên sỏi nhỏ ngẫu nhiên trên lòng đường")]
    public bool spawnRoadPebbles = true;

    [Tooltip("Mật độ cụm sỏi nhỏ trên tuyến đường")]
    [Range(4, 50)]
    public int pebbleClusters = 16;

    [Header("--- DANH SÁCH ĐIỂM ĐIỀU KHIỂN (WAYPOINTS) ---")]
    [Tooltip("Các mốc tọa độ định hình con đường")]
    public List<Vector3> waypoints = new List<Vector3>();

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        InitializeComponents();
        EnsureRoadMaterial();
        if (waypoints == null || waypoints.Count < 2)
        {
            ResetToDefaultSPath();
        }
    }

    private void OnValidate()
    {
        InitializeComponents();
        EnsureRoadMaterial();
        if (waypoints != null && waypoints.Count >= 2)
        {
            GenerateRoadMesh();
        }
    }

    public void InitializeComponents()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
    }

    public void EnsureRoadMaterial()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
#if UNITY_EDITOR
            Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Road_DirtTrail.mat");
            if (mat != null)
            {
                if (meshRenderer.sharedMaterial == null || meshRenderer.sharedMaterial.shader == null || meshRenderer.sharedMaterial.shader.name.Contains("Error"))
                {
                    meshRenderer.sharedMaterial = mat;
                }
                return;
            }
#endif
            if (meshRenderer.sharedMaterial == null || meshRenderer.sharedMaterial.shader == null || meshRenderer.sharedMaterial.shader.name.Contains("Error"))
            {
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (litShader != null)
                {
                    Material fallback = new Material(litShader);
                    fallback.name = "Mat_Road_DirtTrail_Fallback";
                    fallback.SetColor("_BaseColor", new Color(0.81f, 0.60f, 0.38f, 1f));
                    fallback.SetFloat("_Smoothness", 0.08f);
                    meshRenderer.sharedMaterial = fallback;
                }
            }
        }
    }

    public void ResetToDefaultSPath()
    {
        waypoints = new List<Vector3>
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(2.5f, 0f, 4.0f),
            new Vector3(-2.2f, 0f, 9.0f),
            new Vector3(1.8f, 0f, 14.0f)
        };

        if (heightMode == RoadHeightMode.PureGroundOnly)
        {
            SnapAllWaypointsToGround();
        }

        GenerateRoadMesh();
    }

    public void AddWaypointAtEnd()
    {
        if (waypoints.Count < 2)
        {
            ResetToDefaultSPath();
            return;
        }

        Vector3 last = waypoints[waypoints.Count - 1];
        Vector3 prev = waypoints[waypoints.Count - 2];
        Vector3 dir = (last - prev).normalized;
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;

        Vector3 newPt = last + dir * 3.5f;
        if (heightMode == RoadHeightMode.PureGroundOnly)
        {
            newPt.y = CalculateGroundHeight(newPt.x, newPt.z);
        }
        else if (heightMode == RoadHeightMode.ManualWaypointY)
        {
            newPt.y = last.y;
        }

        waypoints.Add(newPt);
        GenerateRoadMesh();
    }

    /// <summary>
    /// Bám sát tất cả các mốc vào mặt đất tự nhiên (Bỏ qua mọi model/cube)
    /// </summary>
    public void SnapAllWaypointsToGround()
    {
        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 pt = waypoints[i];
            pt.y = CalculateGroundHeight(pt.x, pt.z);
            waypoints[i] = pt;
        }
        GenerateRoadMesh();
    }

    public void SnapAllWaypointsToTerrain() => SnapAllWaypointsToGround();
    public float GetTerrainHeight(float x, float z) => CalculateGroundHeight(x, z);
    public bool autoSnapToTerrain => heightMode == RoadHeightMode.PureGroundOnly;

    /// <summary>
    /// Hàm tính toán cao độ mặt đất tự nhiên (Không bị ảnh hưởng bởi Cube hay Model đè lên)
    /// </summary>
    public static float CalculateGroundHeight(float x, float z)
    {
        float centralDist = Mathf.Sqrt(x * x * 0.9f + z * z);
        if (centralDist < 13.5f) return 0f;

        if (Mathf.Abs(x - 1.5f) < 4.5f && z >= 9.5f && z <= 16.5f) return 0f;

        float quarryDist = Mathf.Sqrt(Mathf.Pow(x - (-10.34f), 2) + Mathf.Pow(z - (-13.66f), 2));
        if (quarryDist < 4.5f) return 0f;

        float height = 0f;

        if (z > 10f)
        {
            float zBlend = Mathf.Clamp01((z - 10f) / 10f);

            float towerHillDist = Mathf.Sqrt(Mathf.Pow(x - (-8f), 2) + Mathf.Pow(z - 26f, 2));
            float towerHill = Mathf.Max(0f, 1f - towerHillDist / 12f);
            towerHill = towerHill * towerHill * (3f - 2f * towerHill) * 8.2f;

            float rightHillDist = Mathf.Sqrt(Mathf.Pow(x - 26f, 2) + Mathf.Pow(z - 26f, 2));
            float rightHill = Mathf.Max(0f, 1f - rightHillDist / 14f);
            rightHill = rightHill * rightHill * (3f - 2f * rightHill) * 6.2f;

            float midHillDist = Mathf.Sqrt(Mathf.Pow(x - 3f, 2) + Mathf.Pow(z - 30f, 2));
            float midHill = Mathf.Max(0f, 1f - midHillDist / 12f);
            midHill = midHill * midHill * (3f - 2f * midHill) * 6.8f;

            float backgroundRidge = zBlend * 4.0f;
            height += Mathf.Max(backgroundRidge, Mathf.Max(towerHill, Mathf.Max(rightHill, midHill)));

            float lakeDist = Mathf.Sqrt(Mathf.Pow(x - 14f, 2) + Mathf.Pow(z - 20f, 2));
            if (lakeDist < 9.5f)
            {
                float lakeDip = Mathf.Cos(lakeDist / 9.5f * Mathf.PI * 0.5f) * 3.2f;
                height = Mathf.Max(1.0f, height - lakeDip);
            }
        }

        if (x < -10f)
        {
            float xBlend = Mathf.Clamp01((-x - 10f) / 10f);
            float distW1 = Mathf.Sqrt(Mathf.Pow(x - (-22f), 2) + Mathf.Pow(z - 6f, 2));
            float hillW1 = Mathf.Max(0f, 1f - distW1 / 14f);
            hillW1 = hillW1 * hillW1 * (3f - 2f * hillW1) * 4.6f;

            float distW2 = Mathf.Sqrt(Mathf.Pow(x - (-24f), 2) + Mathf.Pow(z - (-14f), 2));
            float hillW2 = Mathf.Max(0f, 1f - distW2 / 14f);
            hillW2 = hillW2 * hillW2 * (3f - 2f * hillW2) * 4.0f;

            float westBase = xBlend * 3.4f;
            height = Mathf.Max(height, Mathf.Max(westBase, Mathf.Max(hillW1, hillW2)));
        }

        if (x > 10f && z <= 12f)
        {
            float xBlend = Mathf.Clamp01((x - 10f) / 10f);
            float distE1 = Mathf.Sqrt(Mathf.Pow(x - 24f, 2) + Mathf.Pow(z - 2f, 2));
            float hillE1 = Mathf.Max(0f, 1f - distE1 / 14f);
            hillE1 = hillE1 * hillE1 * (3f - 2f * hillE1) * 5.0f;

            float distE2 = Mathf.Sqrt(Mathf.Pow(x - 22f, 2) + Mathf.Pow(z - (-16f), 2));
            float hillE2 = Mathf.Max(0f, 1f - distE2 / 14f);
            hillE2 = hillE2 * hillE2 * (3f - 2f * hillE2) * 3.8f;

            height = Mathf.Max(height, Mathf.Max(xBlend * 3.0f, Mathf.Max(hillE1, hillE2)));
        }

        if (z < -12f)
        {
            float sBlend = Mathf.Clamp01((-z - 12f) / 10f);
            float distS = Mathf.Sqrt(Mathf.Pow(x - 12f, 2) + Mathf.Pow(z - (-26f), 2));
            float frontKnoll = Mathf.Max(0f, 1f - distS / 12f);
            frontKnoll = frontKnoll * frontKnoll * (3f - 2f * frontKnoll) * 2.6f;
            height = Mathf.Max(height, sBlend * 1.8f + frontKnoll);
        }

        if (centralDist < 20f)
        {
            float t = (centralDist - 13.5f) / 6.5f;
            float smooth = t * t * (3f - 2f * t);
            height *= smooth;
        }

        float mainRoadDistX = Mathf.Abs(x - (-2.2f));
        if (mainRoadDistX < 4.2f && z < 5f)
        {
            float roadCut = Mathf.Clamp01(mainRoadDistX / 4.2f);
            height *= roadCut * roadCut;
        }

        return height;
    }

    private float GetTerrainColliderHeight(float x, float z)
    {
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, 100f, z), Vector3.down, 200f);
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject.name.Contains("Terrain"))
            {
                return hit.point.y;
            }
        }
        return CalculateGroundHeight(x, z);
    }

    /// <summary>
    /// Quyết định độ cao thực tế của điểm trên đường dựa vào HeightMode
    /// </summary>
    public float ResolveHeight(float x, float currentY, float z)
    {
        switch (heightMode)
        {
            case RoadHeightMode.ManualWaypointY:
                return currentY;

            case RoadHeightMode.TerrainColliderOnly:
                return GetTerrainColliderHeight(x, z);

            case RoadHeightMode.PureGroundOnly:
            default:
                return CalculateGroundHeight(x, z);
        }
    }

    /// <summary>
    /// Đánh giá danh sách các điểm tim đường (Centerline) dựa theo CurveStyle & ClosedLoop
    /// </summary>
    public List<Vector3> EvaluateSmoothPathPoints()
    {
        List<Vector3> pts = new List<Vector3>();
        if (waypoints == null || waypoints.Count < 2) return pts;

        if (waypoints.Count == 2)
        {
            int steps = segmentsPerCurve;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 p = Vector3.Lerp(waypoints[0], waypoints[1], t);
                p.y = ResolveHeight(p.x, p.y, p.z);
                pts.Add(p);
            }
            return pts;
        }

        switch (curveStyle)
        {
            case RoadCurveStyle.ControlledCorner:
                pts = GenerateControlledCornerPath();
                break;

            case RoadCurveStyle.StraightSegments:
                pts = GenerateStraightSegmentsPath();
                break;

            case RoadCurveStyle.SmoothCatmullRom:
            default:
                pts = GenerateCatmullRomPath();
                break;
        }

        // Áp dụng ResolveHeight cho toàn bộ các điểm tim đường
        for (int i = 0; i < pts.Count; i++)
        {
            Vector3 p = pts[i];
            p.y = ResolveHeight(p.x, p.y, p.z);
            pts[i] = p;
        }

        // Nếu khép kín vòng lặp mà điểm cuối quá sát điểm đầu thì loại bỏ điểm cuối để tránh lỗi nối lưới
        if (isClosedLoop && pts.Count > 3)
        {
            if (Vector3.Distance(pts[0], pts[pts.Count - 1]) < 0.2f)
            {
                pts.RemoveAt(pts.Count - 1);
            }
        }

        return pts;
    }

    private List<Vector3> GenerateControlledCornerPath()
    {
        List<Vector3> result = new List<Vector3>();
        int count = waypoints.Count;

        if (isClosedLoop)
        {
            for (int i = 0; i < count; i++)
            {
                int prevIdx = (i - 1 + count) % count;
                int nextIdx = (i + 1) % count;
                AddCornerFillet(result, waypoints[prevIdx], waypoints[i], waypoints[nextIdx]);
            }
        }
        else
        {
            result.Add(waypoints[0]);
            for (int i = 1; i < count - 1; i++)
            {
                AddCornerFillet(result, waypoints[i - 1], waypoints[i], waypoints[i + 1]);
            }
            result.Add(waypoints[count - 1]);
        }

        return result;
    }

    private void AddCornerFillet(List<Vector3> pts, Vector3 prev, Vector3 cur, Vector3 next)
    {
        Vector3 inVec = cur - prev;
        Vector3 outVec = next - cur;
        float inLen = inVec.magnitude;
        float outLen = outVec.magnitude;

        if (inLen < 0.05f || outLen < 0.05f)
        {
            pts.Add(cur);
            return;
        }

        Vector3 inDir = inVec / inLen;
        Vector3 outDir = outVec / outLen;

        float maxR = Mathf.Min(inLen * 0.45f, outLen * 0.45f);
        float r = Mathf.Clamp(cornerFilletRadius, 0f, maxR);

        // Kéo về 0 = Góc vuông 90° sắc nét dứt khoát!
        if (r <= 0.08f)
        {
            if (pts.Count > 0)
            {
                Vector3 lastPt = pts[pts.Count - 1];
                float dist = Vector3.Distance(lastPt, cur);
                int subDivs = Mathf.FloorToInt(dist / 2.5f);
                for (int s = 1; s <= subDivs; s++)
                {
                    pts.Add(Vector3.Lerp(lastPt, cur, (float)s / (subDivs + 1)));
                }
            }
            pts.Add(cur);
            return;
        }

        // Bo góc cua kiểm soát (Fillet)
        Vector3 pStart = cur - inDir * r;
        Vector3 pEnd = cur + outDir * r;

        if (pts.Count > 0)
        {
            Vector3 lastPt = pts[pts.Count - 1];
            float dist = Vector3.Distance(lastPt, pStart);
            int subDivs = Mathf.FloorToInt(dist / 2.5f);
            for (int s = 1; s <= subDivs; s++)
            {
                pts.Add(Vector3.Lerp(lastPt, pStart, (float)s / (subDivs + 1)));
            }
        }

        int arcSteps = Mathf.Clamp(Mathf.RoundToInt(r * 3.5f), 3, 8);
        for (int k = 0; k <= arcSteps; k++)
        {
            float t = (float)k / arcSteps;
            Vector3 pt = (1f - t) * (1f - t) * pStart + 2f * (1f - t) * t * cur + t * t * pEnd;
            pts.Add(pt);
        }
    }

    private List<Vector3> GenerateStraightSegmentsPath()
    {
        List<Vector3> pts = new List<Vector3>();
        int count = isClosedLoop ? waypoints.Count : waypoints.Count - 1;
        for (int i = 0; i < count; i++)
        {
            Vector3 a = waypoints[i];
            Vector3 b = waypoints[(i + 1) % waypoints.Count];
            float dist = Vector3.Distance(a, b);
            int subDivs = Mathf.Max(1, Mathf.RoundToInt(dist / 2.0f));
            for (int s = 0; s < subDivs; s++)
            {
                pts.Add(Vector3.Lerp(a, b, (float)s / subDivs));
            }
        }
        if (!isClosedLoop)
        {
            pts.Add(waypoints[waypoints.Count - 1]);
        }
        return pts;
    }

    private List<Vector3> GenerateCatmullRomPath()
    {
        List<Vector3> pts = new List<Vector3>();
        int count = waypoints.Count;
        int segCount = isClosedLoop ? count : count - 1;

        for (int i = 0; i < segCount; i++)
        {
            Vector3 p0, p1, p2, p3;
            if (isClosedLoop)
            {
                p0 = waypoints[(i - 1 + count) % count];
                p1 = waypoints[i];
                p2 = waypoints[(i + 1) % count];
                p3 = waypoints[(i + 2) % count];
            }
            else
            {
                p0 = i > 0 ? waypoints[i - 1] : waypoints[i] + (waypoints[i] - waypoints[i + 1]);
                p1 = waypoints[i];
                p2 = waypoints[i + 1];
                p3 = (i + 2 < count) ? waypoints[i + 2] : p2 + (p2 - p1);
            }

            int steps = segmentsPerCurve;
            for (int s = 0; s < steps; s++)
            {
                float t = (float)s / steps;
                Vector3 pt = GetCatmullRomPosition(t, p0, p1, p2, p3);
                pts.Add(pt);
            }
        }

        if (!isClosedLoop)
        {
            pts.Add(waypoints[count - 1]);
        }

        return pts;
    }

    private Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 a = 2f * p1;
        Vector3 b = p2 - p0;
        Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;
        return 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));
    }

    /// <summary>
    /// Tái tạo toàn bộ Mesh 3D của con đường chuẩn theo model trong ảnh mẫu tham khảo
    /// </summary>
    public void GenerateRoadMesh()
    {
        InitializeComponents();
        EnsureRoadMaterial();

        List<Vector3> centerLine = EvaluateSmoothPathPoints();
        if (centerLine.Count < 2) return;

        int crossCount = 10;
        int ringCount = centerLine.Count;
        Vector3[][] rings = new Vector3[ringCount][];

        for (int r = 0; r < ringCount; r++)
        {
            Vector3 cur = centerLine[r];
            Vector3 dir;
            if (isClosedLoop)
            {
                Vector3 next = centerLine[(r + 1) % ringCount];
                Vector3 prev = centerLine[(r - 1 + ringCount) % ringCount];
                dir = (next - prev).normalized;
            }
            else
            {
                if (r < ringCount - 1) dir = (centerLine[r + 1] - cur).normalized;
                else dir = (cur - centerLine[r - 1]).normalized;
            }

            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            else dir.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

            float yBase = cur.y + groundOffset;
            float halfW = roadWidth * 0.5f;

            rings[r] = new Vector3[crossCount];
            // 0: Thành đứng ngoài Trái (đáy)
            rings[r][0] = cur - right * halfW + Vector3.up * (yBase - skirtDepth);
            // 1: Đỉnh ngoài gờ Trái
            rings[r][1] = cur - right * halfW + Vector3.up * (yBase + curbHeight);
            // 2: Đỉnh trong gờ Trái
            rings[r][2] = cur - right * (halfW - curbWidth) + Vector3.up * (yBase + curbHeight);
            // 3: Chân trong gờ Trái (chạm lòng đường)
            rings[r][3] = cur - right * (halfW - curbWidth) + Vector3.up * (yBase - bedDip);
            // 4: Lòng đường Trái (vát đa giác low-poly)
            rings[r][4] = cur - right * (halfW * 0.35f) + Vector3.up * (yBase - bedDip);
            // 5: Lòng đường Phải (vát đa giác low-poly)
            rings[r][5] = cur + right * (halfW * 0.35f) + Vector3.up * (yBase - bedDip);
            // 6: Chân trong gờ Phải (chạm lòng đường)
            rings[r][6] = cur + right * (halfW - curbWidth) + Vector3.up * (yBase - bedDip);
            // 7: Đỉnh trong gờ Phải
            rings[r][7] = cur + right * (halfW - curbWidth) + Vector3.up * (yBase + curbHeight);
            // 8: Đỉnh ngoài gờ Phải
            rings[r][8] = cur + right * halfW + Vector3.up * (yBase + curbHeight);
            // 9: Thành đứng ngoài Phải (đáy)
            rings[r][9] = cur + right * halfW + Vector3.up * (yBase - skirtDepth);
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        int segCount = isClosedLoop ? ringCount : ringCount - 1;

        for (int r = 0; r < segCount; r++)
        {
            int rNext = (r + 1) % ringCount;
            float v0 = (float)r / ringCount * 8f;
            float v1 = (float)(r + 1) / ringCount * 8f;

            for (int c = 0; c < crossCount - 1; c++)
            {
                float u0 = (float)c / (crossCount - 1);
                float u1 = (float)(c + 1) / (crossCount - 1);

                Vector3 p00 = rings[r][c];
                Vector3 p10 = rings[rNext][c];
                Vector3 p01 = rings[r][c + 1];
                Vector3 p11 = rings[rNext][c + 1];

                AddFacetedTriangle(vertices, normals, uvs, triangles, p00, p11, p01, new Vector2(u0, v0), new Vector2(u1, v1), new Vector2(u1, v0));
                AddFacetedTriangle(vertices, normals, uvs, triangles, p00, p10, p11, new Vector2(u0, v0), new Vector2(u0, v1), new Vector2(u1, v1));
            }
        }

        // Bổ sung nắp đóng 2 đầu (End Caps) nếu không khép kín vòng lặp
        if (!isClosedLoop && ringCount >= 2)
        {
            AddEndCap(vertices, normals, uvs, triangles, rings[0], isStart: true);
            AddEndCap(vertices, normals, uvs, triangles, rings[ringCount - 1], isStart: false);
        }

        Mesh mesh = new Mesh();
        mesh.name = $"{gameObject.name}_Mesh";
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();

        if (!lowPolyFaceted)
        {
            mesh.RecalculateNormals();
        }

        meshFilter.sharedMesh = mesh;

        GenerateRoadPebbles(centerLine);
    }

    private void AddEndCap(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3[] ring, bool isStart)
    {
        // 0: Skirt đáy Trái, 1: Gờ ngoài đỉnh Trái, 2: Gờ trong đỉnh Trái, 3: Chân gờ trong Trái
        // 4: Lòng Trái, 5: Lòng Phải, 6: Chân gờ trong Phải, 7: Gờ trong đỉnh Phải, 8: Gờ ngoài đỉnh Phải, 9: Skirt đáy Phải

        // Khối thành đáy: 0, 1, 8, 9
        AddCapQuad(verts, norms, uvs, tris, ring[0], ring[1], ring[8], ring[9], isStart);
        // Gờ bên trái: 1, 2, 3
        AddCapTri(verts, norms, uvs, tris, ring[1], ring[2], ring[3], isStart);
        // Khối lòng đường: 1, 3, 6, 8
        AddCapQuad(verts, norms, uvs, tris, ring[1], ring[3], ring[6], ring[8], isStart);
        // Gờ bên phải: 8, 6, 7
        AddCapTri(verts, norms, uvs, tris, ring[8], ring[6], ring[7], isStart);
    }

    private void AddCapQuad(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, bool isStart)
    {
        if (isStart)
        {
            AddFacetedTriangle(verts, norms, uvs, tris, p0, p2, p1, Vector2.zero, Vector2.one, Vector2.up);
            AddFacetedTriangle(verts, norms, uvs, tris, p0, p3, p2, Vector2.zero, Vector2.right, Vector2.one);
        }
        else
        {
            AddFacetedTriangle(verts, norms, uvs, tris, p0, p1, p2, Vector2.zero, Vector2.up, Vector2.one);
            AddFacetedTriangle(verts, norms, uvs, tris, p0, p2, p3, Vector2.zero, Vector2.one, Vector2.right);
        }
    }

    private void AddCapTri(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 p0, Vector3 p1, Vector3 p2, bool isStart)
    {
        if (isStart)
        {
            AddFacetedTriangle(verts, norms, uvs, tris, p0, p2, p1, Vector2.zero, Vector2.right, Vector2.up);
        }
        else
        {
            AddFacetedTriangle(verts, norms, uvs, tris, p0, p1, p2, Vector2.zero, Vector2.up, Vector2.right);
        }
    }

    private void AddFacetedTriangle(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris,
        Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC)
    {
        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
        int start = verts.Count;

        verts.Add(a);
        verts.Add(b);
        verts.Add(c);

        norms.Add(normal);
        norms.Add(normal);
        norms.Add(normal);

        uvs.Add(uvA);
        uvs.Add(uvB);
        uvs.Add(uvC);

        tris.Add(start);
        tris.Add(start + 1);
        tris.Add(start + 2);
    }

    public void GenerateRoadPebbles(List<Vector3> centerLine)
    {
        Transform childTf = transform.Find("_Road_Pebbles");
        if (!spawnRoadPebbles || centerLine == null || centerLine.Count < 3)
        {
            if (childTf != null)
            {
                if (Application.isPlaying) Destroy(childTf.gameObject);
                else DestroyImmediate(childTf.gameObject);
            }
            return;
        }

        GameObject pebblesObj;
        if (childTf == null)
        {
            pebblesObj = new GameObject("_Road_Pebbles");
            pebblesObj.transform.SetParent(transform, false);
            pebblesObj.transform.localPosition = Vector3.zero;
            pebblesObj.transform.localRotation = Quaternion.identity;
        }
        else
        {
            pebblesObj = childTf.gameObject;
        }

        MeshFilter pMf = pebblesObj.GetComponent<MeshFilter>();
        if (pMf == null) pMf = pebblesObj.AddComponent<MeshFilter>();

        MeshRenderer pMr = pebblesObj.GetComponent<MeshRenderer>();
        if (pMr == null) pMr = pebblesObj.AddComponent<MeshRenderer>();

        Material stoneMat = null;
#if UNITY_EDITOR
        stoneMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Stone.mat");
#endif
        if (stoneMat == null)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            stoneMat = new Material(lit);
            stoneMat.SetColor("_BaseColor", new Color(0.65f, 0.60f, 0.55f, 1f));
            stoneMat.SetFloat("_Smoothness", 0.05f);
        }
        pMr.sharedMaterial = stoneMat;
        pMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        pMr.receiveShadows = true;

        List<Vector3> pVerts = new List<Vector3>();
        List<Vector3> pNorms = new List<Vector3>();
        List<Vector2> pUvs = new List<Vector2>();
        List<int> pTris = new List<int>();

        System.Random rng = new System.Random(gameObject.name.GetHashCode() ^ 12345);

        float halfBed = Mathf.Max(0.2f, (roadWidth * 0.5f - curbWidth - 0.25f));

        for (int k = 0; k < pebbleClusters; k++)
        {
            float prog = (float)k / pebbleClusters;
            int idx = Mathf.Clamp(Mathf.FloorToInt(prog * (centerLine.Count - 1)), 0, centerLine.Count - 2);

            Vector3 pt = centerLine[idx];
            Vector3 nextPt = centerLine[idx + 1];
            Vector3 dir = (nextPt - pt).normalized;
            dir.y = 0f;
            dir.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

            int stoneCount = rng.Next(1, 4);
            for (int s = 0; s < stoneCount; s++)
            {
                float uOffset = (float)(rng.NextDouble() * 2.0 - 1.0) * halfBed;
                float vOffset = (float)(rng.NextDouble() * 1.5 - 0.75);
                float sc = 0.08f + (float)rng.NextDouble() * 0.12f;

                Vector3 pebbleCenter = pt + right * uOffset + dir * vOffset;
                pebbleCenter.y = pt.y + groundOffset - bedDip + sc * 0.45f;

                AddLowPolyPebbleMesh(pVerts, pNorms, pUvs, pTris, pebbleCenter, sc, rng);
            }
        }

        Mesh pebbleMesh = new Mesh();
        pebbleMesh.name = $"{gameObject.name}_PebblesMesh";
        pebbleMesh.vertices = pVerts.ToArray();
        pebbleMesh.normals = pNorms.ToArray();
        pebbleMesh.uv = pUvs.ToArray();
        pebbleMesh.triangles = pTris.ToArray();
        pebbleMesh.RecalculateBounds();

        pMf.sharedMesh = pebbleMesh;
    }

    private void AddLowPolyPebbleMesh(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 center, float scale, System.Random rng)
    {
        Vector3 top = center + Vector3.up * scale * 0.65f;
        Vector3 bottom = center - Vector3.up * scale * 0.35f;

        float rot = (float)(rng.NextDouble() * Mathf.PI * 2f);
        Vector3[] ring = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            float a = rot + i * Mathf.PI * 0.5f;
            float r = scale * (0.8f + (float)rng.NextDouble() * 0.4f);
            ring[i] = center + new Vector3(Mathf.Cos(a) * r, ((float)rng.NextDouble() * 0.2f - 0.1f) * scale, Mathf.Sin(a) * r);
        }

        AddFacetedTriangle(verts, norms, uvs, tris, ring[0], top, ring[1], Vector2.zero, Vector2.up, Vector2.right);
        AddFacetedTriangle(verts, norms, uvs, tris, ring[1], top, ring[2], Vector2.zero, Vector2.up, Vector2.right);
        AddFacetedTriangle(verts, norms, uvs, tris, ring[2], top, ring[3], Vector2.zero, Vector2.up, Vector2.right);
        AddFacetedTriangle(verts, norms, uvs, tris, ring[3], top, ring[0], Vector2.zero, Vector2.up, Vector2.right);

        AddFacetedTriangle(verts, norms, uvs, tris, ring[0], ring[1], bottom, Vector2.zero, Vector2.right, Vector2.up);
        AddFacetedTriangle(verts, norms, uvs, tris, ring[1], ring[2], bottom, Vector2.zero, Vector2.right, Vector2.up);
        AddFacetedTriangle(verts, norms, uvs, tris, ring[2], ring[3], bottom, Vector2.zero, Vector2.right, Vector2.up);
        AddFacetedTriangle(verts, norms, uvs, tris, ring[3], ring[0], bottom, Vector2.zero, Vector2.right, Vector2.up);
    }
}
