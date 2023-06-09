using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class AIController : MonoBehaviour
{
    public AIStats _stats;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private Transform workplace; //Workplace coords
    [SerializeField] private Animator personAnimator;
    public Transform exit;
    public bool dogNearby = false; //Set Bool for dog nearby
    public bool personNearby = false; //Set Bool for person nearby
    public bool hasWorkToDo = false; //Set Bool for at workplace
    public bool fireAlarm = false; //Set Bool for fire alarm
    public bool turnAlarmOff = false; //Set Bool for fire alarm to turn off
    public bool peeFound = false; //Set Bool for calling janitor if pee is found
    public bool gotFired = false;
    public bool bossMad = false;
    public bool destroyed = false;
    public Collider peeObj;
    private ReffBool canPet = new ReffBool(true);
    private ReffBool isTalking = new ReffBool(false);
    private ReffBool isWorking = new ReffBool(false);
    private ScoreManager scoreManager;
    private QuestBus eventSys;
    public int idelCount = 0;
    private int scoreInc = 1;

    private int currentWaypoint = 0;
    private StateMachine _stateMachine;
#nullable enable
    public Transform? Target { get; private set; }
    public Transform? Work { get; private set; }
    private NavMeshAgent? navMeshAgent;

    public AIStats AIStats => _stats;
    public class ReffBool
    {
        public bool value;
        public ReffBool(bool defult)
        {
            value = defult;
        }
    }

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        scoreManager = FindObjectOfType<ScoreManager>();
        _stateMachine = new StateMachine();
        eventSys = LevelStatic.currentLevel.questBus;
        eventSys.subscribe(HandleEvents);

        var walkingState = new WalkingState(this, navMeshAgent);
        var idleState = new IdleState(this);
        var pettingState = new PettingState(this); //Setting up PettingState
        var talkingState = new TalkingState(this); //Setting up TalkingState
        var workingState = new WorkingState(this); //Setting up WorkingState
        var evacuationState = new EvacuationState(this, navMeshAgent); //Setting up the EvacuationState
        //var calljanitorState = new CallJanitorState(this, navMeshAgent);
        var cleaningState = new CleaningState(this);
        var firedState = new FiredState(this, navMeshAgent);

        At(idleState, walkingState, HasTarget());
        At(walkingState, idleState, ReachedDestination());
        At(pettingState, walkingState, HasTarget()); //For now only allow to transition from petting to walking
        At(talkingState, walkingState, HasTarget()); //For now only allow to transition from talking to walking
        At(workingState, walkingState, HasTarget());
        At(cleaningState, walkingState, HasTarget());
        //At(workingState, evacuationState, AlarmOn());
        //At(calljanitorState, walkingState, HasTarget());
        At(evacuationState, walkingState, AlarmOff()); //Adding way to exit evacuation state that does not trigger everytime
        Aat(pettingState, DogNear()); //Adding petting state as an any
        Aat(talkingState, PersonNear()); //Adding talking state as an any (Bump into them at work)
        Aat(workingState, HasWork()); //Adding petting state as an any
        Aat(evacuationState, AlarmOn()); //Adding evcuation state as an any
        //Aat(calljanitorState, FoundPeeEmp());
        Aat(cleaningState, FoundPeeJan());
        Aat(firedState, Fired());




        _stateMachine.SetState(idleState);
    }
    void At(IState to, IState from, Func<bool> condition) => _stateMachine.AddTransition(to, from, condition);
    void Aat(IState to, Func<bool> condition) => _stateMachine.AddAnyTransition(to, condition); //Shorthand for AddAnyTransition
    

    Func<bool> HasTarget() => () => Target != null;
    Func<bool> HasWork() => () => fireAlarm != true && isWorking.value == true;
    Func<bool> AlarmOn() => () => fireAlarm == true;
    Func<bool> AlarmOff() => () => fireAlarm == false;
    Func<bool> DogNear() => () => fireAlarm != true && dogNearby == true; //Is dog near?
    Func<bool> PersonNear() => () => fireAlarm != true && personNearby == true; //Is there a person near?
    Func<bool> ReachedDestination() => () => Target != null && Vector3.Distance(transform.position, Target.position) < 1f;
    //Func<bool> FoundPeeEmp() => () => peeFound == true && !_stats.IsJanitor == true;
    Func<bool> FoundPeeJan() => () => peeFound == true && _stats.IsJanitor == true;
    Func<bool> Fired() => () => _stats.IsJanitor == true && gotFired == true;



    // Here is where the code is actually ran! Update function calls State Machine's Tick() every frame.
    //State Machine's Tick() checks if it needs to change states based on the transitions set above then calls tick() on current state.
    private void Update()
    {
        _stateMachine.Tick();
        if (idelCount == 5)
        {
            hasWorkToDo = true;
            idelCount = 0;
        }
        if (fireAlarm || gotFired)
        {
            dogNearby = false;
            personNearby = false;
            hasWorkToDo = false;
            peeFound = false;
            idelCount = 0;
        }
    }
    public void CallDestroy(GameObject obj)
    {
        Debug.Log("Destroyed was Called on: " + obj);
        Destroy(obj);
    }

    IEnumerator Cooldown(float waitTime, ReffBool boolToChange)
    {
        yield return new WaitForSecondsRealtime(waitTime);
        boolFlip(boolToChange);
    }
    private void boolFlip(ReffBool changeBool) 
    {
        changeBool.value = !changeBool.value;
    }

    public void SetTarget(Transform t)
    {
        Target = t;
        
    }

    public void GetNewTarget()
    {
        
        if (currentWaypoint >= waypoints.Length -1)
            currentWaypoint = 0;
        else
            currentWaypoint++;

        Debug.Log("getting " + currentWaypoint);

        if (hasWorkToDo)
        {
            SetTarget(workplace);
        }
        else
        {
            SetTarget(waypoints[currentWaypoint]);
        }


    }
    private void HandleEvents(QuestObject q)
    {
        switch (q.eventEnum)
        {
            case LevelData.publicEvents.FIREALARM:
                {
                    fireAlarm = !fireAlarm;
                }
                break;
        }
    }
    bool ComparePlayerTag(string tag) { 
        return tag == "small" || tag == "big" || tag == "medium" ;
    }
    public void OnTriggerEnter(Collider other)
    {
        if (fireAlarm && other.CompareTag("Exit"))
        {
            AnimationStop();
        }
        if (_stats.IsJanitor && gotFired && other.CompareTag("Exit"))
        {
            Destroy(this.gameObject);
        }
        Debug.LogWarning(canPet.value);
        Debug.LogWarning(other.gameObject.tag);
        if (ComparePlayerTag(other.gameObject.tag) && !fireAlarm)
        {
            GameObject bone = other.GetComponent<PickUpSystem>().GetItem();
            if(bone.CompareTag("Special") && _stats.IsBoss)
            {
                bossMad = true;
                Destroy(bone);
            }
            
            if (canPet.value)
            {
                if (bone != null && !bone.CompareTag("Special"))
                {
                    scoreInc = 10;
                    Destroy(bone);
                    SpawnBones.Instance.SpawnNewBone();
                    AudioManager.Instance.PlaySFX(AudioNames.ScoreUp, transform.position);
                }
                else
                {
                    AudioManager.Instance.PlaySFX(AudioNames.hover, transform.position);
                }
                canPet.value = false;
                dogNearby = true;

                StartCoroutine(Cooldown(_stats.PettingCooldown, canPet));

            }
        }
        if(!isTalking.value && other.CompareTag("AI") && !other.gameObject.GetComponent<AIController>().isTalking.value && !fireAlarm && !hasWorkToDo && !other.gameObject.GetComponent<AIController>().hasWorkToDo)
        {
            isTalking.value = true;
            personNearby = true;
            StartCoroutine(Cooldown(_stats.TalkingCooldown, isTalking));
        }
        if(!isWorking.value && other.CompareTag("Workplace") && !fireAlarm)
        {
            isWorking.value = true;
            hasWorkToDo = false;
            StartCoroutine(Cooldown(_stats.WorkingCooldown, isWorking));
        }
        if (other.CompareTag("Pee"))
        {
            peeFound = true;
            peeObj = other;
        }
        if (_stats.IsJanitor == true && other.gameObject.GetComponent<AIController>()._stats.IsBoss && other.gameObject.GetComponent<AIController>().bossMad == true)
        {
            gotFired = true;
        }
    }

    public void AnimationStart(float _sp)
    {
        scoreManager.IncrementScore(scoreInc);
        scoreInc = 1;
        personAnimator.SetFloat("Speed", _sp);
        personAnimator.SetFloat("Forward", -0.5f);
    }

    public void AnimationStop()
    {
        personAnimator.SetFloat("Speed", 1);
        personAnimator.SetFloat("Forward", 0f);
    }

    public void AnimationWalking()
    {
        personAnimator.SetFloat("Forward", 0.5f);
    }
}
