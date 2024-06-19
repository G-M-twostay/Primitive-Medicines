using Barotrauma;
using HarmonyLib;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(CharacterHealth))]
    static class _CharacterHealth
    {
        [HarmonyPrefix]
        [HarmonyPatch("InitIrremovableAfflictions", new Type[0])]
        public static void _InitIrremovableAfflictions(CharacterHealth __instance)
        {
            if (__instance.Character.IsHuman)
                __instance.irremovableAfflictions.Add(new Affs.Mortal(AfflictionPrefab.Prefabs["mortal"], 1f));
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetStatValue", new Type[] { typeof(StatTypes) })]
        public static void _GetStatValue(CharacterHealth __instance, ref float __result, in StatTypes statType)
        {
            /*this method is called from Character.GetStatValue, which contains a IsHuman check, so we don't have to do that here.*/
            float mod;
            switch (statType)
            {
                case StatTypes.PropulsionSpeed:
                    {
                        Limb torso = __instance.Character.AnimController.GetLimb(LimbType.Torso);
                        mod = Utils.getLimbPain(torso, __instance) * 0.125f;
                    }
                    break;
                case StatTypes.RepairToolDeattachTimeMultiplier:
                case StatTypes.RepairToolStructureDamageMultiplier:
                case StatTypes.RepairToolStructureRepairMultiplier:
                case StatTypes.HoldBreathMultiplier:
                    {
                        Limb torso = __instance.Character.AnimController.GetLimb(LimbType.Torso);
                        mod = Utils.getLimbPain(torso, __instance) * 0.25f;
                    }
                    break;
                case StatTypes.FlowResistance:
                    {
                        Limb torso = __instance.Character.AnimController.GetLimb(LimbType.Torso);
                        mod = Utils.getLimbPain(torso, __instance);
                    }
                    break;
                case StatTypes.MeleeAttackMultiplier:
                    {
                        Limb leftArm = __instance.Character.AnimController.GetLimb(LimbType.LeftArm), rightArm = __instance.Character.AnimController.GetLimb(LimbType.RightArm);
                        mod = (Utils.getLimbPain(leftArm, __instance) + Utils.getLimbPain(rightArm, __instance)) / 2f;
                    }
                    break;
                case StatTypes.MeleeAttackSpeed:
                    {
                        Limb leftArm = __instance.Character.AnimController.GetLimb(LimbType.LeftArm), rightArm = __instance.Character.AnimController.GetLimb(LimbType.RightArm);
                        mod = Math.Max(0f, Utils.getLimbPain(leftArm, __instance) + Utils.getLimbPain(rightArm, __instance) - 10f) / 2f;
                    }
                    break;
                case StatTypes.ExperienceGainMultiplier:
                case StatTypes.SkillGainSpeed:
                    {
                        Limb head = __instance.Character.AnimController.GetLimb(LimbType.Head);
                        mod = Utils.getLimbPain(head, __instance);
                    }
                    break;
                default:
                    mod = 0f;
                    break;
            }
            __result *= 1f - mod / Utils.PAIN_PFB.MaxStrength;
        }
    }
}