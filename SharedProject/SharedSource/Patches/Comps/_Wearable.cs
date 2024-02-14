using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using System.Collections.Immutable;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(Wearable))]
    static class _Wearable
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(Item), typeof(ContentXElement) })]
        public static void PostCtor(ItemComponent __instance)
        {
            static bool decO2(ImmutableArray<(Identifier propertyName, object value)> pe)
            {
                byte ct = 0;
                foreach (var (propertyName, value) in pe)
                    if (propertyName == "OxygenAvailable" && value is float oa)
                    {
                        if (oa <= 0f)
                            ++ct;
                    }
                    else if (propertyName == "Oxygen" && value is float od)
                        if (od <= 0f)
                            ++ct;
                return ct > 1;
            }
            static bool requires(in Identifier name, List<RelatedItem> ris)
            {
                if (ris is not null)
                    foreach (RelatedItem ri in ris)
                        if (ri.Type == RelatedItem.RelationType.Contained && ri.Identifiers.Contains(name))
                            return true;
                return false;
            }
            if (__instance.statusEffectLists is not null && __instance.statusEffectLists.TryGetValue(ActionType.OnWearing, out List<StatusEffect> l))
            {
                foreach (var se in l)
                    if (decO2(se.PropertyEffects))
                        if (requires("weldingfueltank", se.requiredItems))
                        {
                            se.Afflictions.RemoveAll(static a => a.Prefab.AfflictionType == "burn");
                            se.Afflictions.Add(new Affs.LungDmg(Utils.LUNG_DMG_PFB, 0.2f));
                        }
                        else if (requires("incendiumfueltank", se.requiredItems))
                        {
                            se.Afflictions.RemoveAll(static a => a.Prefab.AfflictionType == "burn");
                            se.Afflictions.Add(new Affs.LungDmg(Utils.LUNG_DMG_PFB, 1.25f));
                        }
            }
        }
    }
}
