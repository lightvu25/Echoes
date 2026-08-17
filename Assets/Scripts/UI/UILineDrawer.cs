using UnityEngine;

public static class UILineDrawer
{
    public static GameObject DrawLine(GameObject linePrefab, Transform parent, RectTransform nodeA, RectTransform nodeB)
    {
        GameObject lineObj = Object.Instantiate(linePrefab, parent);
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        // MapContent reserves sibling 0 for its opaque/star background. Keep
        // connectors above that background while still placing them below all
        // node objects.
        lineRect.SetSiblingIndex(Mathf.Min(1, parent.childCount - 1));

        // anchoredPosition is only comparable when both nodes have the same
        // RectTransform parent and anchors. Mind Garden nodes do not always do
        // that, so convert their actual visual centres into the line parent's
        // local space before calculating the connector.
        Vector3 worldA = nodeA.TransformPoint(nodeA.rect.center);
        Vector3 worldB = nodeB.TransformPoint(nodeB.rect.center);
        Vector2 posA = parent.InverseTransformPoint(worldA);
        Vector2 posB = parent.InverseTransformPoint(worldB);
        Vector2 direction = posB - posA;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0f, 0.5f);
        lineRect.localScale = Vector3.one;
        lineRect.anchoredPosition = posA;
        lineRect.sizeDelta = new Vector2(distance, lineRect.sizeDelta.y);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);

        return lineObj;
    }
}
