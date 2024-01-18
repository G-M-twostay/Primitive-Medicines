using Barotrauma;
using HarmonyLib;
using System.Xml.Linq;
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

        private static float rmvBdgProb(in Identifier id) => id.Value switch
        {
            "lacerations" => 0.7f,
            "bitewounds" => 0.9f,
            "blunttrauma" => 0.2f,
            "gunshotwound" => 0.9f,
            "explosiondamage" => 0.8f,
            "burn" => 0.6f,
            "acidburn" => 0.95f,
            "pierce" => 0.1f,
            _ => 0f
        };
        private const float STEP = 3f;
        [HarmonyPrefix]
        [HarmonyPatch("AddLimbAffliction", new Type[] { typeof(Limb), typeof(Affliction), typeof(bool) })]
        public static bool _AddLimbAffliction(CharacterHealth __instance, Limb limb, Affliction newAffliction, bool allowStacking)
        {
            if (newAffliction.Prefab.LimbSpecific && limb is not null)
            {
                //if (limb.HealthIndex < 0 || limb.HealthIndex >= limbHealths.Count)
                var lhs = __instance.limbHealths;
                if (__instance.Character.IsHuman)
                    foreach ((Affliction aff, CharacterHealth.LimbHealth lh) in __instance.afflictions)
                        if (aff is Affs.Bandaged)
                            if (lh == lhs[limb.HealthIndex])
                            {
                                float prob = rmvBdgProb(newAffliction.Identifier);
                                if (prob > 0f)
                                    if (Rand.Value(Rand.RandSync.ServerAndClient) < prob)
                                    {
                                        float dec = aff.Prefab.MaxStrength / STEP;
                                        aff.SetStrength(Math.Max(0f, aff.Strength - dec));
                                        __instance.addLimbAffFast(lh, Utils.SCAR_PFB, dec, newAffliction.Source, true, true);
                                    }
                                break;
                            }


                __instance.AddLimbAffliction(lhs[limb.HealthIndex], newAffliction, allowStacking);
            }
            return false;
        }
        [HarmonyPostfix]
        [HarmonyPatch("GetStatValue", new Type[] { typeof(StatTypes) })]
        public static void _GetStatValue(CharacterHealth __instance, ref float __result, in StatTypes statType)
        {
            /*this method is called from Character.GetStatValue, which contains a IsHuman check, so we don't have to do that here.*/
            float mod;
            switch (statType)
            {
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
                        Limb leftArm = __instance.Character.AnimController.GetLimb(LimbType.LeftArm), rightArm = __instance.Character.AnimController.GetLimb(LimbType.RightArm), torso = __instance.Character.AnimController.GetLimb(LimbType.Torso);
                        mod = (Utils.getLimbPain(leftArm, __instance) + Utils.getLimbPain(rightArm, __instance)) / 2f;
                    }
                    break;
                case StatTypes.MeleeAttackSpeed:
                    {
                        Limb leftArm = __instance.Character.AnimController.GetLimb(LimbType.LeftArm), rightArm = __instance.Character.AnimController.GetLimb(LimbType.RightArm);
                        mod = Math.Max(0f, Utils.getLimbPain(leftArm, __instance) + Utils.getLimbPain(rightArm, __instance) - 10f) / 2f;
                    }
                    break;
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
        [HarmonyPostfix]
        [HarmonyPatch("Load", new Type[] { typeof(XElement), typeof(Func<AfflictionPrefab, bool>) })]
        public static void _Load(CharacterHealth __instance, XElement element, Func<AfflictionPrefab, bool> afflictionPredicate)
        {//ensure blood type on character load(backward compatibility) and respawn.
            if (__instance.Character.IsHuman)
            {
                var affs = __instance.afflictions;
                bool hasBloodType = false;
                foreach (Affliction aff in affs.Keys)
                    if (aff is Affs.BloodType)
                        if (hasBloodType)
                            affs.Remove(aff);
                        else
                            hasBloodType = true;
                if (!hasBloodType)
                    affs.Add(Affs.BloodType.chooseType(), null);
            }
        }
    }
}