
using BepInEx;
using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;

namespace BetterAdvancedTablet
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class BetterAdvancedTabletPlugin : BaseUnityPlugin
    {
        private ConfigEntry<int> configTabletSlots;
        private ConfigEntry<bool> configDebugMode;

        public static int TabletSlots;
        public static bool DebugMode;

        public static void ModLog(string text)
        {
            Debug.Log($"{PluginInfo.PLUGIN_NAME}: " + text);
        }

        private void Awake()
        {
            // Plugin startup logic
            HandleConfig();
            Patch();
        }

        private void Patch()
        {
            Debug.Log($"Plugin {PluginInfo.PLUGIN_NAME} {PluginInfo.PLUGIN_VERSION} is loaded!");
            var harmony = new Harmony($"{PluginInfo.PLUGIN_GUID}");
			
            AccessTools.GetTypesFromAssembly(typeof(Patches).Assembly).Do(type =>
            {
                try
                {
					Debug.Log($"Patching {type.FullName}");
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            });
            Debug.Log($"{PluginInfo.PLUGIN_NAME} Patching complete!");
        }

        public void OnLoad()
        {
            Patch();
        }

        public void OnUnload()
        {
            Debug.Log($"{PluginInfo.PLUGIN_NAME} bye!"); ;
        }

        void HandleConfig()
        {
            configTabletSlots = Config.Bind("General",   // The section under which the option is shown
                                     "AdvancedTabletSlots",  // The key of the configuration option in the configuration file
                                     2, // The default value
                                     "Number of slots to add on the Advanced Tablet.\nVanilla has 2 already. You can add up to 6 extra slots for a total of 8 slots.\nCAUTION! Removing slots on a already created world with more slots will crash the game."); // Description of the option to show in the config file
            TabletSlots = configTabletSlots.Value;

            configDebugMode = Config.Bind("Debug",   // The section under which the option is shown
                                     "DebugMode",  // The key of the configuration option in the configuration file
                                     false, // The default value
                                     "Turns debug mode"); // Description of the option to show in the config file
            DebugMode = configDebugMode.Value;
        }
    }
}
