using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCombat : AbstractBehavior {

    public float combatDistance;
    public float evadeTime;
    public float evadeDistance;

    Transform attackTarget;
    float combatHeight;
    float underFireTimer;
    Vector3 evadeDirection;

    public SimpleCombat(SimpleBehaviorData data, Transform transform) {
        this.data = data;
        this.transform = transform;
        combatDistance = data.f4;
        evadeTime = data.f5;
        evadeDistance = data.f6;
    }

    public override bool Step() {
        if (underFireTimer > 0) underFireTimer -= Time.deltaTime;
        return true;
    }

    public override Transform Search(ManageFactions.Faction faction, Vector3 head) {
        RaycastHit hit;
        Debug.DrawRay(head, attackTarget.position - transform.position);
        if (Physics.Raycast(head, attackTarget.position - transform.position, out hit, 100, ~ManageFactions.instance.deployableShield, QueryTriggerInteraction.Ignore)) {
            if (hit.transform == attackTarget) {
                return attackTarget;
            }
        }
        return null;
    }

    public override Vector3 GetMove() {
        Vector3 lookDir = (attackTarget.position - transform.position).normalized;
        Vector3 h = (-(new Vector3(lookDir.x, 0, lookDir.z)).normalized * combatDistance + Vector3.up * combatHeight).normalized * combatDistance;
        Vector3 evade = underFireTimer > 0 ? evadeDirection : Vector3.zero;
        Debug.DrawLine(transform.position, attackTarget.position + h + evade, Color.red);
        return attackTarget.position + h + evade;
    }

    public override Vector3 GetLook() {
        return attackTarget.position - transform.position;
    }

    public override void Alert(Vector3 direction, bool origin, Vector3 point, AbstractAI alerter) {
        if (underFireTimer <= 0) {
            underFireTimer = evadeTime;
            evadeDirection = transform.up * data.hoverDelta * (Random.Range(0f, 1f) < .5f ? -1 : 1) + transform.right * evadeDistance * (Random.Range(0f, 1f) < .5f ? -1 : 1);
        }
    }

    public override void SetAttackTarget(Transform target) {
        attackTarget = target;
        combatHeight = Random.Range(data.hoverHeight.x, data.hoverHeight.y);
    }
}
