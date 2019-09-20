using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreeWeaponController : MonoBehaviour {


    public AudioSource source;
    public WeaponSystem weaponPrimary;
    public WeaponSystem weaponSecondary;
    public WeaponSystem weaponSidearm;
    public float interactTime;
    public float reloadAnimTime;
    public Transform defaultWeaponPos;
    public Transform secondaryWeaponPos;
    public bool[] activeGrenades;
    public float throwGrenadeTime;
    public float grenadeReleaseDelay;
    public GameObject[] grenades;

    public int CurWeapon { get { return curWeapon; } }
    public int PrevWeapon { get { return prevWeapon; } }


    int curWeapon;
    int prevWeapon;
    float interactTimer;
    bool interacting;
    bool canFire;
    bool switching;
    int nextWeapon;
    float switchTimer;
    float reloadAnimTimer;
    int activeGrenade;
    bool usingGrenade;
    float throwGrenadeTimer;
    bool grenadeThrown;

    // Use this for initialization
    void Start () {
        for (int i = 0; i < activeGrenades.Length; i++) {
            HUDManager.instance.grenadeIcons[i].enabled = activeGrenades[i];
            HUDManager.instance.grenadeSelections[i].enabled = false;
        }
        HUDManager.instance.UpdateGrenadeSelection(0);
    }

    // Update is called once per frame
    void Update() {
        if (CheckWeapon()) {
            WeaponSystem weapon = GetCurWeapon();

            if (Controls.instance.GetFireDown()) {
                canFire = true;
            }
            if (Controls.instance.GetFireUp()) {
                canFire = false;
            }

            if (switching) {
                if (nextWeapon == curWeapon) {
                    weapon.transform.position = Vector3.Lerp(defaultWeaponPos.position, defaultWeaponPos.position - Vector3.up * .2f, switchTimer / weapon.data.switchTime);
                    weapon.transform.localEulerAngles = Vector3.Lerp(defaultWeaponPos.localEulerAngles, defaultWeaponPos.localEulerAngles + Vector3.right * 30f, switchTimer / weapon.data.switchTime);

                    switchTimer -= Time.deltaTime;
                    if (switchTimer <= 0) {
                        weapon.SwitchingWeapon(false);
                        switching = false;
                        weapon.transform.position = defaultWeaponPos.position;
                        weapon.transform.localEulerAngles = defaultWeaponPos.localEulerAngles;

                        if (weapon.GetCurAmmo() == 0 && !weapon.IsReloading()) weapon.Reload();
                    }
                }
                else {
                    weapon.transform.position = Vector3.Lerp(defaultWeaponPos.position - Vector3.up * .2f, defaultWeaponPos.position, switchTimer / weapon.data.switchTime);
                    weapon.transform.localEulerAngles = Vector3.Lerp(defaultWeaponPos.localEulerAngles + Vector3.right * 30f, defaultWeaponPos.localEulerAngles, switchTimer / weapon.data.switchTime);

                    switchTimer -= Time.deltaTime;
                    if (switchTimer <= 0) {
                        weapon.SwitchingWeapon(false);
                        SetWeaponActive(weapon, false);

                        curWeapon = nextWeapon;
                        weapon = GetCurWeapon();
                        weapon.SwitchingWeapon(true);
                        SetWeaponActive(weapon, true);
                        switchTimer = weapon.data.switchTime;

                        weapon.transform.position = defaultWeaponPos.position - Vector3.up * .2f;
                        weapon.transform.localEulerAngles = defaultWeaponPos.localEulerAngles + Vector3.right * 30f;
                        LoadoutManager.instance.UpdateWeapon(weapon.GetCurAmmo(), weapon.data.ammo);
                    }
                }
            }
            else if (usingGrenade) {
                float scaledTimer = Mathf.Abs(throwGrenadeTimer - throwGrenadeTime * .5f) * 4f - throwGrenadeTime;
                weapon.transform.position = Vector3.Lerp(defaultWeaponPos.position - Vector3.up * .1f, defaultWeaponPos.position, scaledTimer / throwGrenadeTime);
                weapon.transform.localEulerAngles = Vector3.Lerp(defaultWeaponPos.localEulerAngles + Vector3.right * 20f, defaultWeaponPos.localEulerAngles, scaledTimer / throwGrenadeTime);

                if (!grenadeThrown && throwGrenadeTimer < throwGrenadeTime - grenadeReleaseDelay) {
                    if (grenades.Length > activeGrenade && grenades[activeGrenade] != null) {
                        Instantiate(grenades[activeGrenade], secondaryWeaponPos.position, secondaryWeaponPos.rotation);
                    }
                    grenadeThrown = true;
                }

                throwGrenadeTimer -= Time.deltaTime;

                if (throwGrenadeTimer <= 0) {
                    usingGrenade = false;
                }
            }
            else if (Controls.instance.GetGrenade()) {
                ThrowGrenade();
            }
            else {
                Vector3 reticleCenter = Quaternion.AngleAxis(5, Camera.main.transform.right) * Camera.main.transform.forward;

                RaycastHit hit;
                if (Physics.Raycast(Camera.main.transform.position, reticleCenter, out hit, 100, ~0, QueryTriggerInteraction.Ignore)) {
                    weapon.RotateWeapon(hit.point);
                }
                else {
                    weapon.RotateWeapon(weapon.transform.position + reticleCenter);
                }

                if (weapon.IsReloading() && reloadAnimTimer == -1) {
                    reloadAnimTimer = 0;
                }
                if (weapon.IsReloading() && reloadAnimTimer >= 0) {
                    weapon.transform.position = Vector3.Lerp(defaultWeaponPos.position, defaultWeaponPos.position - Vector3.up * .1f, reloadAnimTimer / reloadAnimTime);
                    weapon.transform.localEulerAngles = Vector3.Lerp(defaultWeaponPos.localEulerAngles, defaultWeaponPos.localEulerAngles + Vector3.right * 20f, reloadAnimTimer / reloadAnimTime);
                    reloadAnimTimer = Mathf.Clamp(reloadAnimTimer + Time.deltaTime, 0, reloadAnimTime);
                }
                if (!weapon.IsReloading() && reloadAnimTimer >= 0) {
                    reloadAnimTimer = -1;
                }

                if (canFire) {
                    if (weapon.data.semiAuto && weapon.Ready()) canFire = false;
                    StartCoroutine(weapon.Fire());
                }
                if (Controls.instance.GetReload()) {
                    weapon.Reload();
                }
                if (Controls.instance.GetSwitch()) Switch(weapon, false);
                else if (Controls.instance.GetSwitchToSidearm()) Switch(weapon, true);
            }
        }

        if (Controls.instance.GetInteractDown()) interacting = true;

        if (interacting && Controls.instance.GetInteract()) Swap();
        else {
            interactTimer = 0;
            interacting = false;
        }

        int grenadeSwitch = Controls.instance.LeftRightArrow();
        if (!usingGrenade && grenadeSwitch != 0) {
            int looper = 0;
            do {
                activeGrenade = (activeGrenade + grenadeSwitch + activeGrenades.Length) % activeGrenades.Length;
            } while (!activeGrenades[activeGrenade] && looper++ < activeGrenades.Length);
            if (activeGrenades[activeGrenade]) HUDManager.instance.UpdateGrenadeSelection(activeGrenade);
        }
    }

    void ThrowGrenade() {
        usingGrenade = true;
        throwGrenadeTimer = throwGrenadeTime;
        grenadeThrown = false;
    }

    public bool AddWeapon(WeaponData weapon, int index = -1) {
            if ((index == -1 || index == 0) && weaponPrimary == null) {
                weaponPrimary = Instantiate(weapon.prefab).GetComponent<WeaponSystem>();
                weaponPrimary.name = weapon.name;
                weaponPrimary.Initialize(weapon);
                weaponPrimary.PickupWeapon(defaultWeaponPos, 0);
                LoadoutManager.instance.InitializeWeapon(weaponPrimary, 0);
                SetWeaponActive(weaponPrimary, false);
            }
            else if ((index == -1 || index == 1) && weaponSecondary == null) {
                weaponSecondary = Instantiate(weapon.prefab).GetComponent<WeaponSystem>();
                weaponSecondary.name = weapon.name;
                weaponSecondary.Initialize(weapon);
                weaponSecondary.PickupWeapon(defaultWeaponPos, 1);
                LoadoutManager.instance.InitializeWeapon(weaponSecondary, 1);
                SetWeaponActive(weaponSecondary, false);
            }
            else if ((index == -1 || index == 2) && weaponSidearm == null) {
                weaponSidearm = Instantiate(weapon.prefab).GetComponent<WeaponSystem>();
                weaponSidearm.name = weapon.name;
                weaponSidearm.Initialize(weapon);
                weaponSidearm.PickupWeapon(defaultWeaponPos, 2);
                LoadoutManager.instance.InitializeWeapon(weaponSidearm, 2);
                SetWeaponActive(weaponSidearm, false);
            }
            else {
                return false;
            }
        return true;
    }

    public void SetWeapon(int next, int prev) {
        if (CheckWeapon()) {
            WeaponSystem weapon = GetCurWeapon();
            weapon.CancelReload();
            SetWeaponActive(weapon, false);
        }
        int cur = curWeapon;
        curWeapon = next;
        prevWeapon = prev;

        WeaponSystem nextWeapon = GetCurWeapon();
        if (cur != next) LoadoutManager.instance.SwitchWeapon(cur, next);
        SetWeaponActive(nextWeapon, true);

        LoadoutManager.instance.UpdateWeapon(nextWeapon.GetCurAmmo(), nextWeapon.data.ammo);
    }

    bool CheckWeapon() {
        switch (curWeapon) {
            case 1: return weaponSecondary != null;
            case 2: return weaponSidearm != null;
            default: return weaponPrimary != null;
        }
    }

    WeaponSystem GetCurWeapon() {
        switch(curWeapon) {
            case 1: return weaponSecondary;
            case 2: return weaponSidearm;
            default: return weaponPrimary;
        }
    }

    void Switch(WeaponSystem weapon, bool toSidearm) {
        if (curWeapon == 2 && toSidearm) toSidearm = false;

        nextWeapon = curWeapon;
        switch (curWeapon) {
            case 0:
                if (!toSidearm && weaponSecondary != null) {
                    nextWeapon = 1;
                }
                else if (weaponSidearm != null) {
                    prevWeapon = 0;
                    nextWeapon = 2;
                }
                break;
            case 1:
                if (!toSidearm && weaponPrimary != null) {
                    nextWeapon = 0;
                }
                else if (weaponSidearm != null) {
                    prevWeapon = 1;
                    nextWeapon = 2;
                }
                break;
            case 2:
                if (weaponPrimary != null && prevWeapon == 0 || prevWeapon != 1) {
                    nextWeapon = 0;
                }
                else if (weaponSecondary != null && prevWeapon == 1 || prevWeapon != 0) {
                    nextWeapon = 1;
                }
                break;
            default:
                nextWeapon = 0;
                break;
        }

        if (nextWeapon != curWeapon) {
            if (source.isPlaying) source.Stop();

            if (weapon != null) {
                weapon.CancelReload();
            }

            weapon.SwitchingWeapon(true);
            LoadoutManager.instance.SwitchWeapon(curWeapon, nextWeapon);
            switching = true;
            switchTimer = weapon.data.switchTime;
        }
    }

    void Swap() {
        if (interactTimer >= interactTime && HUDManager.instance.CanInteractWithWeapon()) {
            WeaponSystem next = HUDManager.instance.GetClosestWeapon();
            switch (curWeapon) {
                case 0:
                    if (next.data.sidearm && weaponSidearm == null) {
                        weaponSidearm = next;
                        weaponSidearm.PickupWeapon(defaultWeaponPos, 2);
                        Switch(weaponPrimary, true);
                        LoadoutManager.instance.UpdateWeapon(weaponSidearm);
                    }
                    else if (weaponPrimary == null) {
                        weaponPrimary = next;
                        weaponPrimary.PickupWeapon(defaultWeaponPos, 0);
                        LoadoutManager.instance.UpdateWeapon(weaponPrimary);
                    }
                    else if (weaponSecondary == null) {
                        weaponSecondary = next;
                        weaponSecondary.PickupWeapon(defaultWeaponPos, 1);
                        Switch(weaponPrimary, false);
                        LoadoutManager.instance.UpdateWeapon(weaponSecondary);
                    }
                    else {
                        weaponPrimary.DropWeapon();
                        weaponPrimary = next;
                        weaponPrimary.PickupWeapon(defaultWeaponPos, 0);
                        nextWeapon = 0;
                        switching = true;
                        switchTimer = weaponPrimary.data.switchTime;
                        LoadoutManager.instance.UpdateWeapon(weaponPrimary);
                    }
                    break;
                case 1:
                    if (next.data.sidearm && weaponSidearm == null) {
                        weaponSidearm = next;
                        weaponSidearm.PickupWeapon(defaultWeaponPos, 2);
                        Switch(weaponSecondary, true);
                        LoadoutManager.instance.UpdateWeapon(weaponSidearm);
                    }
                    else if (weaponPrimary == null) {
                        weaponPrimary = next;
                        weaponPrimary.PickupWeapon(defaultWeaponPos, 0);
                        Switch(weaponSecondary, false);
                        LoadoutManager.instance.UpdateWeapon(weaponPrimary);
                    }
                    else {
                        weaponSecondary.DropWeapon();
                        weaponSecondary = next;
                        weaponSecondary.PickupWeapon(defaultWeaponPos, 1);
                        nextWeapon = 1;
                        switching = true;
                        switchTimer = weaponSecondary.data.switchTime;
                        LoadoutManager.instance.UpdateWeapon(weaponSecondary);
                    }
                    break;
                case 2: 
                    if (weaponPrimary == null) {
                        weaponPrimary = next;
                        weaponPrimary.PickupWeapon(defaultWeaponPos, 0);
                        Switch(weaponSidearm, false);
                        LoadoutManager.instance.UpdateWeapon(weaponPrimary);
                    }
                    else if (weaponSecondary == null) {
                        weaponSecondary = next;
                        weaponSecondary.PickupWeapon(defaultWeaponPos, 1);

                        nextWeapon = 1;
                        switching = true;
                        switchTimer = GetCurWeapon().data.switchTime;
                        GetCurWeapon().SwitchingWeapon(true);
                        LoadoutManager.instance.SwitchWeapon(2, 0);
                        LoadoutManager.instance.SwitchWeapon(0, 1);
                        LoadoutManager.instance.UpdateWeapon(weaponSecondary);
                    }
                    else if (next.data.sidearm) {
                        weaponSidearm.DropWeapon();
                        weaponSidearm = next;
                        weaponSidearm.PickupWeapon(defaultWeaponPos, 2);
                        nextWeapon = 2;
                        switching = true;
                        switchTimer = weaponSidearm.data.switchTime;
                        LoadoutManager.instance.UpdateWeapon(weaponSidearm);
                    }
                    break;
            }

            interactTimer = 0;
            interacting = false;
        }
        interactTimer += Time.deltaTime;
    }

    void SetWeaponActive(WeaponSystem weapon, bool active) {
        if (weapon != null) {
            if (weapon.GetComponent<MeshRenderer>() != null) weapon.GetComponent<MeshRenderer>().enabled = active;
            else weapon.transform.GetChild(0).gameObject.SetActive(active);
        }
    }
}
