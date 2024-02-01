using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using PrimMed.Replace;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(ItemComponent))]
    static class _ItemComponent
    {
        private static readonly ContentXElement PierceStatus = new(PMMod.CntPkg, XElement.Parse($"<StatusEffect tags=\"\" type=\"\" target=\"Limb\"  disabledeltatime=\"true\" stack=\"false\">          <Affliction identifier=\"pierce\" amount=\"1\" />        </StatusEffect>"));


        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(Item), typeof(ContentXElement) })]
        public static void PostCtor(ItemComponent __instance, Item item)
        {
            void LoadStatusEffect(ContentXElement ele, string tag, FastSE.FuncCond cond)
            {
                ele.SetAttributeValue("tags", tag);
                ele.SetAttributeValue("type", "OnSuccess");
                var se = new FastSE(ele, item.Name, cond);
                __instance.statusEffectLists.addSE(se, tag);
                ele.SetAttributeValue("type", "OnFailure");
                se = new FastSE(ele, item.Name, cond);
                __instance.statusEffectLists.addSE(se, tag);
            }
            if (item.HasTag("syringe"))
            {
                static bool findStrg(FastSE se, Entity _, IReadOnlyList<ISerializableEntity> targets)
                {
                    float strg;
                    if (se.user is not null)
                    {
                        if (ReferenceEquals(se.user, Unsafe.As<Limb>(targets[0]).character))
                            if (se.user.HasTalent("selfcare"))
                                return false;
                        if (se.user.HasTalent("deliverysystem"))
                            strg = 0.5f;
                    }
                    strg = 1.25f;
                    se.Afflictions[0].SetStrength(strg);
                    return true;
                }
                LoadStatusEffect(PierceStatus, "syringe_pierce", findStrg);
            }
        }
    }
}