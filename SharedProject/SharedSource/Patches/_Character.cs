using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using Microsoft.Xna.Framework;
using System.Collections.Immutable;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(Character))]
    static class _Character
    {
        [HarmonyPrefix]
        [HarmonyPatch("GetSkillLevel", new Type[] { typeof(Identifier) })]
        public static bool _GetSkillLevel(Character __instance, ImmutableDictionary<Identifier, StatTypes> ___overrideStatTypes, ref/*only ref has default value*/ float __result, Identifier skillIdentifier)
        {
            //be careful of when skillIdentifier is null. This should be invalid and return 0f in the original implementation, but I'm not sure.   
            if (__instance.Info?.Job is not null && skillIdentifier != null)
            {
                float skillLevel = __instance.Info.Job.GetSkillLevel(skillIdentifier);

                if (___overrideStatTypes.TryGetValue(skillIdentifier, out StatTypes statType))
                {
                    float skillOverride = __instance.GetStatValue(statType);
                    if (skillOverride > skillLevel)
                        skillLevel = skillOverride;
                }

                /*
                These come from effects(like talents); pains affect skills gained through talents.
                Vanilla uses chain of multiplication to calculate the final value. However, with pain, 
                this value might get extreme. 
                */
                foreach (Affliction affliction in __instance.CharacterHealth.GetAllAfflictions())
                    skillLevel *= affliction.GetSkillMultiplier();

                skillLevel += __instance.GetStatValue(Character.GetSkillStatType(skillIdentifier));

                static float helmMod(in float leftLegPain, in float rightLegPain, in float curSkill)
                {
                    float total = leftLegPain + rightLegPain;
                    //linear interpolate between (0, 1) and (200,0.7)
                    return MathHelper.Lerp(1f, 0.7f, total / (2f * Utils.PAIN_PFB.MaxStrength));
                }
                static float weaponsMod(in float leftArmPain, in float rightArmPain, in float curSkill)
                {
                    const float leftArmWei = 0.9f;
                    //right arm weighs more.
                    float total = leftArmPain * leftArmWei + rightArmPain * (2f - leftArmWei);
                    //linear interpolate between (0, 1) and (200,0.7)
                    return MathHelper.Lerp(1f, 0.7f, total / (2f * Utils.PAIN_PFB.MaxStrength));
                }

                if (skillIdentifier == "helm")
                {//pains on legs hinders helming(footpad).
                    skillLevel *= helmMod(Utils.getLimbPain(__instance.AnimController.GetLimb(LimbType.LeftLeg), __instance.CharacterHealth), Utils.getLimbPain(__instance.AnimController.GetLimb(LimbType.RightLeg), __instance.CharacterHealth), skillLevel);
                }
                else if (skillIdentifier == "weapons")
                {//pains on arms hinders combat.
                    skillLevel *= weaponsMod(Utils.getLimbPain(__instance.AnimController.GetLimb(LimbType.LeftArm), __instance.CharacterHealth), Utils.getLimbPain(__instance.AnimController.GetLimb(LimbType.RightArm), __instance.CharacterHealth), skillLevel);
                }
                //pains don't affect skills gained through items.

                //there was a `if(skillIdentifier!=null)` statement here in the original implementation.
                foreach (Item item in __instance.Inventory.AllItems)
                    if (item?.GetComponent<Wearable>() is Wearable wearable && !__instance.Inventory.IsInLimbSlot(item, InvSlotType.Any))
                        foreach (var allowedSlot in wearable.AllowedSlots)
                            if (allowedSlot != InvSlotType.Any && __instance.Inventory.IsInLimbSlot(item, allowedSlot))
                                if (wearable.SkillModifiers.TryGetValue(skillIdentifier, out float skillValue))
                                {
                                    skillLevel += skillValue;
                                    break;
                                }

                __result = Math.Max(skillLevel, 0);
            }
            //__result defaults to 0
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch("CalculateMovementPenalty", new Type[] { typeof(Limb), typeof(float), typeof(float) })]
        public static bool _CalculateMovementPenalty(Character __instance, out float __result, Limb limb, float sum, float max)
        {
            if (limb is null)
                __result = sum;
            else
            {
                sum += MathHelper.Lerp(0, max, Utils.getLimbPain(limb, __instance.CharacterHealth) / Utils.PAIN_PFB.MaxStrength);
                __result = Math.Clamp(sum, 0, 1f);
            }
            return false;
        }
    }
}