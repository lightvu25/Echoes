using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Text;
using System.IO;

public static class DumpRaycasts
{
    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorApplication.delayCall += DoDump;
    }

    private static void DoDump()
    {
        StringBuilder sb = new StringBuilder();
        var canvas = GameObject.Find("UI Canvas");
        if (canvas == null)
        {
            File.WriteAllText("ui_debug.txt", "No UI Canvas found via delayCall.");
            return;
        }

        sb.AppendLine("--- UI Elements with RaycastTarget = true ---");
        Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic g in graphics)
        {
            if (g.raycastTarget && g.gameObject.activeInHierarchy)
            {
                RectTransform rt = g.rectTransform;
                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                float width = Vector3.Distance(corners[0], corners[3]);
                float height = Vector3.Distance(corners[0], corners[1]);

                // We only care if it's somewhat large, or just list everything
                sb.AppendLine($"- {g.name} [{g.GetType().Name}] (Width: {width:F1}, Height: {height:F1})");
                sb.AppendLine($"  Path: {GetPath(g.transform)}");
            }
        }
        File.WriteAllText("ui_debug.txt", sb.ToString());
    }

    private static string GetPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetPath(t.parent) + "/" + t.name;
    }
}
