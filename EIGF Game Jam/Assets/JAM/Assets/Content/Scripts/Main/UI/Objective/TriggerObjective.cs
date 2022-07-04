﻿/*
 * TriggerObjective.cs - written by ThunderWire Studio
 * version 1.0
*/

using UnityEngine;

public class TriggerObjective : MonoBehaviour, IItemEvent {

    private ObjectiveManager objectiveManager;

    public enum TriggerType { NewObjective, Complete, CompleteAndNew }
    public TriggerType triggerType = TriggerType.NewObjective;

    public int objective;
    public int[] objectivesID;
    public float showTime;

    [Tooltip("Allow Objective Pre-Completion")]
    public bool preComplete;
    [Tooltip("Add new objective only when parent objective is active.")]
    public bool newWhenContains;

    [HideInInspector, SaveableField]
    public bool isTriggered;

    private bool isInteractive = false;

    void Awake () {
        objectiveManager = ObjectiveManager.Instance;
    }

    void Start()
    {
        if (GetComponent<InteractiveItem>())
        {
            isInteractive = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTriggered && !isInteractive)
        {
            ObjectiveTrigger();
        }
    }

    public void UseObject()
    {
        if (isTriggered || isInteractive) return;

        ObjectiveTrigger();
    }

    public void DoEvent()
    {
        ObjectiveTrigger();
    }

    public void ObjectiveTrigger()
    {
        if (triggerType == TriggerType.NewObjective)
        {
            if (objectivesID.Length > 1)
            {
                int[] result = objectiveManager.ReturnNonExistObjectives(objectivesID);

                if (result.Length > 1)
                {
                    objectiveManager.AddObjectives(result, showTime);
                }
                else if (result.Length == 1)
                {
                    objectiveManager.AddObjective(result[0], showTime);
                }
            }
            else
            {
                if (!objectiveManager.ContainsObjective(objectivesID[0]))
                {
                    objectiveManager.AddObjective(objectivesID[0], showTime);
                }
            }

            isTriggered = true;
        }
        else if (triggerType == TriggerType.Complete)
        {
            if (objectiveManager.ContainsObjective(objective))
            {
                objectiveManager.CompleteObjective(objective);
                isTriggered = true;
            }
            else if (preComplete)
            {
                objectiveManager.PreCompleteObjective(objective);
                isTriggered = true;
            }
        }
        else if (triggerType == TriggerType.CompleteAndNew)
        {
            bool contains = newWhenContains ? false : true;

            if (objectiveManager.ContainsObjective(objective))
            {
                objectiveManager.CompleteObjective(objective);
                contains = true;
                isTriggered = true;
            }
            else if (preComplete)
            {
                objectiveManager.PreCompleteObjective(objective);
                contains = true;
                isTriggered = true;
            }

            if (contains)
            {
                if (objectivesID.Length > 1)
                {
                    objectiveManager.AddObjectives(objectivesID, showTime, false);
                }
                else
                {
                    objectiveManager.AddObjective(objectivesID[0], showTime, false);
                }
            }
        }
    }
}
