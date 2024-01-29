using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using System.Xml.Linq;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(ItemContainer))]
    static class _ItemContainer
    {
        private static readonly ContentXElement TypedBloodPackTags = new(PMMod.CntPkg, XElement.Parse("<Containable items=\"raw_bloodpack,proc_bloodpack\" />")),
            MedicalGasTags = new(PMMod.CntPkg, XElement.Parse("<Containable items=\"medicalgas\" />")),
            N2OGasSE = new(PMMod.CntPkg, XElement.Parse("      <StatusEffect type=\"OnWearing\" tags=\"use_n2o\" target=\"Contained,Character\" OxygenAvailable=\"1000.0\" Condition=\"-1\" comparison=\"And\">\r\n        <Conditional IsDead=\"false\" />\r\n        <RequiredItem items=\"anestheticgas0\" type=\"Contained\" />\r\n        <Affliction identifier=\"anesthetized\" amount=\"30\" />\r\n        <Affliction identifier=\"opiateaddiction\" amount=\"0.65\" />\r\n        <ReduceAffliction identifier=\"opiatewithdrawal\" amount=\"1\" />\r\n      </StatusEffect>")),
            DsFGasSE = new(PMMod.CntPkg, XElement.Parse("      <StatusEffect type=\"OnWearing\" tags=\"use_desflurane\" target=\"Contained,Character\" OxygenAvailable=\"1000.0\" Condition=\"-1\" comparison=\"And\">\r\n        <Conditional IsDead=\"false\" />\r\n        <RequiredItem items=\"anestheticgas1\" type=\"Contained\" />\r\n        <Affliction identifier=\"anesthetized\" amount=\"70\" />\r\n        <Affliction identifier=\"opiateoverdose\" amount=\"2.5\" />\r\n      </StatusEffect>")),
            OpiGasSE = new(PMMod.CntPkg, XElement.Parse("      <StatusEffect type=\"OnWearing\" tags=\"use_opiumgas\" target=\"Contained,Character\" OxygenAvailable=\"1000.0\" Condition=\"-1\" comparison=\"And\">\r\n        <Conditional IsDead=\"false\" />\r\n        <RequiredItem items=\"analgesicgas0\" type=\"Contained\" />\r\n        <Affliction identifier=\"analgesicshort\" amount=\"15\" />\r\n        <Affliction identifier=\"opiateaddiction\" amount=\"0.175\" />\r\n        <Affliction identifier=\"opiateoverdose\" amount=\"0.625\" />\r\n        <ReduceAffliction identifier=\"opiatewithdrawal\" amount=\"0.25\" />\r\n      </StatusEffect>"));

        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(Item), typeof(ContentXElement) })]
        public static void PreCtor(Item item, ContentXElement element)
        {
            if ((item.Prefab.Category & (MapEntityCategory.Diving | MapEntityCategory.Equipment)) == (MapEntityCategory.Diving | MapEntityCategory.Equipment) && item.HasTag("diving"))
            {
                foreach (var subEle in element.Elements())
                    if (subEle.Name.ToString().ToLowerInvariant() == "containable")
                        if ((new RelatedItem(subEle, null)).Identifiers.Contains("oxygensource".ToIdentifier()))//breathe oxygen means being able to breathe medical gases.
                        {
                            Wearable cloth = item.GetComponent<Wearable>();
                            if (cloth is not null)
                            {
                                element.Add(MedicalGasTags);
                                cloth.statusEffectLists.addSE(new StatusEffect(N2OGasSE, item.Name), "use_n2o");
                                cloth.statusEffectLists.addSE(new StatusEffect(DsFGasSE, item.Name), "use_desflurane");
                                cloth.statusEffectLists.addSE(new StatusEffect(OpiGasSE, item.Name), "use_opiumgas");
                                break;
                            }
                        }

            }

            if (element.GetAttributeBool("autoinject", false))
            {
                foreach (var subEle in element.Elements())
                    if (subEle.Name.ToString().ToLowerInvariant() == "containable")
                        if ((new RelatedItem(subEle, null)).Identifiers.Contains("chem".ToIdentifier()))//vanilla blood pack is chem
                        {
                            element.Add(TypedBloodPackTags);
                            break;
                        }

            }
        }
    }
}