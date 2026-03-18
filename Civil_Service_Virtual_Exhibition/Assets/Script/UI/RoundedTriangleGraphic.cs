using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class RoundedTriangleGraphic : MaskableGraphic
{
    [SerializeField] private float cornerRadius = 24f;
    [SerializeField] private int cornerSegments = 8;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();

        Vector2 p0 = new Vector2(rect.xMin, rect.yMin);
        Vector2 p1 = new Vector2(rect.xMax, rect.yMin);
        Vector2 p2 = new Vector2((rect.xMin + rect.xMax) * 0.5f, rect.yMax);

        float maxRadius = Mathf.Min(
            Vector2.Distance(p0, p1),
            Mathf.Min(Vector2.Distance(p1, p2), Vector2.Distance(p2, p0))
        ) * 0.25f;

        float r = Mathf.Clamp(cornerRadius, 0f, maxRadius);
        int seg = Mathf.Max(1, cornerSegments);

        List<Vector2> polygon = BuildRoundedTriangle(p0, p1, p2, r, seg);

        if (polygon.Count < 3)
            return;

        Vector2 center = Vector2.zero;
        for (int i = 0; i < polygon.Count; i++)
            center += polygon[i];
        center /= polygon.Count;

        int centerIndex = 0;
        vh.AddVert(center, color, Vector2.zero);

        for (int i = 0; i < polygon.Count; i++)
            vh.AddVert(polygon[i], color, Vector2.zero);

        for (int i = 0; i < polygon.Count; i++)
        {
            int current = i + 1;
            int next = (i + 1) % polygon.Count + 1;
            vh.AddTriangle(centerIndex, current, next);
        }
    }

    private List<Vector2> BuildRoundedTriangle(Vector2 a, Vector2 b, Vector2 c, float radius, int segments)
    {
        List<Vector2> result = new List<Vector2>();

        AddRoundedCorner(result, a, b, c, radius, segments);
        AddRoundedCorner(result, b, c, a, radius, segments);
        AddRoundedCorner(result, c, a, b, radius, segments);

        return result;
    }

    private void AddRoundedCorner(
        List<Vector2> points,
        Vector2 corner,
        Vector2 prev,
        Vector2 next,
        float radius,
        int segments)
    {
        Vector2 dirToPrev = (prev - corner).normalized;
        Vector2 dirToNext = (next - corner).normalized;

        float angle = Vector2.Angle(dirToPrev, dirToNext) * Mathf.Deg2Rad;
        float offset = radius / Mathf.Tan(angle * 0.5f);

        Vector2 start = corner + dirToPrev * offset;
        Vector2 end = corner + dirToNext * offset;

        Vector2 bisector = (dirToPrev + dirToNext).normalized;
        float centerDistance = radius / Mathf.Sin(angle * 0.5f);
        Vector2 arcCenter = corner + bisector * centerDistance;

        float startAngle = Mathf.Atan2(start.y - arcCenter.y, start.x - arcCenter.x);
        float endAngle = Mathf.Atan2(end.y - arcCenter.y, end.x - arcCenter.x);

        while (endAngle < startAngle)
            endAngle += Mathf.PI * 2f;

        float delta = (endAngle - startAngle) / segments;

        for (int i = 0; i <= segments; i++)
        {
            float t = startAngle + delta * i;
            Vector2 p = arcCenter + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * radius;
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