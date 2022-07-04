/*
 * ObjectiveManager.cs - wirted by ThunderWire Games
 * ver. 1.0
*/

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveManager : Singleton<ObjectiveManager> {

    private HFPS_GameManager gameManager;

    [Header("Main")]
    public ObjectivesScriptable SceneObjectives;
    public List<ObjectiveModel> objectiveCache = new List<ObjectiveModel>();
    private List<ObjectiveModel> objectives = new List<ObjectiveModel>();

    [Header("UI")]
    public GameObject ObjectivesUI;
    public GameObject PushObjectivesUI;
    public GameObject ObjectivePrefab;
    public GameObject PushObjectivePrefab;

    [Header("Timing")]
    public float CompleteTime = 3f;

    [Header("Other")]
    public bool isUppercased;
    public string multipleObjectivesText;
    public AudioClip newObjective;
    public AudioClip completeObjective;
    [Range(0,1f)] public float volume;

    private AudioSource soundEffects;
    private bool objShown;

    void Start()
    {
        foreach (var obj in SceneObjectives.Objectives)
        {
            objectives.Add(new ObjectiveModel(obj.objectiveID, obj.completeCount, obj.objectiveText));
        }

        gameManager = GetComponent<HFPS_GameManager>();
        soundEffects = ScriptManager.Instance.SoundEffects;
        objShown = true;
    }

    void Update()
    {
        if (objShown)
        {
            if (objectiveCache.Count > 0 && objectiveCache.Any(obj => obj.isCompleted == false))
            {
                ObjectivesUI.SetActive(true);

                foreach (var obj in objectiveCache)
                {
                    if (obj.objective != null)
                    {
                        if (obj.objectiveText.Count(ch => ch == '{') > 1 && obj.objectiveText.Count(ch => ch == '}') > 1)
                        {
                            obj.objective.transform.GetChild(0).GetComponent<Text>().text = string.Format(obj.objectiveText, obj.completion, obj.toComplete);
                        }
                    }
                }
            }
            else
            {
                ObjectivesUI.SetActive(false);
            }
        }
        else
        {
            ObjectivesUI.SetActive(false);
        }
    }

    void PlaySound(AudioClip audio)
    {
        if (audio != null)
        {
            soundEffects.clip = audio;
            soundEffects.volume = volume;
            soundEffects.Play();
        }
    }

    public void ShowObjectives(bool show)
    {
        objShown = show;
        ObjectivesUI.SetActive(show);
    }

    public void AddObjective(int objectiveID, float time, bool sound = true)
    {
        if (!CheckObjective(objectiveID))
        {
            ObjectiveModel objModel = objectives.SingleOrDefault(o => o.identifier == objectiveID);

            if (!objModel.isCompleted)
            {
                GameObject obj = Instantiate(ObjectivePrefab, ObjectivesUI.transform);
                obj.transform.GetChild(0).GetComponent<Text>().text = objModel.objectiveText;
                objModel.objective = obj;

                objectiveCache.Add(objModel);

                string text = objModel.objectiveText;

                if (text.Count(ch => ch == '{') > 1 && text.Count(ch => ch == '}') > 1)
                {
                    text = string.Format(text, objModel.completion, objModel.toComplete);
                }

                PushObjectiveText(text, time, isUppercased);

                if (sound) { PlaySound(newObjective); }
            }
        }
    }

    public void AddObjectives(int[] objectivesID, float time, bool sound = true)
    {
        int newObjectives = 0;
        ObjectiveModel objModel = new ObjectiveModel();

        foreach (var obj in objectivesID)
        {
            if (!CheckObjective(obj))
            {
                objModel = objectives.SingleOrDefault(o => o.identifier == obj);

                if (!objModel.isCompleted)
                {
                    GameObject objObject = Instantiate(ObjectivePrefab, ObjectivesUI.transform);
                    objObject.transform.GetChild(0).GetComponent<Text>().text = objModel.objectiveText;
                    objModel.objective = objObject;
                    objectiveCache.Add(objModel);
                    newObjectives++;
                }
            }
        }

        if (newObjectives != 0)
        {
            if (newObjectives > 1)
            {
                PushObjectiveText(multipleObjectivesText, time, isUppercased);
            }
            else
            {
                PushObjectiveText(objModel.objectiveText, time, isUppercased);
            }

            if (sound) { PlaySound(newObjective); }
        }
    }

    public void AddObjectiveModel(ObjectiveModel model)
    {
        ObjectiveModel objModel = new ObjectiveModel(model.identifier, model.toComplete, model.isCompleted);
        ObjectiveModel original = objectives.SingleOrDefault(o => o.identifier == objModel.identifier);
        objModel.objectiveText = original.objectiveText;
        objModel.toComplete = original.toComplete;
        objModel.completion = original.completion;

        if (!objModel.isCompleted)
        {
            GameObject objObject = Instantiate(ObjectivePrefab, ObjectivesUI.transform);
            objObject.transform.GetChild(0).GetComponent<Text>().text = objModel.objectiveText;
            objModel.objective = objObject;
            objectiveCache.Add(objModel);
        }
    }

    void PushObjectiveText(string text, float time, bool upper = false)
    {
        GameObject obj = Instantiate(PushObjectivePrefab, PushObjectivesUI.transform);
        obj.GetComponent<Notification>().SetMessage(text, time, upper: upper);
    }

    public void CompleteObjective(int ID, bool sound = true)
    {
        foreach (var obj in objectiveCache)
        {
            if(obj.identifier == ID)
            {
                obj.completion++;

                if(obj.completion >= obj.toComplete)
                {
                    obj.isCompleted = true;
                    Destroy(obj.objective);
                    PushObjectiveText("Objective Updated", CompleteTime);
                    if (sound) { PlaySound(completeObjective); }
                }
            }
        }
    }

    public void PreCompleteObjective(int ID)
    {
        foreach (var obj in objectives)
        {
            if (obj.identifier == ID)
            {
                obj.completion++;

                if (obj.completion >= obj.toComplete)
                {
                    obj.isCompleted = true;
                    PushObjectiveText("Objective Pre-Completed", CompleteTime);
                    PlaySound(completeObjective);
                }
            }
        }
    }

    public bool CheckObjective(int ID)
    {
        foreach (var obj in objectiveCache)
        {
            if (obj.identifier == ID)
            {
                if (obj.isCompleted)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool ContainsObjective(int ID)
    {
        foreach (var obj in objectiveCache)
        {
            if (obj.identifier == ID)
            {
                return true;
            }
        }

        return false;
    }

    public int[] ReturnNonExistObjectives(int[] Objectives)
    {
        int[] result = Objectives.Except(objectiveCache.Select(x => x.identifier).ToArray()).ToArray();
        return result;
    }
}

public class ObjectiveModel
{
    public string objectiveText;
    public int identifier;

    public int toComplete;
    public int completion;

    public GameObject objective;
    public bool isCompleted;

    public ObjectiveModel(int id, int count, string text)
    {
        identifier = id;
        toComplete = count;
        objectiveText = text;
    }

    public ObjectiveModel(int id, int count, bool completed)
    {
        identifier = id;
        toComplete = count;
        isCompleted = completed;
        objectiveText = "";
    }

    public ObjectiveModel() { }
}
