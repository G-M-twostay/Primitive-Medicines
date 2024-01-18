using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;

namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(Repairable))]
    static class _Repairable
    {
        private static float fixItemSpeedMod(in float pain, in float curSkill) => 1f - pain / Utils.PAIN_PFB.MaxStrength;
        [HarmonyPrefix]
        [HarmonyPatch("RepairDegreeOfSuccess", new Type[] { typeof(Character), typeof(List<Skill>) })]
        public static bool _RepairDegreeOfSuccess(Repairable __instance, out float __result, Character character, List<Skill> skills)
        {
            //the first 2 statements here are all immediate returns in vanilla.
            if (skills.Count == 0)
                __result = 1f;

            else if (character is null)
                __result = 0.0f;
            else
            {
                float pain;
                if (object.ReferenceEquals(character.Inventory.GetItemInLimbSlot(InvSlotType.RightHand), __instance.currentRepairItem))//if item is null and nothing is on hand, then we assume character use right hand to do stuff.
                    pain = Utils.getLimbPain(character.AnimController.GetLimb(LimbType.RightArm), character.CharacterHealth);
                else
                    pain = Utils.getLimbPain(character.AnimController.GetLimb(LimbType.LeftArm), character.CharacterHealth);

                float skillSum = 0f;
                foreach (Skill skillType in skills)
                {
                    float skillLevel = character.GetSkillLevel(skillType.Identifier);
                    float painMod = fixItemSpeedMod(pain, skillLevel);
                    skillSum += skillLevel * painMod - (skillType.Level * __instance.SkillRequirementMultiplier);
                }
                __result = (skillSum / skills.Count + 100.0f) / 200.0f;
            }
            return false;
        }
    }
}