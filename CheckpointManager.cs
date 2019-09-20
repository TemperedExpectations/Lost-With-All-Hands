using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour {

    public static CheckpointManager instance;


    [System.Serializable]
    public struct UnitSave {
        public Transform transform;
    }

    [System.Serializable]
	public struct SaveInfo {
        public Vector3 playerPos;
        public Vector3 playerRot;
        public Vector3 playerVel;
        public Vector3 cameraRot;

        public float shield;
        public float health;
        public float rechargeTimer;

        public int curWeapon;
        public int prevWeapon;
        public WeaponData primary;
        public WeaponData secondary;
        public WeaponData sidearm;

        public bool levelZones;
        public List<int> loadZones;
        public List<bool> hasSpawnedList;
        public List<List<EnemySpawner.SaveInfo>> unitList;
    }
    
    public SaveInfo saveInfo;
    public bool hasCheckpointed;

    void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(instance);
        }
        else {
            Destroy(gameObject);
        }
    }

    public void CheckpointPlayer(PlayerManager player, List<int> loadZones, List<bool> spawned, List<List<EnemySpawner.SaveInfo>> unitList) {
        if (player != null && player.CanSave) {
            hasCheckpointed = true;

            saveInfo.playerPos = player.transform.position;
            saveInfo.playerRot = player.transform.localEulerAngles;
            saveInfo.playerVel = player.Rig.velocity;
            saveInfo.cameraRot = player.Controller.cam.transform.localEulerAngles;

            saveInfo.shield = player.Health.CurShield;
            saveInfo.health = player.Health.CurHealth;
            saveInfo.rechargeTimer = player.Health.RechargeTimer;

            saveInfo.curWeapon = player.WeaponController.CurWeapon;
            saveInfo.prevWeapon = player.WeaponController.PrevWeapon;
            saveInfo.primary = player.WeaponController.weaponPrimary == null ? null : player.WeaponController.weaponPrimary.data;
            saveInfo.secondary = player.WeaponController.weaponSecondary == null ? null : player.WeaponController.weaponSecondary.data;
            saveInfo.sidearm = player.WeaponController.weaponSidearm == null ? null : player.WeaponController.weaponSidearm.data;


            if (loadZones != null) {
                saveInfo.levelZones = true;
                saveInfo.loadZones = loadZones;
                saveInfo.hasSpawnedList = spawned;
                saveInfo.unitList = unitList;
            }
            else {
                saveInfo.levelZones = false;
            }
        }
    }
}
