﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Upgrade;
using UnityEngine;
using Ship;

namespace Bombs
{

    public class BombDetonationEventArgs : EventArgs
    {
        public GenericShip DetonatedShip;
        public GameObject BombObject;
    }

    public enum BombDropTemplates
    {
        Straight1,
        Straight2,
        Straight3,
        Turn1Left,
        Turn1Right,
        Turn3Left,
        Turn3Right
    }

    public static class BombsManager
    {
        public static GenericBomb CurrentBomb { get; set; }
        public static GameObject CurrentBombObject { get; set; }

        private static List<Vector3> generatedBombPoints = new List<Vector3>();
        private static Dictionary<GameObject, GenericBomb> minesList;

        public delegate void EventHandlerBomb(GenericBomb bomb, GameObject model);
        public static event EventHandlerBomb OnBombIsRemoved;

        public static void Initialize()
        {
            minesList = new Dictionary<GameObject, GenericBomb>();
            CurrentBomb = null;
        }

        public static List<Vector3> GetBombPoints()
        {
            if (generatedBombPoints.Count == 0)
            {
                int precision = 10;
                for (int i = 0; i <= precision; i++)
                {
                    generatedBombPoints.Add(new Vector3(-1.6f + (3.2f / precision) * precision, 0, 0.05f));
                    generatedBombPoints.Add(new Vector3(1.6f, 0, 0.05f + (3 / precision) * precision));
                    generatedBombPoints.Add(new Vector3(-1.6f, 0, 0.05f + (3 / precision) * precision));
                    generatedBombPoints.Add(new Vector3(-1.6f + (3.2f / precision) * precision, 0, 3.05f));
                }
            }

            return generatedBombPoints;
        }

        public static void RegisterMines(List<GameObject> mineObjects, GenericBomb bombUpgrade)
        {
            foreach (var mineObject in mineObjects)
            {
                minesList.Add(mineObject, bombUpgrade);
            }
        }

        public static void UnregisterMine(GameObject mineObject)
        {
            minesList.Remove(mineObject);
        }

        public static GenericBomb GetMineByObject(GameObject mineObject)
        {
            return minesList[mineObject];
        }

        public static List<GenericShip> GetShipsInRange(GameObject bombObject)
        {
            List<GenericShip> result = new List<GenericShip>();

            foreach (var ship in Roster.AllShips.Select(n => n.Value))
            {
                if (!ship.IsDestroyed)
                {
                    if (IsShipInDetonationRange(ship, bombObject))
                    {
                        result.Add(ship);
                    }
                }
            }

            return result;
        }

        private static bool IsShipInDetonationRange(GenericShip ship, GameObject bombObject)
        {
            List<Vector3> bombPoints = GetBombPoints();

            foreach (var localBombPoint in bombPoints)
            {
                Vector3 globalBombPoint = bombObject.transform.TransformPoint(localBombPoint);
                foreach (var globalShipBasePoint in ship.ShipBase.GetStandPoints().Select(n => n.Value))
                {
                    if (Board.BoardManager.GetRangeBetweenPoints(globalBombPoint, globalShipBasePoint) == 1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void ResolveDetonationTriggers(GameObject bombObject)
        {
            CurrentBombObject = bombObject;
            Triggers.ResolveTriggers(TriggerTypes.OnBombIsDetonated, ResolveRemoveModelTriggers);
        }

        private static void ResolveRemoveModelTriggers()
        {
            if (OnBombIsRemoved != null) OnBombIsRemoved(CurrentBomb, CurrentBombObject);

            Triggers.ResolveTriggers(TriggerTypes.OnBombIsRemoved, RemoveModel);
        }

        private static void RemoveModel()
        {
            GameObject.Destroy(CurrentBombObject);
            Triggers.FinishTrigger();
        }
    }
}



