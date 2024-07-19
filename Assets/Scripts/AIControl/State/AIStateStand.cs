using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateStand : AIStateBase
{
    protected override void StateMethod()
    {
        UnitLink.SetStandFPA();
    }
}
