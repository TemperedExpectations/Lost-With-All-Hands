using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Shoot : MonoBehaviour {
	
    public static Shoot instance;
    public AudioSource source;
    public AudioClip shoot;
    public AudioClip reload;
	public List<WeaponSystem> weapons;
    public float interactTime;
    public Transform defaultWeaponPos;

	int curWeapon;
    float interactTimer;
    bool interacting;
    bool canFire;

    void Awake() {
        if (instance == null) instance = this;
        else Destroy(this);
    }

	// Use this for initialization
	void Start () {
		curWeapon = 0;
        LoadoutManager.instance.InitializeWeapons(weapons);
        if (weapons.Count > 0) {
            if (weapons[curWeapon].GetComponent<MeshRenderer>() != null) {
                weapons[curWeapon].GetComponent<MeshRenderer>().enabled = true;
            }
            else {
                foreach (MeshRenderer m in weapons[curWeapon].GetComponentsInChildren<MeshRenderer>()) {
                    m.enabled = true;
                }
            }
        }
        interactTimer = 0;
        interacting = false;
        foreach (WeaponSystem weapon in weapons) weapon.PickupWeapon(defaultWeaponPos, 0);
	}
	
	// Update is called once per frame
	void Update () {
        if (weapons.Count > 0) {
            Vector3 reticleCenter = Vector3.RotateTowards(Camera.main.transform.forward, -Camera.main.transform.up, 5 * Mathf.Deg2Rad, 1);
            //Debug.DrawRay(Camera.main.transform.position, reticleCenter * 100, Color.blue);
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, reticleCenter, out hit, 100, ~0, QueryTriggerInteraction.Ignore)) {
                //Debug.DrawLine(weapons[curWeapon].firePoint.position, hit.point, Color.green);
                weapons[curWeapon].RotateWeapon(hit.point);
            }
            else {
                //Debug.DrawRay(weapons[curWeapon].firePoint.position, weapons[curWeapon].firePoint.forward * 100, Color.red);
                weapons[curWeapon].RotateWeapon(weapons[curWeapon].transform.position + reticleCenter);
            }

            if (Controls.instance.GetFireDown()) {
                canFire = true;
            }
            if (Controls.instance.GetFireUp()) {
                canFire = false;
            }
            if (canFire) {
                if (weapons[curWeapon].data.semiAuto && weapons[curWeapon].Ready()) canFire = false;
                StartCoroutine(weapons[curWeapon].Fire());
            }
            if (Controls.instance.GetReload()) {
                weapons[curWeapon].Reload();
            }
            if (Controls.instance.GetSwitch()) Switch(curWeapon == 0 ? 1 : 0);
            if (Controls.instance.GetInteractDown()) interacting = true;
            if (interacting && Controls.instance.GetInteract()) {
                if (interactTimer >= interactTime && HUDManager.instance.CanInteractWithWeapon()) {
                    weapons[curWeapon].DropWeapon();
                    weapons.RemoveAt(curWeapon);
                    weapons.Insert(curWeapon, HUDManager.instance.GetClosestWeapon());
                    weapons[curWeapon].PickupWeapon(defaultWeaponPos, curWeapon);
                    LoadoutManager.instance.UpdateWeapon(weapons[curWeapon]);
                    interactTimer = 0;
                    interacting = false;
                }
                interactTimer += Time.deltaTime;
            }
            else {
                interactTimer = 0;
                interacting = false;
            }
        }
    }

    public void Switch(int next) {
        if (source.clip == reload && source.isPlaying) source.Stop();
        weapons[curWeapon].CancelReload();
        if (weapons[curWeapon].GetComponent<MeshRenderer>() != null)
            weapons[curWeapon].GetComponent<MeshRenderer>().enabled = false;
        else 
            foreach(MeshRenderer m in weapons[curWeapon].GetComponentsInChildren<MeshRenderer>())
                m.enabled = false;
        LoadoutManager.instance.SwitchWeapon(curWeapon, next);
        curWeapon = next;
        if (weapons[curWeapon].GetComponent<MeshRenderer>() != null)
            weapons[curWeapon].GetComponent<MeshRenderer>().enabled = true;
        else
            foreach (MeshRenderer m in weapons[curWeapon].GetComponentsInChildren<MeshRenderer>())
                m.enabled = true;
        LoadoutManager.instance.UpdateWeapon(weapons[curWeapon].GetCurAmmo(), weapons[curWeapon].data.ammo);
    }

    public int GetCurWeapon() {
        return curWeapon;
    }
}
