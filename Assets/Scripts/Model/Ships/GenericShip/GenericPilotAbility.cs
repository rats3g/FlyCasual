﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ship;
using SubPhases;
using UnityEngine;

namespace PilotAbilities
{
    public class GenericPilotAbility
    {
        public string Name;
        private GenericShip host;

        public GenericShip Host
        {
            get { return host; }
            private set { host = value; }
        }

        public virtual void Initialize(GenericShip host)
        {
            Host = host;
            Name = Host.PilotName + "'s ability";
        }

        // REGISTER TRIGGER

        protected void RegisterAbilityTrigger(TriggerTypes triggerType, EventHandler eventHandler)
        {
            Triggers.RegisterTrigger(new Trigger()
            {
                Name = Name,
                TriggerType = triggerType,
                TriggerOwner = Host.Owner.PlayerNo,
                EventHandler = eventHandler,
                Sender = Host
            });
        }

        // DECISION USE ABILITY YES/NO

        protected void AskToUseAbility(Func<bool> useByDefault, EventHandler useAbility, EventHandler dontUseAbility = null)
        {
            if (dontUseAbility == null) dontUseAbility = DontUseAbility;

            DecisionSubPhase pilotAbilityDecision = (DecisionSubPhase) Phases.StartTemporarySubPhaseNew(
                Name,
                typeof(PilotAbilityDecisionSubphase),
                Triggers.FinishTrigger
            );

            pilotAbilityDecision.InfoText = "Use " + Name + "?";

            pilotAbilityDecision.AddDecision("Yes", useAbility);
            pilotAbilityDecision.AddDecision("No", dontUseAbility);

            pilotAbilityDecision.DefaultDecision = (useByDefault()) ? "Yes" : "No";

            pilotAbilityDecision.Start();
        }

        private class PilotAbilityDecisionSubphase : DecisionSubPhase { }

        private void DontUseAbility(object sender, System.EventArgs e)
        {
            DecisionSubPhase.ConfirmDecision();
        }

        // SELECT SHIP AS TARGET OF ABILITY

        protected GenericShip TargetShip;

        protected void SelectTargetForAbility(System.Action selectTargetAction, List<TargetTypes> targetTypes, Vector2 rangeLimits, bool showSkipButton = false)
        {
            SelectShipSubPhase selectTargetSubPhase = (SelectShipSubPhase) Phases.StartTemporarySubPhaseNew(
                "Select target for Lando Calrissian's ability",
                typeof(PilotAbilitySelectTarget),
                Triggers.FinishTrigger
            );

            selectTargetSubPhase.PrepareByParameters(
                delegate { SelectShipForAbility(selectTargetAction); },
                targetTypes,
                rangeLimits,
                showSkipButton
            );

            selectTargetSubPhase.Start();
        }

        private void SelectShipForAbility(System.Action selectTargetAction)
        {
            MovementTemplates.ReturnRangeRuler();

            TargetShip = (Phases.CurrentSubPhase as SelectShipSubPhase).TargetShip;
            selectTargetAction();
        }

        private class PilotAbilitySelectTarget: SelectShipSubPhase
        {
            protected override void RevertSubPhase() { }

            public override void SkipButton()
            {
                Phases.FinishSubPhase(this.GetType());
                Triggers.FinishTrigger();
            }
        }
    }
}
