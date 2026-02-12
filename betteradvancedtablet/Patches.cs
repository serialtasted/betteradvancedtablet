using System;
using System.Reflection;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using BetterAdvancedTablet;
using HarmonyLib;
using UnityEngine;
using static BetterAdvancedTablet.BetterAdvancedTabletPlugin;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace BetterAdvancedTablet
{
    public class Patches
    {
        [HarmonyPatch(typeof(AdvancedTablet))]
        public class AdvancedTabletPatches
        {
            /// <summary>
            /// ***********POSTFIX PATCH********************
            /// Attempt to fix the issue with the Advanced Tablet not
            /// properly remembering the last cartridge selected on load
            /// </summary>
            /// <param name="__instance">The calling instance</param>
            [HarmonyPostfix]
            [HarmonyPatch(nameof(AdvancedTablet.DeserializeSave), new Type[] { typeof(ThingSaveData) })]
            public static void DeserializeSavePatch(AdvancedTablet __instance)
            {
                Traverse.Create(__instance).Method("GetCartridge").GetValue();
            }

            /// <summary>
            /// Alter next and prev buttons to skip empty cartridge slots.
            /// </summary>
            /// <param name="__instance"></param>
            /// <param name="__result"></param>
            /// <param name="___currentCartSlot"></param>
            /// <param name="interactable"></param>
            /// <param name="interaction"></param>
            /// <param name="doAction"></param>
            /// <returns></returns>
            [HarmonyPrefix]
            [HarmonyPatch(nameof(AdvancedTablet.InteractWith))]
            public static bool InteractWithPrefix(
                AdvancedTablet __instance,
                Interactable interactable,
                Interaction interaction,
                bool doAction,
                ref Thing.DelayedActionInstance __result)
            {
                Debug.Log($"[BAT] InteractWithPrefix called. Action={interactable.Action} ModeBefore={__instance.Mode}");

                if (!doAction)
                    return true;

                if (interactable.Action != InteractableType.Button1 &&
                    interactable.Action != InteractableType.Button2)
                    return true;

                int slotCount = __instance.CartridgeSlots.Count;
                if (slotCount <= 1)
                    return true;

                int currentMode = __instance.Mode;

                for (int i = 1; i <= slotCount; i++)
                {
                    int index = interactable.Action == InteractableType.Button1
                        ? (currentMode + i) % slotCount
                        : (currentMode - i + slotCount) % slotCount;

                    if (!__instance.CartridgeSlots[index].IsEmpty())
                    {
                        Debug.Log($"[BAT] Setting Mode to {index}");
                        __instance.Mode = index;
                        break;
                    }
                }

                Debug.Log($"[BAT] InteractWithPrefix end. ModeAfter={__instance.Mode}");
                __result = Thing.DelayedActionInstance.Success("Set");
                return false;
            }

            


        }

        public static int hasRun = 0;

        /// <summary>
        /// Alters ItemAdvancedTablet.Slots list, adding 2 cartridge slots.
        /// Also enables OnPrimaryUse for the advanced tablet by
        /// setting AllowSelfUse to true
        /// </summary>
        public static void AdvancedTabletPrefabPatch()
        {
            if (hasRun != 0)
                return;

            int i = 0;
            if (DebugMode) Debug.Log($"{PluginInfo.PLUGIN_NAME}: looking up ItemAdvancedTablet");
            var ItemAdvancedTablet = Prefab.Find("ItemAdvancedTablet");
            if (ItemAdvancedTablet == null)
            {
                if (DebugMode) Debug.Log($"{PluginInfo.PLUGIN_NAME}: Failed to find ItemAdvancedTablet");
                return;
            }
            if (ItemAdvancedTablet.Slots == null)
            {
                if (DebugMode) Debug.Log($"{PluginInfo.PLUGIN_NAME} ItemAdvancedTablet.Slots is null");
                return;
            }
            else
            {
                if (DebugMode) Debug.Log($"{PluginInfo.PLUGIN_NAME} ItemAdvancedTablet.Slots.count: {ItemAdvancedTablet.Slots.Count}");
            }

            //get the first cartridge slot
            var CartridgeSlot = ItemAdvancedTablet.Slots[1];

            var slotsToAdd = Math.Min(TabletSlots, 6);
            if (TabletSlots > 6 && DebugMode)
            {
                Debug.Log($"{PluginInfo.PLUGIN_NAME}: Requested {TabletSlots} extra slots but only 6 are supported. Clamping to 6 extra slots.");
            }

            //create slots dynamically (up to 6 extra slots)
            for (int countSlots = 0; countSlots < slotsToAdd; countSlots++)
            {
                Slot newSlot = new Slot();
                newSlot.Type = CartridgeSlot.Type;
                newSlot.IsHiddenInSeat = CartridgeSlot.IsHiddenInSeat;
                newSlot.HidesOccupant = CartridgeSlot.HidesOccupant;
                newSlot.Interactable = CartridgeSlot.Interactable;
                newSlot.IsInteractable = CartridgeSlot.IsInteractable;
                newSlot.IsSwappable = CartridgeSlot.IsSwappable;
                newSlot.OccupantCastsShadows = CartridgeSlot.OccupantCastsShadows;

                ItemAdvancedTablet.Slots.Add(newSlot);
            }

            (ItemAdvancedTablet as AdvancedTablet).AllowSelfUse = true;

            //no need to alter DynamicThing.DynamicThingPrefabs, as it is modified with the above code.

            //Only patch once.
            hasRun = 1;
            if (DebugMode)
                foreach (var slot in ItemAdvancedTablet.Slots)
                {
                    if (slot == null)
                    {
                        Debug.Log($"{PluginInfo.PLUGIN_NAME} slot({i}) is null");
                        continue;
                    }
                    Debug.Log($"{PluginInfo.PLUGIN_NAME} slot({i}): {slot.Type}");
                    i++;
                }

        }

        /* Disable the old patch but don't remove it
		[HarmonyPatch(typeof(World), "NewOrContinue")]
        public class MainMenuWindowManagerPatches
        {
            /// <summary>
            /// Injects a call to AdvancedTabletPrefabPatch when world is loaded or created.
            /// </summary>
            public static void Prefix()
            {
                AdvancedTabletPrefabPatch();
            }
        }
        */

        [HarmonyPatch(typeof(World), "StartNewWorld")]
        public class StartNewWorldPatches
        {
            /// <summary>
            /// Injects a call to AdvancedTabletPrefabPatch when world is loaded or created.
            /// </summary>
            public static void Prefix()
            {
                AdvancedTabletPrefabPatch();
            }
        }
        
        [HarmonyPatch(typeof(XmlSaveLoad), "LoadWorld")]
        public class LoadWorldPatches
        {
            /// <summary>
            /// Injects a call to AdvancedTabletPrefabPatch when world is loaded or created.
            /// </summary>
            public static void Prefix()
            {
                AdvancedTabletPrefabPatch();
            }
        }
        [HarmonyPatch(typeof(NetworkClient), "ProcessJoinData")]
        public class NetworkClientProcessJoinDataPatches
        {
            /// <summary>
            /// Injects a call to AdvancedTabletPrefabPatch when connecting to server.
            /// </summary>
            public static void Prefix()
            {
                AdvancedTabletPrefabPatch();
            }
        }
        
        public class AsyncMethods
        {
            public static bool MouseDown = false;
            /// <summary>
            /// Sends an InteractWith InteractButton1 to advancedTablet on left click
            /// or a InteractWith InteractButton2 on left click + QuantityModifier key
            /// </summary>
            /// <param name="advancedTablet"></param>
            /// <returns></returns>
            public static async UniTaskVoid NextCartridge(AdvancedTablet advancedTablet)
            {
                Debug.Log($"[BAT] NextCartridge START. ModeBefore={advancedTablet.Mode}");

                if (DebugMode) Debug.Log($"{PluginInfo.PLUGIN_NAME}:NextCartridge mouse down");
                if (MouseDown)
                    return;
                MouseDown = true;
                Interaction interaction = new Interaction((Assets.Scripts.Objects.Thing)InventoryManager.Parent, InventoryManager.ActiveHandSlot, CursorManager.CursorThing, KeyManager.GetButton(KeyMap.QuantityModifier));
                if (KeyManager.GetButton(KeyMap.QuantityModifier))
                {
                    Debug.Log("[BAT] Calling InteractButton2");
                    advancedTablet.InteractWith(advancedTablet.InteractButton2, interaction);
                }
                else
                {
                    Debug.Log("[BAT] Calling InteractButton1");
                    advancedTablet.InteractWith(advancedTablet.InteractButton1, interaction);
                }

                while (KeyManager.GetMouse("Primary") && !KeyManager.GetButton(KeyMap.SwapHands) && advancedTablet.OnOff && advancedTablet.Powered)
                    await UniTask.NextFrame();
                    Debug.Log($"[BAT] NextCartridge END. ModeAfter={advancedTablet.Mode}");

                MouseDown = false;
                if (DebugMode) Debug.Log($"{PluginInfo.PLUGIN_NAME}:NextCartridge mouse up");
            }
        }

        [UsedImplicitly]
        [HarmonyPatch(typeof(Item), nameof(Item.OnUsePrimary))]
        public class OnUsePrimaryPatch
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="__instance"></param>
            /// <param name="targetLocation"></param>
            /// <param name="targetRotation"></param>
            /// <param name="steamId"></param>
            /// <param name="authoringMode"></param>
            /// <returns></returns>
            public static bool Prefix(Item __instance,
                                    Vector3 targetLocation,
                                    Quaternion targetRotation,
                                    ulong steamId,
                                    bool authoringMode)
            {
                Debug.Log("[BAT] OnUsePrimary triggered");


                if (DebugMode) Debug.Log($"{PluginInfo.PLUGIN_NAME}:OnUsePrimaryPatch called Item.OnUsePrimary");

                if (!ClickForNextSlot)
                {
                    if (DebugMode) Debug.Log($"{PluginInfo.PLUGIN_NAME}:OnUsePrimaryPatch ClickForNextSlot is disabled");
                    return true;
                }

                var advancedTablet = __instance as AdvancedTablet;
                if (!(bool)(UnityEngine.Object)advancedTablet)
                {
                    if (DebugMode) Debug.Log($"{PluginInfo.PLUGIN_NAME}:OnUsePrimaryPatch __instance is not AdvancedTablet");
                    return true;
                }
                if (!advancedTablet.OnOff || !advancedTablet.Powered)
                    return true;
                if (DebugMode) Debug.Log($"{PluginInfo.PLUGIN_NAME}: calling NextCartridge");
                Debug.Log("[BAT] About to call NextCartridge");
                AsyncMethods.NextCartridge(advancedTablet).Forget();
                return true;
            }
        }
    }
}
