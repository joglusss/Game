using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateBase : StateMachineBehaviour
{
    protected AIUnitBase UnitLink;

    [SerializeField] protected bool _pauseUpdatePath;
    [Space]
    [SerializeField] protected float _speed;
    

    public override sealed void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (UnitLink == null)
        {
            UnitLink = animator.GetComponent<AIUnitBase>();
        }
        animator.SetFloat("Time", 0.0f);

        StateMethod();

        UnitLink._currentFPAUpdateData.speed = _speed;
        UnitLink._currentFPAUpdateData.pauseUpdate = _pauseUpdatePath;
    }

    protected virtual void StateMethod()
    { 
    
    }

}
