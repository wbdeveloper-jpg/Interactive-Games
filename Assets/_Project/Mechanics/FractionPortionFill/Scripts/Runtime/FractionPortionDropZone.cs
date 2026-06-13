using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class FractionPortionDropZone : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("Zone")]
    public int zoneIndex;
    public FractionPortionWedgeGraphic wedgeGraphic;

    [Header("Placed Item UI")]
    public Image itemIcon;
    public TMP_Text itemLabel;
    public TMP_Text portionNumberText;

    private FractionPortionFillManager manager;
    private RectTransform itemIconRect;
    private RectTransform itemLabelRect;
    private RectTransform portionNumberRect;
    private string assignedItemId = string.Empty;
    private string assignedItemName = string.Empty;
    private Color baseZoneColor = Color.white;
    private bool hasBaseZoneColor;
    private float setupStartAngle;
    private float setupEndAngle;
    private readonly List<GameObject> spawnedToppingCopies = new List<GameObject>();

    public string AssignedItemId => assignedItemId;
    public bool IsOccupied => !string.IsNullOrEmpty(assignedItemId);

    private void Awake()
    {
        CacheRects();
    }

    public void Setup(FractionPortionFillManager owner, int index, float startAngle, float endAngle, Color zoneColor)
    {
        manager = owner;
        zoneIndex = index;
        setupStartAngle = startAngle;
        setupEndAngle = endAngle;
        CacheRects();

        baseZoneColor = zoneColor;
        hasBaseZoneColor = true;

        if (wedgeGraphic != null)
        {
            wedgeGraphic.color = baseZoneColor;
            wedgeGraphic.SetAngles(startAngle, endAngle);
            wedgeGraphic.raycastTarget = true;
        }

        if (portionNumberText != null)
        {
            portionNumberText.gameObject.SetActive(true);
            portionNumberText.text = (zoneIndex + 1).ToString();
            portionNumberText.raycastTarget = false;
        }

        PositionPlacedVisual(startAngle, endAngle);
        ClearZone();
    }

    public void OnDrop(PointerEventData eventData)
    {
        FractionPortionBasketCard card = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<FractionPortionBasketCard>()
            : null;

        if (card == null || manager == null)
            return;

        manager.TryPlaceFromBasket(this, card);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager != null)
            manager.ClearDropZone(this);
    }

    public void SetItem(FractionPortionFillManager.PortionItemData itemData)
    {
        if (itemData == null)
            return;

        assignedItemId = itemData.id;
        assignedItemName = itemData.displayName;

        ClearSpawnedToppingCopies();
        ApplyFilledSliceVisual(itemData);
        SpawnToppingCopies(itemData);

        if (itemIcon != null)
        {
            bool showIcon = manager == null || manager.showPlacedItemIconOnPizza || (!manager.fillEntireSliceWithDroppedTopping && !manager.scatterToppingCopiesOnPlacedSlice);
            itemIcon.gameObject.SetActive(showIcon);
            itemIcon.color = itemData.icon != null ? Color.white : itemData.color;
            itemIcon.sprite = itemData.icon;
            itemIcon.preserveAspect = true;
            itemIcon.enabled = showIcon;
            itemIcon.raycastTarget = false;
        }

        if (itemLabel != null)
        {
            bool showLabel = manager == null || manager.showPlacedItemLabelOnPizza;
            itemLabel.gameObject.SetActive(showLabel);
            itemLabel.text = showLabel ? assignedItemName : string.Empty;
            itemLabel.raycastTarget = false;
        }

        KeepPortionNumberOnTop();
    }

    public void ClearZone()
    {
        assignedItemId = string.Empty;
        assignedItemName = string.Empty;

        ClearSpawnedToppingCopies();

        if (itemIcon != null)
            itemIcon.gameObject.SetActive(false);

        if (itemLabel != null)
        {
            itemLabel.gameObject.SetActive(false);
            itemLabel.text = string.Empty;
        }

        RestoreBaseSliceVisual();
        KeepPortionNumberOnTop();
    }

    private void ApplyFilledSliceVisual(FractionPortionFillManager.PortionItemData itemData)
    {
        if (wedgeGraphic == null || itemData == null)
            return;

        if (manager != null && manager.fillEntireSliceWithDroppedTopping)
        {
            Color fillColor = itemData.color;
            fillColor.a = Mathf.Clamp01(manager.filledSliceToppingAlpha);
            wedgeGraphic.color = fillColor;
        }
        else
        {
            RestoreBaseSliceVisual();
        }
    }

    private void SpawnToppingCopies(FractionPortionFillManager.PortionItemData itemData)
    {
        if (manager == null || !manager.scatterToppingCopiesOnPlacedSlice || itemData == null)
            return;

        RectTransform zoneRect = GetComponent<RectTransform>();
        if (zoneRect == null)
            return;

        float width = zoneRect.rect.width > 1f ? zoneRect.rect.width : 620f;
        float height = zoneRect.rect.height > 1f ? zoneRect.rect.height : 620f;
        float boardRadius = Mathf.Min(width, height) * 0.5f;

        int count = Mathf.Max(1, manager.GetToppingCopyCountForCurrentPortion());
        float minSize = Mathf.Max(8f, Mathf.Min(manager.toppingCopyMinSize, manager.toppingCopyMaxSize));
        float maxSize = Mathf.Max(minSize, manager.toppingCopyMaxSize);
        float innerPercent = Mathf.Clamp01(manager.toppingScatterInnerRadiusPercent);
        float outerPercent = Mathf.Clamp(manager.toppingScatterOuterRadiusPercent, innerPercent + 0.04f, 0.96f);
        float innerRadius = boardRadius * innerPercent;
        float outerRadius = boardRadius * outerPercent;

        float range = Mathf.DeltaAngle(setupStartAngle, setupEndAngle);
        if (range <= 0f)
            range += 360f;

        // Keep only a small angular inset. Large padding makes narrow slices look empty near the edge.
        float anglePadding = Mathf.Clamp01(manager.toppingScatterAnglePaddingPercent) * range;
        float angleMin = setupStartAngle + anglePadding;
        float angleMax = setupStartAngle + range - anglePadding;
        if (angleMax <= angleMin)
        {
            angleMin = setupStartAngle + range * 0.08f;
            angleMax = setupStartAngle + range * 0.92f;
        }

        List<Vector2> placedPoints = new List<Vector2>(count);
        int attemptsPerCopy = Mathf.Max(1, manager.toppingScatterPlacementAttempts);
        float sliceDensityT = Mathf.InverseLerp(4f, 12f, Mathf.Clamp(manager.CurrentPortionCount, 4, 12));
        float minDistance = Mathf.Lerp(manager.toppingScatterMinDistance, manager.toppingScatterMinDistance * 0.55f, sliceDensityT);
        minDistance = Mathf.Max(4f, Mathf.Min(minDistance, maxSize * 0.95f));

        for (int i = 0; i < count; i++)
        {
            GameObject copy = new GameObject("Topping Copy - " + itemData.displayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            copy.transform.SetParent(transform, false);

            RectTransform copyRect = copy.GetComponent<RectTransform>();
            copyRect.anchorMin = new Vector2(0.5f, 0.5f);
            copyRect.anchorMax = new Vector2(0.5f, 0.5f);
            copyRect.pivot = new Vector2(0.5f, 0.5f);

            float size = Random.Range(minSize, maxSize);
            copyRect.sizeDelta = new Vector2(size, size);

            Vector2 localPoint = FindSpreadPointInWedge(angleMin, angleMax, innerRadius, outerRadius, placedPoints, minDistance, attemptsPerCopy);
            placedPoints.Add(localPoint);
            copyRect.anchoredPosition = localPoint;
            copyRect.localScale = Vector3.one;
            copyRect.localRotation = manager.randomizeToppingCopyRotation
                ? Quaternion.Euler(0f, 0f, Random.Range(-35f, 35f))
                : Quaternion.identity;

            Image image = copy.GetComponent<Image>();
            image.sprite = itemData.icon;
            image.color = itemData.icon != null ? Color.white : itemData.color;
            image.preserveAspect = true;
            image.raycastTarget = false;

            spawnedToppingCopies.Add(copy);
        }
    }

    private Vector2 FindSpreadPointInWedge(float angleMinDeg, float angleMaxDeg, float innerRadius, float outerRadius, List<Vector2> existingPoints, float minDistance, int attempts)
    {
        Vector2 bestPoint = Vector2.zero;
        float bestNearestDistance = -1f;

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            Vector2 candidate = RandomPointInWedge(angleMinDeg, angleMaxDeg, innerRadius, outerRadius);
            float nearestDistance = GetNearestDistance(candidate, existingPoints);

            if (nearestDistance >= minDistance)
                return candidate;

            if (nearestDistance > bestNearestDistance)
            {
                bestNearestDistance = nearestDistance;
                bestPoint = candidate;
            }
        }

        return bestNearestDistance >= 0f ? bestPoint : RandomPointInWedge(angleMinDeg, angleMaxDeg, innerRadius, outerRadius);
    }

    private Vector2 RandomPointInWedge(float angleMinDeg, float angleMaxDeg, float innerRadius, float outerRadius)
    {
        float angle = Random.Range(angleMinDeg, angleMaxDeg) * Mathf.Deg2Rad;
        // Uniform area sampling. This naturally uses the outer part of the wedge more than plain Random.Range(radius).
        float radius = Mathf.Sqrt(Random.Range(innerRadius * innerRadius, outerRadius * outerRadius));
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    private float GetNearestDistance(Vector2 candidate, List<Vector2> existingPoints)
    {
        if (existingPoints == null || existingPoints.Count == 0)
            return float.MaxValue;

        float nearest = float.MaxValue;
        for (int i = 0; i < existingPoints.Count; i++)
        {
            float distance = Vector2.Distance(candidate, existingPoints[i]);
            if (distance < nearest)
                nearest = distance;
        }

        return nearest;
    }

    private void ClearSpawnedToppingCopies()
    {
        for (int i = spawnedToppingCopies.Count - 1; i >= 0; i--)
        {
            if (spawnedToppingCopies[i] != null)
                Destroy(spawnedToppingCopies[i]);
        }

        spawnedToppingCopies.Clear();
    }

    private void KeepPortionNumberOnTop()
    {
        if (portionNumberText != null)
            portionNumberText.transform.SetAsLastSibling();

        if (itemLabel != null && itemLabel.gameObject.activeSelf)
            itemLabel.transform.SetAsLastSibling();
    }

    private void RestoreBaseSliceVisual()
    {
        if (wedgeGraphic != null && hasBaseZoneColor)
            wedgeGraphic.color = baseZoneColor;
    }

    private void CacheRects()
    {
        if (itemIcon != null && itemIconRect == null)
            itemIconRect = itemIcon.GetComponent<RectTransform>();

        if (itemLabel != null && itemLabelRect == null)
            itemLabelRect = itemLabel.GetComponent<RectTransform>();

        if (portionNumberText != null && portionNumberRect == null)
            portionNumberRect = portionNumberText.GetComponent<RectTransform>();
    }

    private void PositionPlacedVisual(float startAngle, float endAngle)
    {
        RectTransform rect = GetComponent<RectTransform>();
        float width = rect != null && rect.rect.width > 1f ? rect.rect.width : 620f;
        float height = rect != null && rect.rect.height > 1f ? rect.rect.height : 620f;
        float radius = Mathf.Min(width, height) * 0.28f;
        float numberRadius = Mathf.Min(width, height) * 0.43f;

        float range = Mathf.DeltaAngle(startAngle, endAngle);
        if (range <= 0f)
            range += 360f;

        float midAngle = startAngle + range * 0.5f;
        float rad = midAngle * Mathf.Deg2Rad;
        Vector2 position = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

        if (itemIconRect != null)
            itemIconRect.anchoredPosition = position;

        if (itemLabelRect != null)
            itemLabelRect.anchoredPosition = position + new Vector2(0f, -48f);

        if (portionNumberRect != null)
            portionNumberRect.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * numberRadius;
    }
}
