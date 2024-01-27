using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(ItemContainer))]
    static class _ItemContainer
    {
        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(Item), typeof(ContentXElement) })]
        public static void PreCtor(ContentXElement element)
        {
            if (element.GetAttributeBool("autoinject", false))
                foreach (var subEle in element.Elements())
                {
                    if (subEle.Name.ToString().ToLowerInvariant() == "containable")
                    {
                        var ids = (new RelatedItem(subEle, null)).Identifiers;
                        if (ids.Contains("chem".ToIdentifier()))//vanilla blood pack is chem
                        {
                            var newIds = new List<Identifier>(ids)
                            {
                                "raw_bloodpack".ToIdentifier(),
                                "proc_bloodpack".ToIdentifier()
                            };
                            subEle.SetAttributeValue("items", string.Join(",", newIds));
                        }
                    }
                }
        }
    }
}