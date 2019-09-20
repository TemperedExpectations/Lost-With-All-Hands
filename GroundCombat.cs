using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCombat : AbstractBehavior {

    public float combatDistance;
    public float evadeTime;
    public float evadeDistance;

    Transform attackTarget;
    float underFireTimer;
    Vector3 evadePoint;

    public GroundCombat(GroundBehaviorData data, Transform transform) {
        this.data = data;
        this.transform = transform;
        combatDistance = data.f1;
        evadeTime = data.f2;
        evadeDistance = data.f3;
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
        if (underFireTimer > 0) {
            Debug.DrawLine(transform.position, evadePoint, Color.red);
            return evadePoint;
        }
        else {
            Vector3 lookDir = (attackTarget.position - transform.position).normalized;
            Vector3 h = (-new Vector3(lookDir.x, 0, lookDir.z)).normalized * combatDistance;
            Debug.DrawLine(transform.position, attackTarget.position + h, Color.red);
            return attackTarget.position + h;
        }
    }

    public override Vector3 GetLook() {
        Vector3 dir = attackTarget.position - transform.position;
        return dir;// - Vector3.up * dir.y * .35f;
    }

    public override void Alert(Vector3 direction, bool origin, Vector3 point, AbstractAI alerter) {
        if (underFireTimer <= 0) {
            underFireTimer = evadeTime;
            Vector3 moveDirection = Quaternion.LookRotation(Vector3.right, Vector3.up) * direction;
            evadePoint = transform.position + moveDirection * evadeDistance * (Random.Range(0f, 1f) < .5f ? -1 : 1);
        }
    }

    public override void SetAttackTarget(Transform target) {
        attackTarget = target;
    }
}
