using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour {

    public float startingVelocity;
    public float range;
    public int damage;
    public float maxNormal;
    public float detonateTime;
    public float maxLife;
    public GameObject explosion;

    protected Rigidbody rig;
    protected float triggeredTimer;
    protected bool triggered;
    protected float lifeTime;

    private void Awake() {
        rig = GetComponent<Rigidbody>();
    }

    // Use this for initialization
    void Start () {
        rig.velocity = transform.forward * startingVelocity;
	}
	
	// Update is called once per frame
	void Update () {
		if (triggered) {
            triggeredTimer -= Time.deltaTime;

            if (triggeredTimer <= 0) {
                Instantiate(explosion, transform.position, Quaternion.identity);
                Destroy(gameObject);
                foreach (Transform unit in ManageFactions.instance.GetAllyFactionList(ManageFactions.Faction.green)) {
                    if (Vector3.Distance(unit.position, transform.position) < range) {
                        unit.GetComponent<Health>().Damage(damage, new RaycastHit() { point = transform.position }, unit.position - transform.position);
                    }
                }
                foreach (Transform unit in ManageFactions.instance.GetAllyFactionList(ManageFactions.Faction.purple)) {
                    if (Vector3.Distance(unit.position, transform.position) < range) {
                        unit.GetComponent<Health>().Damage(damage, new RaycastHit() { point = transform.position }, unit.position - transform.position);
                    }
                }
                foreach (Transform unit in ManageFactions.instance.GetAllyFactionList(ManageFactions.Faction.security)) {
                    if (Vector3.Distance(unit.position, transform.position) < range) {
                        unit.GetComponent<Health>().Damage(damage, new RaycastHit() { point = transform.position }, unit.position - transform.position);
                    }
                }
            }
        }
        else if (rig.velocity.magnitude < 1) {
            triggeredTimer = detonateTime;
            triggered = true;
        }
        else {
            lifeTime += Time.deltaTime;
            if (lifeTime >= maxLife) {
                triggeredTimer = 0;
                triggered = true;
            }
        }
	}

    public virtual void OnCollisionEnter(Collision collision) {
        if (!triggered) {
            foreach (ContactPoint contact in collision.contacts) {
                if (Vector3.Angle(Vector3.up, contact.normal) < maxNormal) {
                    triggeredTimer = detonateTime;
                    triggered = true;
                }
            }
        }
    }
}
