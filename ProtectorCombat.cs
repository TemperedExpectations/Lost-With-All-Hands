using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProtectorCombat : AbstractBehavior {

    public float combatDistance;
    public float evadeTime;
    public float evadeDistance;
    public float protectDistance;
    public GameObject shield;
    public GameObject indicator;
    public float deployDistance;
    public float deployRechargeTime;

    Transform attackTarget;
    Transform protectTarget;
    float combatHeight;
    //bool hasView;
    float underFireTimer;
    Vector3 evadeDirection;
    float protectStartDistance;
    Transform deployedShield;
    LineRenderer deployedIndicator;
    float rechargeTimer;

    public ProtectorCombat(ProtectorBehaviorData data, Transform transform) {
        this.data = data;
        this.transform = transform;
        combatDistance = data.f1;
        evadeTime = data.f2;
        evadeDistance = data.f3;
        protectDistance = data.f4;
        deployDistance = data.f5;
        deployRechargeTime = data.f6;
        shield = data.o1;
        indicator = data.o2;
    }

    public override bool Step() {
        if (underFireTimer > 0) underFireTimer -= Time.deltaTime;
        if (rechargeTimer > 0) rechargeTimer -= Time.deltaTime;
        if (deployedShield != null) {
            if (protectTarget == null || protectTarget.GetComponent<Health>().IsDead()) {
                GameObject.Destroy(deployedShield.gameObject);
                GameObject.Destroy(deployedIndicator.gameObject);
                deployedShield = null;
                protectTarget = null;
                return false;
            }
            deployedShield.position = protectTarget.position + protectTarget.forward * deployDistance;
            deployedShield.rotation = protectTarget.rotation;
            deployedIndicator.SetPosition(0, transform.position);
            deployedIndicator.SetPosition(1, protectTarget.position);
        }
        else if (protectTarget != null && deployedShield == null) {
            protectTarget = null;
            rechargeTimer = deployRechargeTime;
            GameObject.Destroy(deployedIndicator.gameObject);
            return false;
        }
        return true;
    }

    public override Transform Search(ManageFactions.Faction faction, Vector3 head) {
        RaycastHit hit;
        if (attackTarget != null) {
            Debug.DrawRay(head, attackTarget.position - transform.position);
            if (Physics.Raycast(head, attackTarget.position - transform.position, out hit, 100, ~ManageFactions.instance.deployableShield, QueryTriggerInteraction.Ignore)) {
                if (hit.transform == attackTarget) {
                    //hasView = true;
                    return attackTarget;
                }
            }
            //hasView = false;
            return null;
        }
        if (protectTarget != null) {
            Debug.DrawRay(head, protectTarget.position - transform.position);
            if (Physics.Raycast(head, protectTarget.position - transform.position, out hit, protectDistance, ~ManageFactions.instance.deployableShield, QueryTriggerInteraction.Ignore)) {
                if (hit.transform == protectTarget) {
                    if (hit.transform.GetComponent<Health>().IsDead()) {
                        protectTarget = null;
                        //hasView = false;
                        return null;
                    }
                    //hasView = true;
                    return protectTarget;
                }
            }
            //hasView = false;
            return protectTarget;
        }
        return null;
    }

    public override Vector3 GetMove() {
        if (attackTarget != null) {
            Vector3 lookDir = (attackTarget.position - transform.position).normalized;
            Vector3 h = (-(new Vector3(lookDir.x, 0, lookDir.z)).normalized * combatDistance + Vector3.up * combatHeight).normalized * combatDistance;
            Vector3 evade = underFireTimer > 0 ? evadeDirection : Vector3.zero;
            Debug.DrawLine(transform.position, attackTarget.position + h + evade, Color.red);
            return attackTarget.position + h + evade;
        }
        else if (protectTarget != null) {
            Vector3 lookDir = (protectTarget.position - transform.position).normalized;
            Vector3 h = -(new Vector3(lookDir.x, 0, lookDir.z)).normalized * protectStartDistance;
            Debug.DrawLine(transform.position, protectTarget.position + h, Color.blue);
            return protectTarget.position + h;
        }
        else {
            return transform.position;
        }
    }

    public override Vector3 GetLook() {
        if (attackTarget != null) {
            return attackTarget.position - transform.position;
        }
        if (protectTarget != null) {
            return protectTarget.position - transform.position;
        }
        return transform.position;
    }

    public override void Alert(Vector3 direction, bool origin, Vector3 point, AbstractAI alerter) {
        if (origin) {
            if (underFireTimer <= 0) {
                underFireTimer = evadeTime;
                evadeDirection = transform.up * data.hoverDelta * (Random.Range(0f, 1f) < .5f ? -1 : 1) + transform.right * evadeDistance * (Random.Range(0f, 1f) < .5f ? -1 : 1) - transform.forward * evadeDistance;
            }
        }
        else {
            if (protectTarget == null) {
                foreach (Transform drone in ManageFactions.instance.GetAllyFactionList(transform.GetComponent<Health>().faction)) {
                    if (drone.GetComponent<DroneAI>().GetAttackTarget() == alerter.transform) {
                        if (underFireTimer <= 0) {
                            underFireTimer = evadeTime;
                            evadeDirection = transform.up * data.hoverDelta * (Random.Range(0f, 1f) < .5f ? -1 : 1) + transform.right * evadeDistance * (Random.Range(0f, 1f) < .5f ? -1 : 1) - transform.forward * evadeDistance;
                        }
                        return;
                    }
                }
                SetAttackTarget(alerter.transform);
            }
        }
    }

    public override void SetAttackTarget(Transform target) {
        if (target.GetComponent<Health>().faction == transform.GetComponent<Health>().faction) {
            if (deployedShield == null && target != transform && rechargeTimer <= 0) {
                protectTarget = target;
                combatHeight = 0;
                protectStartDistance = Vector3.Distance(target.position, transform.position);
                attackTarget = null;
                deployedShield = GameObject.Instantiate(shield, target.position + target.forward * deployDistance, target.rotation).transform;
                deployedShield.GetComponent<DeployableShield>().SetCaster(transform.GetComponent<Health>());
                deployedIndicator = GameObject.Instantiate(indicator, transform.position, Quaternion.identity, transform).GetComponent<LineRenderer>();
                deployedIndicator.SetPosition(0, transform.position);
                deployedIndicator.SetPosition(1, target.position);
            }
        }
        else {
            attackTarget = target;
            combatHeight = Random.Range(data.hoverHeight.x, data.hoverHeight.y);
            protectTarget = null;
        }
    }
}
