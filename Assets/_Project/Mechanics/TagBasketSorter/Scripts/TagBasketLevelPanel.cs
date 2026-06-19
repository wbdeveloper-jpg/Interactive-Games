using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TagBasketSorter
{
    [DisallowMultipleComponent]
    public sealed class TagBasketLevelPanel : MonoBehaviour
    {
        [Header("Level Info")]
        public bool isLevelEnabled = true;
        public string levelTitle = "Level 1";
        [TextArea(2, 4)] public string instruction = "Drag each object into the correct basket.";
        public Sprite backgroundSprite;

        [Header("Per Level Hint Limit")]
        [Min(0)] public int maxHintsAllowed = 3;

        [Header("Placed Basket Feel")]
        [Tooltip("Objects keep the exact designer-placed start position. Random offset/rotation is applied only after correct drop, inside each basket/drop zone.")]
        public bool useBasketOrganicPlacement = true;

        [Header("Scene References")]
        public Image backgroundImage;
        public TMP_Text titleText;
        public TMP_Text instructionText;
        public RectTransform objectsHolder;
        public RectTransform basketsHolder;

        [Header("Editable Level Content")]
        public List<TagBasketDraggableItem> draggableItems = new List<TagBasketDraggableItem>();
        public List<TagBasketDropZone> dropZones = new List<TagBasketDropZone>();

        public int TotalItems => CountPlayableItems();

        private bool hasSetup;

        public void Setup(TagBasketSortGameManager manager)
        {
            AutoCollectIfNeeded();
            ApplyStaticVisuals();

            foreach (TagBasketDropZone zone in dropZones)
            {
                if (zone != null)
                    zone.Setup(manager, this);
            }

            foreach (TagBasketDraggableItem item in draggableItems)
            {
                if (item != null)
                    item.Setup(manager, this);
            }

            hasSetup = true;
            ValidateTagsIfNeeded();
        }

        public void StartLevel()
        {
            if (!hasSetup)
                AutoCollectIfNeeded();

            ApplyStaticVisuals();
            ResetLevel(false);
        }

        public void ResetLevel(bool animate)
        {
            AutoCollectIfNeeded();

            foreach (TagBasketDropZone zone in dropZones)
            {
                if (zone != null)
                    zone.ClearRuntimePlacedItems();
            }

            foreach (TagBasketDraggableItem item in draggableItems)
            {
                if (item != null)
                    item.ResetItem(animate);
            }
        }

        public bool OwnsItem(TagBasketDraggableItem item)
        {
            return item != null && draggableItems != null && draggableItems.Contains(item);
        }

        public bool OwnsDropZone(TagBasketDropZone zone)
        {
            return zone != null && dropZones != null && dropZones.Contains(zone);
        }

        public TagBasketDropZone FindDropZoneForTag(string tagValue)
        {
            if (dropZones == null)
                return null;

            foreach (TagBasketDropZone zone in dropZones)
            {
                if (zone != null && zone.AcceptsTag(tagValue))
                    return zone;
            }

            return null;
        }

        public List<TagBasketDraggableItem> GetUnplacedItems(List<TagBasketDraggableItem> buffer = null)
        {
            if (buffer == null)
                buffer = new List<TagBasketDraggableItem>();
            else
                buffer.Clear();

            if (draggableItems == null)
                return buffer;

            foreach (TagBasketDraggableItem item in draggableItems)
            {
                if (item != null && item.CanReceiveHint)
                    buffer.Add(item);
            }

            return buffer;
        }

        [ContextMenu("Auto Collect Children")]
        public void AutoCollectChildrenManual()
        {
            AutoCollectChildren(true);
        }

        public void AutoCollectIfNeeded()
        {
            if (draggableItems == null) draggableItems = new List<TagBasketDraggableItem>();
            if (dropZones == null) dropZones = new List<TagBasketDropZone>();

            if (draggableItems.Count == 0 || dropZones.Count == 0)
                AutoCollectChildren(false);
        }

        private void AutoCollectChildren(bool forceRefresh)
        {
            if (forceRefresh || draggableItems == null || draggableItems.Count == 0)
            {
                draggableItems = new List<TagBasketDraggableItem>();
                TagBasketDraggableItem[] items = GetComponentsInChildren<TagBasketDraggableItem>(true);
                foreach (TagBasketDraggableItem item in items)
                {
                    if (!draggableItems.Contains(item))
                        draggableItems.Add(item);
                }
            }

            if (forceRefresh || dropZones == null || dropZones.Count == 0)
            {
                dropZones = new List<TagBasketDropZone>();
                TagBasketDropZone[] zones = GetComponentsInChildren<TagBasketDropZone>(true);
                foreach (TagBasketDropZone zone in zones)
                {
                    if (!dropZones.Contains(zone))
                        dropZones.Add(zone);
                }
            }
        }

        private void ApplyStaticVisuals()
        {
            if (backgroundImage != null && backgroundSprite != null)
                backgroundImage.sprite = backgroundSprite;

            if (titleText != null)
                titleText.text = levelTitle;

            if (instructionText != null)
                instructionText.gameObject.SetActive(false);
        }

        private int CountPlayableItems()
        {
            if (draggableItems == null)
                return 0;

            int count = 0;
            foreach (TagBasketDraggableItem item in draggableItems)
            {
                if (item != null && item.gameObject.activeSelf)
                    count++;
            }

            return count;
        }

        private void ValidateTagsIfNeeded()
        {
            if (draggableItems == null || dropZones == null)
                return;

            foreach (TagBasketDraggableItem item in draggableItems)
            {
                if (item == null)
                    continue;

                if (FindDropZoneForTag(item.itemTag) == null)
                    Debug.LogWarning($"{name}: Item '{item.name}' has tag '{item.itemTag}' but no basket accepts it.", item);
            }
        }
    }
}
