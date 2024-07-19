using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateFollow : AIStateBase
{
    protected override void StateMethod()
    {
        UnitLink.SetFollowFPA(AINavGrid.main.Observer);
    }
}
