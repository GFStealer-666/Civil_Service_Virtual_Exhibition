using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class RoundedRectGraphic : MaskableGraphic
{
    [SerializeField] private float cornerRadius = 20f;
    [SerializeField] private int cornerSegments = 8;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float radius = Mathf.Min(cornerRadius, rect.width * 0.5f, rect.height * 0.5f);
        int segments = Mathf.Max(1, cornerSegments);

        List<Vector2> points = new List<Vector2>();

        AddCorner(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, segments);
        AddCorner(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, segments);
        AddCorner(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, segments);
        AddCorner(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f, segments);

        Vector2 center = rect.center;
        vh.AddVert(center, color, Vector2.zero);

        for (int i = 0; i < points.Count; i++)
            vh.AddVert(points[i], color, Vector2.zero);

        for (int i = 0; i < points.Count; i++)
        {
            int current = i + 1;
            int next = (i + 1) % points.Count + 1;
            vh.AddTriangle(0, current, next);
        }
    }

    private void AddCorner(List<Vector2> points, Vector2 center, float radius, float startDeg, float endDeg, int segments)
    {
        float step = (endDeg - startDeg) / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * (startDeg + step * i);
            Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            points.Add(p);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}