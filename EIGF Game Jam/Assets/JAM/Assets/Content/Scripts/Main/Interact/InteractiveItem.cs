using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class InteractiveItem : MonoBehaviour, ISaveable {

    [System.Serializable]
    public class MessageTip
    {
        public string InputString;
        public string KeyMessage;
    }

    private AudioSource audioSource;

    public enum Type { OnlyExamine, GenericItem, InventoryItem, ArmsItem, BackpackExpand }
    public enum ExamineType { None, Object, AdvancedObject, Paper }
    public enum ExamineRotate { None, Horizontal, Vertical, Both }
    public enum MessageType { None, Hint, PickupHint, Message, ItemName }
    public enum DisableType { Disable, Destroy, None }

	public Type ItemType = Type.GenericItem;
    public ExamineType examineType = ExamineType.None;
    public ExamineRotate examineRotate = ExamineRotate.Both;
    public MessageType messageType = MessageType.None;
    public DisableType disableType = DisableType.Disable;

    public string ItemName;
    public string Message;
    public float MessageTime = 3f;

    public MessageTip[] MessageTips;

    public AudioClip PickupSound;
    public AudioClip ExamineSound;

    [Range(0, 1)] public float Volume = 1f;
	public int Amount = 1;

    public bool pickupSwitch;
    public bool markLightObject;
    public bool examineCollect;
    public bool enableCursor;
    public bool showItemName;
    public bool floatingIconEnabled = true;

	public int WeaponID;
	public int InventoryID;
	public int BackpackExpand;

    public float ExamineDistance;
    public bool faceToCamera = false;

    //AdvancedObject - Examine
    [Tooltip("Colliders which will be disabled when object will be examined.")]
    public Collider[] CollidersDisable;
    [Tooltip("Colliders which will be enabled when object will be examined.")]
    public Collider[] CollidersEnable;


    [Multiline]
    public string paperReadText;
    public int textSize;

    public Vector3 faceRotation;
    public bool isExamined;

    void Start()
    {
        audioSource = ScriptManager.Instance.SoundEffects;
    }

    public void UseObject()
	{
        if (ItemType == Type.OnlyExamine) return;

        if (PickupSound)
        {
            audioSource.clip = PickupSound;
            audioSource.volume = Volume;
            audioSource.Play();
        }

        foreach (var item in GetComponents<MonoBehaviour>())
        {
            if (typeof(IItemEvent).IsAssignableFrom(item.GetType()))
            {
                (item as IItemEvent).DoEvent();
            }
        }

        if (disableType == DisableType.Destroy)
        {
            Destroy(gameObject);
        }
        else if(disableType == DisableType.Disable)
        {
            DisableObject(false);
        }
	}

    public void DisableObject(bool state)
    {
        if (state == false)
        {
            if (GetComponent<Rigidbody>())
            {
                GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                GetComponent<Rigidbody>().useGravity = false;
                GetComponent<Rigidbody>().isKinematic = true;
            }

            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<Collider>().enabled = false;

            if (transform.childCount > 0)
            {
                foreach (Transform child in transform.transform)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    public Dictionary<string, object> OnSave()
    {
        if (GetComponent<MeshRenderer>())
        {
            return new Dictionary<string, object>()
            {
                { "isDisabled", GetComponent<MeshRenderer>().enabled },
                { "position", transform.position },
                { "rotation", transform.eulerAngles }
            };
        }

        return null;
    }

    public void OnLoad(JToken token)
    {
        DisableObject(token["isDisabled"].ToObject<bool>());
        transform.position = token["position"].ToObject<Vector3>();
        transform.eulerAngles = token["rotation"].ToObject<Vector3>();
    }
}
