using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using System.Xml.Linq;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(ItemComponent))]
    static class _ItemComponent
    {
        private static readonly ContentXElement RadItemStatus = new(PMMod.CntPkg, XElement.Parse($"<StatusEffect type=\"\" target=\"NearbyItems\" range=\"\" interval=\"1\" condition=\"\"  disabledeltatime=\"true\" tags=\"bp_rad\" TargetIdentifiers=\"proc_bloodpack,raw_bloodpack,alienblood,organ\"/>"));


        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(Item), typeof(ContentXElement) })]
        public static void PostCtor(ItemComponent __instance, Item item)
        {
            foreach ((ActionType at, List<StatusEffect> lse) in __instance.statusEffectLists ?? new Dictionary<ActionType, List<StatusEffect>>(0))
            {
                byte l = (byte)lse.Count;
                for (byte i = 0; i < l; ++i)
                {
                    var se = lse[i];
                    Affliction rad = null;
                    if (se.targetTypes == StatusEffect.TargetType.NearbyCharacters && (rad = se.Afflictions.Find(static aff => aff.Identifier == "radiationsickness")) is not null)
                    {
                        RadItemStatus.SetAttributeValue("type", at.ToString());
                        RadItemStatus.SetAttributeValue("range", se.Range.ToString());
                        RadItemStatus.SetAttributeValue("condition", (Math.Sqrt(rad.Strength) * -1).ToString());
                        __instance.statusEffectLists.addSE(new StatusEffect(RadItemStatus, item.Name), "bp_rad");
                    }
                }
            }
        }
    }
}