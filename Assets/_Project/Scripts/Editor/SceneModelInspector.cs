using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneModelInspector
{
    [InitializeOnLoadMethod]
    private static void AutoInspectOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            Inspect();
        };
    }

    [MenuItem("Factory/Debug/Inspect Scene Models", priority = 99)]
    public static void Inspect()
    {
        var scene = SceneManager.GetActiveScene();
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== SCENE INSPECTOR: {scene.name} ===");

        var rootObjs = scene.GetRootGameObjects();
        foreach (var root in rootObjs)
        {
            InspectGameObject(root, 0, sb);
        }

        string logPath = "D:/lamviec/Unity project/Dự án chính/Factory Kit/Logs/SceneInspection.txt";
        string dir = Path.GetDirectoryName(logPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[SceneInspector] Đã ghi kết quả kiểm tra vào {logPath}");
    }

    private static void InspectGameObject(GameObject go, int depth, StringBuilder sb)
    {
        string indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}- [{go.name}] localPos={go.transform.localPosition} localRot={go.transform.localEulerAngles} localScale={go.transform.localScale} worldPos={go.transform.position} active={go.activeSelf}");

        var comps = go.GetComponents<Component>();
        foreach (var c in comps)
        {
            if (c == null) continue;
            string compName = c.GetType().Name;
            if (c is BoxCollider bc)
            {
                sb.AppendLine($"{indent}    * BoxCollider: center={bc.center}, size={bc.size}, isTrigger={bc.isTrigger}");
            }
            else if (c is MachineController mc)
            {
                sb.AppendLine($"{indent}    * MachineController: config={(mc.Config != null ? mc.Config.name : "null")}, tier={mc.CurrentTier}");
            }
            else if (c is ConveyorBelt cb)
            {
                sb.AppendLine($"{indent}    * ConveyorBelt: pad={(cb.TargetStoragePad != null ? cb.TargetStoragePad.name : "null")}");
            }
            else if (c is StoragePad sp)
            {
                sb.AppendLine($"{indent}    * StoragePad: maxCap={sp.MaxCapacity}");
            }
            else if (c is ProductionLine pl)
            {
                sb.AppendLine($"{indent}    * ProductionLine: clusterID={pl.ClusterID}, slots={pl.MachineSlots.Count}");
            }
        }

        for (int i = 0; i < go.transform.childCount; i++)
        {
            InspectGameObject(go.transform.GetChild(i).gameObject, depth + 1, sb);
        }
    }
}
