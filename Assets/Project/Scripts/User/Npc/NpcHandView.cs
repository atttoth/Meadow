using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcHandView : HandView
{
    public override void AddCardHandler(GameTask task, Card card)
    {
        task.Complete();
    }
}
