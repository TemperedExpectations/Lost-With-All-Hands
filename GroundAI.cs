using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GroundAI : AbstractAI {

    public Transform body;

    NavMeshAgent agent;

    public override void Start() {
        agent = GetComponent<NavMeshAgent>();

        base.Start();
    }

    protected override void SwitchMode(BehaviorType nextMode) {
        mode = nextMode;
        AbstractBehavior b = CurBehavior();
        agent.speed = b.data.moveSpeed;
        agent.angularSpeed = b.data.turnSpeed;
        agent.acceleration = b.data.acceleration;
    }

    public override void Alert(Vector3 direction, bool origin, Vector3 point, AbstractAI alerter) {
        if (health == null || health.IsDead() || alerter == null) return;
        if (mode == BehaviorType.patrol) SwitchMode(BehaviorType.alert);
        if (origin) {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, 100, ~ManageFactions.instance.deployableShield, QueryTriggerInteraction.Ignore)) {
                Vector3 investigatePoint = hit.point - direction * investigateDistance;
                if (Physics.Raycast(investigatePoint, Vector3.down, out hit, CurBehavior().data.hoverHeight.x, ~ManageFactions.instance.deployableShield, QueryTriggerInteraction.Ignore)) {
                    CurBehavior().Alert(direction, origin, hit.point + Vector3.up * CurBehavior().data.hoverHeight.x, alerter);
                    Debug.DrawLine(transform.position, hit.point + Vector3.up * CurBehavior().data.hoverHeight.x, Color.cyan);
                }
                else {
                    CurBehavior().Alert(direction, origin, investigatePoint, alerter);
                }
                foreach (Transform unit in ManageFactions.instance.GetAllyFactionList(health.faction)) {
                    if (unit != null && unit != transform && Vector3.Distance(transform.position, unit.transform.position) < CurBehavior().data.searchRadius) unit.GetComponent<AbstractAI>().Alert(direction, false, investigatePoint, alerter);
                }
            }
        }
        else if (Random.Range(0f, 1f) < investigateChance) {
            CurBehavior().Alert(direction, origin, point, alerter);
        }
    }

    public override void Alerted(Vector3 direction) {
        if (health.IsDead()) return;
        Alert(direction, true, Vector3.zero, this);
    }

    public override AbstractBehavior CurBehavior() {
        switch (mode) {
            case BehaviorType.patrol: return behaviorList[0];
            case BehaviorType.alert: return behaviorList[1];
            case BehaviorType.combat: return behaviorList[2];
            default: return behaviorList[0];
        }
    }

    public override Transform GetAttackTarget() {
        return attackTarget;
    }

    protected override void Attack() {
        if (weaponTimer <= 0 && weapons[curWeapon].Ready() && Vector3.Distance(transform.position, attackTarget.position) < weapons[curWeapon].data.effectiveRange) {
            StartCoroutine(weapons[curWeapon].Fire(true));
            weaponTimer = weapons[curWeapon].FireRate * delayMult;
            burst += weaponTimer;
            if (burst >= burstTime) {
                weaponTimer = weapons[curWeapon].FireRate * burstMult;
                burst = 0;
            }
        }
        if (weapons[curWeapon].IsReloading() && reloadTimer == 0) {
            reloadTimer = weapons[curWeapon].data.reload * reloadMult;
        }
    }

    protected override void LookAtTarget(Vector3 look) {
        if (look == Vector3.zero) return;
        look.Normalize();

        if (mode == BehaviorType.patrol) {
            body.localRotation = Quaternion.RotateTowards(body.localRotation, Quaternion.identity, (CurBehavior().data.turnSpeed) * Time.deltaTime);

            Vector3 gunLook = Vector3.up * -.3f + Vector3.forward * (.7f);
            Debug.DrawRay(gunArm.position, look.normalized * 2, Color.magenta);
            gunArm.localRotation = Quaternion.RotateTowards(gunArm.localRotation, Quaternion.LookRotation(gunLook), (CurBehavior().data.turnSpeed) * Time.deltaTime);
        }
        else {
            Vector3 bodyLook = new Vector3(look.x, 0, look.z).normalized;
            body.rotation = Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(bodyLook), (CurBehavior().data.turnSpeed) * Time.deltaTime);
            
            Debug.DrawRay(gunArm.position, look * 20, Color.magenta);
            gunArm.rotation = Quaternion.RotateTowards(gunArm.rotation, Quaternion.LookRotation(look), (CurBehavior().data.turnSpeed) * Time.deltaTime);
            //Vector3 gunLook = Vector3.up * look.y + Vector3.forward * (1 - look.y);
            //gunArm.localRotation = Quaternion.RotateTowards(gunArm.localRotation, Quaternion.LookRotation(gunLook), (CurBehavior().data.turnSpeed) * Time.deltaTime);
        }
    }

    protected override void MoveToTarget(Vector3 moveTo) {
        foreach (Transform unit in ManageFactions.instance.GetAllyFactionList(health.faction)) {
            if (unit != null && unit != transform && Vector3.Distance(transform.position, unit.position) < repulsionDistance) {
                Vector3 rep = (transform.position - unit.position).normalized * (1 - Vector3.Distance(transform.position, unit.position) / repulsionDistance) * repulsionForce;
                Debug.DrawRay(transform.position, rep, Color.cyan);
                moveTo += rep;
            }
        }

        agent.SetDestination(moveTo);
    }

    protected override void StopMove() {
        rig.velocity = Vector3.Lerp(rig.velocity, Vector3.zero, CurBehavior().data.acceleration * Time.deltaTime);
    }
}
