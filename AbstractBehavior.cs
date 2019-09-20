using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractBehavior {

    public AbstractBehaviorData data;
    protected Transform transform;

    public abstract bool Step();
    public abstract Transform Search(ManageFactions.Faction faction, Vector3 head);
    public abstract Vector3 GetMove();
    public abstract Vector3 GetLook();
    public abstract void Alert(Vector3 direction, bool origin, Vector3 point, AbstractAI alerter);
    public abstract void SetAttackTarget(Transform target);
}
