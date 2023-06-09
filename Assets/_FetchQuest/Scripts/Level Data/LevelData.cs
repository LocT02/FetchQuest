using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Daichi M

// manager/container to dynamically find level components
// for ocd/management reasons

public class LevelData : MonoBehaviour
{
    // the things you want
    public QuestBus questBus;
    public QuestManager questManager;

    public enum publicEvents {NOEVENT, QUESTSTARTED, QUESTFINISHED, FIREALARM};

    // on (level) load, puts itself in the level static
    private void Awake()
    {
        LevelStatic.currentLevel = this;
        print("INJECTED LEVEL DATA INTO STATIC");
    }
}
