using UnityEngine;
using UnityEngine.EventSystems;

public class OrderSortBankDropArea : MonoBehaviour, IDropHandler
{
    private OrderSortDragManager manager;

    public void Init(OrderSortDragManager sortManager)
    {
        manager = sortManager;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (manager == null || !manager.IsGameInputEnabled)
            return;

        OrderSortDragItem item = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<OrderSortDragItem>()
            : null;

        if (item == null)
            return;

        manager.DropItemOnBank(item);
    }
}
