using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(RangedWeapon))]
    static class _RangedWeapon
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetSpread", new Type[] { typeof(Character) })]
        public static void _GetSpread(RangedWeapon __instance, ref float __result, Character user)
        {
            byte count = 0;
            float pain = 0f;
            if (ReferenceEquals(user.Inventory.GetItemInLimbSlot(InvSlotType.RightHand), __instance.item))
            {
                count = 1;
                pain = Utils.getLimbPain(user.AnimController.GetLimb(LimbType.RightArm), user.CharacterHealth);
            }
            if (ReferenceEquals(user.Inventory.GetItemInLimbSlot(InvSlotType.LeftHand), __instance.item))
            {
                ++count;
                pain += Utils.getLimbPain(user.AnimController.GetLimb(LimbType.LeftArm), user.CharacterHealth);
            }
            if (count == 0)
                pain = 1f;
            else
                pain /= Utils.PAIN_PFB.MaxStrength * count;
            __result *= pain;
        }
    }
}