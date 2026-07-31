using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text;
using System.IO;

public static class DebugUI
{
    [InitializeOnLoadMethod]
    public static void Run()
    {
        StringBuilder sb = new StringBuilder();
        Canvas canvas = null;
        GameObject canvasGo = GameObject.Find("UI Canvas");
        if (canvasGo != null) canvas = canvasGo.GetComponent<Canvas>();
        
        if (canvas == null)
        {
            File.WriteAllText("ui_debug.txt", "No UI Canvas found");
            return;
        }

        Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic g in graphics)
        {
            if (g.raycastTarget)
            {
                // check if it is active in hierarchy
                if (g.gameObject.activeInHierarchy)
                {
                    RectTransform rt = g.rectTransform;
                    Vector3[] corners = new Vector3[4];
                    rt.GetWorldCorners(corners);
                    // check size
                    float width = Vector3.Distance(corners[0], corners[3]);
                    float height = Vector3.Distance(corners[0], corners[1]);
                    if (width > 100 && height > 100)
                    {
                        sb.AppendLine($"Graphic: {g.name} (Type: {g.GetType().Name}, Path: {GetPath(g.transform)})");
                        sb.AppendLine($"  Size: {width} x {height}");
                        sb.AppendLine($"  Corners: BL={corners[0]} TR={corners[2]}");
                    }
                }
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
