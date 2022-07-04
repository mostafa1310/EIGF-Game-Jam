using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType { Normal, Heal, ItemPart, Weapon, Bullets, Light }

public class InventoryScriptable : ScriptableObject {

	public List<ItemMapper> ItemDatabase = new List<ItemMapper> ();
    private List<ItemCombine> Combines = new List<ItemCombine>();

	[Serializable]
	public class ItemMapper {

		public string Title;
        [ReadOnly] public int ID;
        [Multiline] public string Description;
        public ItemType itemType;
        public Sprite itemSprite;
        public GameObject dropObject;
        public GameObject packDropObject;

        [Serializable]
        public class Booleans
        {
            public bool isStackable;
            public bool isUsable;
            public bool isCombinable;
            public bool isDroppable;
            public bool canInspect;
            public bool canBindShortcut;
            public bool CombineGetItem;
            public bool CombineNoRemove;
            public bool CombineGetSwItem;
            public bool UseItemSwitcher;
        }
        public Booleans itemToggles = new Booleans();

        [Serializable]
        public class Sounds
        {
            public AudioClip useSound;
            public AudioClip combineSound;
            [Range(0,1f)]
            public float soundVolume = 1f;
        }
        public Sounds itemSounds = new Sounds();

        [Serializable]
        public class Settings
        {
            public int maxItemCount;
            public int useSwitcherID = -1;
            public int healAmount;
        }

		public Settings itemSettings = new Settings();

        [Serializable]
        public class CombineSettings
        {
            public int combineWithID;
            public int resultCombineID;
            public int combineSwitcherID;
        }

        public CombineSettings[] combineSettings;
    }

    class ItemCombine
    {
        public class CombinePair
        {
            int with;
            int result;

            public CombinePair(int one, int two)
            {
                with = one;
                result = two;
            }
        }

        public int item;
        public CombinePair[] combinePairs;

        public ItemCombine(int id, CombinePair[] combines)
        {
            item = id;
            combinePairs = combines;
        }
    }

    public void Reseed()
    {
        foreach (ItemMapper x in ItemDatabase)
        {
            x.ID = ItemDatabase.IndexOf(x);
        }
    }
}
