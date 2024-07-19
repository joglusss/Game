using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateSneak : AIStateBase
{
    protected override void StateMethod()
    {
        UnitLink.SetSneakFPA(AINavGrid.main.Observer);
    }
}
