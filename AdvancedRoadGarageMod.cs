using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TGCore; 

public class AdvancedRoadGarageMod : MonoBehaviour
{
    private bool showMenu = false; 
    private float customSpeedMultiplier = 1.0f; 
    private int selectedCarIndex = 0; 

    private readonly string[] goodFastCars = { "VwGolfPrefab", "LadaPrefab", "PlymouthFuryPrefab", "DaciaPrefab" };
    private readonly string[] fluids = { "GasCanisterPrefab", "OilCanisterPrefab", "WaterCanisterPrefab" };
    private readonly string[] carParts = { "V8EnginePrefab", "CarWheelPrefab", "RadiatorPrefab" };
    private readonly string[] generalItems = { "FirstAidKitPrefab", "SpongePrefab", "LicensePlatePrefab" };

    private List<GameObject> spawnedObjectsList = new List<GameObject>();

    private bool mysteryBoxActive = false;
    private float mysteryBoxTimer = 0f;
    private bool challengeTriggeredForCurrentBuilding = false;

    void Update()
    {
        if (Input.GetKey(KeyCode.Y) && Input.GetKeyDown(KeyCode.G))
        {
            showMenu = !showMenu; 
        }

        Time.timeScale = customSpeedMultiplier;
        CheckPlayerBuildingStatus();
    }

    void OnGUI()
    {
        if (showMenu)
        {
            GUILayout.BeginArea(new Rect(Screen.width - 260, 20, 240, 340), GUI.skin.box);
            GUILayout.Label("🛠️ لوحة تحكم الـ Mod المطورة", GUILayout.Width(220));
            GUILayout.Space(5);

            GUILayout.Label($"السيارة الحالية: {goodFastCars[selectedCarIndex]}");
            if (GUILayout.Button("🔄 تغيير السيارة التالية"))
            {
                selectedCarIndex = (selectedCarIndex + 1) % goodFastCars.Length;
            }
            GUILayout.Space(10);

            if (GUILayout.Button("🏗️ رندرة جراج وموارد متوافقة", GUILayout.Height(35)))
            {
                SpawnCustomGarageSetup();
            }
            GUILayout.Space(10);

            GUILayout.Label($"🏃 سرعة اللعبة: {customSpeedMultiplier:F1}x");
            customSpeedMultiplier = GUILayout.HorizontalSlider(customSpeedMultiplier, 0.1f, 4.0f);
            GUILayout.Space(15);

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("🗑️ تنظيف الخريطة ومنع الـ Lag"))
            {
                ClearSpawnedObjects();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndArea();
        }

        if (mysteryBoxActive && mysteryBoxTimer > 0)
        {
            Rect boxRect = new Rect((Screen.width / 2) - 100, (Screen.height / 2) - 40, 200, 80);
            GUI.backgroundColor = Color.yellow;
            
            if (GUI.Button(boxRect, $"🎁 MYSTERY BOX 🎁\nاضغط سريعاً! ({mysteryBoxTimer:F1}s)"))
            {
                WinMysteryBoxChallenge();
            }
            
            GUI.backgroundColor = Color.white;
            mysteryBoxTimer -= Time.unscaledDeltaTime;

            if (mysteryBoxTimer <= 0)
            {
                LoseMysteryBoxChallenge();
            }
        }
    }

    void CheckPlayerBuildingStatus()
    {
        if (TransformManager.PlayerTransform == null) return;

        Collider[] hitColliders = Physics.OverlapSphere(TransformManager.PlayerTransform.position, 6f);
        bool foundBuilding = false;

        foreach (var col in hitColliders)
        {
            if (col != null && col.name != null && 
               (col.name.Contains("Garage") || col.name.Contains("House") || col.name.Contains("Building")))
            {
                foundBuilding = true;
                break;
            }
        }

        if (foundBuilding)
        {
            if (!challengeTriggeredForCurrentBuilding && !mysteryBoxActive)
            {
                mysteryBoxActive = true;
                mysteryBoxTimer = 5.0f;
                challengeTriggeredForCurrentBuilding = true;
            }
        }
        else
        {
            challengeTriggeredForCurrentBuilding = false;
        }
    }

    // دالة حماية مركزية لعمل الـ Spawn بدون أخطاء (Safe Spawn)
    private GameObject SafeSpawn(string prefabName, Vector3 position, Quaternion rotation)
    {
        try
        {
            if (string.IsNullOrEmpty(prefabName)) return null;

            // استدعاء الرندرة من مكتبة اللعبة
            GameObject obj = AssetLoader.SpawnPrefab(prefabName, position, rotation);
            
            if (obj == null)
            {
                Debug.LogWarning($"[AdvancedMod] التحذير: فشل رندرة المجسم {prefabName}. قد يكون الاسم غير صحيح في ملفات اللعبة.");
            }
            return obj;
        }
        catch (Exception ex)
        {
            // كتم الخطأ وطباعته في الـ Console بدلاً من كراش اللعبة
            Debug.LogError($"[AdvancedMod] خطأ غير متوقع أثناء رندرة {prefabName}: {ex.Message}");
            return null;
        }
    }

    void WinMysteryBoxChallenge()
    {
        mysteryBoxActive = false;
        Vector3 spawnPos = GetGroundedPosition(TransformManager.PlayerTransform.position + TransformManager.PlayerTransform.forward * 5f);
        
        GameObject superCar = SafeSpawn(goodFastCars[selectedCarIndex], spawnPos, TransformManager.PlayerTransform.rotation);
        GameObject v8Engine = SafeSpawn("V8EnginePrefab", spawnPos + Vector3.up * 0.5f, Quaternion.identity);
        
        if (superCar != null) { RegisterObjectForSave(superCar); spawnedObjectsList.Add(superCar); }
        if (v8Engine != null) { RegisterObjectForSave(v8Engine); spawnedObjectsList.Add(v8Engine); }

        RefreshGameSaveSystem();
    }

    void LoseMysteryBoxChallenge()
    {
        mysteryBoxActive = false;
        if (TransformManager.PlayerTransform == null) return;

        Vector3 playerPos = TransformManager.PlayerTransform.position;
        Rigidbody playerRb = TransformManager.PlayerTransform.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.AddExplosionForce(500f, playerPos + Vector3.down, 5f);
        }
    }

    void SpawnCustomGarageSetup()
    {
        if (TransformManager.PlayerTransform == null) return;

        Vector3 playerPos = TransformManager.PlayerTransform.position;
        Vector3 playerForward = TransformManager.PlayerTransform.forward;
        
        Vector3 rawSpawnPos = playerPos + playerForward * 15f;
        Vector3 garageSpawnPos = GetGroundedPosition(rawSpawnPos);
        Quaternion buildingRotation = Quaternion.LookRotation(-playerForward);
        
        // 1. جراج آمن
        GameObject roadGarage = SafeSpawn("Garage", garageSpawnPos, buildingRotation);
        if (roadGarage != null) { RegisterObjectForSave(roadGarage); spawnedObjectsList.Add(roadGarage); }

        // 2. سيارة آمنة
        Vector3 carSpawnPos = GetGroundedPosition(garageSpawnPos + playerForward * 8f);
        GameObject car = SafeSpawn(goodFastCars[selectedCarIndex], carSpawnPos, buildingRotation);
        if (car != null) { RegisterObjectForSave(car); spawnedObjectsList.Add(car); }

        // 3. موارد آمنة
        for (int i = 0; i < 6; i++) 
        {
            int chance = UnityEngine.Random.Range(0, 100);
            Vector3 itemOffset = new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), 0.3f, UnityEngine.Random.Range(1.5f, 3.5f));
            Vector3 itemPos = garageSpawnPos + (buildingRotation * itemOffset);

            GameObject spawnedItem = null;

            if (chance < 40)
                spawnedItem = SafeSpawn(fluids[UnityEngine.Random.Range(0, fluids.Length)], itemPos, buildingRotation);
            else if (chance >= 40 && chance < 70)
                spawnedItem = SafeSpawn(carParts[UnityEngine.Random.Range(0, carParts.Length)], itemPos, buildingRotation);
            else 
                spawnedItem = SafeSpawn(generalItems[UnityEngine.Random.Range(0, generalItems.Length)], itemPos, buildingRotation);

            if (spawnedItem != null)
            {
                RegisterObjectForSave(spawnedItem);
                spawnedObjectsList.Add(spawnedItem);
            }
        }

        RefreshGameSaveSystem();
    }

    Vector3 GetGroundedPosition(Vector3 targetPos)
    {
        RaycastHit hit;
        if (Physics.Raycast(targetPos + Vector3.up * 10f, Vector3.down, out hit, 30f))
        {
            return hit.point;
        }
        return targetPos; 
    }

    void ClearSpawnedObjects()
    {
        for (int i = spawnedObjectsList.Count - 1; i >= 0; i--)
        {
            if (spawnedObjectsList[i] != null)
            {
                Destroy(spawnedObjectsList[i]);
            }
        }
        spawnedObjectsList.Clear();
        RefreshGameSaveSystem();
    }

    void RegisterObjectForSave(GameObject obj)
    {
        if (obj == null) return;
        try 
        {
            obj.SendMessage("OnSpawnedByMod", SendMessageOptions.DontRequireReceiver);
            
            Component[] rigidbodies = obj.GetComponentsInChildren(typeof(Rigidbody), true);
            foreach (Rigidbody rb in rigidbodies)
            {
                if (rb == null) continue;
                int uniqueID = UnityEngine.Random.Range(1000000, 9999999);
                rb.gameObject.SendMessage("SetId", uniqueID, SendMessageOptions.DontRequireReceiver);
                rb.gameObject.SendMessage("SaveID", uniqueID, SendMessageOptions.DontRequireReceiver);
            }
        }
        catch (Exception) {}
    }

    void RefreshGameSaveSystem()
    {
        GameObject mainController = GameObject.Find("maininstance") ?? GameObject.FindGameObjectWithTag("GameController");
        if (mainController != null)
        {
            mainController.SendMessage("UpdateSaveables", SendMessageOptions.DontRequireReceiver);
            mainController.SendMessage("ForceSaveRefresh", SendMessageOptions.DontRequireReceiver);
        }
    }
}
