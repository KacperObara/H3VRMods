using System;
using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using FistVR;
using HarmonyLib;
using UnityEngine;

namespace BulletCatch
{
    [BepInPlugin("h3vr.kodeman.bulletcatch", "Bullet Catch", "1.0.0")]
    [BepInProcess("h3vr.exe")]
    public class BulletCatch : BaseUnityPlugin
    {
        private static BulletCatch _instance;
        
        private static ConfigEntry<bool> EnableBulletCatch;
        
        private static ConfigEntry<float> BulletCatchRadius;
        private static ConfigEntry<float> BulletCatchTime;
        private static ConfigEntry<float> DelayBeforeBulletCatch;
        
        private void Start()
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            EnableBulletCatch = Config.Bind("BulletCatch", "Enable Bullet Catch", true, "Allows for easier way to catch bullets when ejected.");

            BulletCatchRadius = Config.Bind("BulletCatch", "Bullet Catch Radius", 0.125f, "How big the bullet catch radius is.");
            BulletCatchTime = Config.Bind("BulletCatch", "Bullet Catch Time", 0.75f, "How long the bullet catch lasts for.");
            DelayBeforeBulletCatch = Config.Bind("BulletCatch", "Delay Before Bullet Catch", 0.1f, "How long between bullet being ejected and the bullet catch being active."); 

            Harmony.CreateAndPatchAll(typeof(BulletCatch));
            
            Logger.LogInfo("Bullet catch mod loaded.");
        }

        [HarmonyPatch(typeof(FVRFireArmChamber), nameof(FVRFireArmChamber.EjectRound), new Type[] {typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(bool)})]
        [HarmonyPatch(typeof(FVRFireArmChamber), nameof(FVRFireArmChamber.EjectRound), new Type[] {typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(Quaternion), typeof(bool)})]
        [HarmonyPostfix]
        public static void OnEjectRound(FVRFireArmChamber __instance, ref FVRFireArmRound __result)
        {
            if (!EnableBulletCatch.Value || __result == null || __result.IsSpent)
                return;
            
            _instance.StartCoroutine(DoTheCatchingCR(__result));
        }
        
        private static IEnumerator DoTheCatchingCR(FVRFireArmRound __result)
        { 
            yield return new WaitForSeconds(DelayBeforeBulletCatch.Value);
            
            var trigger = __result.gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = BulletCatchRadius.Value;
            
            yield return new WaitForSeconds(BulletCatchTime.Value);
            Destroy(trigger);
        }
    }
}