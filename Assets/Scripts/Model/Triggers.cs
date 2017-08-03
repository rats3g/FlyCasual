﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SubPhases;

public enum TriggerTypes
{
    None,
    OnSetupPhaseStart,
    OnShipMovementExecuted,
    OnShipMovementFinish,
    OnPositionFinish,
    OnActionSubPhaseStart,
    OnCombatPhaseStart,
    OnFaceupCritCardReadyToBeDealt,
    OnDamageCardIsDealt,
    OnCritDamageCardIsDealt
}

public class Trigger
{
    public string Name;
    public Players.PlayerNo TriggerOwner;
    public TriggerTypes triggerType;
    public EventHandler eventHandler;
    public object sender;
    public EventArgs eventArgs;

    public StackLevel parentStackLevel;

    public void Fire()
    {
        eventHandler(sender, eventArgs);
    }
}

public class StackLevel
{
    private List<Trigger> triggers = new List<Trigger>();
    public bool IsActive;
    public Action CallBack;

    public int GetSize()
    {
        return triggers.Count;
    }

    public bool Empty()
    {
        return GetSize() == 0;
    }

    public Trigger GetFirst()
    {
        return triggers[0];
    }

    public void AddTrigger(Trigger trigger)
    {
        triggers.Add(trigger);
    }

    public void RemoveTrigger(Trigger trigger)
    {
        triggers.Remove(trigger);
    }

    public List<Trigger> GetTriggersByPlayer(Players.PlayerNo playerNo)
    {
        return triggers.Where(n => n.TriggerOwner == playerNo).ToList<Trigger>();
    }

}

public static partial class Triggers
{

    private static List<StackLevel> triggersStackList = new List<StackLevel>();
    private static Trigger currentTrigger;
    private static Players.PlayerNo currentPlayer;
    private static List<Trigger> currentTriggersList;

    // PUBLIC

    public static void RegisterTrigger(Trigger trigger)
    {
        if (triggersStackList.Count == 0)
        {
            CreateTriggerInNewLevel(trigger);
        }
        else
        {
            AddTriggerToCurrentStackLevel(trigger);
        }
        if (DebugManager.DebugTriggers) Debug.Log("Trigger is registered. Level of stack is " + triggersStackList.Count);
    }

    public static void ResolveTriggersByType(TriggerTypes triggerType, Action callBack = null)
    {
        if (DebugManager.DebugTriggers) Debug.Log("Resolve triggers by type: " + triggerType.ToString());

        StackLevel currentStackLevel = GetCurrentStackLevel();

        if ((currentStackLevel != null) && (!currentStackLevel.Empty()))
        {
            SetStackLevelCallBack(callBack);
            GetTriggerListAndPlayer();

            CreateNewLevelOfStack();
            
            ResolveTriggersByTypeAndPlayer(currentStackLevel, triggerType);
        }
        else
        {
            if (currentStackLevel == null)
            {
                CreateNewLevelOfStack(callBack);
            }
            DoCallBack();
        }
    }

    public static void FinishTrigger()
    {
        if (DebugManager.DebugTriggers) Debug.Log("Finish Trigger");
        StackLevel currentStackLevel = GetCurrentStackLevel();
        //currentStackLevel.RemoveTrigger(currentTrigger);
        if (currentStackLevel.Empty())
        {
            currentTrigger.parentStackLevel.IsActive = false;
            DoCallBack();
        }
        else
        {
            RunDecisionSubPhase();
        }
    }

    // PRIVATE

    private static void GetTriggerListAndPlayer()
    {
        StackLevel currentStackLevel = GetCurrentStackLevel();

        currentTriggersList = currentStackLevel.GetTriggersByPlayer(Phases.PlayerWithInitiative);
        currentPlayer = (currentTriggersList.Count > 0) ? Phases.PlayerWithInitiative : Roster.AnotherPlayer(Phases.PlayerWithInitiative);
        currentTriggersList = currentStackLevel.GetTriggersByPlayer(currentPlayer);
    }

    private static void SetStackLevelCallBack(Action callBack)
    {
        if (callBack != null)
        {
            StackLevel currentStackLevel = GetCurrentStackLevel();
            currentStackLevel.CallBack = callBack;
        }
    }

    private static void ResolveTriggersByTypeAndPlayer(StackLevel currentStackLevel, TriggerTypes triggerType)
    {
        if (currentTriggersList.Count != 0)
        {
            if (currentTriggersList.Count == 1)
            {
                ResolveTrigger(currentTriggersList[0]);
            }
            else
            {
                RunDecisionSubPhase();
            }
        }
    }

    private static void ResolveTrigger(Trigger trigger)
    {
        currentTrigger = trigger;
        currentTrigger.parentStackLevel.IsActive = true;
        trigger.parentStackLevel.RemoveTrigger(currentTrigger);
        currentTrigger.Fire();
    }

    private static void RunDecisionSubPhase()
    {
        Phases.StartTemporarySubPhase("Triggers Order", typeof(TriggersOrderSubPhase));
    }

    private static void DoCallBack()
    {
        StackLevel currentStackLevel = GetCurrentStackLevel();
        if (!currentStackLevel.IsActive)
        {
            Action callBack = currentStackLevel.CallBack;
            RemoveLastLevelOfStack();
            callBack();
        }
    }

    private static void RemoveLastLevelOfStack()
    {
        triggersStackList.Remove(triggersStackList[triggersStackList.Count - 1]);
    }

    private static StackLevel GetCurrentStackLevel()
    {
        StackLevel result = null;
        if (triggersStackList.Count > 0)
        {
            result = triggersStackList[triggersStackList.Count - 1];
        }
        return result;
    }

    private static TriggerTypes GetCurrentStackLevelTriggerType()
    {
        return triggersStackList[triggersStackList.Count - 1].GetFirst().triggerType;
    }

    private static void CreateTriggerInNewLevel(Trigger trigger)
    {
        CreateNewLevelOfStack();
        AddTriggerToCurrentStackLevel(trigger);
    }

    private static void AddTriggerToCurrentStackLevel(Trigger trigger)
    {
        if (DebugManager.DebugTriggers) Debug.Log("Trigger is added: " + trigger.Name);
        triggersStackList[triggersStackList.Count - 1].AddTrigger(trigger);
        trigger.parentStackLevel = GetCurrentStackLevel();
    }

    private static void CreateNewLevelOfStack(Action callBack = null)
    {
        if (DebugManager.DebugTriggers) Debug.Log("New level of stack created: " + (triggersStackList.Count + 1));
        triggersStackList.Add(new StackLevel());
        GetCurrentStackLevel().CallBack = (callBack != null) ? callBack : (Action)delegate () { ResolveTriggersByType(TriggerTypes.None); };
    }

    // SUBPHASE

    private class TriggersOrderSubPhase : DecisionSubPhase
    {

        public override void Prepare()
        {
            infoText = "Select a trigger to resolve";

            foreach (var trigger in currentTriggersList)
            {
                if (trigger.TriggerOwner == currentPlayer)
                {
                    AddDecision(trigger.Name, delegate {
                        Phases.FinishSubPhase(this.GetType());
                        ResolveTrigger(trigger);
                    });
                }
            }

            defaultDecision = GetDecisions().First().Key;
        }

    }

}
