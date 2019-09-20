using UnityEngine;
using System.Collections;

public class WeaponSystem : MonoBehaviour {

    public WeaponData data;
    public bool dropped;
    public Transform[] firePoint;

    public float FireRate { get { return fireRate; } }

    protected int curAmmo;
    protected float fireRate;
    protected float weaponTimer;
    protected bool reloading;
    protected bool dropping;
    protected bool near;
    protected int fireingPoint;
    protected AudioSource source;
    protected Vector3 recoilSmoothDampVelocity;
    protected float recoilAngle;
    protected float recoilRotSmoothDampVelocity;
    protected float reloadTimer;
    protected float reloadReadyTimer;
    protected int loadoutIndex;
    protected Quaternion lookAngle;
    protected bool switching;

    // Use this for initialization
    public virtual void Initialize (WeaponData data, float startingAmmo = 1, bool startDropped = false) {
        this.data = data;
        curAmmo = Mathf.CeilToInt(data.ammo * startingAmmo);
        fireRate = 60f / data.rateOfFire;
        weaponTimer = 0;
        reloading = false;
        dropping = false;
        near = false;
        fireingPoint = 0;
        reloadTimer = 0;
        if (startDropped) {
            dropping = true;
            dropped = true;
            GetComponent<Collider>().isTrigger = false;
            GetComponent<Rigidbody>().isKinematic = false;
        }
	}
	
	// Update is called once per frame
	protected virtual void Update () {
        if (data != null) {
            weaponTimer = Mathf.Clamp(weaponTimer - Time.deltaTime, 0, fireRate);
            if (dropping && GetComponent<Rigidbody>().velocity.magnitude < .001f) {
                GetComponent<Rigidbody>().isKinematic = true;
                GetComponent<Collider>().isTrigger = true;
                dropping = false;
                RaycastHit hit;
                if (Physics.Raycast(transform.position, Vector3.down, out hit, 1, ~0, QueryTriggerInteraction.Ignore)) {
                    if (hit.transform.localScale.x == 1 && hit.transform.localScale.y == 1 && hit.transform.localScale.z == 1) transform.SetParent(hit.transform);
                }
            }
            if (Controls.instance.player != null) {
                if (dropped && !near && Vector3.Distance(transform.position, Controls.instance.player.transform.position) <= Controls.instance.distanceToInteract) {
                    HUDManager.instance.AddWeaponInRange(this);
                    near = true;
                }
                else if (dropped && near && Vector3.Distance(transform.position, Controls.instance.player.transform.position) > Controls.instance.distanceToInteract) {
                    HUDManager.instance.RemoveWeaponInRange(this);
                    near = false;
                }
            }

            if (reloading && reloadTimer > 0) {
                if (reloadReadyTimer > 0) {
                    reloadReadyTimer -= Time.deltaTime;
                }
                else {
                    reloadTimer -= Time.deltaTime;
                }
            }
            if (reloading && reloadTimer <= 0) {
                Reloaded();
            }

            //Animate recoil
            if (!dropped) {
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, Vector3.zero, ref recoilSmoothDampVelocity, .2f);
                recoilAngle = Mathf.SmoothDamp(recoilAngle, 0, ref recoilRotSmoothDampVelocity, .2f);
                //transform.localEulerAngles += Vector3.left * recoilAngle;

            }
        }
    }

    public int GetCurAmmo() {
        return curAmmo;
    }

    public float GetTimer() {
        return weaponTimer;
    }

    public bool IsReloading() {
        return reloading;
    }

    public virtual bool Ready() {
        return weaponTimer == 0 && (data.magReload && !reloading || !data.magReload && reloadReadyTimer <= 0) && !dropping && !switching;
    }

    public IEnumerator Fire(bool ignoreTimer = false) {
        if (Ready()) {
            if (curAmmo == 0) {
                Reload();
            }
            else {
                if (reloading) {
                    CancelReload();
                }
                if (!ignoreTimer) weaponTimer = fireRate;
                for (int i = 0; i < data.shotsPerFire; ++i) {
                    float angle1 = Random.Range(-data.accuracy * Mathf.Deg2Rad, data.accuracy * Mathf.Deg2Rad);
                    float angle2 = Random.Range(0, 2 * Mathf.PI);
                    Vector3 rotation = Vector3.RotateTowards(firePoint[fireingPoint].forward,
                                                             firePoint[fireingPoint].right * Mathf.Sin(angle2) + firePoint[fireingPoint].up * Mathf.Cos(angle2),
                                                             angle1, Mathf.PI);
                    //Vector3 rotation = firePoint[fireingPoint].forward;
                    GameObject projectileInstance = (Instantiate(data.projectile, firePoint[fireingPoint].position, Quaternion.LookRotation(rotation, firePoint[fireingPoint].up)) as GameObject);
                    projectileInstance.GetComponent<ProjectileRaycaster>().InitializeProjectile(true, data.damage, data.range);
                    if (data.burstTime > 0 && i + 1 < data.shotsPerFire && curAmmo > 1) {
                        if (transform.parent.tag == "Player") LoadoutManager.instance.UpdateWeapon(curAmmo - 1, data.ammo);
                        --curAmmo;
                        transform.localPosition -= Vector3.forward * data.recoil * .05f;
                        if (source != null) source.PlayOneShot(data.shootClip, data.volume);
                        yield return new WaitForSeconds(data.burstTime);
                    }
                    if (transform.parent.tag == "Player") LoadoutManager.instance.UpdateWeapon(curAmmo - 1, data.ammo);
                    ++fireingPoint;
                    fireingPoint %= firePoint.Length;
                }

                transform.localPosition -= Vector3.forward * data.recoil * .05f;
                if (transform.parent.tag == "Player") {
                    recoilAngle += data.recoil * 1f;
                    recoilAngle = Mathf.Clamp(recoilAngle, 0, 15);
                }
                if (source != null) source.PlayOneShot(data.shootClip, data.volume);
                --curAmmo;
                if (curAmmo == 0) {
                    Reload();
                }
            }
        }
    }

    public virtual void Reload() {
        if (!reloading && curAmmo < data.ammo) {
            reloading = true;
            reloadTimer = data.reload;
            if (!data.magReload) reloadReadyTimer = data.reloadReadyTime;
            if (transform.parent.tag == "Player" && source != null) source.PlayOneShot(data.reloadClip, data.volume);
            if (transform.parent.tag == "Player") LoadoutManager.instance.Reload(data.reload, data.magReload);
        }
    }

    public virtual void Reloaded() {
        if (data.magReload) {
            reloading = false;
            curAmmo = data.ammo;
            fireingPoint = 0;
            if (transform.parent != null && transform.parent.tag == "Player") LoadoutManager.instance.UpdateWeapon(curAmmo, data.ammo, true);
        }
        else {
            curAmmo++;
            fireingPoint = 0;
            if (transform.parent != null && transform.parent.tag == "Player") LoadoutManager.instance.UpdateWeapon(curAmmo, data.ammo, curAmmo >= data.ammo);

            if (curAmmo >= data.ammo) reloading = false;
            else reloadTimer = data.reload;
        }
    }

    public virtual void CancelReload() {
        reloading = false;
        reloadReadyTimer = 0;
    }

    public void SwitchingWeapon(bool isSwitching) {
        switching = isSwitching;
    }

    public void DropWeapon() {
        transform.SetParent(null);
        dropping = true;
        dropped = true;
        GetComponent<Collider>().isTrigger = false;
        GetComponent<Rigidbody>().isKinematic = false;
        GetComponent<Rigidbody>().velocity = transform.forward * 2 + Vector3.up * 2;
        source = null;
    }

    public void PickupWeapon(Transform parent, int index) {
        if (parent != null) {
            transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        GetComponent<Rigidbody>().isKinematic = true;
        GetComponent<Collider>().isTrigger = true;
        source = GetComponentInParent<AudioSource>();
        dropped = false;
        dropping = false;
        near = false;
        loadoutIndex = index;
        HUDManager.instance.RemoveWeaponInRange(this);
    }

    public void RotateWeapon(Vector3 point) {
        if (Ready())
            transform.rotation = Quaternion.LookRotation(point - transform.position);
    }
}
