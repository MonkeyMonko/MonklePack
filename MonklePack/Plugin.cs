using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace MonklePack
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class MonklePackModBase : BaseUnityPlugin
    {
        public const string modGUID = "Monkle.VariousMods";
        public const string modName = "MonklePack";
        public const string modVersion = "1.0.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);
        [CanBeNull] public static AudioClip TomScream;
        [CanBeNull] public static AudioClip Pedro;
        [CanBeNull] private static MonklePackModBase Instance;
        
        public static AssetBundle ItemAssetBundle;
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            var location = Instance.Info.Location;
            var bepInExLogSource = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            bepInExLogSource.LogMessage(modGUID + " location " + location);
            var bundleLocation = location.TrimEnd("MonklePack.dll".ToCharArray()) + "monklepack";
            ItemAssetBundle = AssetBundle.LoadFromFile(bundleLocation);
            TomScream = ItemAssetBundle.LoadAsset<AudioClip>("Assets/TomScream.mp3");
            Pedro = ItemAssetBundle.LoadAsset<AudioClip>("Assets/Pedro.mp3");
            
            bepInExLogSource.LogMessage(modGUID + " has loaded succesfully.");

            harmony.PatchAll(typeof(TomScreamMod));
            harmony.PatchAll(typeof(JesterPedroMod));
        }
    }

    [HarmonyPatch(typeof(MouthDogAI))]
    [HarmonyPatch("Update")]
    class TomScreamMod
    {
        [HarmonyPostfix]
        static void Postfix(ref MouthDogAI __instance)
        {
            __instance.screamSFX = MonklePackModBase.TomScream;
        }
    }
    
    [HarmonyPatch(typeof(JesterAI))]
    [HarmonyPatch("Update")]
    class JesterPedroMod
    {
        [HarmonyPostfix]
        static void Postfix(ref JesterAI __instance)
        {
            __instance.screamingSFX = MonklePackModBase.Pedro;
        }
    }
}