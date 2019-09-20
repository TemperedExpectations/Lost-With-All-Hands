using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundPatrol : AbstractBehavior {

    Vector3 moveTarget;
    float moveWaitTimer;
    bool hasMoveTarget;

    public GroundPatrol(AbstractBehaviorData data, Transform transform) {
        this.data = data;
        this.transform = transform;
    }

    public override bool Step() {
        if (hasMoveTarget) {
            moveWaitTimer = Mathf.Clamp(moveWaitTimer - Time.deltaTime, 0, moveWaitTimer);
            if (moveWaitTimer == 0) {
                hasMoveTarget = false;
            }
        }
        else {
            Vector2 randomCircle = Random.insideUnitCircle * data.moveRadius;
            Vector3 randomSphere = new Vector3(randomCircle.x, 0, randomCircle.y);
            Vector3 nextMove = randomSphere + transform.position;
            Debug.DrawLine(transform.position, nextMove, Color.cyan);
            moveTarget = nextMove;
            moveWaitTimer = Random.Range(data.moveWaitTime.x, data.moveWaitTime.y);
            hasMoveTarget = true;
        }
        return true;
    }

    public override Transform Search(ManageFactions.Faction faction, Vector3 head) {
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, data.searchRadius, transform.forward, 0);
        foreach (RaycastHit hit in hits) {
            Health targetHealth = hit.transform.GetComponent<Health>();
            RaycastHit confirmHit;
            if (targetHealth != null && targetHealth.faction != faction && Mathf.Acos(Vector3.Dot(transform.forward, (hit.transform.position - transform.position).normalized)) * Mathf.Rad2Deg < data.searchCone) {
                Debug.DrawRay(head, hit.transform.position - transform.position);
                if (Physics.Raycast(head, hit.transform.position - transform.position, out confirmHit, 100, ~ManageFactions.instance.deployableShield, QueryTriggerInteraction.Ignore)) {
                    if (confirmHit.transform == hit.transform) {
                        return hit.transform;
                    }
                }
            }
        }
        return null;
    }

    public override Vector3 GetLook() {
        return moveTarget - transform.position;
    }

    public override Vector3 GetMove() {
        Debug.DrawLine(transform.position, moveTarget, data.behavior == BehaviorType.combat ? Color.red / 2 + Color.yellow / 2 : data.behavior == BehaviorType.alert ? Color.yellow : Color.green);
        return moveTarget;
    }

    public override void Alert(Vector3 direction, bool origin, Vector3 point, AbstractAI alerter) {
        throw new System.NotImplementedException();
    }

    public override void SetAttackTarget(Transform target) {
        throw new System.NotImplementedException();
    }
}
