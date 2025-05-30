using UnityEngine;
using System.Collections.Generic;

namespace RPG.Events
{
    public abstract class GameEvent
    {
        public float timestamp;
        
        protected GameEvent()
        {
            timestamp = Time.time;
        }
    }

    public class BossDefeatedEvent : GameEvent
    {
        public string bossName;
        public int bossLevel;
        public Vector3 defeatPosition;
        public float experienceReward;
        public List<Item> droppedItems;
    }

    public class AreaExploredEvent : GameEvent
    {
        public string areaName;
        public Vector3 explorationPosition;
        public float explorationPercentage;
        public bool isFirstTime;
    }
} 