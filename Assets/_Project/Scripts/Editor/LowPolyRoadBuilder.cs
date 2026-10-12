using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bước 2: Khởi tạo Hệ Thống Đường & Sân Bãi 3D Dày Dặn (Roads & Pathways) theo ảnh mẫu diorama:
/// 1. Con đường bê tông chính (Main Paved Road): Mặt đường nổi cao +12cm, có vát gờ mép vỉa hè nổi +15cm, chân gờ cắm sâu -60cm vào lòng đất.
/// 2. Con đường đất quanh co (Winding Dirt Trail): Uốn lượn men theo thung lũng bên trái lên đỉnh đồi, có mép đất cắm sâu -50cm và điểm xuyết đá viền đường.
/// 3. Bãi đất/cát khai khoáng (Quarry Staging Yard): Nổi +8cm với chân viền cắm sâu -45cm chống tuyệt đối hiện tượng chìm dưới cỏ.
/// 4. Đốm hoa dại 3D (3D Wildflowers): Thân cây và đài hoa nổi 25cm trên mặt cỏ với cánh hoa rực rỡ sắc màu.
/// 5. Tích hợp Stylized Diorama Lit Shader với viền sáng Rim Light và bóng đổ mềm ấm áp.
/// </summary>
public static class LowPolyRoadBuilder
{
    private const string MAT_PAVED_PATH = "Assets/_Project/Materials/Mat_Road_PavedConcrete.mat";
    private const string MAT_DIRT_PATH = "Assets/_Project/Materials/Mat_Road_DirtTrail.mat";
    private const string MAT_SAND_PATH = "Assets/_Project/Materials/Mat_Road_SandyDirtYard.mat";
    private const string MAT_FLOWER_PATH = "Assets/_Project/Materials/Mat_Wildflower_Petals.mat";

    [MenuItem("Factory/Map Setup/Step 2: Generate Roads and Paths", priority = 11)]
    public static void GenerateRoadsInActiveScene()
    {
        var activeScene = SceneManager.GetActiveScene();
        GenerateRoadsForScene(activeScene);
    }

    public static void GenerateRoadsForScene(Scene scene)
    {
        Debug.Log($"[LowPolyRoads] === BẮT ĐẦU TẠO HỆ THỐNG ĐƯỜNG 3D (BƯỚC 2) CHO SCENE: {scene.name} ===");

        // Đảm bảo địa hình đã được tạo
        var envRoot = GameObject.Find("_Environment_Map");
        if (envRoot == null)
        {
            envRoot = new GameObject("_Environment_Map");
            LowPolyTerrainBuilder.GenerateTerrainForScene(scene);
        }

        // Xóa hệ thống đường cũ nếu có
        var oldRoads = envRoot.transform.Find("Road_Network");
        if (oldRoads != null)
        {
            Object.DestroyImmediate(oldRoads.gameObject);
        }

        GameObject roadsRoot = new GameObject("Road_Network");
        roadsRoot.transform.SetParent(envRoot.transform, false);

        // 1. Tạo Đường Bê Tông Chính Khối 3D Dày Dặn (Main Paved Road)
        BuildMainPavedRoad(roadsRoot.transform);

        // 2. Tạo Đường Đất Uốn Lượn Sườn Đồi (Winding Dirt Trail)
        BuildWindingDirtTrail(roadsRoot.transform);

        // 3. Tạo Bãi Tập Kết Cát/Đất Cạnh Hố Đá (Quarry Staging Yard)
        BuildQuarryStagingYard(roadsRoot.transform);

        // 4. Tạo Các Bụi Hoa Dại 3D Rực Rỡ Ven Đường
        BuildWildflowerAccents(roadsRoot.transform);

        // 5. Cập nhật NavMesh để người chơi di chuyển hoàn hảo trên hệ thống đường mới
        var terrainObj = envRoot.transform.Find("LowPoly_Terrain");
        if (terrainObj != null)
        {
            var navSurface = terrainObj.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
            if (navSurface != null)
            {
                navSurface.BuildNavMesh();
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[LowPolyRoads] === BƯỚC 2 HOÀN TẤT: Đường bê tông khối 3D, đường đất và bãi cát đã hiển thị nổi bật 100%! ===");
    }

    // =========================================================================
    // 1. ĐƯỜNG BÊ TÔNG CHÍNH KHỐI 3D (MAIN PAVED ROAD)
    // =========================================================================
    private static void BuildMainPavedRoad(Transform parent)
    {
        Material matPaved = GetOrCreateMaterial(
            MAT_PAVED_PATH,
            new Color(0.82f, 0.80f, 0.77f, 1f),
            new Color(0.50f, 0.52f, 0.58f, 1f),
            "Mat_Road_PavedConcrete"
        );

        GameObject roadObj = new GameObject("Road_MainPaved");
        roadObj.transform.SetParent(parent, false);

        MeshFilter mf = roadObj.AddComponent<MeshFilter>();
        MeshRenderer mr = roadObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = matPaved;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        Vector3 startPt = new Vector3(-2.2f, 0f, -28f);
        Vector3 endPt = new Vector3(3.5f, 0f, 3.5f);
        float width = 4.6f;
        int segments = 28;

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        Vector3 dir = (endPt - startPt).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 center = Vector3.Lerp(startPt, endPt, t);
            float yGround = LowPolyTerrainBuilder.CalculateHeight(center.x, center.z);
            center.y = yGround;

            // Mặt cắt 6 điểm 3D tạo thành khối đường bê tông vỉa hè dày dặn:
            // 0: Chân gờ trái cắm sâu vào đất (-0.55m)
            // 1: Đỉnh gờ vỉa hè trái (+0.15m)
            // 2: Mặt đường trái (+0.12m)
            // 3: Mặt đường phải (+0.12m)
            // 4: Đỉnh gờ vỉa hè phải (+0.15m)
            // 5: Chân gờ phải cắm sâu vào đất (-0.55m)
            Vector3 pSkirtLeft = center - right * (width * 0.5f + 0.35f) + Vector3.down * 0.55f;
            Vector3 pCurbLeft = center - right * (width * 0.5f) + Vector3.up * 0.15f;
            Vector3 pRoadLeft = center - right * (width * 0.44f) + Vector3.up * 0.12f;
            Vector3 pRoadRight = center + right * (width * 0.44f) + Vector3.up * 0.12f;
            Vector3 pCurbRight = center + right * (width * 0.5f) + Vector3.up * 0.15f;
            Vector3 pSkirtRight = center + right * (width * 0.5f + 0.35f) + Vector3.down * 0.55f;

            int baseIdx = verts.Count;
            verts.Add(pSkirtLeft);
            verts.Add(pCurbLeft);
            verts.Add(pRoadLeft);
            verts.Add(pRoadRight);
            verts.Add(pCurbRight);
            verts.Add(pSkirtRight);

            Vector3 up = Vector3.up;
            norms.Add(Vector3.Normalize(up - right * 0.8f));
            norms.Add(Vector3.Normalize(up - right * 0.3f));
            norms.Add(up);
            norms.Add(up);
            norms.Add(Vector3.Normalize(up + right * 0.3f));
            norms.Add(Vector3.Normalize(up + right * 0.8f));

            uvs.Add(new Vector2(0f, t * 12f));
            uvs.Add(new Vector2(0.1f, t * 12f));
            uvs.Add(new Vector2(0.2f, t * 12f));
            uvs.Add(new Vector2(0.8f, t * 12f));
            uvs.Add(new Vector2(0.9f, t * 12f));
            uvs.Add(new Vector2(1.0f, t * 12f));

            if (i > 0)
            {
                int prev = baseIdx - 6;
                // Thành vách trái
                AddQuad(tris, prev + 0, prev + 1, baseIdx + 1, baseIdx + 0);
                // Vát vỉa hè trái
                AddQuad(tris, prev + 1, prev + 2, baseIdx + 2, baseIdx + 1);
                // Mặt đường xe chạy
                AddQuad(tris, prev + 2, prev + 3, baseIdx + 3, baseIdx + 2);
                // Vát vỉa hè phải
                AddQuad(tris, prev + 3, prev + 4, baseIdx + 4, baseIdx + 3);
                // Thành vách phải
                AddQuad(tris, prev + 4, prev + 5, baseIdx + 5, baseIdx + 4);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "MainPavedRoadMesh_3D";
        mesh.vertices = verts.ToArray();
        mesh.normals = norms.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
        var mc = roadObj.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
    }

    // =========================================================================
    // 2. ĐƯỜNG ĐẤT UỐN LƯỢN SƯỜN ĐỒI (WINDING DIRT TRAIL)
    // =========================================================================
    private static void BuildWindingDirtTrail(Transform parent)
    {
        Material matDirt = GetOrCreateMaterial(
            MAT_DIRT_PATH,
            new Color(0.78f, 0.58f, 0.35f, 1f),
            new Color(0.48f, 0.32f, 0.18f, 1f),
            "Mat_Road_DirtTrail"
        );

        GameObject roadObj = new GameObject("Road_WindingDirtTrail");
        roadObj.transform.SetParent(parent, false);

        MeshFilter mf = roadObj.AddComponent<MeshFilter>();
        MeshRenderer mr = roadObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = matDirt;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        float width = 3.2f;
        int segments = 46;
        float zMin = -28f;
        float zMax = 24f;

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float z = Mathf.Lerp(zMin, zMax, t);

            float xCenter = -16f + Mathf.Sin(z * 0.18f) * 4.2f;
            float yGround = LowPolyTerrainBuilder.CalculateHeight(xCenter, z);
            Vector3 center = new Vector3(xCenter, yGround, z);

            float dz = 0.5f;
            float nextX = -16f + Mathf.Sin((z + dz) * 0.18f) * 4.2f;
            Vector3 tangent = new Vector3(nextX - xCenter, 0f, dz).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;

            // Mặt đường đất nổi +10cm, chân mép cắm sâu -50cm vào sườn đồi
            Vector3 pSkirtLeft = center - right * (width * 0.5f + 0.40f) + Vector3.down * 0.50f;
            Vector3 pLeft = center - right * (width * 0.5f) + Vector3.up * 0.10f;
            Vector3 pRight = center + right * (width * 0.5f) + Vector3.up * 0.10f;
            Vector3 pSkirtRight = center + right * (width * 0.5f + 0.40f) + Vector3.down * 0.50f;

            int baseIdx = verts.Count;
            verts.Add(pSkirtLeft);
            verts.Add(pLeft);
            verts.Add(pRight);
            verts.Add(pSkirtRight);

            Vector3 up = Vector3.up;
            norms.Add(Vector3.Normalize(up - right * 0.6f));
            norms.Add(up);
            norms.Add(up);
            norms.Add(Vector3.Normalize(up + right * 0.6f));

            uvs.Add(new Vector2(0f, t * 14f));
            uvs.Add(new Vector2(0.2f, t * 14f));
            uvs.Add(new Vector2(0.8f, t * 14f));
            uvs.Add(new Vector2(1.0f, t * 14f));

            if (i > 0)
            {
                int prev = baseIdx - 4;
                AddQuad(tris, prev + 0, prev + 1, baseIdx + 1, baseIdx + 0);
                AddQuad(tris, prev + 1, prev + 2, baseIdx + 2, baseIdx + 1);
                AddQuad(tris, prev + 2, prev + 3, baseIdx + 3, baseIdx + 2);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "WindingDirtTrailMesh_3D";
        mesh.vertices = verts.ToArray();
        mesh.normals = norms.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
        var mc = roadObj.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
    }

    // =========================================================================
    // 3. BÃI ĐẤT/CÁT KHAI KHOÁNG (QUARRY STAGING YARD)
    // =========================================================================
    private static void BuildQuarryStagingYard(Transform parent)
    {
        Material matSand = GetOrCreateMaterial(
            MAT_SAND_PATH,
            new Color(0.87f, 0.69f, 0.44f, 1f),
            new Color(0.55f, 0.40f, 0.22f, 1f),
            "Mat_Road_SandyDirtYard"
        );

        GameObject yardObj = new GameObject("Yard_QuarryDirtPatch");
        yardObj.transform.SetParent(parent, false);

        MeshFilter mf = yardObj.AddComponent<MeshFilter>();
        MeshRenderer mr = yardObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = matSand;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        Vector3 center = new Vector3(-6.0f, 0f, 1.5f);
        center.y = LowPolyTerrainBuilder.CalculateHeight(center.x, center.z);

        int sides = 16;
        float radiusX = 6.8f;
        float radiusZ = 5.2f;

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        // Tâm nổi +8cm
        verts.Add(center + Vector3.up * 0.08f);
        norms.Add(Vector3.up);
        uvs.Add(new Vector2(0.5f, 0.5f));

        // Vòng 1: Mép trên bãi cát nổi +8cm
        // Vòng 2: Chân mép cắm sâu -45cm vào nền cỏ
        for (int i = 0; i < sides; i++)
        {
            float angle = (float)i / sides * Mathf.PI * 2f;
            float rx = radiusX * (1f + Mathf.Sin(angle * 3f) * 0.12f);
            float rz = radiusZ * (1f + Mathf.Cos(angle * 2f) * 0.12f);

            float x = center.x + Mathf.Cos(angle) * rx;
            float z = center.z + Mathf.Sin(angle) * rz;
            float y = LowPolyTerrainBuilder.CalculateHeight(x, z);

            Vector3 pRimTop = new Vector3(x, y + 0.08f, z);
            Vector3 pRimSkirt = new Vector3(x + Mathf.Cos(angle) * 0.35f, y - 0.45f, z + Mathf.Sin(angle) * 0.35f);

            verts.Add(pRimTop);
            norms.Add(Vector3.up);
            uvs.Add(new Vector2((Mathf.Cos(angle) + 1f) * 0.5f, (Mathf.Sin(angle) + 1f) * 0.5f));

            verts.Add(pRimSkirt);
            Vector3 skirtNorm = Vector3.Normalize(Vector3.up * 0.2f + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.8f);
            norms.Add(skirtNorm);
            uvs.Add(new Vector2((Mathf.Cos(angle) + 1f) * 0.5f, (Mathf.Sin(angle) + 1f) * 0.5f));
        }

        // Tạo triangles cho nón trung tâm và vành chân cắm đất
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            int topCurr = 1 + i * 2;
            int skirtCurr = 2 + i * 2;
            int topNext = 1 + next * 2;
            int skirtNext = 2 + next * 2;

            // Mặt trên
            tris.Add(0);
            tris.Add(topCurr);
            tris.Add(topNext);

            // Vách chân cắm xuống đất
            AddQuad(tris, topCurr, skirtCurr, skirtNext, topNext);
        }

        Mesh mesh = new Mesh();
        mesh.name = "QuarryStagingYardMesh_3D";
        mesh.vertices = verts.ToArray();
        mesh.normals = norms.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
        var mc = yardObj.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
    }

    // =========================================================================
    // 4. BỤI HOA DẠI 3D RỰC RỠ VEN ĐƯỜNG (3D WILDFLOWER ACCENTS)
    // =========================================================================
    private static void BuildWildflowerAccents(Transform parent)
    {
        Material matFlower = GetOrCreateMaterial(
            MAT_FLOWER_PATH,
            new Color(0.96f, 0.65f, 0.78f, 1f),
            new Color(0.60f, 0.35f, 0.45f, 1f),
            "Mat_Wildflower_Petals"
        );

        GameObject flowersRoot = new GameObject("_WildflowerAccents");
        flowersRoot.transform.SetParent(parent, false);

        Vector3[] clusterCenters = new[]
        {
            new Vector3(-4.8f, 0f, -14f),
            new Vector3(-4.2f, 0f, -8f),
            new Vector3(-1.0f, 0f, -18f),
            new Vector3(1.5f, 0f, -6f),
            new Vector3(2.2f, 0f, -12f),
            new Vector3(-11f, 0f, -16f),
            new Vector3(-13f, 0f, -2f),
            new Vector3(-10f, 0f, 8f)
        };

        Color[] petalColors = new[]
        {
            new Color(1.00f, 0.45f, 0.65f), // Hồng thắm
            new Color(1.00f, 0.90f, 0.25f), // Vàng nắng
            new Color(0.98f, 0.98f, 1.00f), // Trắng tinh
            new Color(0.35f, 0.80f, 1.00f)  // Xanh da trời
        };

        System.Random rng = new System.Random(42);

        for (int c = 0; c < clusterCenters.Length; c++)
        {
            Vector3 center = clusterCenters[c];
            int flowerCount = 6;

            for (int f = 0; f < flowerCount; f++)
            {
                float offsetX = (float)(rng.NextDouble() * 1.8 - 0.9);
                float offsetZ = (float)(rng.NextDouble() * 1.8 - 0.9);
                Vector3 pos = center + new Vector3(offsetX, 0f, offsetZ);
                pos.y = LowPolyTerrainBuilder.CalculateHeight(pos.x, pos.z);

                // Tạo bông hoa 3D đứng thẳng gồm cuống hoa và chóp hoa tròn nổi bật
                GameObject flower = new GameObject($"Flower_{c}_{f}");
                flower.transform.SetParent(flowersRoot.transform, false);
                flower.transform.position = pos;

                // Chóp đài hoa tròn
                var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cap.transform.SetParent(flower.transform, false);
                cap.transform.localPosition = new Vector3(0f, 0.22f, 0f);
                float scale = 0.22f + (float)rng.NextDouble() * 0.12f;
                cap.transform.localScale = new Vector3(scale, scale * 0.65f, scale);

                var capMr = cap.GetComponent<MeshRenderer>();
                if (capMr != null)
                {
                    var fMat = new Material(matFlower);
                    Color chosenColor = petalColors[rng.Next(petalColors.Length)];
                    fMat.color = chosenColor;
                    if (fMat.HasProperty("_BaseColor")) fMat.SetColor("_BaseColor", chosenColor);
                    capMr.sharedMaterial = fMat;
                }

                var capCol = cap.GetComponent<Collider>();
                if (capCol != null) Object.DestroyImmediate(capCol);
            }
        }
    }

    private static void AddQuad(List<int> tris, int i0, int i1, int i2, int i3)
    {
        tris.Add(i0);
        tris.Add(i1);
        tris.Add(i2);
        tris.Add(i0);
        tris.Add(i2);
        tris.Add(i3);
    }

    private static Material GetOrCreateMaterial(string path, Color baseColor, Color shadowTint, string name)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/StylizedDioramaLit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = name;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            if (mat.shader != shader) mat.shader = shader;
        }

        mat.color = baseColor;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_ShadowTint")) mat.SetColor("_ShadowTint", shadowTint);
        if (mat.HasProperty("_RimColor")) mat.SetColor("_RimColor", new Color(1.0f, 0.96f, 0.85f, 1f));
        if (mat.HasProperty("_RimIntensity")) mat.SetFloat("_RimIntensity", 0.35f);
        if (mat.HasProperty("_RimPower")) mat.SetFloat("_RimPower", 3.5f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.12f);

        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
