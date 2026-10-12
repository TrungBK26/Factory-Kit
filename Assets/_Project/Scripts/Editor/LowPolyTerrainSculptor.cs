using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bộ công cụ Chỉnh Sửa & San Phẳng Địa Hình Low-Poly (Terrain Sculptor):
/// 1. Tự động san phẳng đất dưới chân Xưởng mộc, Mỏ đá hoặc bất kỳ mô hình nào được di chuyển.
/// 2. Cung cấp cửa sổ cọ vẽ (Sculpt Brush) trực tiếp trong Scene View để nâng/hạ/san phẳng địa hình bằng chuột y hệt Unity Terrain!
/// </summary>
public class LowPolyTerrainSculptor : EditorWindow
{
    private enum BrushMode { Flatten, Lower, Raise, Smooth }

    private BrushMode currentMode = BrushMode.Flatten;
    private float brushRadius = 4.5f;
    private float targetHeight = 0.0f;
    private float brushStrength = 0.5f;
    private bool enableSceneBrush = false;

    [MenuItem("Factory/Terrain/Open Low-Poly Terrain Sculptor Window", priority = 14)]
    public static void OpenWindow()
    {
        var window = GetWindow<LowPolyTerrainSculptor>("Terrain Sculptor");
        window.minSize = new Vector2(340, 420);
        window.Show();
    }

    [MenuItem("Factory/Terrain/Auto-Flatten Ground Under Buildings", priority = 15)]
    public static void AutoFlattenUnderBuildingsMenu()
    {
        FlattenAllBuildings();
    }

    [MenuItem("Factory/Terrain/Flatten Ground Under Selected Object", priority = 16)]
    public static void FlattenUnderSelectedMenu()
    {
        var selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Thông báo", "Vui lòng chọn một GameObject (Ví dụ: Ngôi nhà hoặc Hang đá) trong Hierarchy/Scene trước!", "OK");
            return;
        }

        FlattenUnderGameObject(selected);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        GUILayout.Label("CÔNG CỤ SAN PHẲNG & CHỈNH ĐỊA HÌNH", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Địa hình game là Mesh Low-Poly nghệ thuật. Công cụ này cho phép bạn san phẳng đất dưới chân nhà hoặc dùng cọ vẽ hạ thấp đất theo ý muốn!", MessageType.Info);

        EditorGUILayout.Space(10);
        GUILayout.Label("1. SAN PHẲNG NHANH THEO MÔ HÌNH", EditorStyles.boldLabel);

        if (GUILayout.Button("🏡 San Phẳng Dưới Toàn Bộ Công Trình (Auto-Flatten)", GUILayout.Height(34)))
        {
            FlattenAllBuildings();
        }

        if (GUILayout.Button("🎯 San Phẳng Dưới Vật Thể Đang Chọn (Selected)", GUILayout.Height(28)))
        {
            if (Selection.activeGameObject != null)
            {
                FlattenUnderGameObject(Selection.activeGameObject);
            }
            else
            {
                EditorUtility.DisplayDialog("Nhắc nhở", "Hãy click chọn một model (nhà, mỏ đá, máy...) trong Scene trước!", "OK");
            }
        }

        EditorGUILayout.Space(14);
        GUILayout.Label("2. CỌ VẼ TRỰC TIẾP TRÊN SCENE VIEW (BRUSH)", EditorStyles.boldLabel);

        enableSceneBrush = EditorGUILayout.Toggle("Bật Cọ Vẽ Scene (Enable Brush)", enableSceneBrush);

        if (enableSceneBrush)
        {
            currentMode = (BrushMode)EditorGUILayout.EnumPopup("Chế độ cọ (Mode):", currentMode);
            brushRadius = EditorGUILayout.Slider("Bán kính cọ (Radius):", brushRadius, 1.0f, 15.0f);
            brushStrength = EditorGUILayout.Slider("Lực cọ (Strength):", brushStrength, 0.05f, 2.0f);

            if (currentMode == BrushMode.Flatten)
            {
                targetHeight = EditorGUILayout.FloatField("Độ cao mục tiêu (Target Y):", targetHeight);
                if (GUILayout.Button("Lấy độ cao từ vật thể đang chọn"))
                {
                    if (Selection.activeGameObject != null)
                    {
                        targetHeight = Selection.activeGameObject.transform.position.y;
                    }
                }
            }

            EditorGUILayout.HelpBox("Rê chuột lên địa hình trong cửa sổ Scene và Click / Kéo chuột trái để chỉnh sửa độ cao!", MessageType.Warning);
        }

        EditorGUILayout.Space(14);
        GUILayout.Label("3. LÀM MỚI NAVMESH", EditorStyles.boldLabel);
        if (GUILayout.Button("🔄 Cập Nhật Lại NavMesh", GUILayout.Height(26)))
        {
            RebakeNavMesh();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!enableSceneBrush) return;

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        var terrainObj = GameObject.Find("LowPoly_Terrain");
        if (terrainObj == null) return;

        var mc = terrainObj.GetComponent<MeshCollider>();
        if (mc == null) return;

        if (mc.Raycast(ray, out RaycastHit hit, 200f))
        {
            // Vẽ vòng tròn cọ vẽ trực quan
            Handles.color = currentMode == BrushMode.Flatten ? Color.cyan : (currentMode == BrushMode.Lower ? Color.red : Color.green);
            Handles.DrawWireDisc(hit.point, Vector3.up, brushRadius);
            Handles.DrawSolidDisc(hit.point, Vector3.up, 0.25f);
            sceneView.Repaint();

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                ApplyBrushAt(hit.point);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && !e.alt)
            {
                ApplyBrushAt(hit.point);
                e.Use();
            }
        }
    }

    private void ApplyBrushAt(Vector3 center)
    {
        switch (currentMode)
        {
            case BrushMode.Flatten:
                ModifyTerrainHeight(center, brushRadius, targetHeight, true, brushStrength);
                break;
            case BrushMode.Lower:
                ModifyTerrainHeight(center, brushRadius, -brushStrength * 0.5f, false, brushStrength);
                break;
            case BrushMode.Raise:
                ModifyTerrainHeight(center, brushRadius, brushStrength * 0.5f, false, brushStrength);
                break;
            case BrushMode.Smooth:
                // Smooth height
                ModifyTerrainHeight(center, brushRadius, center.y, true, brushStrength * 0.5f);
                break;
        }
    }

    /// <summary>
    /// Tự động quét và san phẳng đất dưới chân toàn bộ công trình
    /// </summary>
    public static void FlattenAllBuildings()
    {
        var terrainObj = GameObject.Find("LowPoly_Terrain");
        if (terrainObj == null)
        {
            Debug.LogWarning("[LowPolyTerrainSculptor] Không tìm thấy LowPoly_Terrain trong Scene!");
            return;
        }

        int count = 0;

        // 1. San phẳng dưới chân Xưởng mộc
        var ws = GameObject.Find("Model_RusticWorkshopShed") ?? GameObject.Find("Environment_Workshop");
        if (ws != null)
        {
            FlattenUnderGameObject(ws, radius: 5.2f, falloff: 2.0f);
            count++;
        }

        // 2. San phẳng dưới chân Mỏ đá / Hang đá
        var cave = GameObject.Find("Model_QuarryPitCave") ?? GameObject.Find("Environment_Cave");
        if (cave != null)
        {
            FlattenUnderGameObject(cave, radius: 5.8f, falloff: 2.2f);
            count++;
        }

        // 3. San phẳng dưới chân Tháp canh đỉnh đồi
        var tower = GameObject.Find("Model_HilltopWatchtower");
        if (tower != null)
        {
            FlattenUnderGameObject(tower, radius: 3.5f, falloff: 1.8f);
            count++;
        }

        RebakeNavMesh();

        EditorUtility.DisplayDialog(
            "San Phẳng Địa Hình Thành Công!",
            $"Đã tự động san phẳng hoàn hảo nền đất dưới chân {count} công trình!\n\n" +
            "✨ Toàn bộ phần đất nhô cao đâm xuyên sàn nhà/hang mỏ đã được gọt phẳng phiu.\n" +
            "✨ Đã cập nhật lại va chạm vật lý và NavMesh!",
            "Tuyệt vời!"
        );
    }

    /// <summary>
    /// San phẳng nền đất dưới chân một GameObject cụ thể
    /// </summary>
    public static void FlattenUnderGameObject(GameObject go, float radius = 0f, float falloff = 1.8f)
    {
        var terrainObj = GameObject.Find("LowPoly_Terrain");
        if (terrainObj == null || go == null) return;

        Vector3 pos = go.transform.position;

        // Tự động tính bán kính nếu không chỉ định
        if (radius <= 0f)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                radius = Mathf.Max(b.extents.x, b.extents.z) * 1.15f + 0.5f;
            }
            else
            {
                radius = 4.0f;
            }
        }

        float targetY = pos.y; // San phẳng bằng đúng đáy của vật thể
        ModifyTerrainHeight(pos, radius, targetY, isAbsoluteFlatten: true, falloff: falloff);
        RebakeNavMesh();
        Debug.Log($"[LowPolyTerrainSculptor] Đã san phẳng đất dưới chân '{go.name}' tại bán kính {radius:F1}m, độ cao Y={targetY:F2}m!");
    }

    /// <summary>
    /// Biến đổi độ cao các đỉnh Mesh địa hình quanh vị trí tâm
    /// </summary>
    public static void ModifyTerrainHeight(Vector3 center, float radius, float value, bool isAbsoluteFlatten, float falloff = 1.5f)
    {
        var terrainObj = GameObject.Find("LowPoly_Terrain");
        if (terrainObj == null) return;

        var mf = terrainObj.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        Mesh mesh = mf.sharedMesh;
        Vector3[] verts = mesh.vertices;
        bool modified = false;

        float totalRadius = radius + falloff;

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 v = verts[i];
            float dist = Vector2.Distance(new Vector2(v.x, v.z), new Vector2(center.x, center.z));

            if (dist < totalRadius)
            {
                if (isAbsoluteFlatten)
                {
                    if (dist <= radius)
                    {
                        // Khu vực cốt lõi dưới chân nhà: hạ phẳng tuyệt đối
                        v.y = value;
                    }
                    else
                    {
                        // Khu vực chuyển tiếp: vuốt dốc mượt mà (smooth blend)
                        float t = (dist - radius) / falloff;
                        float smooth = t * t * (3f - 2f * t);
                        v.y = Mathf.Lerp(value, v.y, smooth);
                    }
                }
                else
                {
                    // Chế độ nâng/hạ tương đối
                    float factor = 1.0f - Mathf.Clamp01(dist / radius);
                    v.y += value * factor;
                }

                verts[i] = v;
                modified = true;
            }
        }

        if (modified)
        {
            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var mc = terrainObj.GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.sharedMesh = null;
                mc.sharedMesh = mesh;
            }

            EditorSceneManager.MarkSceneDirty(terrainObj.scene);
        }
    }

    private static void RebakeNavMesh()
    {
        var terrainObj = GameObject.Find("LowPoly_Terrain");
        if (terrainObj != null)
        {
            var navSurface = terrainObj.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
            if (navSurface != null)
            {
                navSurface.BuildNavMesh();
            }
        }
    }
}
