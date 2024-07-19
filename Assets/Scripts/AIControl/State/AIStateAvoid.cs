using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateAvoid : AIStateBase
{
    protected override void StateMethod()
    { 
        UnitLink.SetAvoidFPA();
    }

}
