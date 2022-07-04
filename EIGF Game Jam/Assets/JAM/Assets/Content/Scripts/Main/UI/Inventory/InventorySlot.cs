/*
 * InventorySlot.cs - script by ThunderWire Games
 * ver. 1.2
*/

using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	public int id;

    [HideInInspector]
    public Inventory inventory;

    [HideInInspector]
    public Item slotItem;

    [HideInInspector]
    public InventoryItemData itemData; 

    [HideInInspector]
	public bool isCombining;

	[HideInInspector]
	public bool isCombinable;

    [HideInInspector]
    public bool isSelected;

    [HideInInspector]
    public bool contexVisible;

    private GameObject ShortcutObj;
    private bool isTriggered;
    private bool isPressed;
    private bool slotEmpty;

    void Start()
    {
        ShortcutObj = transform.GetChild(0).GetChild(1).gameObject;
    }

    void Update()
    {
        if (!inventory) return;

        if (GetComponentInChildren<InventoryItemData>())
        {
            itemData = GetComponentInChildren<InventoryItemData>();
            slotItem = itemData.item;
        }
        else
        {
            itemData = null;
            slotItem = null;
        }

        if (transform.childCount > 1 && itemData)
        {
            transform.GetChild(0).GetComponent<Image>().sprite = inventory.slotItemOutline;
            GetComponent<Image>().enabled = true;
            slotEmpty = false;

            if (Input.GetKeyDown(KeyCode.Mouse1) && !isCombining && isTriggered && !isPressed)
            {
                inventory.selectedID = id;
                IsClicked();

                if (!contexVisible)
                {
                    contexVisible = true;
                }
                else
                {
                    inventory.ShowContexMenu(false, null);
                    contexVisible = false;
                }

                isPressed = true;
            }
            else if (isPressed)
            {
                isPressed = false;
            }

            if (isSelected && contexVisible)
            {
                if (!slotItem.useItemSwitcher)
                {
                    inventory.ShowContexMenu(true, slotItem, id, ctx_use: !inventory.isStoring, ctx_examine: !inventory.isStoring, ctx_combine: inventory.HasCombinePartner(slotItem), ctx_shortcut: !inventory.isStoring, ctx_store: inventory.isStoring);
                }
                else
                {
                    inventory.ShowContexMenu(true, slotItem, id, inventory.GetSwitcher().currentItem != slotItem.useSwitcherID && !inventory.isStoring, inventory.HasCombinePartner(slotItem) && !inventory.isStoring, ctx_shortcut: !inventory.isStoring, ctx_store: inventory.isStoring);
                }
            }
            else if(contexVisible)
            {
                inventory.ShowContexMenu(false, null);
                contexVisible = false;
            }

            if (itemData.selected)
            {
                GetComponent<Image>().color = Color.white;
                GetComponent<Image>().sprite = inventory.slotItemSelect;
            }
            else if (!isCombining)
            {
                GetComponent<Image>().color = Color.white;
                GetComponent<Image>().sprite = inventory.slotWithItem;
            }

            if (isCombining)
            {
                itemData.isDisabled = true;
                inventory.ShowContexMenu(false, null);
            }
            else
            {
                itemData.isDisabled = false;
            }

            if (ShortcutObj)
            {
                if(itemData.shortKey >= 0)
                {
                    ShortcutObj.SetActive(true);
                    ShortcutObj.GetComponentInChildren<Text>().text = itemData.shortKey.ToString();
                }
                else
                {
                    ShortcutObj.SetActive(false);
                }
            }
        }
        else if (transform.childCount < 2)
        {
            contexVisible = false;
            transform.GetChild(0).GetComponent<Image>().sprite = inventory.slotsNormal;
            transform.GetChild(0).GetComponent<Image>().color = Color.white;
            transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "";
            GetComponent<Image>().enabled = false;
            ShortcutObj.SetActive(false);

            if (!slotEmpty)
            {
                inventory.ShowContexMenu(false, null);
                slotEmpty = true;
            }
        }

        if (!inventory.contexMenu.activeSelf)
        {
            contexVisible = false;
        }

        if (!isSelected)
        {
            contexVisible = false;
        }

        if (itemData)
        {
            itemData.isCombining = isCombining;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventoryItemData itemDrop = eventData.pointerDrag.GetComponent<InventoryItemData>();
        itemData = itemDrop;

        if (!isCombining)
        {
            if (inventory.Slots[id].transform.childCount < 2)
            {
                itemDrop.slotID = id;
            }
            else if (itemDrop.slotID != id)
            {
                Transform item = transform.GetChild(1);
                item.GetComponent<InventoryItemData>().slotID = itemDrop.slotID;
                item.transform.SetParent(inventory.Slots[itemDrop.slotID].transform);
                item.transform.position = inventory.Slots[itemDrop.slotID].transform.position;

                itemDrop.slotID = id;
                itemDrop.transform.SetParent(transform);
                itemDrop.transform.position = transform.position;
            }
            if (itemDrop.selected)
            {
                inventory.selectedID = itemDrop.slotID;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (transform.childCount > 1)
        {
            if (isCombinable)
            {
                inventory.CombineWith(itemData.item, id);
                isSelected = false;
            }
            else if(!isCombining)
            {
                IsClicked();
            }
        }
    }

    void IsClicked()
    {
        for (int i = 0; i < inventory.Slots.Count; i++)
        {
            if (inventory.Slots[i].transform.childCount > 1)
            {
                inventory.Slots[i].GetComponentInChildren<InventoryItemData>().selected = false;
                inventory.Slots[i].GetComponent<InventorySlot>().isSelected = false;
            }
        }

        GetComponent<Image>().color = Color.white;
        GetComponent<Image>().sprite = inventory.slotItemSelect;

        inventory.ItemLabel.text = itemData.item.Title;
        inventory.ItemDescription.text = itemData.item.Description;
        inventory.selectedID = id;

        inventory.ItemInfoPanel.SetActive(true);

        itemData.selected = true;
        isSelected = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isTriggered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isTriggered = false;
    }
}
