/*
 * Inventory.cs - script by ThunderWire Games
 * ver. 1.3
*/

using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ThunderWire.Helpers;
using ThunderWire.Utility;

/// <summary>
/// Main Inventory Script
/// </summary>
public class Inventory : Singleton<Inventory> {

    private HFPS_GameManager gameManager;
    private ItemSwitcher switcher;
    private InventoryContainer currentContainer;
    private ObjectiveManager objectives;
    private UIFader fader;

    public class ShortcutModel
    {
        public Item item;
        public KeyCode shortcutKey;

        public ShortcutModel(Item item, KeyCode key)
        {
            this.item = item;
            this.shortcutKey = key;
        }
    }

    [HideInInspector]
    public List<GameObject> Slots = new List<GameObject>();
    [HideInInspector]
    public List<ShortcutModel> Shortcuts = new List<ShortcutModel>();

    [HideInInspector]
    public List<ContainerItemData> FixedContainerData = new List<ContainerItemData>();
    [HideInInspector]
    public List<ContainerItem> ContainterItemsCache = new List<ContainerItem>();

    private List<Item> AllItems = new List<Item>();
    private List<Item> ItemsCache = new List<Item>();

    [Tooltip("Database of all inventory items.")]
    public InventoryScriptable inventoryDatabase;

    [Header("Panels")]
    public GameObject ContainterPanel;
    public GameObject ItemInfoPanel;

    [Header("Contents")]
    public GameObject SlotsContent;
    public GameObject ContainterContent;

    [Space(7)]
    public Text ItemLabel;
    public Text ItemDescription;
    public Text ContainerNameText;
    public Text ContainerEmptyText;
    public Text ContainerCapacityText;
    public Image InventoryNotification;
    public Button TakeBackButton;

    [Header("Contex Menu")]
    public GameObject contexMenu;
    public Button contexUse;
    public Button contexCombine;
    public Button contexExamine;
    public Button contexDrop;
    public Button contexStore;
    public Button contexShortcut;

    [Header("Inventory Prefabs")]
    public GameObject inventorySlot;
    public GameObject inventoryItem;
    public GameObject containerItem;

    [Header("Slot Settings")]
    public Sprite slotsNormal;
    public Sprite slotWithItem;
    public Sprite slotItemSelect;
    public Sprite slotItemOutline;

    [Header("Inventory Items")]
    public int slotAmount;
    public int cornerSlot;
    public int maxSlots = 16;

    [Header("Inventory Settings")]
    public KeyCode TakeBackKey = KeyCode.Mouse0;
    public int itemDropStrenght = 10;
    public Color slotDisabled = Color.white;

    private bool notiFade;
    private bool isPressed;
    private bool isKeyUp;
    private bool isFixed;

    private bool shortcutBind;
    private int selectedBind;

    [HideInInspector]
    public bool isStoring;

    [HideInInspector]
    public int selectedID;

    [HideInInspector]
    public int selectedSwitcherID = -1;

    private ContainerItem selectedCoItem;

    public ItemSwitcher GetSwitcher()
    {
        return switcher;
    }

    void Awake()
    {
        if (!inventoryDatabase) { Debug.LogError("Inventory Database does not set!"); return; }

        for (int i = 0; i < inventoryDatabase.ItemDatabase.Count; i++)
        {
            AllItems.Add(new Item(i, inventoryDatabase.ItemDatabase[i]));
        }

        for (int i = 0; i < slotAmount; i++)
        {
            GameObject slot = Instantiate(inventorySlot);
            Slots.Add(slot);
            slot.GetComponent<InventorySlot>().inventory = this;
            slot.GetComponent<InventorySlot>().id = i;
            slot.transform.SetParent(SlotsContent.transform);
            slot.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
        }
    }

    void Start() {
        gameManager = GetComponent<HFPS_GameManager>();
        objectives = GetComponent<ObjectiveManager>();
        fader = new UIFader();

        ItemLabel.text = "";
        ItemDescription.text = "";
        ShowContexMenu(false);
        ItemInfoPanel.SetActive(false);
        TakeBackButton.gameObject.SetActive(false);

        selectedID = -1;
    }

    void Update()
    {
        if (!switcher) {
            switcher = gameManager.scriptManager.GetScript<ItemSwitcher>();
        }
        else
        {
            selectedSwitcherID = switcher.currentItem;
        }

        if (!gameManager.TabButtonPanel.activeSelf)
        {
            ShowContexMenu(false);
            ItemInfoPanel.SetActive(false);
            isStoring = false;
            shortcutBind = false;
            fader.fadeOut = true;

            foreach (var item in ContainterContent.GetComponentsInChildren<ContainerItem>())
            {
                Destroy(item.gameObject);
            }

            if (currentContainer)
            {
                currentContainer.isOpened = false;
                currentContainer = null;
            }

            ContainterPanel.SetActive(false);
            TakeBackButton.gameObject.SetActive(false);
            objectives.ShowObjectives(true);

            foreach (var slot in Slots)
            {
                slot.GetComponent<InventorySlot>().isCombining = false;
                slot.GetComponent<InventorySlot>().isCombinable = false;
                slot.GetComponent<InventorySlot>().contexVisible = false;
            }

            StopAllCoroutines();
            InventoryNotification.gameObject.SetActive(false);
            notiFade = false;
        }

        if (currentContainer != null || isFixed)
        {
            if (isFixed)
            {
                selectedCoItem = ContainterItemsCache.SingleOrDefault(item => item.IsSelected());
            }
            else if(currentContainer.IsSelecting())
            {
                selectedCoItem = currentContainer.GetSelectedItem();
            }
            else
            {
                selectedCoItem = null;
            }

            if (selectedCoItem != null)
            {
                TakeBackButton.gameObject.SetActive(true);
                Vector3 itemPos = TakeBackButton.transform.position;
                itemPos.y = selectedCoItem.transform.position.y;
                TakeBackButton.transform.position = itemPos;

                if (TakeBackKey != KeyCode.Mouse0)
                {
                    isKeyUp = true;
                }

                if (Input.GetKeyUp(TakeBackKey) && !isKeyUp)
                {
                    isKeyUp = true;
                }
                else if (isKeyUp)
                {
                    if (Input.GetKeyDown(TakeBackKey) && !isPressed)
                    {
                        TakeBackToInventory();
                        isPressed = true;
                        isKeyUp = false;
                    }
                    else if (isPressed)
                    {
                        isPressed = false;
                    }
                }
            }
            else
            {
                if (!TakeBackButton.gameObject.GetComponent<UIRaycastEvent>().pointerEnter)
                {
                    TakeBackButton.gameObject.SetActive(false);
                }

                isKeyUp = false;
            }

            if (!isFixed)
            {
                if (currentContainer.GetContainerCount() < 1)
                {
                    ContainerEmptyText.text = currentContainer.containerName.TitleCase() + " is Empty!";
                    ContainerEmptyText.gameObject.SetActive(true);
                }

                ContainerCapacityText.text = string.Format("Capacity {0}/{1}", currentContainer.GetContainerCount(), currentContainer.containerSpace);
            }
            else
            {
                if (FixedContainerData.Count < 1)
                {
                    ContainerEmptyText.gameObject.SetActive(true);
                }

                ContainerCapacityText.text = string.Format("Items Count: {0}", FixedContainerData.Count);
            }
        }

        if (shortcutBind && selectedBind == selectedID && selectedBind > -1)
        {
            foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(kcode))
                {
                    if (kcode == KeyCode.Alpha1 || kcode == KeyCode.Alpha2 || kcode == KeyCode.Alpha3 || kcode == KeyCode.Alpha4)
                    {
                        ShortcutBind(Slots[selectedID].GetComponentInChildren<InventoryItemData>().item, kcode);
                    }
                }
            }
        }
        else
        {
            if (Shortcuts.Count > 0)
            {
                for (int i = 0; i < Shortcuts.Count; i++)
                {
                    if (!CheckItemInventory(Shortcuts[i].item.ID))
                    {
                        Shortcuts.RemoveAt(i);
                        break;
                    }

                    if (Input.GetKeyDown(Shortcuts[i].shortcutKey))
                    {
                        UseItem(Shortcuts[i].item);
                    }
                }
            }

            fader.fadeOut = true;
            shortcutBind = false;
            selectedBind = -1;
        }

        if (!fader.fadeCompleted && notiFade)
        {
            Color colorN = InventoryNotification.color;
            Color colorT = InventoryNotification.transform.GetComponentInChildren<Text>().color;
            colorN.a = fader.GetFadeAlpha();
            colorT.a = fader.GetFadeAlpha();
            InventoryNotification.color = colorN;
            InventoryNotification.transform.GetComponentInChildren<Text>().color = colorT;
        }
        else
        {
            InventoryNotification.gameObject.SetActive(false);
            notiFade = false;
        }
    }

    public void DeselectContainerItem()
    {
        if (currentContainer != null && currentContainer.IsSelecting())
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void TakeBackToInventory()
    {
        if (!isFixed)
        {
            currentContainer.TakeBack();
        }
        else
        {
            if (CheckInventorySpace())
            {
                GameObject destroyObj = selectedCoItem.gameObject;
                AddItem(selectedCoItem.item.ID, selectedCoItem.amount);
                FixedContainerData.RemoveAll(x => x.item.ID == selectedCoItem.item.ID);
                ContainterItemsCache.RemoveAll(x => x.item.ID == selectedCoItem.item.ID);
                Destroy(destroyObj);
            }
            else
            {
                DeselectSelected();
                ShowNotification("No Space in Inventory!");
            }
        }

        TakeBackButton.gameObject.SetActive(false);
        TakeBackButton.gameObject.GetComponent<UIRaycastEvent>().pointerEnter = false;
    }

    public void ShowInventoryContainer(InventoryContainer container, ContainerItemData[] containerItems, string name = "CONTAINER")
    {
        if (!string.IsNullOrEmpty(name))
        {
            ContainerNameText.text = name.ToUpper();
        }
        else
        {
            ContainerNameText.text = "CONTAINER";
        }

        if (containerItems.Length > 0)
        {
            ContainerEmptyText.gameObject.SetActive(false);

            foreach (var citem in containerItems)
            {
                GameObject coItem = Instantiate(containerItem, ContainterContent.transform);
                ContainerItemData itemData = new ContainerItemData(citem.item, citem.amount);
                coItem.GetComponent<ContainerItem>().item = citem.item;
                coItem.GetComponent<ContainerItem>().amount = citem.amount;
                coItem.name = "CoItem_" + citem.item.Title.Replace(" ", "");
                ContainterItemsCache.Add(coItem.GetComponent<ContainerItem>());
            }
        }
        else
        {
            ContainerEmptyText.text = name.TitleCase() + " is Empty!";
            ContainerEmptyText.gameObject.SetActive(true);
        }

        isFixed = false;
        currentContainer = container;
        objectives.ShowObjectives(false);
        ContainterPanel.SetActive(true);
        gameManager.ShowInventory();
        isStoring = true;
    }

    public Dictionary<int, int> GetFixedContainerData()
    {
        return FixedContainerData.ToDictionary(x => x.item.ID, x => x.amount);
    }

    public void ShowFixedInventoryContainer(string name = "CONTAINER")
    {
        if (!string.IsNullOrEmpty(name))
        {
            ContainerNameText.text = name.ToUpper();
        }
        else
        {
            ContainerNameText.text = "CONTAINER";
        }

        if (FixedContainerData.Count > 0)
        {
            ContainerEmptyText.gameObject.SetActive(false);

            foreach (var citem in FixedContainerData)
            {
                GameObject coItem = Instantiate(containerItem, ContainterContent.transform);
                ContainerItemData itemData = new ContainerItemData(citem.item, citem.amount);
                coItem.GetComponent<ContainerItem>().item = citem.item;
                coItem.GetComponent<ContainerItem>().amount = citem.amount;
                coItem.name = "CoItem_" + citem.item.Title.Replace(" ", "");
                ContainterItemsCache.Add(coItem.GetComponent<ContainerItem>());
            }
        }
        else
        {
            ContainerEmptyText.text = name.TitleCase() + " is Empty!";
            ContainerEmptyText.gameObject.SetActive(true);
        }

        isFixed = true;
        objectives.ShowObjectives(false);
        ContainterPanel.SetActive(true);
        gameManager.ShowInventory();
        isStoring = true;
    }

    public void StoreSelectedItem()
    {
        if (!isFixed)
        {
            if (selectedID != -1 && currentContainer != null)
            {
                InventoryItemData itemData = Slots[selectedID].GetComponentInChildren<InventoryItemData>();

                if (currentContainer.ContainsItemID(itemData.item.ID) && itemData.item.isStackable)
                {
                    currentContainer.AddItemAmount(itemData.item, itemData.m_amount);
                    RemoveItemAll(itemData.item.ID);
                }
                else
                {
                    if (currentContainer.GetContainerCount() < currentContainer.containerSpace)
                    {
                        ContainerEmptyText.gameObject.SetActive(false);
                        currentContainer.StoreItem(itemData.item, itemData.m_amount);

                        if (switcher.currentItem == itemData.item.useSwitcherID)
                        {
                            switcher.DeselectItems();
                        }

                        RemoveItemAll(itemData.item.ID);
                    }
                    else
                    {
                        ShowNotification("No Space in Container!");
                        DeselectSelected();
                    }
                }
            }
        }
        else
        {
            if (selectedID != -1)
            {
                InventoryItemData itemData = Slots[selectedID].GetComponentInChildren<InventoryItemData>();

                if (FixedContainerData.Any(item => item.item.ID == itemData.item.ID) && itemData.item.isStackable)
                {
                    ContainerItemData containerData = FixedContainerData.SingleOrDefault(item => item.item.ID == itemData.item.ID);
                    containerData.amount += itemData.m_amount;
                    RemoveItemAll(itemData.item.ID);
                }
                else
                {
                    ContainerEmptyText.gameObject.SetActive(false);
                    StoreFixedContainerItem(itemData.item, itemData.m_amount);

                    if (switcher.currentItem == itemData.item.useSwitcherID)
                    {
                        switcher.DeselectItems();
                    }

                    RemoveItemAll(itemData.item.ID);
                }
            }
        }
    }

    void StoreFixedContainerItem(Item item, int amount)
    {
        GameObject coItem = Instantiate(containerItem, ContainterContent.transform);
        ContainerItemData itemData = new ContainerItemData(item, amount);
        coItem.GetComponent<ContainerItem>().inventoryContainer = null;
        coItem.GetComponent<ContainerItem>().item = item;
        coItem.GetComponent<ContainerItem>().amount = amount;
        coItem.name = "CoItem_" + item.Title.Replace(" ", "");
        FixedContainerData.Add(new ContainerItemData(item, amount));
        ContainterItemsCache.Add(coItem.GetComponent<ContainerItem>());
    }

    public void BindShortcutItem()
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            Slots[i].GetComponent<InventorySlot>().isSelected = false;
            Slots[i].GetComponent<InventorySlot>().contexVisible = false;
        }

        ShowContexMenu(false);
        ShowNotificationFixed("Select 1, 2, 3, 4 to bind item to a shortcut key.");
        selectedBind = selectedID;
        shortcutBind = true;
    }

    public void ShortcutBind(Item item, KeyCode kcode)
    {
        if (Shortcuts.Count > 0) {
            if (Shortcuts.All(s => s.item.ID != item.ID && s.shortcutKey != kcode))
            {
                Shortcuts.Add(new ShortcutModel(item, kcode));
                GetItemData(item).shortKey = ShortcutKeyToInt(kcode);
            }
            else
            {
                for (int i = 0; i < Shortcuts.Count; i++)
                {
                    if (Shortcuts[i].item.ID == item.ID)
                    {
                        if (Shortcuts.Any(s => s.shortcutKey == kcode))
                        {
                            ShortcutModel equalModel = Shortcuts.SingleOrDefault(s => s.shortcutKey == kcode);

                            equalModel.shortcutKey = Shortcuts[i].shortcutKey;
                            GetItemData(equalModel.item).shortKey = ShortcutKeyToInt(Shortcuts[i].shortcutKey);
                        }

                        Shortcuts[i].shortcutKey = kcode;
                        GetItemData(Shortcuts[i].item).shortKey = ShortcutKeyToInt(Shortcuts[i].shortcutKey);
                        break;
                    }
                    else if (Shortcuts[i].shortcutKey == kcode)
                    {
                        GetItemData(Shortcuts[i].item).shortKey = -1;
                        GetItemData(item).shortKey = ShortcutKeyToInt(kcode);
                        Shortcuts[i].item = item;
                        break;
                    }
                }
            }
        }
        else
        {
            Shortcuts.Add(new ShortcutModel(item, kcode));
            GetItemData(item).shortKey = ShortcutKeyToInt(kcode);
        }

        DeselectSelected();
        fader.fadeOut = true;
        shortcutBind = false;
    }

    int ShortcutKeyToInt(KeyCode kcode)
    {
        if (kcode == KeyCode.Alpha1)
        {
            return 1;
        }
        else if (kcode == KeyCode.Alpha2)
        {
            return 2;
        }
        else if (kcode == KeyCode.Alpha3)
        {
            return 3;
        }
        else if (kcode == KeyCode.Alpha4)
        {
            return 4;
        }

        return -1;
    }

    /// <summary>
    /// Get Item from item database by ID
    /// </summary>
    public Item GetItem(int ID)
    {
        return inventoryDatabase.ItemDatabase.Where(item => item.ID == ID).Select(item => new Item(item.ID, item)).SingleOrDefault();
    }

    /// <summary>
    /// Function to add new item to specific slot
    /// </summary>
    public void AddItemSlot(int slotID, int itemID, int amount)
    {
        Item itemToAdd = GetItem(itemID);
        if (CheckInventorySpace() || CheckItemInventory(itemID))
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (i == slotID)
                {
                    ItemsCache.Add(itemToAdd);
                    GameObject item = Instantiate(inventoryItem, Slots[i].transform);
                    InventoryItemData itemData = item.GetComponent<InventoryItemData>();
                    itemData.item = itemToAdd;
                    itemData.m_amount = amount;
                    itemData.slotID = i;
                    Slots[i].GetComponent<InventorySlot>().slotItem = itemToAdd;
                    Slots[i].GetComponent<InventorySlot>().itemData = itemData;
                    Slots[i].transform.GetChild(0).GetComponent<Image>().sprite = slotItemOutline;
                    Slots[i].GetComponent<Image>().sprite = slotWithItem;
                    Slots[i].GetComponent<Image>().enabled = true;
                    item.GetComponent<Image>().sprite = itemToAdd.itemSprite;
                    item.GetComponent<RectTransform>().position = Vector2.zero;
                    item.name = itemToAdd.Title;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Function to add new item to slot
    /// </summary>
    public void AddItem(int itemID, int amount)
    {
        Item itemToAdd = GetItem(itemID);

        if (CheckInventorySpace() || CheckItemInventory(itemID))
        {
            if (itemToAdd.isStackable && CheckItemInventory(itemToAdd) && GetItemData(itemToAdd) != null)
            {
                InventoryItemData itemData = GetItemData(itemToAdd);
                itemData.m_amount += amount;
            }
            else
            {
                for (int i = 0; i < Slots.Count; i++)
                {
                    if (Slots[i].transform.childCount == 1)
                    {
                        ItemsCache.Add(itemToAdd);
                        GameObject item = Instantiate(inventoryItem, Slots[i].transform);
                        InventoryItemData itemData = item.GetComponent<InventoryItemData>();
                        itemData.item = itemToAdd;
                        itemData.m_amount = amount;
                        itemData.slotID = i;
                        Slots[i].GetComponent<InventorySlot>().slotItem = itemToAdd;
                        Slots[i].GetComponent<InventorySlot>().itemData = itemData;
                        Slots[i].transform.GetChild(0).GetComponent<Image>().sprite = slotItemOutline;
                        Slots[i].GetComponent<Image>().sprite = slotWithItem;
                        Slots[i].GetComponent<Image>().enabled = true;
                        item.GetComponent<Image>().sprite = itemToAdd.itemSprite;
                        item.GetComponent<RectTransform>().position = Vector2.zero;
                        item.name = itemToAdd.Title;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Function to remove one item
    /// </summary>
    public void RemoveItem(int id)
    {
        Item itemToRemove = GetItem(id);
        int slotID = GetItemSlotID(itemToRemove);
        if (itemToRemove.isStackable && CheckItemInventory(itemToRemove))
        {
            InventoryItemData data = Slots[slotID].GetComponentInChildren<InventoryItemData>();
            data.m_amount--;
            data.textAmount.text = data.m_amount.ToString();
            if (data.m_amount <= 0)
            {
                Destroy(Slots[slotID].transform.GetChild(1).gameObject);
                ItemsCache.RemoveAll(x => x.ID == itemToRemove.ID);
                DeselectSelected();
            }
            if (data.m_amount == 1)
            {
                data.textAmount.text = "";
            }
        }
        else
        {
            Destroy(Slots[slotID].transform.GetChild(1).gameObject);
            Slots[slotID].transform.GetChild(0).GetComponent<Image>().sprite = slotsNormal;
            Slots[slotID].transform.GetChild(0).GetComponent<Image>().color = Color.white;
            Slots[slotID].GetComponent<Image>().enabled = false;
            ItemsCache.RemoveAll(x => x.ID == itemToRemove.ID);
        }
    }

    /// <summary>
    /// Function to remove all item stacks
    /// </summary>
    public void RemoveItemAll(int ID)
    {
        Item itemRemove = GetItem(ID);

        if (CheckItemInventory(itemRemove))
        {
            Destroy(Slots[GetItemSlotID(itemRemove)].GetComponentInChildren<InventoryItemData>().gameObject);
            ItemsCache.RemoveAll(x => x.ID == itemRemove.ID);
            DeselectSelected();
        }
    }

    /// <summary>
    /// Function to remove specific item amount
    /// </summary>
    public void RemoveItemAmount(int ID, int Amount)
    {
        Item itemToRemove = GetItem(ID);
        if (CheckItemInventory(itemToRemove))
        {
            InventoryItemData data = Slots[GetItemSlotID(itemToRemove)].GetComponentInChildren<InventoryItemData>();

            if (data.m_amount > Amount)
            {
                data.m_amount = data.m_amount - Amount;
                data.transform.parent.GetChild(0).GetChild(0).GetComponent<Text>().text = data.m_amount.ToString();
            }
            else
            {
                RemoveItemAll(ID);
            }
        }
    }

    public void UseSelectedItem()
    {
        UseItem();
    }

    public void UseItem(Item item = null)
    {
        Item usableItem = item;

        if(usableItem == null)
        {
            usableItem = Slots[selectedID].GetComponentInChildren<InventoryItemData>().item;
        }

        if (GetItemAmount(usableItem.ID) < 2 || usableItem.useItemSwitcher)
        {
            ShowContexMenu(false);

            if (usableItem.useItemSwitcher)
            {
                DeselectSelected();
            }
        }

        if (usableItem.itemType == ItemType.Heal)
        {
            gameManager.healthManager.ApplyHeal(usableItem.healAmount);
            if (!gameManager.healthManager.isMaximum)
            {
                if (usableItem.useSound)
                {
                    Tools.PlayOneShot2D(Tools.MainCamera().transform.position, usableItem.useSound, usableItem.soundVolume);
                }
                RemoveItem(usableItem.ID);
            }
        }

        if (usableItem.itemType == ItemType.Light)
        {
            switcher.currentLightObject = usableItem.useSwitcherID;
        }

        if (usableItem.itemType == ItemType.Weapon || usableItem.useItemSwitcher)
        {
            switcher.SelectItem(usableItem.useSwitcherID);
            switcher.weaponItem = usableItem.useSwitcherID;
        }
    }

    public void DropItemGround()
    {
        Item item = Slots[selectedID].GetComponentInChildren<InventoryItemData>().item;
        Transform dropPos = Tools.MainCamera().transform.parent.parent.GetComponent<PlayerFunctions>().inventoryDropPos;
        GameObject dropObject = GetDropObject(item);

        if (item.itemType == ItemType.Weapon || item.useItemSwitcher)
        {
            if (switcher.currentItem == item.useSwitcherID)
            {
                switcher.DisableItems();
            }
        }

        if (item.itemType == ItemType.Light && switcher.currentLightObject == item.useSwitcherID)
        {
            switcher.currentLightObject = -1;
        }

        GameObject worldItem = null;

        if (GetItemAmount(item.ID) >= 2 && item.itemType != ItemType.Weapon)
        {
            worldItem = Instantiate(item.packDropObject, dropPos.position, dropPos.rotation);
            worldItem.name = "PackDrop_" + dropObject.name;
            InteractiveItem interactiveItem = worldItem.GetComponent<InteractiveItem>();

            if (string.IsNullOrEmpty(interactiveItem.ItemName))
            {
                interactiveItem.ItemName = "Sack of " + item.Title;
            }

            if (interactiveItem.messageType != InteractiveItem.MessageType.None && string.IsNullOrEmpty(interactiveItem.Message))
            {
                interactiveItem.Message = "Sack of " + item.Title;
            }

            interactiveItem.ItemType = InteractiveItem.Type.InventoryItem;
            interactiveItem.InventoryID = item.ID;
        }
        else if(GetItemAmount(item.ID) == 1 || item.itemType == ItemType.Weapon)
        {
            worldItem = Instantiate(dropObject, dropPos.position, dropPos.rotation);
            worldItem.name = "Drop_" + dropObject.name;
        }

        Physics.IgnoreCollision(worldItem.GetComponent<Collider>(), Tools.MainCamera().transform.root.GetComponent<Collider>());

        worldItem.GetComponent<Rigidbody>().AddForce(Tools.MainCamera().transform.forward * (itemDropStrenght * 10));
        worldItem.GetComponent<InteractiveItem>().disableType = InteractiveItem.DisableType.Destroy;

        if (worldItem.GetComponent<SaveObject>())
        {
            Destroy(worldItem.GetComponent<SaveObject>());
        }

        if (GetItemAmount(item.ID) < 2 || item.useItemSwitcher || item.itemType == ItemType.Bullets)
        {
            ShowContexMenu(false);
        }

        if (GetItemAmount(item.ID) > 1)
        {
            worldItem.GetComponent<InteractiveItem>().Amount = GetItemAmount(item.ID);
            RemoveItemAll(item.ID);
        }
        else
        {
            RemoveItem(item.ID);
        }
    }

    public void CombineItem()
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            Slots[i].GetComponent<InventorySlot>().isCombining = true;

            if (!IsCombineSlot(i))
            {
                Slots[i].GetComponent<Image>().color = slotDisabled;
                Slots[i].GetComponent<InventorySlot>().isCombinable = false;
            }
            else
            {
                Slots[i].GetComponent<InventorySlot>().isCombinable = true;
            }
        }

        ShowContexMenu(false);
    }

    bool IsCombineSlot(int slotID)
    {
        InventoryScriptable.ItemMapper.CombineSettings[] combineSettings = Slots[selectedID].GetComponentInChildren<InventoryItemData>().item.combineSettings;

        foreach (var id in combineSettings)
        {
            if (Slots[slotID].GetComponent<InventorySlot>().itemData != null)
            {
                if (Slots[slotID].GetComponent<InventorySlot>().itemData.item.ID == id.combineWithID)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool HasCombinePartner(Item Item)
    {
        InventoryScriptable.ItemMapper.CombineSettings[] combineSettings = Item.combineSettings;
        return ItemsCache.Any(item => combineSettings.Any(item2 => item.ID == item2.combineWithID));
    }

    public void CombineWith(Item SecondItem, int slotID)
    {
        if (slotID != selectedID)
        {
            Item SelectedItem = Slots[selectedID].GetComponentInChildren<InventoryItemData>().item;
            InventoryScriptable.ItemMapper.CombineSettings[] selectedCombineSettings = SelectedItem.combineSettings;

            int CombinedItemID = -1;
            int CombineSwitcherID = -1;

            foreach(var item in selectedCombineSettings)
            {
                if(item.combineWithID == SecondItem.ID)
                {
                    CombinedItemID = item.resultCombineID;
                    CombineSwitcherID = item.combineSwitcherID;
                }
            }

            for (int i = 0; i < Slots.Count; i++)
            {
                Slots[i].GetComponent<InventorySlot>().isCombining = false;
                Slots[i].GetComponent<InventorySlot>().isCombinable = false;
                Slots[i].GetComponent<Image>().color = Color.white;
            }

            if (SelectedItem.combineSound)
            {
                Tools.PlayOneShot2D(Tools.MainCamera().transform.position, SelectedItem.combineSound, SelectedItem.soundVolume);
            }
            else
            {
                if (SecondItem.combineSound)
                {
                    Tools.PlayOneShot2D(Tools.MainCamera().transform.position, SecondItem.combineSound, SecondItem.soundVolume);
                }
            }

            if (SelectedItem.itemType == ItemType.ItemPart && SelectedItem.isCombinable)
            {
                int switcherID = GetItem(SelectedItem.combineSettings[0].combineWithID).useSwitcherID;
                GameObject MainObject = switcher.ItemList[switcherID];

                MonoBehaviour script = MainObject.GetComponents<MonoBehaviour>().SingleOrDefault(sc => sc.GetType().GetField("CanReload") != null);
                FieldInfo info = script.GetType().GetField("CanReload");

                if (info != null)
                {
                    bool canReload = Parser.Convert<bool>(script.GetType().InvokeMember("CanReload", BindingFlags.GetField, null, script, null).ToString());

                    if (canReload)
                    {
                        MainObject.SendMessage("Reload", SendMessageOptions.DontRequireReceiver);
                        RemoveItem(SelectedItem.ID);
                    }
                    else
                    {
                        gameManager.AddMessage("Cannot reload yet!");
                        DeselectSelected();
                    }
                }
                else
                {
                    Debug.Log(MainObject.name + " object does not have script with CanReload property!");
                }
            }
            else if (SelectedItem.isCombinable)
            {
                if (SelectedItem.combineGetSwItem && CombineSwitcherID != -1)
                {
                    if (CombineSwitcherID != -1)
                        switcher.SelectItem(CombineSwitcherID);
                }

                if (SelectedItem.combineGetItem && CombinedItemID != -1)
                {
                    int a_count = GetItemAmount(SelectedItem.ID);
                    int b_count = GetItemAmount(SecondItem.ID);

                    if (a_count < 2 && b_count >= 2)
                    {
                        if (!SelectedItem.combineNoRemove)
                        {
                            StartCoroutine(WaitForRemoveAddItem(SelectedItem, CombinedItemID));
                        }
                        else
                        {
                            AddItem(CombinedItemID, 1);
                        }
                    }
                    if (a_count >= 2 && b_count < 2)
                    {
                        if (!SecondItem.combineNoRemove)
                        {
                            StartCoroutine(WaitForRemoveAddItem(SecondItem, CombinedItemID));
                        }
                        else
                        {
                            AddItem(CombinedItemID, 1);
                        }
                    }
                    if (a_count < 2 && b_count < 2)
                    {
                        if (!SelectedItem.combineNoRemove)
                        {
                            StartCoroutine(WaitForRemoveAddItem(SelectedItem, CombinedItemID));
                        }
                        else
                        {
                            AddItem(CombinedItemID, 1);
                        }
                    }
                    if (a_count >= 2 && b_count >= 2)
                    {
                        AddItem(CombinedItemID, 1);
                    }
                }

                if (!SelectedItem.combineNoRemove)
                {
                    RemoveItem(SelectedItem.ID);
                }
                if (!SecondItem.combineNoRemove)
                {
                    RemoveItem(SecondItem.ID);
                }
            }
        }
    }

    public void ExamineItem()
    {
        Item item = Slots[selectedID].GetComponentInChildren<InventoryItemData>().item;
        gameManager.TabButtonPanel.SetActive(false);
        gameManager.ShowCursor(false);

        if (item.dropObject && item.dropObject.GetComponent<InteractiveItem>())
        {
            gameManager.scriptManager.gameObject.GetComponent<ExamineManager>().ExamineObject(Instantiate(GetDropObject(item)));
        }
    }

    /// <summary>
    /// Get Item Drop Object by Item
    /// </summary>
    public GameObject GetDropObject(Item item)
    {
        return AllItems.Where(x => x.ID == item.ID).Select(x => x.dropObject).SingleOrDefault();
    }

    /// <summary>
    /// Function to set specific item amount
    /// </summary>
    public void SetItemAmount(int ID, int Amount)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].transform.childCount > 1)
            {
                if (Slots[i].GetComponentInChildren<InventoryItemData>().item.ID == ID)
                {
                    Slots[i].GetComponentInChildren<InventoryItemData>().m_amount = Amount;
                }
            }
        }
    }

    /// <summary>
    /// Function to expand item slots
    /// </summary>
    public void ExpandSlots(int SlotsAmount)
    {
        int extendedSlots = slotAmount + SlotsAmount;

        if (extendedSlots > maxSlots)
        {
            gameManager.WarningMessage("Cannot carry more backpacks");
            return;
        }

        for (int i = slotAmount; i < extendedSlots; i++)
        {
            GameObject slot = Instantiate(inventorySlot);
            Slots.Add(slot);
            slot.GetComponent<InventorySlot>().inventory = this;
            slot.GetComponent<InventorySlot>().id = i;
            slot.transform.SetParent(SlotsContent.transform);
            slot.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
        }

        slotAmount = extendedSlots;
    }

    /// <summary>
    /// Check if there is space in Inevntory
    /// </summary>
    public bool CheckInventorySpace()
    {
        return Slots.Any(x => x.transform.childCount < 2);
    }

    /// <summary>
    /// Check if Item is in Inventory
    /// </summary>
    public bool CheckItemInventory(Item item)
    {
        return ItemsCache.Any(x => x.ID == item.ID);
    }

    /// <summary>
    /// Check if Item is in Inventory by ID
    /// </summary>
    public bool CheckItemInventory(int ID)
    {
        return ItemsCache.Any(x => x.ID == ID);
    }

    /// <summary>
    /// Check if Switcher Item is in Inventory
    /// </summary>
    public bool CheckSWIDInventory(int SwitcherID)
    {
        return ItemsCache.Any(x => x.useSwitcherID == SwitcherID);
    }

    InventoryItemData GetItemData(Item item)
    {
        if (CheckItemInventory(item))
        {
            foreach (var slot in Slots)
            {
                if(slot.GetComponentInChildren<InventoryItemData>() && slot.GetComponentInChildren<InventoryItemData>().item.ID == item.ID)
                {
                    return slot.GetComponentInChildren<InventoryItemData>();
                }
            }
        }

        return null;
    }

    GameObject GetItemSlotGO(Item item)
    {
        foreach (var slot in Slots)
        {
            if (slot.GetComponentInChildren<InventoryItemData>() && slot.GetComponentInChildren<InventoryItemData>().item.ID == item.ID)
            {
                return slot;
            }
        }

        return null;
    }

    int GetItemSlotID(Item item)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].GetComponentInChildren<InventoryItemData>() && Slots[i].GetComponentInChildren<InventoryItemData>().item.ID == item.ID)
            {
                return i;
            }
        }

        return -1;
    }

    int GetItemSlotID(int ID)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].GetComponentInChildren<InventoryItemData>() && Slots[i].GetComponentInChildren<InventoryItemData>().item.ID == ID)
            {
                return i;
            }
        }

        return -1;
    }

    public int GetItemAmount(int itemID)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].GetComponentInChildren<InventoryItemData>() && Slots[i].GetComponentInChildren<InventoryItemData>().item.ID == itemID)
            {
                return Slots[i].GetComponentInChildren<InventoryItemData>().m_amount;
            }
        }

        return -1;
    }

    IEnumerator WaitForRemoveAddItem(Item item, int combinedID)
	{
		yield return new WaitUntil (() => !CheckItemInventory(item));
        AddItem(combinedID, 1);
	}

	public void Deselect(int id){
        Slots[id].GetComponent<Image>().color = Color.white;

        if (Slots[id].transform.childCount > 1)
        {
            Slots[id].GetComponentInChildren<InventoryItemData>().selected = false;
        }

		ItemLabel.text = "";
		ItemDescription.text = "";
        ShowContexMenu(false);

        selectedID = -1;
	}

    public void DeselectSelected()
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            Slots[i].GetComponent<InventorySlot>().isCombining = false;
            Slots[i].GetComponent<InventorySlot>().isCombinable = false;
            Slots[i].GetComponent<InventorySlot>().isSelected = false;
            Slots[i].GetComponent<InventorySlot>().contexVisible = false;
        }

        if (selectedID != -1)
        {
            if (Slots[selectedID].GetComponentInChildren<InventoryItemData>())
            {
                Slots[selectedID].GetComponentInChildren<InventoryItemData>().selected = false;
            }

            Slots[selectedID].GetComponent<Image>().color = Color.white;
            TakeBackButton.gameObject.SetActive(false);
            ShowContexMenu(false);
            ItemLabel.text = "";
            ItemDescription.text = "";
            selectedID = -1;
        }

        if(ContainterItemsCache.Count > 0)
        {
            foreach (var item in ContainterItemsCache)
            {
                item.Deselect();
            }
        }

        ItemInfoPanel.SetActive(false);
    }

    public void ShowContexMenu(bool show, Item item = null, int slot = -1, bool ctx_use = true, bool ctx_combine = true, bool ctx_examine = true, bool ctx_drop = true, bool ctx_shortcut = false, bool ctx_store = false)
    {
        if (show && item != null && slot > -1) {
            Vector3[] corners = new Vector3[4];
            Slots[slot].GetComponent<RectTransform>().GetWorldCorners(corners);
            int[] cornerSlots = Enumerable.Range(0, maxSlots + 1).Where(x => x % cornerSlot == 0).ToArray();
            int n_slot = slot + 1;

            if (!cornerSlots.Contains(n_slot))
            {
                contexMenu.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                contexMenu.transform.position = corners[2];
            }
            else
            {
                contexMenu.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
                contexMenu.transform.position = corners[1];
            }
        }

        if (!show)
        {
            contexUse.gameObject.SetActive(false);
            contexCombine.gameObject.SetActive(false);
            contexExamine.gameObject.SetActive(false);
            contexDrop.gameObject.SetActive(false);
            contexShortcut.gameObject.SetActive(false);
            contexStore.gameObject.SetActive(false);
        }
        else
        {
            contexUse.gameObject.SetActive(item.isUsable && ctx_use);
            contexCombine.gameObject.SetActive(item.isCombinable && ctx_combine);
            contexExamine.gameObject.SetActive(item.canInspect && ctx_examine);
            contexDrop.gameObject.SetActive(item.isDroppable && ctx_drop);
            contexShortcut.gameObject.SetActive(item.canBindShortcut && ctx_shortcut);
            contexStore.gameObject.SetActive(ctx_store);
        }

        if(item != null)
        {
            if(item.isUsable || item.isCombinable || item.canInspect || item.isDroppable || ctx_store)
            {
                contexMenu.SetActive(true);
            }
            else
            {
                contexMenu.SetActive(false);
            }
        }
        else
        {
            contexMenu.SetActive(show);
        }
    }

    public void ShowNotification(string text)
    {
        InventoryNotification.transform.GetComponentInChildren<Text>().text = text;
        InventoryNotification.gameObject.SetActive(true);
        notiFade = true;
        StartCoroutine(fader.StartFadeIO(InventoryNotification.color.a, 1.2f, 0.8f, 3, 4, UIFader.FadeOutAfter.Time));
    }

    public void ShowNotificationFixed(string text)
    {
        InventoryNotification.transform.GetComponentInChildren<Text>().text = text;
        InventoryNotification.gameObject.SetActive(true);
        notiFade = true;
        fader.fadeOut = false;
        StartCoroutine(fader.StartFadeIO(InventoryNotification.color.a, 1.2f, 0.8f, 3, 3, UIFader.FadeOutAfter.Bool));
    }
}

public class Item
{
    //Main
    public int ID { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public ItemType itemType { get; set; }
    public Sprite itemSprite { get; set; }
    public GameObject dropObject { get; set; }
    public GameObject packDropObject { get; set; }

    //Toggles
    public bool isStackable { get; set; }
    public bool isUsable { get; set; }
    public bool isCombinable { get; set; }
    public bool isDroppable { get; set; }
    public bool canInspect { get; set; }
    public bool canBindShortcut { get; set; }
    public bool combineGetItem { get; set; }
    public bool combineNoRemove { get; set; }
    public bool combineGetSwItem { get; set; }
    public bool useItemSwitcher { get; set; }

    //Sounds
    public AudioClip useSound { get; set; }
    public AudioClip combineSound { get; set; }
    public float soundVolume { get; set; }

    //Settings
    public int maxItemCount { get; set; }
    public int useSwitcherID { get; set; }
    public int healAmount { get; set; }

    //Combine Settings
    public InventoryScriptable.ItemMapper.CombineSettings[] combineSettings { get; set; }

    public Item()
    {
        ID = 0;
    }

    public Item(int itemId, InventoryScriptable.ItemMapper mapper)
    {
        ID = itemId;
        Title = mapper.Title;
        Description = mapper.Description;
        itemType = mapper.itemType;
        itemSprite = mapper.itemSprite;
        dropObject = mapper.dropObject;
        packDropObject = mapper.packDropObject;

        isStackable = mapper.itemToggles.isStackable;
        isUsable = mapper.itemToggles.isUsable;
        isCombinable = mapper.itemToggles.isCombinable;
        isDroppable = mapper.itemToggles.isDroppable;
        canInspect = mapper.itemToggles.canInspect;
        canBindShortcut = mapper.itemToggles.canBindShortcut;
        combineGetItem = mapper.itemToggles.CombineGetItem;
        combineNoRemove = mapper.itemToggles.CombineNoRemove;
        combineGetSwItem = mapper.itemToggles.CombineGetSwItem;
        useItemSwitcher = mapper.itemToggles.UseItemSwitcher;

        useSound = mapper.itemSounds.useSound;
        combineSound = mapper.itemSounds.combineSound;
        soundVolume = mapper.itemSounds.soundVolume;

        maxItemCount = mapper.itemSettings.maxItemCount;
        useSwitcherID = mapper.itemSettings.useSwitcherID;
        healAmount = mapper.itemSettings.healAmount;

        combineSettings = mapper.combineSettings;
    }
}
