using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BehaviorType {
    patrol, alert, combat
}

public abstract class AbstractAI : MonoBehaviour {
    
    public Transform head;
    public Transform gunArm;
    public float investigateDistance;
    public float investigateChance;
    public float repulsionDistance;
    public float repulsionForce;
    public List<WeaponData> weaponData;
    public float delayMult;
    public float burstMult;
    public float reloadMult;
    public float burstTime;
    public List<AbstractBehaviorData> behaviorData;

    protected Rigidbody rig;
    protected Health health;
    protected Transform attackTarget;
    protected List<WeaponSystem> weapons;
    protected List<AbstractBehavior> behaviorList;
    protected float pathEpsilon = .05f;
    protected int curWeapon;
    protected float weaponTimer;
    protected float burst;
    protected Vector3 maxVelocity;
    protected BehaviorType mode;
    protected float reloadTimer;

    // Use this for initialization
    public virtual void Start() {
        rig = GetComponent<Rigidbody>();
        health = GetComponent<Health>();
        curWeapon = 0;
        weapons = new List<WeaponSystem>();
        foreach (WeaponData weapon in weaponData) {
            AddWeapon(weapon);
        }
        if (weapons.Count > 0) {
            weapons[0].gameObject.SetActive(true);
        }
        behaviorList = new List<AbstractBehavior>();
        foreach (AbstractBehaviorData data in behaviorData) {
            behaviorList.Add(data.GetBehavior(transform));
        }
        SwitchMode(BehaviorType.patrol);
    }

    // Update is called once per frame
    void Update() {
        if (!health.IsDead()) {
            if (weaponTimer > 0) weaponTimer -= Time.deltaTime;

            if (!CurBehavior().Step()) {
                if (mode == BehaviorType.combat) SwitchMode(BehaviorType.alert);
                else if (mode == BehaviorType.alert) SwitchMode(BehaviorType.patrol);
            }
            if (mode != BehaviorType.combat) {
                Transform target = CurBehavior().Search(health.faction, head.position);
                if (target != null) {
                    attackTarget = target;
                    SwitchMode(BehaviorType.combat);
                    CurBehavior().SetAttackTarget(attackTarget);
                }
            }
            else {
                Transform target = CurBehavior().Search(health.faction, head.position);
                if (target == null) {
                    SwitchMode(BehaviorType.alert);
                    CurBehavior().Alert(attackTarget.position - transform.position, false, attackTarget.position, null);
                }
                else {
                    if (target.GetComponent<Health>().faction != health.faction)
                        Attack();
                }
            }
            if (reloadTimer > 0) {
                reloadTimer -= Time.deltaTime;
                if (reloadTimer <= 0) {
                    weapons[curWeapon].Reloaded();
                    reloadTimer = 0;
                }
            }
        }
        else {
            if (!rig.useGravity) rig.useGravity = true;
        }
    }

    void FixedUpdate() {
        if (!health.IsDead()) {
            MoveToTarget(CurBehavior().GetMove());
            LookAtTarget(CurBehavior().GetLook());
        }
    }

    public abstract void Alerted(Vector3 direction);
    public abstract void Alert(Vector3 direction, bool origin, Vector3 point, AbstractAI alerter);
    public abstract AbstractBehavior CurBehavior();
    public abstract Transform GetAttackTarget();

    protected virtual void AddWeapon(WeaponData data) {
        WeaponSystem newWeapon = Instantiate(data.prefab).GetComponent<WeaponSystem>();
        newWeapon.name = data.name;
        newWeapon.Initialize(data);
        newWeapon.PickupWeapon(gunArm, weapons.Count);
        newWeapon.gameObject.SetActive(false);
        weapons.Add(newWeapon);
    }

    protected abstract void Attack();
    protected abstract void SwitchMode(BehaviorType nextMode);
    protected abstract void StopMove();
    protected abstract void MoveToTarget(Vector3 moveTo);
    protected abstract void LookAtTarget(Vector3 look);
}
