using HarmonyLib;
using Barotrauma;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(Job))]
    static class _Job
    {
        [HarmonyPostfix]
        [HarmonyPatch("GiveJobItems",new Type[] {typeof(Character),typeof(WayPoint)})]
        public static void _GiveJobItems(Character character, WayPoint spawnPoint)
        {
            var affs = character.CharacterHealth.afflictions;
            affs.Add(Affs.BloodType.chooseType(), null);
        }
    }
}