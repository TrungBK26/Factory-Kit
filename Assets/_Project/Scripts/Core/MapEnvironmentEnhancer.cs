using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý & Tự Động Nâng Cấp Môi Trường Trò Chơi (Map Environment Enhancer).
/// Tự động chạy khi bắt đầu chơi (Play Mode) và hỗ trợ chạy trong Editor:
/// 1. Tự động làm mịn Mesh địa hình đồi núi (Smooth Normals, xóa sạch góc cạnh nhọn).
/// 2. Thiết lập ánh nắng vàng ấm rực rỡ và Trilight Ambient chuẩn phong cách mỹ thuật ảnh Menu.
/// 3. Dựng 4 bức tường vô hình (Map Boundaries) ngăn tuyệt đối người chơi và NPC chạy ra ngoài bản đồ.
/// 4. Tạo mạng lưới đường mòn đất (Rustic Dirt Footpaths) uốn lượn kết nối cổng, xưởng, máy móc và mỏ đá.
/// 5. Sinh các cụm hoa dại rực rỡ sắc màu (vàng, hồng, tím, cam, trắng, đỏ, xanh) với cuống lá và nhụy vàng.
/// 6. Bổ sung các cụm đá cuội diorama ven sườn đồi giúp phân định rõ rệt độ dốc và chỗ cao chỗ thấp.
/// </summary>
public class MapEnvironmentEnhancer : MonoBehaviour
{
    private static MapEnvironmentEnhancer _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitializeOnSceneLoad()
    {
        var existing = FindAnyObjectByType<MapEnvironmentEnhancer>();
        if (existing == null)
        {
            var go = new GameObject("_MapEnvironmentEnhancer_Runtime");
            existing = go.AddComponent<MapEnvironmentEnhancer>();
        }

        existing.ApplyAllEnhancements();
    }

    public void ApplyAllEnhancements()
    {
        // 1. Dựng tường rào vô hình ngăn Player ra ngoài
        SetupMapBoundaries();

        // 2. Làm mịn địa hình đồi núi
        ApplySmoothTerrainMesh();

        // 3. Tinh chỉnh ánh sáng & bầu không khí ấm áp chuẩn ảnh Menu
        SetupWarmAtmosphere();

        // 4. Tạo hệ thống đường mòn đất kết nối toàn bộ nhà máy
        SetupRusticDirtTrails();

        // 5. Bổ sung cụm đá diorama giúp nhìn rõ độ dốc sườn đồi
        SetupDecorativeBoulders();

        // 6. Sinh hoa dại đa sắc màu & bụi cỏ 3D
        SpawnStylizedFlora();
    }

    // =========================================================================
    // 1. TƯỜNG RÀO VÔ HÌNH (MAP BOUNDARIES)
    // =========================================================================
    private void SetupMapBoundaries()
    {
        var envRoot = GameObject.Find("_Environment_Map");
        if (envRoot == null) envRoot = new GameObject("_Environment_Map");

        Transform existingBounds = envRoot.transform.Find("_MapBoundaries");
        if (existingBounds != null) return;

        GameObject boundsObj = new GameObject("_MapBoundaries");
        boundsObj.transform.SetParent(envRoot.transform, false);

        CreateWall(boundsObj.transform, "Wall_North", new Vector3(0f, 4f, 16.5f), new Vector3(52f, 10f, 2f));
        CreateWall(boundsObj.transform, "Wall_South", new Vector3(0f, 4f, -20.5f), new Vector3(52f, 10f, 2f));
        CreateWall(boundsObj.transform, "Wall_West", new Vector3(-20.5f, 4f, -2.0f), new Vector3(2f, 10f, 44f));
        CreateWall(boundsObj.transform, "Wall_East", new Vector3(21.0f, 4f, -2.0f), new Vector3(2f, 10f, 44f));
    }

    private void CreateWall(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.position = pos;
        var col = wall.AddComponent<BoxCollider>();
        col.size = size;
        col.isTrigger = false;
        wall.layer = 0;
    }

    // =========================================================================
    // 2. LÀM MỊN ĐỊA HÌNH ĐỒI NÚI (SMOOTH TERRAIN MESH)
    // =========================================================================
    private void ApplySmoothTerrainMesh()
    {
        var terrainObj = GameObject.Find("LowPoly_Terrain");
        if (terrainObj == null) return;

        var mf = terrainObj.GetComponent<MeshFilter>();
        var mc = terrainObj.GetComponent<MeshCollider>();

        if (mf != null && (mf.sharedMesh == null || mf.sharedMesh.name != "SmoothStylizedTerrain"))
        {
            Mesh smoothMesh = BuildSmoothMesh();
            mf.sharedMesh = smoothMesh;
            if (mc != null) mc.sharedMesh = smoothMesh;
        }
    }

    private Mesh BuildSmoothMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "SmoothStylizedTerrain";

        int gridSize = 64;
        float totalWidth = 84f;
        float step = totalWidth / gridSize;
        float halfWidth = totalWidth * 0.5f;

        int vertCount = (gridSize + 1) * (gridSize + 1);
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];

        for (int i = 0; i <= gridSize; i++)
        {
            for (int j = 0; j <= gridSize; j++)
            {
                float x = -halfWidth + i * step;
                float z = -halfWidth + j * step;
                float y = CalculateHeight(x, z);

                int vIdx = i * (gridSize + 1) + j;
                vertices[vIdx] = new Vector3(x, y, z);
                uvs[vIdx] = new Vector2((float)i / gridSize, (float)j / gridSize);
            }
        }

        int quadCount = gridSize * gridSize;
        int[] triangles = new int[quadCount * 6];
        int tIdx = 0;

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                int v00 = i * (gridSize + 1) + j;
                int v10 = (i + 1) * (gridSize + 1) + j;
                int v01 = i * (gridSize + 1) + (j + 1);
                int v11 = (i + 1) * (gridSize + 1) + (j + 1);

                triangles[tIdx++] = v00;
                triangles[tIdx++] = v01;
                triangles[tIdx++] = v11;

                triangles[tIdx++] = v00;
                triangles[tIdx++] = v11;
                triangles[tIdx++] = v10;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        return mesh;
    }

    public float CalculateHeight(float x, float z)
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

    // =========================================================================
    // 3. ÁNH SÁNG & KHÍ QUYỂN ẤM ÁP
    // =========================================================================
    private void SetupWarmAtmosphere()
    {
        var lightObj = GameObject.Find("Directional Light");
        if (lightObj != null)
        {
            var l = lightObj.GetComponent<Light>();
            if (l != null)
            {
                l.intensity = 1.25f;
                l.color = new Color(1.0f, 0.98f, 0.92f);
                l.shadows = LightShadows.Soft;
                l.shadowStrength = 0.88f; // Giữ nguyên bóng đổ sắc nét 0.88 như khi ở ngoài Scene!
                l.shadowNormalBias = 0.4f;
                l.shadowBias = 0.1f;
            }
            lightObj.transform.rotation = Quaternion.Euler(50f, 135f, 0f); // Giữ nguyên góc xiên 50°, 135° chuẩn đẹp!
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.48f, 0.52f, 0.58f); // Tông dịu mắt, tránh bị lóa màu neon xanh
    }

    // =========================================================================
    // 4. MẠNG LƯỚI ĐƯỜNG MÒN ĐẤT (RUSTIC DIRT TRAILS & STEPPING STONES)
    // =========================================================================
    private void SetupRusticDirtTrails()
    {
        var envRoot = GameObject.Find("_Environment_Map");
        if (envRoot == null) envRoot = new GameObject("_Environment_Map");

        Transform existingRoadNetwork = envRoot.transform.Find("Road_Network");
        if (existingRoadNetwork != null)
        {
            // Bảo toàn 100% các điều chỉnh thủ công của User đối với Road_Network
            return;
        }

        GameObject roadRoot = new GameObject("Road_Network");
        roadRoot.transform.SetParent(envRoot.transform, false);

        // Chuẩn bị Material Đất ấm & Đá lát
        Material dirtMat = GetOrCreateRoadMaterial("Mat_Road_DirtTrail", new Color(0.78f, 0.60f, 0.38f, 1f));
        Material stoneMat = GetOrCreateRoadMaterial("Mat_Road_SteppingStone", new Color(0.68f, 0.66f, 0.62f, 1f));

        // 1. Trục Đường Chính (Main Central Avenue) từ Cổng Nam lên Xưởng Gỗ Bắc
        List<Vector3> mainPath = new List<Vector3>
        {
            new Vector3(-2.0f, 0f, -19.0f),
            new Vector3(-1.8f, 0f, -14.5f),
            new Vector3(-1.4f, 0f, -10.0f),
            new Vector3(-0.8f, 0f, -5.5f),
            new Vector3(0.0f, 0f, -1.0f),
            new Vector3(0.4f, 0f, 3.5f),
            new Vector3(0.8f, 0f, 7.5f),
            new Vector3(1.2f, 0f, 10.5f)
        };
        BuildPathStrip(roadRoot.transform, "Trail_MainAvenue", mainPath, 2.8f, dirtMat);

        // 2. Nhánh Rẽ Sang Xưởng Trái (3 Máy: Cưa Gỗ, Luyện Đồng, Tinh Chế Bạc)
        List<Vector3> leftBranch = new List<Vector3>
        {
            new Vector3(-0.8f, 0f, 0.0f),
            new Vector3(-3.2f, 0f, 0.0f),
            new Vector3(-5.8f, 0f, 0.0f)
        };
        BuildPathStrip(roadRoot.transform, "Trail_LeftBranch", leftBranch, 2.2f, dirtMat);

        List<Vector3> leftSpur = new List<Vector3>
        {
            new Vector3(-5.8f, 0f, -4.5f),
            new Vector3(-5.8f, 0f, 0.0f),
            new Vector3(-5.8f, 0f, 4.5f)
        };
        BuildPathStrip(roadRoot.transform, "Trail_LeftMachineSpur", leftSpur, 2.0f, dirtMat);

        // 3. Nhánh Rẽ Sang Xưởng Phải (3 Máy: Nghiền Đá, Đúc Thép, Chiết Xuất Vàng)
        List<Vector3> rightBranch = new List<Vector3>
        {
            new Vector3(0.4f, 0f, 0.0f),
            new Vector3(3.2f, 0f, 0.0f),
            new Vector3(5.8f, 0f, 0.0f)
        };
        BuildPathStrip(roadRoot.transform, "Trail_RightBranch", rightBranch, 2.2f, dirtMat);

        List<Vector3> rightSpur = new List<Vector3>
        {
            new Vector3(5.8f, 0f, -4.5f),
            new Vector3(5.8f, 0f, 0.0f),
            new Vector3(5.8f, 0f, 4.5f)
        };
        BuildPathStrip(roadRoot.transform, "Trail_RightMachineSpur", rightSpur, 2.0f, dirtMat);

        // 4. Đường Mòn Sang Mỏ Đá (Quarry Trail)
        List<Vector3> quarryTrail = new List<Vector3>
        {
            new Vector3(-1.4f, 0f, -10.0f),
            new Vector3(-4.0f, 0f, -11.5f),
            new Vector3(-7.0f, 0f, -12.5f),
            new Vector3(-9.6f, 0f, -13.5f)
        };
        BuildPathStrip(roadRoot.transform, "Trail_Quarry", quarryTrail, 2.2f, dirtMat);

        // 5. Khoảng Sân Đất Tròn Quanh SellZone và Quầy Nâng Cấp
        BuildPlazaPatch(roadRoot.transform, "Plaza_SellZone", new Vector3(0.0f, 0f, -3.5f), 3.8f, 3.8f, dirtMat);
        BuildPlazaPatch(roadRoot.transform, "Plaza_UpgradeKiosk", new Vector3(2.5f, 0f, 10.2f), 3.6f, 2.8f, dirtMat);

        // 6. Điểm Xuyết Đá Cuội Bước Đi (Stepping Stones)
        SpawnSteppingStones(roadRoot.transform, stoneMat);
    }

    private void BuildPathStrip(Transform parent, string name, List<Vector3> pts, float width, Material mat)
    {
        if (pts.Count < 2) return;

        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for (int i = 0; i < pts.Count; i++)
        {
            Vector3 cur = pts[i];
            float yGround = CalculateHeight(cur.x, cur.z);
            cur.y = yGround;

            Vector3 dir;
            if (i < pts.Count - 1)
                dir = (pts[i + 1] - cur).normalized;
            else
                dir = (cur - pts[i - 1]).normalized;

            dir.y = 0f;
            dir.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

            float yLift = 0.08f;
            Vector3 pSkirtL = cur - right * (width * 0.5f + 0.35f) + Vector3.down * 0.30f;
            Vector3 pEdgeL = cur - right * (width * 0.5f) + Vector3.up * yLift;
            Vector3 pCrownL = cur - right * (width * 0.2f) + Vector3.up * (yLift + 0.025f);
            Vector3 pCrownR = cur + right * (width * 0.2f) + Vector3.up * (yLift + 0.025f);
            Vector3 pEdgeR = cur + right * (width * 0.5f) + Vector3.up * yLift;
            Vector3 pSkirtR = cur + right * (width * 0.5f + 0.35f) + Vector3.down * 0.30f;

            int bIdx = verts.Count;
            verts.Add(pSkirtL);
            verts.Add(pEdgeL);
            verts.Add(pCrownL);
            verts.Add(pCrownR);
            verts.Add(pEdgeR);
            verts.Add(pSkirtR);

            Vector3 up = Vector3.up;
            norms.Add(Vector3.Normalize(up - right * 0.7f));
            norms.Add(up);
            norms.Add(up);
            norms.Add(up);
            norms.Add(up);
            norms.Add(Vector3.Normalize(up + right * 0.7f));

            float v = (float)i / (pts.Count - 1) * 8f;
            uvs.Add(new Vector2(0f, v));
            uvs.Add(new Vector2(0.2f, v));
            uvs.Add(new Vector2(0.4f, v));
            uvs.Add(new Vector2(0.6f, v));
            uvs.Add(new Vector2(0.8f, v));
            uvs.Add(new Vector2(1.0f, v));

            if (i > 0)
            {
                int prev = bIdx - 6;
                for (int q = 0; q < 5; q++)
                {
                    // Tam giác 1: Winding Clockwise nhìn từ trên xuống
                    tris.Add(prev + q);
                    tris.Add(bIdx + q + 1);
                    tris.Add(prev + q + 1);

                    // Tam giác 2: Winding Clockwise nhìn từ trên xuống
                    tris.Add(prev + q);
                    tris.Add(bIdx + q);
                    tris.Add(bIdx + q + 1);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = $"{name}_Mesh";
        mesh.vertices = verts.ToArray();
        mesh.normals = norms.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();

        var mf = obj.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;
    }

    private void BuildPlazaPatch(Transform parent, string name, Vector3 center, float rx, float rz, Material mat)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        int sides = 18;
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        float yCenter = CalculateHeight(center.x, center.z) + 0.085f;
        verts.Add(new Vector3(center.x, yCenter, center.z));
        norms.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 0.5f));

        for (int i = 0; i < sides; i++)
        {
            float angle = (float)i / sides * Mathf.PI * 2f;
            float px = center.x + Mathf.Cos(angle) * rx;
            float pz = center.z + Mathf.Sin(angle) * rz;
            float py = CalculateHeight(px, pz);

            Vector3 pTop = new Vector3(px, py + 0.08f, pz);
            Vector3 pSkirt = new Vector3(px + Mathf.Cos(angle) * 0.35f, py - 0.30f, pz + Mathf.Sin(angle) * 0.35f);

            verts.Add(pTop);
            norms.Add(Vector3.up);
            uvs.Add(new Vector2((Mathf.Cos(angle) + 1f) * 0.5f, (Mathf.Sin(angle) + 1f) * 0.5f));

            verts.Add(pSkirt);
            norms.Add(Vector3.Normalize(Vector3.up * 0.3f + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.7f));
            uvs.Add(new Vector2((Mathf.Cos(angle) + 1f) * 0.5f, (Mathf.Sin(angle) + 1f) * 0.5f));
        }

        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            int topCur = 1 + i * 2;
            int skirtCur = 2 + i * 2;
            int topNext = 1 + next * 2;
            int skirtNext = 2 + next * 2;

            // Mặt trên: Winding Clockwise nhìn từ trên xuống
            tris.Add(0);
            tris.Add(topNext);
            tris.Add(topCur);

            // Gờ chân mép: Winding Clockwise
            tris.Add(topCur);
            tris.Add(skirtNext);
            tris.Add(skirtCur);

            tris.Add(topCur);
            tris.Add(topNext);
            tris.Add(skirtNext);
        }

        Mesh mesh = new Mesh();
        mesh.name = $"{name}_Mesh";
        mesh.vertices = verts.ToArray();
        mesh.normals = norms.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();

        var mf = obj.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;
    }

    private void SpawnSteppingStones(Transform parent, Material stoneMat)
    {
        Vector3[] stoneCoords = new[]
        {
            new Vector3(-1.9f, 0f, -17.0f),
            new Vector3(-1.7f, 0f, -13.0f),
            new Vector3(-1.3f, 0f, -8.0f),
            new Vector3(-0.6f, 0f, -3.5f),
            new Vector3(0.2f, 0f, 1.5f),
            new Vector3(0.6f, 0f, 5.5f),
            new Vector3(1.0f, 0f, 9.0f),
            new Vector3(-2.2f, 0f, 0.0f),
            new Vector3(-4.2f, 0f, 0.0f),
            new Vector3(2.2f, 0f, 0.0f),
            new Vector3(4.2f, 0f, 0.0f),
            new Vector3(-3.2f, 0f, -11.0f),
            new Vector3(-5.8f, 0f, -12.0f),
            new Vector3(-8.2f, 0f, -13.0f)
        };

        for (int i = 0; i < stoneCoords.Length; i++)
        {
            Vector3 pos = stoneCoords[i];
            pos.y = CalculateHeight(pos.x, pos.z) + 0.088f;

            var stone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stone.name = $"SteppingStone_{i}";
            stone.transform.SetParent(parent, false);
            stone.transform.position = pos;
            stone.transform.rotation = Quaternion.Euler(0f, (i * 37) % 360, 0f);
            float r = 0.38f + (i % 3) * 0.08f;
            stone.transform.localScale = new Vector3(r, 0.035f, r * 0.85f);

            var col = stone.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = stone.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = stoneMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = true;
            }
        }
    }

    // =========================================================================
    // 5. CỤM ĐÁ DIORAMA VEN SƯỜN ĐỒI (ELEVATION MARKERS)
    // =========================================================================
    private void SetupDecorativeBoulders()
    {
        var envRoot = GameObject.Find("_Environment_Map");
        if (envRoot == null) envRoot = new GameObject("_Environment_Map");

        Transform oldBoulders = envRoot.transform.Find("_DecorativeBoulders");
        if (oldBoulders != null) Destroy(oldBoulders.gameObject);

        GameObject boulderRoot = new GameObject("_DecorativeBoulders");
        boulderRoot.transform.SetParent(envRoot.transform, false);

        Material rockMat = GetOrCreateRoadMaterial("Mat_Stylized_RockBoulder", new Color(0.58f, 0.54f, 0.48f, 1f));

        Vector3[] rockSites = new[]
        {
            new Vector3(-13.5f, 0f, 7.5f),
            new Vector3(13.5f, 0f, 7.5f),
            new Vector3(-7.5f, 0f, 17.5f),
            new Vector3(7.5f, 0f, 17.5f),
            new Vector3(-15.0f, 0f, -5.0f),
            new Vector3(15.0f, 0f, -5.0f),
            new Vector3(-11.0f, 0f, -8.5f),
            new Vector3(12.5f, 0f, -14.0f)
        };

        for (int i = 0; i < rockSites.Length; i++)
        {
            Vector3 site = rockSites[i];
            site.y = CalculateHeight(site.x, site.z);

            // Sinh 2 tảng đá lớn nhỏ tạo thành cụm
            for (int r = 0; r < 2; r++)
            {
                float ox = (r == 0) ? 0f : 0.65f;
                float oz = (r == 0) ? 0f : -0.45f;
                Vector3 rPos = site + new Vector3(ox, 0f, oz);
                rPos.y = CalculateHeight(rPos.x, rPos.z) + 0.18f;

                var boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                boulder.name = $"Boulder_{i}_{r}";
                boulder.transform.SetParent(boulderRoot.transform, false);
                boulder.transform.position = rPos;
                boulder.transform.rotation = Quaternion.Euler(15f * r, i * 43f, 8f);

                float s = (r == 0) ? 0.85f : 0.55f;
                boulder.transform.localScale = new Vector3(s, s * 0.72f, s * 0.90f);

                var col = boulder.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var mr = boulder.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = rockMat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    mr.receiveShadows = true;
                }
            }
        }
    }

    // =========================================================================
    // 6. TRANG TRÍ HOA DẠI ĐA SẮC MÀU & BỤI CỎ 3D
    // =========================================================================
    private void SpawnStylizedFlora()
    {
        var envRoot = GameObject.Find("_Environment_Map");
        if (envRoot == null) envRoot = new GameObject("_Environment_Map");

        // Dọn sạch các hoa cũ
        var oldWildflowers = envRoot.transform.Find("Road_Network/_WildflowerAccents");
        if (oldWildflowers != null) Destroy(oldWildflowers.gameObject);

        Transform existingFlora = envRoot.transform.Find("_StylizedFlora_Decor");
        if (existingFlora != null) return; // Bảo toàn nếu đã sinh sẵn hoặc user tùy chỉnh

        GameObject floraRoot = new GameObject("_StylizedFlora_Decor");
        floraRoot.transform.SetParent(envRoot.transform, false);

        // BẢNG MÀU HOA TƯƠI SÁNG RỰC RỠ ĐA DẠNG:
        Color[] flowerPalette = new[]
        {
            new Color(1.00f, 0.82f, 0.12f, 1f), // 🌻 Vàng hoa cúc rực rỡ (Buttercup Yellow)
            new Color(1.00f, 0.45f, 0.65f, 1f), // 🌸 Hồng thắm hoa anh đào (Sakura Pink)
            new Color(1.00f, 0.55f, 0.10f, 1f), // 🍊 Cam hoàng hôn (Sunset Tangerine)
            new Color(0.68f, 0.42f, 0.95f, 1f), // 💜 Tím oải hương dịu dàng (Lavender Violet)
            new Color(0.98f, 0.98f, 1.00f, 1f), // 🌼 Trắng cúc họa mi tinh khôi (Daisy White)
            new Color(0.95f, 0.25f, 0.25f, 1f), // 🌺 Đỏ san hô rạng ngời (Coral Poppy)
            new Color(0.20f, 0.75f, 1.00f, 1f)  // 🩵 Xanh lưu ly trong trẻo (Sky Blue)
        };

        // Cache các Material hoa theo màu để tối ưu bộ nhớ
        Dictionary<int, Material> flowerMatCache = new Dictionary<int, Material>();
        for (int c = 0; c < flowerPalette.Length; c++)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = $"Mat_Flower_Color_{c}";
            mat.color = flowerPalette[c];
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", flowerPalette[c]);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.12f);
            mat.enableInstancing = true;
            flowerMatCache[c] = mat;
        }

        // Material cho Cuống & Lá (Xanh mạ tươi)
        Material stemMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        stemMat.color = new Color(0.28f, 0.68f, 0.16f, 1f);
        if (stemMat.HasProperty("_BaseColor")) stemMat.SetColor("_BaseColor", stemMat.color);

        // Material cho Nhụy hoa (Vàng nắng ấm)
        Material centerDiscMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        centerDiscMat.color = new Color(1.00f, 0.84f, 0.10f, 1f);
        if (centerDiscMat.HasProperty("_BaseColor")) centerDiscMat.SetColor("_BaseColor", centerDiscMat.color);

        // Material cho Bụi cỏ 3D
        Material tuftMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        tuftMat.color = new Color(0.48f, 0.82f, 0.20f, 1f);
        if (tuftMat.HasProperty("_BaseColor")) tuftMat.SetColor("_BaseColor", tuftMat.color);

        // 22 Vị trí cụm hoa duyên dáng ôm lấy con đường và sân bãi
        Vector3[] floraAnchorPoints = new[]
        {
            new Vector3(-3.2f, 0f, -16.0f),
            new Vector3(-0.6f, 0f, -16.5f),
            new Vector3(-3.0f, 0f, -12.5f),
            new Vector3(-0.2f, 0f, -11.5f),
            new Vector3(-2.6f, 0f, -7.5f),
            new Vector3(1.2f, 0f, -7.0f),
            new Vector3(-2.2f, 0f, -2.5f),
            new Vector3(1.8f, 0f, -2.5f),
            new Vector3(-1.8f, 0f, 2.5f),
            new Vector3(2.0f, 0f, 2.5f),
            new Vector3(-1.2f, 0f, 7.5f),
            new Vector3(2.5f, 0f, 7.5f),
            new Vector3(-7.5f, 0f, 2.5f),
            new Vector3(-7.5f, 0f, -2.5f),
            new Vector3(7.5f, 0f, 2.5f),
            new Vector3(7.5f, 0f, -2.5f),
            new Vector3(-3.8f, 0f, -10.5f),
            new Vector3(-6.5f, 0f, -11.5f),
            new Vector3(-11.5f, 0f, -15.5f),
            new Vector3(10.5f, 0f, 7.5f),
            new Vector3(-10.5f, 0f, 7.5f),
            new Vector3(11.5f, 0f, -12.5f)
        };

        Mesh grassMesh = CreateGrassTuftMesh();
        System.Random rng = new System.Random(2026);

        for (int i = 0; i < floraAnchorPoints.Length; i++)
        {
            Vector3 center = floraAnchorPoints[i];

            // 1. Sinh 2-3 bụi cỏ 3D
            int tuftCount = rng.Next(2, 4);
            for (int t = 0; t < tuftCount; t++)
            {
                float ox = (float)(rng.NextDouble() * 1.8 - 0.9);
                float oz = (float)(rng.NextDouble() * 1.8 - 0.9);
                Vector3 tPos = center + new Vector3(ox, 0f, oz);
                tPos.y = CalculateHeight(tPos.x, tPos.z);

                GameObject tuftObj = new GameObject($"GrassTuft_{i}_{t}");
                tuftObj.transform.SetParent(floraRoot.transform, false);
                tuftObj.transform.position = tPos;
                tuftObj.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360), 0f);
                float s = 0.75f + (float)rng.NextDouble() * 0.40f;
                tuftObj.transform.localScale = new Vector3(s, s, s);

                var mf = tuftObj.AddComponent<MeshFilter>();
                mf.sharedMesh = grassMesh;
                var mr = tuftObj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = tuftMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = true;
            }

            // 2. Sinh 4-6 bông hoa dại với MÀU SẮC NGẪU NHIÊN ĐA DẠNG CHO TỪNG BÔNG
            int flowerCount = rng.Next(4, 7);
            for (int f = 0; f < flowerCount; f++)
            {
                float ox = (float)(rng.NextDouble() * 1.6 - 0.8);
                float oz = (float)(rng.NextDouble() * 1.6 - 0.8);
                Vector3 fPos = center + new Vector3(ox, 0f, oz);
                fPos.y = CalculateHeight(fPos.x, fPos.z);

                // Mỗi bông hoa lấy 1 màu riêng biệt ngẫu nhiên từ bảng màu
                int colorIdx = (i * 3 + f * 2 + rng.Next(flowerPalette.Length)) % flowerPalette.Length;
                Material petalMat = flowerMatCache[colorIdx];

                GameObject flowerObj = CreateFlowerInstance($"Flower_{i}_{f}", petalMat, stemMat, centerDiscMat, rng);
                flowerObj.transform.SetParent(floraRoot.transform, false);
                flowerObj.transform.position = fPos;
            }
        }
    }

    private GameObject CreateFlowerInstance(string name, Material petalMat, Material stemMat, Material centerDiscMat, System.Random rng)
    {
        GameObject root = new GameObject(name);

        // Cuống hoa xanh mạ (Thon dài, đứng thẳng)
        var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stem.name = "Stem";
        stem.transform.SetParent(root.transform, false);
        stem.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        stem.transform.localScale = new Vector3(0.045f, 0.12f, 0.045f);
        var stemCol = stem.GetComponent<Collider>();
        if (stemCol != null) Destroy(stemCol);

        var stemMr = stem.GetComponent<MeshRenderer>();
        if (stemMr != null)
        {
            stemMr.sharedMaterial = stemMat;
            stemMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Đóa hoa nở tròn xinh xắn (Đường kính ~0.34m, rõ nét ở mọi góc nhìn)
        var blossom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        blossom.name = "Blossom";
        blossom.transform.SetParent(root.transform, false);
        blossom.transform.localPosition = new Vector3(0f, 0.24f, 0f);
        float sc = 0.32f + (float)rng.NextDouble() * 0.08f;
        blossom.transform.localScale = new Vector3(sc, sc * 0.62f, sc);
        var bCol = blossom.GetComponent<Collider>();
        if (bCol != null) Destroy(bCol);

        var blossomMr = blossom.GetComponent<MeshRenderer>();
        if (blossomMr != null)
        {
            blossomMr.sharedMaterial = petalMat;
            blossomMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Nhụy hoa vàng nắng ở giữa
        var centerDisc = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        centerDisc.name = "CenterDisc";
        centerDisc.transform.SetParent(blossom.transform, false);
        centerDisc.transform.localPosition = new Vector3(0f, 0.38f, 0f);
        centerDisc.transform.localScale = new Vector3(0.48f, 0.35f, 0.48f);
        var cCol = centerDisc.GetComponent<Collider>();
        if (cCol != null) Destroy(cCol);

        var centerMr = centerDisc.GetComponent<MeshRenderer>();
        if (centerMr != null)
        {
            centerMr.sharedMaterial = centerDiscMat;
            centerMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        return root;
    }

    private Mesh CreateGrassTuftMesh()
    {
        Mesh m = new Mesh();
        m.name = "StylizedGrassTuft_Mesh";

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        AddGrassBlade(verts, uvs, tris, new Vector3(-0.06f, 0f, 0f), 0.32f, -18f, 0.08f);
        AddGrassBlade(verts, uvs, tris, new Vector3(0.00f, 0f, 0.04f), 0.40f, 6f, 0.09f);
        AddGrassBlade(verts, uvs, tris, new Vector3(0.07f, 0f, -0.02f), 0.28f, 22f, 0.07f);

        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    private void AddGrassBlade(List<Vector3> verts, List<Vector2> uvs, List<int> tris, Vector3 root, float height, float rotY, float width)
    {
        int start = verts.Count;
        Quaternion rot = Quaternion.Euler(0f, rotY, 0f);

        Vector3 left = root + rot * new Vector3(-width * 0.5f, 0f, 0f);
        Vector3 right = root + rot * new Vector3(width * 0.5f, 0f, 0f);
        Vector3 tip = root + rot * new Vector3(0.02f, height, 0.06f);

        verts.Add(left);
        verts.Add(right);
        verts.Add(tip);

        verts.Add(right);
        verts.Add(left);
        verts.Add(tip);

        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(1f, 0f));
        uvs.Add(new Vector2(0.5f, 1f));

        uvs.Add(new Vector2(1f, 0f));
        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(0.5f, 1f));

        tris.Add(start);
        tris.Add(start + 1);
        tris.Add(start + 2);

        tris.Add(start + 3);
        tris.Add(start + 4);
        tris.Add(start + 5);
    }

    private Material GetOrCreateRoadMaterial(string matName, Color baseCol)
    {
        Material loaded = Resources.Load<Material>(matName);
        if (loaded != null) return loaded;

        Shader shader = Shader.Find("Universal Render Pipeline/StylizedDioramaLit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");

        Material mat = new Material(shader);
        mat.name = matName;
        mat.color = baseCol;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseCol);
        if (mat.HasProperty("_HighlightColor")) mat.SetColor("_HighlightColor", baseCol * 1.15f);
        if (mat.HasProperty("_ShadowTint")) mat.SetColor("_ShadowTint", baseCol * 0.65f);
        if (mat.HasProperty("_GroundGlow")) mat.SetFloat("_GroundGlow", 0.05f);
        if (mat.HasProperty("_TopographyStrength")) mat.SetFloat("_TopographyStrength", 0f);
        if (mat.HasProperty("_MeadowVariation")) mat.SetFloat("_MeadowVariation", 0f);
        mat.enableInstancing = true;
        return mat;
    }
}
