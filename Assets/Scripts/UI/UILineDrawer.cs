using UnityEngine;

public static class UILineDrawer
{
    public static GameObject DrawLine(GameObject linePrefab, Transform parent, RectTransform nodeA, RectTransform nodeB)
    {
        GameObject lineObj = Object.Instantiate(linePrefab, parent);
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        lineRect.SetAsFirstSibling(); // Push line behind nodes

        Vector2 posA = nodeA.anchoredPosition;
        Vector2 posB = nodeB.anchoredPosition;
        Vector2 direction = posB - posA;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        lineRect.anchoredPosition = posA;
        lineRect.sizeDelta = new Vector2(distance, lineRect.sizeDelta.y);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);

        return lineObj;
    }
}
