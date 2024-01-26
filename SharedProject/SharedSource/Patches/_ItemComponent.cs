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
        public static void _ctor(ItemComponent __instance, Item item)
        {
            if (item.HasTag("syringe"))
            {
                void addSE(StatusEffect se, string tag)
                {
                    if (__instance.statusEffectLists.TryGetValue(se.type, out List<StatusEffect> l))
                    {
                        /*this method somehow gets called multiple times for the same item.
                         * I suspect that the reason is of follows:
                         * the first time is when the meleeweapon or holdable component gets created,
                         * the second time is when the projectile component copies everything from meleeweapon by having `inheritstatuseffectsfrom="MeleeWeapon"`.
                         */
                        if (l.Find(i => i.HasTag(tag)) is null)
                            l.Add(se);
                    }
                    else
                        __instance.statusEffectLists[se.type] = new List<StatusEffect>() { se };

                }
                void LoadStatusEffect(ContentXElement ele, string tag, FastSE.CondAffStrg cond)
                {
                    ele.SetAttributeValue("tags", tag);
                    ele.SetAttributeValue("type", "OnSuccess");
                    var se = new FastSE(ele, item.Name, cond);
                    addSE(se, tag);
                    ele.SetAttributeValue("type", "OnFailure");
                    se = new FastSE(ele, item.Name, cond);
                    addSE(se, tag);
                }
                static float findStrg(Character user, IReadOnlyList<ISerializableEntity> targets)
                {
                    if (user is not null)
                    {
                        if (ReferenceEquals(user, Unsafe.As<Limb>(targets[0]).character))
                            if (user.HasTalent("selfcare"))
                                return 0f;
                        if (user.HasTalent("deliverysystem"))
                            return 0.5f;
                    }
                    return 1.25f;
                }
                LoadStatusEffect(PierceStatus, "syringe_pierce", (a, b) => findStrg(a, b));
            }
        }
    }
}