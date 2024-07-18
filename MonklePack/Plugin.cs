using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using JetBrains.Annotations;
using MemeSoundboard;
using UnityEngine;

namespace MonklePack
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class MonklePackModBase : BaseUnityPlugin
    {
        public const string ModGuid = "Monkle.VariousMods";
        public const string ModName = "MonklePack";
        public const string ModVersion = "1.0.0.2";

        private readonly Harmony _harmony = new Harmony(ModGuid);

        [CanBeNull] public static readonly List<AudioWeight> TomScreamClips = new List<AudioWeight>();
        [CanBeNull] public static AudioClip Pedro;
        [CanBeNull] private static MonklePackModBase _instance;

        public static readonly System.Random Rnd = new System.Random();

        private static ManualLogSource _monkleLogger;
        internal static MonkleConfig BoundConfig { get; private set; } = null;
        private static AssetBundle _itemAssetBundle;
        
        [CanBeNull] public static AudioClip[] RadioTracks;

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            var location = _instance.Info.Location;
            _monkleLogger = BepInEx.Logging.Logger.CreateLogSource(ModGuid);
            BoundConfig = new MonkleConfig(Config);

            LogMessage("location " + location);
            var bundleLocation = location.TrimEnd("MonklePack.dll".ToCharArray()) + "monklepack";
            _itemAssetBundle = AssetBundle.LoadFromFile(bundleLocation);

            var clips = _itemAssetBundle.LoadAllAssets<AudioClip>();

            RadioTracks = clips.Where(x => Regex.IsMatch(x.name, @"^\d")).OrderBy(_ => Guid.NewGuid()).ToArray();
            foreach (var track in RadioTracks)
            {
                LogMessage(track.name);
            }
            
            if (TomScreamClips != null)
            {
                TomScreamClips.Add(new AudioWeight
                {
                    Sound = clips.FirstOrDefault(x => x.name == "TomScream"),
                    Weight = 35
                });
                TomScreamClips.Add(new AudioWeight
                {
                    Sound = clips.FirstOrDefault(x => x.name == "TomScream2"),
                    Weight = 15
                });
                TomScreamClips.Add(new AudioWeight
                {
                    Sound = clips.FirstOrDefault(x => x.name == "UghSound"),
                    Weight = 10
                });
            }
            
            MemeSoundboardBase.AddNewSound("Four naan", clips.FirstOrDefault(x => x.name == "FourNaan"));
            MemeSoundboardBase.AddNewSound("Suspicious", clips.FirstOrDefault(x => x.name == "Suspicious"));
            MemeSoundboardBase.AddNewSound("Here comes the boi", clips.FirstOrDefault(x => x.name == "HereComesTheBoi"));
            MemeSoundboardBase.AddNewSound("Cheeky Monkey", clips.FirstOrDefault(x => x.name == "CheekyMonkey"));
            
            Pedro = clips.FirstOrDefault(x => x.name == "Pedro");
            
            _monkleLogger.LogMessage(ModGuid + " has loaded succesfully.");
            LogMessage("has loaded succesfully.");

            if(BoundConfig.EnableTomScream.Value)
                _harmony.PatchAll(typeof(TomScreamMod));
            if(BoundConfig.EnablePedro.Value)
                _harmony.PatchAll(typeof(JesterPedroMod));
            if(BoundConfig.EnableFear.Value)
                _harmony.PatchAll(typeof(PlayerScaredMod));
            if(BoundConfig.EnableCruiserTunes.Value)
                _harmony.PatchAll(typeof(CruiserMod));
        }

        public static void LogMessage(string message)
        {
            _monkleLogger.LogMessage($"{ModGuid} {message}");
        }
    }
    
        
    class MonkleConfig
    {
        public readonly ConfigEntry<float> FearSpeedMultiplier;
        public readonly ConfigEntry<bool> EnableTomScream;
        public readonly ConfigEntry<bool> EnablePedro;
        public readonly ConfigEntry<bool> EnableFear;
        public readonly ConfigEntry<bool> EnableCruiserTunes;

        public MonkleConfig(ConfigFile cfg)
        {
            cfg.SaveOnConfigSet = false; 

            FearSpeedMultiplier = cfg.Bind(
                "Sanity",
                "FearSpeedMultiplier",
                1f,
                "How much should you be sped up by insanity"
            );
            
            EnableTomScream = cfg.Bind(
                "General",
                "EnableTomScream",
                true,
                "Should dogs scream like Tom"
            );
            
            EnablePedro = cfg.Bind(
                "General",
                "EnablePedro",
                true,
                "Should pedro come out of the box"
            );
            
            EnableFear = cfg.Bind(
                "General",
                "EnableFear",
                false,
                "Should fear speed you up"
            );
            
            EnableCruiserTunes = cfg.Bind(
                "General",
                "EnableCruiserTunes",
                true,
                "Want some tuneskies"
            );
            
            ClearOrphanedEntries(cfg); 
            cfg.Save(); 
            cfg.SaveOnConfigSet = true; 
        }

        static void ClearOrphanedEntries(ConfigFile cfg) 
        { 
            var orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries"); 
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg); 
            orphanedEntries.Clear(); 
        } 
    }

    public class AudioWeight
    {
        public AudioClip Sound { get; set; }
        public int Weight { get; set; }
    }

    [HarmonyPatch(typeof(MouthDogAI))]
    [HarmonyPatch("Update")]
    class TomScreamMod
    {
        private static AudioClip originalScream = null;

        [HarmonyPostfix]
        static void Postfix(ref MouthDogAI __instance)
        {
            //Initialise scream
            if (originalScream == null)
            {
                originalScream = __instance.screamSFX;
                MonklePackModBase.LogMessage("Loaded original scream.");

                MonklePackModBase.TomScreamClips.Add(new AudioWeight
                {
                    Sound = originalScream,
                    Weight = 100 - MonklePackModBase.TomScreamClips.Sum(x => x.Weight)
                });
                
                var sumInit = 0;
                var randInit = MonklePackModBase.Rnd.Next(0, 100);
                foreach (var scream in MonklePackModBase.TomScreamClips.OrderBy(x => x.Weight))
                {
                    sumInit += scream.Weight;
                
                    if(sumInit - randInit < 0)
                        __instance.screamSFX = scream.Sound;
                }
            }

            if (__instance.currentBehaviourStateIndex != 3)
                return;

            var randInt = MonklePackModBase.Rnd.Next(0, 100);

            var sum = 0;
            foreach (var scream in MonklePackModBase.TomScreamClips.OrderBy(x => x.Weight))
            {
                sum += scream.Weight;
                
                if(sum - randInt < 0)
                    __instance.screamSFX = scream.Sound;
            }
        }
    }
    
    [HarmonyPatch(typeof(JesterAI))]
    [HarmonyPatch("Update")]
    class JesterPedroMod
    {
        [HarmonyPostfix]
        static void Postfix(ref JesterAI __instance)
        {
            if(MonklePackModBase.Pedro != null)
                __instance.screamingSFX = MonklePackModBase.Pedro;
        }
    }
    
    [HarmonyPatch(typeof(PlayerControllerB))]
    [HarmonyPatch("Update")]
    class PlayerScaredMod
    {
        private static float currentSanity = 0;
        private static float baseMovement = 0.5f;
        private static float baseSprint = 1.125f;
        private static float baseTargetFOV = 0.5f;
        private static float baseDrunkness = 0.5f;
        [HarmonyPostfix]
        static void Postfix(ref PlayerControllerB __instance)
        {
            //Only change once every 0.1 ticks - Max 50 insanity
            if (Math.Abs(currentSanity - __instance.insanityLevel) > 0.1)
            {
                currentSanity = __instance.insanityLevel;
                var sanityChanger = 1 + ((__instance.insanityLevel / 100) * MonklePackModBase.BoundConfig.FearSpeedMultiplier.Value);

                if (__instance.isSprinting)
                {
                    __instance.movementSpeed = baseSprint * sanityChanger;
                }
                else
                {
                    __instance.movementSpeed = baseMovement * sanityChanger;
                }
                MonklePackModBase.LogMessage($"Sanity Changed: {__instance.movementSpeed}");
                
                if (__instance.isSprinting)
                {
                    __instance.targetFOV = 68f * sanityChanger;
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(VehicleController))]
    [HarmonyPatch("Start")]
    class CruiserMod
    {
        [HarmonyPostfix]
        static void Postfix(ref VehicleController __instance)
        {
            __instance.radioClips = MonklePackModBase.RadioTracks;
        }
    }
    
    // [HarmonyPatch(typeof(ClaySurgeonAI))]
    // [HarmonyPatch("Update")]
    // class BarberMod
    // {
    //     [HarmonyPostfix]
    //     static void Postfix(ref ClaySurgeonAI __instance)
    //     {
    //         __instance.
    //     }
    // }
}