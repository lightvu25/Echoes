using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class MapPolygonUI : MaskableGraphic
{
    private PolygonCollider2D _sourceCollider;
    private float _scale;

    public void Initialize(PolygonCollider2D collider, float scale)
    {
        _sourceCollider = collider;
        _scale = scale;
        SetAllDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (_sourceCollider == null)
            return;

        Mesh mesh = _sourceCollider.CreateMesh(false, false);
        if (mesh == null || mesh.vertices.Length == 0)
            return;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3 worldPos = _sourceCollider.transform.position;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 localVert = vertices[i] - worldPos;
            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;
            vert.position = new Vector3(localVert.x * _scale, localVert.y * _scale, 0);
            vh.AddVert(vert);
        }

        for (int i = 0; i < triangles.Length; i += 3)
        {
            vh.AddTriangle(triangles[i], triangles[i + 1], triangles[i + 2]);
        }

        Destroy(mesh); // Clean up the temporary mesh
    }
}
