using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bước 1: Khởi tạo Địa hình Low-Poly (Chỗ cao chỗ thấp, đồi núi uốn lượn, thung lũng phẳng trung tâm & lòng hồ nước)
/// theo đúng phong cách mỹ thuật của ảnh mẫu (Low-poly faceted rolling hills & valley).
/// Tích hợp Shader Stylized Diorama Lit với ánh sáng đổ bóng dịu và viền phản quang Rim Light cao cấp.
/// Đã khoét hành lang bằng phẳng cho con đường chính và đường đất để không bao giờ bị đè lấp.
/// </summary>
public static class LowPolyTerrainBuilder
{
    private const string MAT_GRASS_PATH = "Assets/_Project/Materials/Mat_Terrain_LowPolyGrass.mat";
    private const string MAT_WATER_PATH = "Assets/_Project/Materials/Mat_Stylized_Water.mat";

    [MenuItem("Factory/Map Setup/Step 1: Generate Low-Poly Hills Terrain", priority = 10)]
    public static void GenerateTerrainInActiveScene()
    {
        var activeScene = SceneManager.GetActiveScene();
        GenerateTerrainForScene(activeScene);
    }

    public static void GenerateTerrainForScene(Scene scene)
    {
        Debug.Log($"[LowPolyTerrain] === BẮT ĐẦU TẠO ĐỊA HÌNH LOW-POLY (BƯỚC 1) CHO SCENE: {scene.name} ===");

        // 1. Dọn sạch các model decor và sàn cũ bị lẫn lộn
        PurgeOldObsoleteDecor();

        // 2. Chuẩn bị Materials với Stylized Shaders mới
        Material matGrass = GetOrCreateGrassMaterial();
        Material matWater = GetOrCreateWaterMaterial();

        // 3. Tìm hoặc tạo GameObject gốc cho Môi trường
        var envRoot = GameObject.Find("_Environment_Map");
        if (envRoot == null)
        {
            envRoot = new GameObject("_Environment_Map");
        }

        // Xóa địa hình cũ nếu đã tồn tại
        var oldTerrain = envRoot.transform.Find("LowPoly_Terrain");
        if (oldTerrain != null)
        {
            Object.DestroyImmediate(oldTerrain.gameObject);
        }
        var oldLake = envRoot.transform.Find("LowPoly_Lake");
        if (oldLake != null)
        {
            Object.DestroyImmediate(oldLake.gameObject);
        }

        // 4. Tạo Mesh Địa Hình Low-Poly Faceted
        GameObject terrainObj = new GameObject("LowPoly_Terrain");
        terrainObj.transform.SetParent(envRoot.transform, false);
        terrainObj.transform.position = Vector3.zero;

        MeshFilter mf = terrainObj.AddComponent<MeshFilter>();
        MeshRenderer mr = terrainObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = matGrass;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        Mesh terrainMesh = BuildFacetedTerrainMesh();
        mf.sharedMesh = terrainMesh;

        // MeshCollider để người chơi và vật lý va chạm chính xác
        MeshCollider mc = terrainObj.AddComponent<MeshCollider>();
        mc.sharedMesh = terrainMesh;

        // NavMeshSurface cho AI di chuyển
        var navSurface = terrainObj.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
        navSurface.collectObjects = Unity.AI.Navigation.CollectObjects.All;
        navSurface.BuildNavMesh();

        // 5. Tạo Hồ Nước Nhỏ ở thung lũng Đông-Bắc (với Shader nước Stylized mới)
        GameObject lakeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lakeObj.name = "LowPoly_Lake";
        lakeObj.transform.SetParent(envRoot.transform, false);
        lakeObj.transform.position = new Vector3(14f, 1.45f, 20f);
        lakeObj.transform.localScale = new Vector3(10f, 0.05f, 10f);
        var lakeMr = lakeObj.GetComponent<MeshRenderer>();
        if (lakeMr != null) lakeMr.sharedMaterial = matWater;
        var lakeCol = lakeObj.GetComponent<Collider>();
        if (lakeCol != null) lakeCol.isTrigger = true;

        // 6. Cân chỉnh Ánh Sáng Ấm & Góc Chiếu chuẩn Diễn Họa (Golden Afternoon Sunlight)
        SetupAtmosphereAndLighting();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[LowPolyTerrain] === BƯỚC 1 HOÀN TẤT: Đã tạo xong địa hình đồi núi low-poly nhấp nhô & hồ nước! ===");
    }

    public static void PurgeOldObsoleteDecor()
    {
        // Xóa hoàn toàn cụm decor cũ _EnvironmentDecor
        var oldEnvDecor = GameObject.Find("_EnvironmentDecor");
        if (oldEnvDecor != null)
        {
            Object.DestroyImmediate(oldEnvDecor);
        }

        // Tắt hoặc xóa mặt sàn phẳng cũ
        var oldFloor = GameObject.Find("Ground_FactoryFloor");
        if (oldFloor != null) oldFloor.SetActive(false);
        var oldShowroom = GameObject.Find("Showroom_Floor");
        if (oldShowroom != null) oldShowroom.SetActive(false);

        // Xóa các đối tượng lẻ cũ nếu còn vương vãi
        string[] oldNames = { "Decor_TimberWorkshop", "Decor_Cave_Left", "Decor_Cave_Right" };
        foreach (var name in oldNames)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    private static Material GetOrCreateGrassMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MAT_GRASS_PATH);
        Shader shader = Shader.Find("Universal Render Pipeline/StylizedDioramaLit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = "Mat_Terrain_LowPolyGrass";
            AssetDatabase.CreateAsset(mat, MAT_GRASS_PATH);
        }
        else
        {
            if (mat.shader != shader) mat.shader = shader;
        }

        // Màu cỏ xanh đồng quê ấm áp, tươi tắn như ảnh mẫu
        mat.color = new Color(0.44f, 0.68f, 0.24f, 1.0f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.44f, 0.68f, 0.24f, 1.0f));
        if (mat.HasProperty("_ShadowTint")) mat.SetColor("_ShadowTint", new Color(0.24f, 0.44f, 0.22f, 1.0f));
        if (mat.HasProperty("_RimColor")) mat.SetColor("_RimColor", new Color(1.0f, 0.96f, 0.82f, 1.0f));
        if (mat.HasProperty("_RimIntensity")) mat.SetFloat("_RimIntensity", 0.40f);
        if (mat.HasProperty("_RimPower")) mat.SetFloat("_RimPower", 3.2f);
        if (mat.HasProperty("_FacetedNormal")) mat.SetFloat("_FacetedNormal", 1.0f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.04f);

        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateWaterMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MAT_WATER_PATH);
        Shader shader = Shader.Find("Universal Render Pipeline/StylizedDioramaWater");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = "Mat_Stylized_Water";
            AssetDatabase.CreateAsset(mat, MAT_WATER_PATH);
        }
        else
        {
            if (mat.shader != shader) mat.shader = shader;
        }

        if (mat.HasProperty("_ShallowColor")) mat.SetColor("_ShallowColor", new Color(0.28f, 0.88f, 0.86f, 0.85f));
        if (mat.HasProperty("_DeepColor")) mat.SetColor("_DeepColor", new Color(0.06f, 0.42f, 0.68f, 0.95f));
        if (mat.HasProperty("_FoamColor")) mat.SetColor("_FoamColor", new Color(0.95f, 1.0f, 1.0f, 0.9f));
        if (mat.HasProperty("_WaveSpeed")) mat.SetFloat("_WaveSpeed", 0.65f);
        if (mat.HasProperty("_WaveStrength")) mat.SetFloat("_WaveStrength", 0.08f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.92f);

        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Mesh BuildFacetedTerrainMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "FacetedLowPolyTerrain";

        int gridSize = 56;
        float totalWidth = 84f;
        float step = totalWidth / gridSize;
        float halfWidth = totalWidth * 0.5f;

        Vector3[,] grid = new Vector3[gridSize + 1, gridSize + 1];
        System.Random rng = new System.Random(1337);

        for (int i = 0; i <= gridSize; i++)
        {
            for (int j = 0; j <= gridSize; j++)
            {
                float x = -halfWidth + i * step;
                float z = -halfWidth + j * step;

                // Không jitter các điểm biên ngoài cùng
                bool isBorder = (i == 0 || i == gridSize || j == 0 || j == gridSize);
                float xJitter = isBorder ? 0f : (float)(rng.NextDouble() * 2f - 1f) * 0.28f;
                float zJitter = isBorder ? 0f : (float)(rng.NextDouble() * 2f - 1f) * 0.28f;

                x += xJitter;
                z += zJitter;

                float y = CalculateHeight(x, z);
                grid[i, j] = new Vector3(x, y, z);
            }
        }

        int totalQuads = gridSize * gridSize;
        int totalTris = totalQuads * 2;
        int totalVerts = totalTris * 3;

        Vector3[] vertices = new Vector3[totalVerts];
        Vector3[] normals = new Vector3[totalVerts];
        Vector2[] uvs = new Vector2[totalVerts];
        int[] triangles = new int[totalVerts];

        int vIdx = 0;

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                Vector3 p00 = grid[i, j];
                Vector3 p10 = grid[i + 1, j];
                Vector3 p01 = grid[i, j + 1];
                Vector3 p11 = grid[i + 1, j + 1];

                bool alternateDiagonal = ((i + j) % 2 == 0);

                if (alternateDiagonal)
                {
                    AddFlatTriangle(p00, p11, p10, vertices, normals, uvs, triangles, ref vIdx, halfWidth);
                    AddFlatTriangle(p00, p01, p11, vertices, normals, uvs, triangles, ref vIdx, halfWidth);
                }
                else
                {
                    AddFlatTriangle(p00, p01, p10, vertices, normals, uvs, triangles, ref vIdx, halfWidth);
                    AddFlatTriangle(p10, p01, p11, vertices, normals, uvs, triangles, ref vIdx, halfWidth);
                }
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        return mesh;
    }

    private static void AddFlatTriangle(
        Vector3 a, Vector3 b, Vector3 c,
        Vector3[] vertices, Vector3[] normals, Vector2[] uvs, int[] triangles,
        ref int idx, float halfWidth)
    {
        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;

        int i0 = idx++;
        int i1 = idx++;
        int i2 = idx++;

        vertices[i0] = a;
        vertices[i1] = b;
        vertices[i2] = c;

        normals[i0] = normal;
        normals[i1] = normal;
        normals[i2] = normal;

        uvs[i0] = new Vector2((a.x + halfWidth) / (halfWidth * 2f), (a.z + halfWidth) / (halfWidth * 2f));
        uvs[i1] = new Vector2((b.x + halfWidth) / (halfWidth * 2f), (b.z + halfWidth) / (halfWidth * 2f));
        uvs[i2] = new Vector2((c.x + halfWidth) / (halfWidth * 2f), (c.z + halfWidth) / (halfWidth * 2f));

        triangles[i0] = i0;
        triangles[i1] = i1;
        triangles[i2] = i2;
    }

    /// <summary>
    /// Hàm tính toán độ cao đồi núi nhấp nhô theo bố cục ảnh mẫu:
    /// - Thung lũng trung tâm phẳng (Y = 0) cho xưởng mộc, hố đá, bệ máy và đường đi.
    /// - Đồi cao phía sau (Bắc) với đỉnh đồi tháp canh (Y = 7m - 8.5m).
    /// - Lòng chảo trũng tạo hồ nước ở Đông-Bắc (Y = 1.0m, mặt hồ 1.45m).
    /// - Dãy đồi uốn lượn bên Tây (Y = 3m - 5m) với khe hõm cho con đường đất uốn lượn ở Bước 2.
    /// - Dãy đồi bên Đông (Y = 3m - 5.5m).
    /// - Gờ đồi thấp bao quanh viền trước (Y = 1m - 2m).
    /// - Hành lang đường bê tông chính được khoét phẳng hoàn toàn để con đường luôn nổi rõ rệt!
    /// </summary>
    public static float CalculateHeight(float x, float z)
    {
        // 1. Thung Lũng Trung Tâm & Sân Xưởng
        // Giữ phẳng tuyệt đối trong bán kính 13m quanh tâm (0, 0)
        float centralDist = Mathf.Sqrt(x * x * 0.9f + z * z);
        if (centralDist < 13f)
        {
            return 0f;
        }

        float height = 0f;

        // 2. Dãy Đồi Cao Phía Sau (North Ridge: Z > 10)
        if (z > 10f)
        {
            float zBlend = Mathf.Clamp01((z - 10f) / 10f);

            // Đỉnh đồi tháp canh cao nhất (X = -8, Z = 26)
            float towerHillDist = Mathf.Sqrt(Mathf.Pow(x - (-8f), 2) + Mathf.Pow(z - 26f, 2));
            float towerHill = Mathf.Max(0f, 1f - towerHillDist / 12f) * 8.5f;

            // Đỉnh đồi phía sau bên phải (X = 26, Z = 26)
            float rightHillDist = Mathf.Sqrt(Mathf.Pow(x - 26f, 2) + Mathf.Pow(z - 26f, 2));
            float rightHill = Mathf.Max(0f, 1f - rightHillDist / 14f) * 6.5f;

            // Đồi nhấp nhô giữa (X = 3, Z = 30)
            float midHillDist = Mathf.Sqrt(Mathf.Pow(x - 3f, 2) + Mathf.Pow(z - 30f, 2));
            float midHill = Mathf.Max(0f, 1f - midHillDist / 12f) * 7.0f;

            // Nền đồi thoai thoải phía sau
            float backgroundRidge = zBlend * 4.2f;

            height += Mathf.Max(backgroundRidge, Mathf.Max(towerHill, Mathf.Max(rightHill, midHill)));

            // Lòng chảo hồ nước (X = 14, Z = 20): Tạo hố lõm tự nhiên giữa các ngọn đồi
            float lakeDist = Mathf.Sqrt(Mathf.Pow(x - 14f, 2) + Mathf.Pow(z - 20f, 2));
            if (lakeDist < 9f)
            {
                float lakeDip = Mathf.Cos(lakeDist / 9f * Mathf.PI * 0.5f) * 3.5f;
                height = Mathf.Max(1.0f, height - lakeDip);
            }
        }

        // 3. Dãy Đồi Bên Trái (West Ridge: X < -10)
        if (x < -10f)
        {
            float xBlend = Mathf.Clamp01((-x - 10f) / 10f);

            float hillW1 = Mathf.Max(0f, 1f - Mathf.Sqrt(Mathf.Pow(x - (-22f), 2) + Mathf.Pow(z - 6f, 2)) / 14f) * 4.8f;
            float hillW2 = Mathf.Max(0f, 1f - Mathf.Sqrt(Mathf.Pow(x - (-24f), 2) + Mathf.Pow(z - (-14f), 2)) / 14f) * 4.2f;

            // Khe hõm tự nhiên cho con đường đất quanh co (Bước 2)
            float trailCenterX = -16f + Mathf.Sin(z * 0.18f) * 4.2f;
            float pathDist = Mathf.Abs(x - trailCenterX);
            float pathDamp = Mathf.Clamp01(pathDist / 3.8f);

            float westBase = xBlend * 3.5f;
            float westTotal = Mathf.Max(westBase, Mathf.Max(hillW1, hillW2)) * Mathf.Lerp(0.35f, 1.0f, pathDamp);
            height = Mathf.Max(height, westTotal);
        }

        // 4. Dãy Đồi Bên Phải (East Ridge: X > 10)
        if (x > 10f && z <= 12f)
        {
            float xBlend = Mathf.Clamp01((x - 10f) / 10f);
            float hillE1 = Mathf.Max(0f, 1f - Mathf.Sqrt(Mathf.Pow(x - 24f, 2) + Mathf.Pow(z - 2f, 2)) / 14f) * 5.2f;
            float hillE2 = Mathf.Max(0f, 1f - Mathf.Sqrt(Mathf.Pow(x - 22f, 2) + Mathf.Pow(z - (-16f), 2)) / 14f) * 4.0f;
            float eastTotal = Mathf.Max(xBlend * 3.2f, Mathf.Max(hillE1, hillE2));
            height = Mathf.Max(height, eastTotal);
        }

        // 5. Gờ Đồi Thấp Viền Trước (South Edge: Z < -12)
        if (z < -12f)
        {
            float sBlend = Mathf.Clamp01((-z - 12f) / 10f);
            float frontKnoll = Mathf.Max(0f, 1f - Mathf.Sqrt(Mathf.Pow(x - 12f, 2) + Mathf.Pow(z - (-26f), 2)) / 12f) * 2.8f;
            height = Mathf.Max(height, sBlend * 2.0f + frontKnoll);
        }

        // 6. Chuyển tiếp mượt mà vào thung lũng trung tâm (Soft Falloff)
        if (centralDist < 20f)
        {
            float t = (centralDist - 13f) / 7f;
            float smooth = t * t * (3f - 2f * t);
            height *= smooth;
        }

        // 7. QUAN TRỌNG: Khoét hành lang phẳng cho Con Đường Bê Tông Chính dẫn vào từ cổng trước (Z < 5m, quanh X = -2.2m)
        // Đảm bảo con đường bê tông KHÔNG BAO GIỜ bị gờ đồi phía trước đè lấp!
        float mainRoadDistX = Mathf.Abs(x - (-2.2f));
        if (mainRoadDistX < 4.0f && z < 5f)
        {
            float roadCutFactor = Mathf.Clamp01(mainRoadDistX / 4.0f);
            height *= roadCutFactor * roadCutFactor; // Hạ phẳng về 0m tại tim đường
        }

        return height;
    }

    private static void SetupAtmosphereAndLighting()
    {
        var lightObj = GameObject.Find("Directional Light");
        if (lightObj == null)
        {
            lightObj = new GameObject("Directional Light");
            var l = lightObj.AddComponent<Light>();
            l.type = LightType.Directional;
        }

        var light = lightObj.GetComponent<Light>();
        if (light != null)
        {
            light.type = LightType.Directional;
            light.intensity = 1.30f;
            light.color = new Color(1.0f, 0.96f, 0.88f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.60f;
        }

        lightObj.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.85f, 0.92f, 1.0f);
        RenderSettings.ambientEquatorColor = new Color(0.92f, 0.88f, 0.78f);
        RenderSettings.ambientGroundColor = new Color(0.42f, 0.52f, 0.30f);
    }
}
