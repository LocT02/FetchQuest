using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CleaningState : IState
{
    private readonly AIController _controller;
    private float waitTime;
    private Collider pee;
    public CleaningState(AIController c)
    {
        _controller = c;
    }

    public void Tick()
    {
        if (waitTime <= Time.time)
        {
            _controller.peeFound = false;
            _controller.CallDestroy(_controller.peeObj.gameObject);//pee.gameObject.  destroy(); //pee is a Transform and not a gameObject, will fix later
            _controller.GetNewTarget();
        }
    }

    public void OnEnter()
    {
        Debug.Log("Janitor Cleaning Entered");
        waitTime = Time.time + _controller.AIStats.RestTime * 2;
    }

    public void OnExit()
    {
    }
}
