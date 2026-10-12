using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor cho LowPolyRoadSpline:
/// Cung cấp Gizmos & Handles trực quan trong Scene View để người dùng kéo thả,
/// nắn đường cong uốn lượn thủ công tự nhiên như ảnh mẫu.
/// Tích hợp công cụ Khép Kín Vòng Lặp (Closed Loop), Bo góc vuông 90° / 30°, và Đồng bộ Road_Network.
/// </summary>
[CustomEditor(typeof(LowPolyRoadSpline))]
public class LowPolyRoadSplineEditor : Editor
{
    private LowPolyRoadSpline road;
    private int selectedWaypointIndex = -1;

    private void OnEnable()
    {
        road = (LowPolyRoadSpline)target;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space(6);

        // 1. HỘP CẤU HÌNH ĐỘ CAO (CHỐNG NHÔ LÊN CUBE/MODEL)
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🛣️ 1. CẤU HÌNH ĐỘ CAO (CHỐNG NHÔ LÊN CUBE / MODEL)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        RoadHeightMode newMode = (RoadHeightMode)EditorGUILayout.EnumPopup("Height Mode", road.heightMode);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(road, "Change Road Height Mode");
            road.heightMode = newMode;
            if (road.heightMode == RoadHeightMode.PureGroundOnly)
            {
                road.SnapAllWaypointsToGround();
            }
            road.GenerateRoadMesh();
            SceneView.RepaintAll();
        }

        if (road.heightMode == RoadHeightMode.PureGroundOnly)
        {
            EditorGUILayout.HelpBox("✅ Pure Ground Only: Đường luôn bám sát mặt đất, đi xuyên BÊN DƯỚI mọi Cube / Model / Nhà xưởng (Không bao giờ bị nhô lên)!", MessageType.Info);
        }
        else if (road.heightMode == RoadHeightMode.ManualWaypointY)
        {
            EditorGUILayout.HelpBox("✏️ Manual Waypoint Y: Giữ nguyên trục Y do bạn tự tay kéo trong Scene View.", MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox("⛰️ Terrain Collider Only: Bám sát Collider mặt đất, bỏ qua vật cản khác.", MessageType.None);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 2. HỘP CẤU HÌNH HÌNH DÁNG & GÓC RẼ (CLOSED LOOP & CORNER RADIUS)
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🔄 2. HÌNH DÁNG & GÓC RẼ (CLOSED LOOP & CORNER)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        bool newLoop = EditorGUILayout.Toggle("Khép Kín Vòng Lặp (Closed Loop)", road.isClosedLoop);
        RoadCurveStyle newStyle = (RoadCurveStyle)EditorGUILayout.EnumPopup("Kiểu Bo Góc (Curve Style)", road.curveStyle);

        float newRadius = road.cornerFilletRadius;
        if (newStyle == RoadCurveStyle.ControlledCorner)
        {
            newRadius = EditorGUILayout.Slider("Bán Kính Bo Góc (Corner Radius)", road.cornerFilletRadius, 0f, 6f);
            if (newRadius <= 0.08f)
            {
                EditorGUILayout.HelpBox("📐 Bán kính = 0: Góc vuông 90° sắc nét tuyệt đối (Dạng ô vuông thẳng tắp)!", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox($"✨ Bán kính = {newRadius:F1}m: Bo góc cua 30°-45° gọn gàng, không bị phình to ra ngoài!", MessageType.None);
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(road, "Change Road Shape Settings");
            road.isClosedLoop = newLoop;
            road.curveStyle = newStyle;
            road.cornerFilletRadius = newRadius;
            road.GenerateRoadMesh();
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 3. HỘP CÔNG CỤ ĐỒNG BỘ ROAD_NETWORK
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🚀 3. CÔNG CỤ TOÀN BỘ HỆ THỐNG ROAD_NETWORK", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🛣️ Đồng Bộ Model Cho Road_Network", GUILayout.Height(32)))
        {
            SyncAllRoadsInNetwork();
        }

        if (GUILayout.Button("🔁 Gộp Thành 1 Tuyến Khép Kín", GUILayout.Height(32)))
        {
            MergeRoadNetworkToClosedLoop();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 4. HỘP TRANG TRÍ MÔI TRƯỜNG & TỐI ƯU THỊ GIÁC (GEMINI)
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎨 4. TRANG TRÍ & TỐI ƯU ĐỘ SÂU THỊ GIÁC (GEMINI)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🌸 Sinh Hoa Đa Sắc (Né Sạch Đường)", GUILayout.Height(30)))
        {
            StylizedDioramaEnvironmentMaster.RegenerateRoadAvoidingFlora();
        }
        if (GUILayout.Button("🪨 Rải Sỏi Nhỏ Trên Đường", GUILayout.Height(30)))
        {
            StylizedDioramaEnvironmentMaster.SpawnRoadPebblesOnAllRoads();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("☀️ Căn Ánh Sáng & Đổ Bóng", GUILayout.Height(30)))
        {
            StylizedDioramaEnvironmentMaster.ApplyGeminiLightingSetup();
        }
        if (GUILayout.Button("🌄 Gradient Cao Độ Địa Hình", GUILayout.Height(30)))
        {
            StylizedDioramaEnvironmentMaster.ApplyGeminiHeightGradientToTerrain();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);
        GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
        if (GUILayout.Button("✨ ÁP DỤNG TOÀN DIỆN 1-CLICK (TẤT CẢ TỐI ƯU)", GUILayout.Height(34)))
        {
            StylizedDioramaEnvironmentMaster.ApplyAllMasterEnhancements();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox("💡 THAO TÁC NHANH TRÊN TUYẾN ĐƯỜNG NÀY:\n" +
            "• Chọn số mốc (#1, #2, #3) trong Scene View để kéo nắn tọa độ.\n" +
            "• Bấm 'Thêm Điểm Mới' để nối dài con đường.\n" +
            "• Bấm 'Bám Sát Mặt Đất' để áp toàn bộ điểm xuống nền đất tự nhiên.", MessageType.Info);

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("➕ Thêm Điểm Mới (Add Point)", GUILayout.Height(34)))
        {
            Undo.RecordObject(road, "Add Road Waypoint");
            road.AddWaypointAtEnd();
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("📍 Bám Sát Mặt Đất (Snap)", GUILayout.Height(34)))
        {
            Undo.RecordObject(road, "Snap Road to Ground");
            road.SnapAllWaypointsToGround();
            SceneView.RepaintAll();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("🔄 Tái Tạo Lưới (Rebuild Mesh)", GUILayout.Height(30)))
        {
            Undo.RecordObject(road, "Rebuild Road Mesh");
            road.GenerateRoadMesh();
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("↩️ Đặt Lại Đường Chữ S Mẫu", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Đặt lại đường mẫu", "Bạn có chắc chắn muốn đặt lại đường chữ S mẫu?", "Đồng ý", "Hủy"))
            {
                Undo.RecordObject(road, "Reset to Default S Path");
                road.ResetToDefaultSPath();
                SceneView.RepaintAll();
            }
        }

        EditorGUILayout.EndHorizontal();

        // Danh sách các mốc tọa độ
        if (road.waypoints != null && road.waypoints.Count > 0)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Danh sách {road.waypoints.Count} mốc tọa độ:", EditorStyles.boldLabel);

            for (int i = 0; i < road.waypoints.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Điểm #{i + 1}", GUILayout.Width(60));
                Vector3 newPos = EditorGUILayout.Vector3Field("", road.waypoints[i]);
                if (newPos != road.waypoints[i])
                {
                    Undo.RecordObject(road, $"Move Waypoint {i}");
                    road.waypoints[i] = newPos;
                    road.GenerateRoadMesh();
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    if (road.waypoints.Count > 2)
                    {
                        Undo.RecordObject(road, $"Remove Waypoint {i}");
                        road.waypoints.RemoveAt(i);
                        road.GenerateRoadMesh();
                        break;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    private void OnSceneGUI()
    {
        if (road == null || road.waypoints == null) return;

        // Vẽ đường nối giữa các điểm trong Scene View
        Handles.color = new Color(1.0f, 0.70f, 0.20f, 0.85f);
        int segCount = road.isClosedLoop ? road.waypoints.Count : road.waypoints.Count - 1;
        for (int i = 0; i < segCount; i++)
        {
            Vector3 pA = road.waypoints[i];
            Vector3 pB = road.waypoints[(i + 1) % road.waypoints.Count];
            Handles.DrawDottedLine(pA, pB, 4f);
        }

        // Vẽ từng điểm điều khiển và nhãn số
        for (int i = 0; i < road.waypoints.Count; i++)
        {
            Vector3 pt = road.waypoints[i];

            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 13;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleCenter;

            Handles.Label(pt + Vector3.up * 0.45f, $"#{i + 1}", labelStyle);

            float handleSize = HandleUtility.GetHandleSize(pt) * 0.18f;
            Handles.color = (selectedWaypointIndex == i) ? Color.green : new Color(1f, 0.5f, 0f, 0.9f);

            if (Handles.Button(pt, Quaternion.identity, handleSize, handleSize * 1.2f, Handles.SphereHandleCap))
            {
                selectedWaypointIndex = i;
            }

            if (selectedWaypointIndex == i)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(pt, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(road, $"Move Waypoint {i}");
                    if (road.heightMode == RoadHeightMode.PureGroundOnly)
                    {
                        newPos.y = LowPolyRoadSpline.CalculateGroundHeight(newPos.x, newPos.z);
                    }
                    else if (road.heightMode == RoadHeightMode.TerrainColliderOnly)
                    {
                        newPos.y = road.ResolveHeight(newPos.x, newPos.y, newPos.z);
                    }
                    road.waypoints[i] = newPos;
                    road.GenerateRoadMesh();
                }
            }
        }
    }

    [MenuItem("Factory/Road Tools/Create Low-Poly Curved Road", priority = 20)]
    public static void CreateNewCurvedRoad()
    {
        var envRoot = GameObject.Find("Road_Network");
        if (envRoot == null) envRoot = GameObject.Find("_Environment_Map");
        if (envRoot == null) envRoot = new GameObject("Road_Network");

        GameObject go = new GameObject("Curved_DirtRoad_Spline");
        go.transform.SetParent(envRoot.transform, false);
        go.transform.position = Vector3.zero;

        var spline = go.AddComponent<LowPolyRoadSpline>();
        spline.ResetToDefaultSPath();

        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create Low-Poly Curved Road");
        Debug.Log("[RoadTools] Đã tạo thành công đối tượng Đường uốn lượn Low-Poly! Bạn có thể kéo thả các điểm #1, #2, #3 trong Scene View.");
    }

    [MenuItem("Factory/Road Tools/Đồng Bộ Model Cho Toàn Bộ Road_Network", priority = 21)]
    public static void SyncAllRoadsInNetwork()
    {
        var roadNetwork = GameObject.Find("Road_Network");
        if (roadNetwork == null)
        {
            EditorUtility.DisplayDialog("Thông Báo", "Không tìm thấy GameObject 'Road_Network' trong Scene!", "OK");
            return;
        }

        var splines = roadNetwork.GetComponentsInChildren<LowPolyRoadSpline>(true);
        if (splines.Length == 0)
        {
            EditorUtility.DisplayDialog("Thông Báo", "Không tìm thấy đối tượng LowPolyRoadSpline nào bên trong Road_Network!", "OK");
            return;
        }

        Material roadMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Road_DirtTrail.mat");
        if (roadMat == null)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            roadMat = new Material(lit);
            roadMat.name = "Mat_Road_DirtTrail";
            roadMat.SetColor("_BaseColor", new Color(0.81f, 0.60f, 0.38f, 1f));
            roadMat.SetFloat("_Smoothness", 0.08f);
        }

        // Dọn sạch mọi lỗi màu tím trên toàn bộ các Renderer thuộc Road_Network
        var allRenderers = roadNetwork.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var mr in allRenderers)
        {
            if (mr.sharedMaterial == null || mr.sharedMaterial.shader == null || mr.sharedMaterial.shader.name.Contains("Error") || mr.sharedMaterial.shader.name.Contains("StylizedDioramaLit"))
            {
                mr.sharedMaterial = roadMat;
                EditorUtility.SetDirty(mr);
            }
        }

        Undo.RecordObjects(splines, "Sync All Roads in Network");
        int count = 0;
        foreach (var s in splines)
        {
            s.heightMode = RoadHeightMode.PureGroundOnly;
            s.lowPolyFaceted = true;
            s.EnsureRoadMaterial();

            var mr = s.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = roadMat;
            }

            s.GenerateRoadMesh();
            EditorUtility.SetDirty(s);
            count++;
        }

        SceneView.RepaintAll();
        EditorUtility.DisplayDialog("Hoàn Tất", $"Đã dọn sạch màu tím và đồng bộ thành công {count} tuyến đường trong Road_Network chuẩn theo model ảnh mẫu!", "Tuyệt vời");
        Debug.Log($"[RoadTools] Đã dọn sạch màu tím và đồng bộ thành công {count} tuyến đường trong Road_Network!");
    }

    [MenuItem("Factory/Road Tools/Gộp Road_Network Thành 1 Tuyến Khép Kín (Merge to 1 Loop)", priority = 22)]
    public static void MergeRoadNetworkToClosedLoop()
    {
        var roadNetwork = GameObject.Find("Road_Network");
        if (roadNetwork == null)
        {
            EditorUtility.DisplayDialog("Thông Báo", "Không tìm thấy GameObject 'Road_Network' trong Scene!", "OK");
            return;
        }

        var splines = roadNetwork.GetComponentsInChildren<LowPolyRoadSpline>(true);
        if (splines.Length == 0)
        {
            EditorUtility.DisplayDialog("Thông Báo", "Không tìm thấy tuyến đường nào trong Road_Network để gộp!", "OK");
            return;
        }

        if (splines.Length == 1)
        {
            var single = splines[0];
            Undo.RecordObject(single, "Set Closed Loop");
            single.isClosedLoop = true;
            single.curveStyle = RoadCurveStyle.ControlledCorner;
            single.cornerFilletRadius = 1.2f;
            single.heightMode = RoadHeightMode.PureGroundOnly;
            single.EnsureRoadMaterial();
            single.GenerateRoadMesh();
            EditorUtility.SetDirty(single);
            SceneView.RepaintAll();
            EditorUtility.DisplayDialog("Hoàn Tất", "Đã bật chế độ Vòng Khép Kín (Closed Loop) cho con đường duy nhất trong Road_Network!", "OK");
            return;
        }

        // Thu thập toàn bộ các mốc tọa độ
        List<Vector3> mergedPoints = new List<Vector3>();
        foreach (var s in splines)
        {
            if (s.waypoints != null)
            {
                foreach (var pt in s.waypoints)
                {
                    bool isDuplicate = false;
                    foreach (var existing in mergedPoints)
                    {
                        if (Vector3.Distance(existing, pt) < 0.5f)
                        {
                            isDuplicate = true;
                            break;
                        }
                    }
                    if (!isDuplicate)
                    {
                        mergedPoints.Add(pt);
                    }
                }
            }
        }

        if (mergedPoints.Count < 3)
        {
            EditorUtility.DisplayDialog("Thông Báo", "Tổng số điểm mốc thu thập được ít hơn 3 điểm, không thể tạo vòng khép kín!", "OK");
            return;
        }

        var masterSpline = splines[0];
        masterSpline.gameObject.name = "Unified_Circuit_Road";
        Undo.RecordObject(masterSpline, "Merge Roads into Single Loop");
        masterSpline.waypoints = mergedPoints;
        masterSpline.isClosedLoop = true;
        masterSpline.curveStyle = RoadCurveStyle.ControlledCorner;
        masterSpline.cornerFilletRadius = 1.2f;
        masterSpline.heightMode = RoadHeightMode.PureGroundOnly;
        masterSpline.EnsureRoadMaterial();
        masterSpline.GenerateRoadMesh();
        EditorUtility.SetDirty(masterSpline);

        // Vô hiệu hóa các nhánh đường con còn lại để tránh trùng lặp
        for (int i = 1; i < splines.Length; i++)
        {
            Undo.RecordObject(splines[i].gameObject, "Deactivate Merged Road Fragment");
            splines[i].gameObject.SetActive(false);
        }

        Selection.activeGameObject = masterSpline.gameObject;
        SceneView.RepaintAll();
        EditorUtility.DisplayDialog("Gộp Thành Công", 
            $"Đã gộp thành công {splines.Length} tuyến đường thành 1 CON ĐƯỜNG DUY NHẤT ({mergedPoints.Count} điểm mốc) khép kín vòng lặp!\n\nBạn có thể chỉnh bán kính bo góc (Corner Radius) về 0 để tạo góc vuông 90° sắc nét, hoặc 0.5 - 2.0 để bo góc cua mềm mại.", "Tuyệt vời");
        Debug.Log($"[RoadTools] Đã gộp thành công các tuyến đường trong Road_Network thành 1 tuyến khép kín duy nhất!");
    }
}
