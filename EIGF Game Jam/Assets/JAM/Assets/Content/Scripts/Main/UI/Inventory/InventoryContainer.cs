/*
 * InventoryContainer.cs - script by ThunderWire Games
 * ver. 1.0
*/

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class InventoryContainer : MonoBehaviour, ISaveable
{
    [System.Serializable]
    public class ItemKeyValue
    {
        public int ItemID;
        public int Amount;
    }

    private Inventory inventory;

    private ContainerItem selectedItem;
    private ContainerItem lastSelectedItem;

    private List<ContainerItemData> ContainterItemsData = new List<ContainerItemData>();

    public List<ItemKeyValue> StartingItems = new List<ItemKeyValue>();

    [Header("Settings")]
    public string containerName;
    public int containerSpace;

    [Header("Sounds")]
    public AudioClip OpenSound;
    [Range(0,1)]
    public float Volume = 1f;

    [HideInInspector] public bool isOpened;

    public bool IsSelecting()
    {
        return selectedItem;
    }

    public ContainerItem GetSelectedItem()
    {
        return selectedItem;
    }

    void Awake()
    {
        inventory = Inventory.Instance;
    }

    void Start()
    {
        if(StartingItems.Count > 0)
        {
            foreach (var item in StartingItems)
            {
                ContainterItemsData.Add(new ContainerItemData(inventory.GetItem(item.ItemID), item.Amount));
            }
        }
    }

    void Update()
    {
        if (!isOpened) return;

        if (inventory.ContainterItemsCache.Count > 0 && inventory.ContainterItemsCache.Any(item => item.IsSelected()))
        {
            selectedItem = inventory.ContainterItemsCache.SingleOrDefault(item => item.IsSelected());
            lastSelectedItem = selectedItem;
        }
        else
        {
            selectedItem = null;
        }
    }

    public void UseObject()
    {
        if (OpenSound) { AudioSource.PlayClipAtPoint(OpenSound, transform.position, Volume); }

        inventory.ContainterItemsCache.Clear();
        inventory.ShowInventoryContainer(this, ContainterItemsData.ToArray(), containerName);
        isOpened = true;
    }

    public void StoreItem(Item item, int amount)
    {
        GameObject coItem = Instantiate(inventory.containerItem, inventory.ContainterContent.transform);
        ContainerItemData itemData = new ContainerItemData(item, amount);
        coItem.GetComponent<ContainerItem>().inventoryContainer = this;
        coItem.GetComponent<ContainerItem>().item = item;
        coItem.GetComponent<ContainerItem>().amount = amount;
        coItem.name = "CoItem_" + item.Title.Replace(" ", "");
        ContainterItemsData.Add(itemData);
        inventory.ContainterItemsCache.Add(coItem.GetComponent<ContainerItem>());
    }

    public void TakeBack(Item item = null)
    {
        if (inventory.CheckInventorySpace())
        {
            if (item != null)
            {
                if (ContainsItemID(item.ID))
                {
                    ContainerItem itemData = GetContainerItem(item.ID);
                    inventory.AddItem(item.ID, itemData.amount);
                    Destroy(itemData.gameObject);
                    RemoveItem(item.ID);
                }
            }
            else
            {
                GameObject destroyObj = lastSelectedItem.gameObject;
                inventory.AddItem(lastSelectedItem.item.ID, lastSelectedItem.amount);
                RemoveItem(lastSelectedItem.item.ID);
                Destroy(destroyObj);
            }
        }
        else
        {
            inventory.DeselectSelected();
            inventory.ShowNotification("No Space in Inventory!");
        }
    }

    public void AddItemAmount(Item item, int amount)
    {
        ContainerItemData itemData = ContainterItemsData.SingleOrDefault(citem => citem.item.ID == item.ID);
        itemData.amount += amount;
        GetContainerItem(item.ID).amount = itemData.amount;
    }

    private void RemoveItem(int id)
    {
        for (int i = 0; i < ContainterItemsData.Count; i++)
        {
            if(ContainterItemsData[i].item.ID == id)
            {
                ContainterItemsData.RemoveAt(i);
                inventory.ContainterItemsCache.RemoveAt(i);
            }
        }
    }

    public int GetContainerCount()
    {
        return ContainterItemsData.Count;
    }

    public bool ContainsItemID(int id)
    {
        return ContainterItemsData.Any(citem => citem.item.ID == id);
    }

    public ContainerItem GetContainerItem(int id)
    {
        return inventory.ContainterItemsCache.SingleOrDefault(citem => citem.item.ID == id);
    }

    public Dictionary<string, object> OnSave()
    {
        if (ContainterItemsData.Count > 0)
        {
            Dictionary<string, object> containerData = new Dictionary<string, object>();

            foreach (var item in ContainterItemsData)
            {
                containerData.Add(item.item.ID.ToString(), item.amount);
            }

            return containerData;
        }
        else
        {
            return null;
        }
    }

    public void OnLoad(JToken token)
    {
        if (token != null && token.HasValues)
        {
            foreach (KeyValuePair<int, int> item in token.ToObject<Dictionary<int,int>>())
            {
                ContainterItemsData.Add(new ContainerItemData(inventory.GetItem(item.Key), item.Value));
            }
        }
    }
}

public class ContainerItemData
{
    public Item item;
    public int amount;

    public ContainerItemData(Item item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}
