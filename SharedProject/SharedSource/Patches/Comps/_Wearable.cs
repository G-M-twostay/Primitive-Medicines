using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using System.Collections.Immutable;
using System.Xml.Linq;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(Wearable))]
    static class _Wearable
    {
        private static readonly ContentXElement PIERCE_PROTECTION = new(PMMod.CntPkg, XElement.Parse("<damagemodifier armorsector=\"0.0,360.0\" afflictiontypes=\"pierce\" damagemultiplier=\"0.625\" damagesound=\"LimbArmor\" deflectprojectiles=\"true\" />"));
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(Item), typeof(ContentXElement) })]
        public static void PostCtor(Wearable __instance)
        {
            if (__instance.item.tags.IsSupersetOf(new Identifier[] { "deepdiving".ToIdentifier(), "deepdivinglarge".ToIdentifier(), "human".ToIdentifier() }))
            {
                foreach (DamageModifier dm in __instance.damageModifiers)
                    if (dm.ParsedAfflictionIdentifiers.Contains("huskinfection"))
                    {
                        if (dm.ProbabilityMultiplier < 0.2f)
                            dm.ProbabilityMultiplier = 0f;
                    }

                    else if (dm.parsedAfflictionIdentifiers.Contains("bitewounds"))
                    {
                        if (dm.ProbabilityMultiplier < 1)
                            dm.DamageMultiplier = Math.Max(dm.DamageMultiplier - 0.05f, 0);
                        dm.ProbabilityMultiplier = 1f;
                    }
                __instance.damageModifiers.Add(new DamageModifier(PIERCE_PROTECTION, __instance.item.Name, false));
            }
            if (__instance.statusEffectLists is not null && __instance.statusEffectLists.TryGetValue(ActionType.OnWearing, out List<StatusEffect> l))
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
