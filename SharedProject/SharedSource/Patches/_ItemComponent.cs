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
        private static readonly ContentXElement PierceStatus = new(PMMod.CntPkg, XElement.Parse($"<StatusEffect tags=\"\" type=\"\" target=\"Limb\"  disabledeltatime=\"true\" stack=\"false\">          <Affliction identifier=\"pierce\" amount=\"1\" />        </StatusEffect>")),
                    RawHemolysisStatus = new(PMMod.CntPkg, XElement.Parse($"<StatusEffect tags=\"\" type=\"\" target=\"UseTarget\"  duration=\"9\" >          <Affliction identifier=\"hemolysis\" amount=\"4.025\" />        </StatusEffect>")),
            ProcHemolysisStatus = new(PMMod.CntPkg, XElement.Parse($"<StatusEffect tags=\"\" type=\"\" target=\"UseTarget\"  duration=\"5\" >          <Affliction identifier=\"hemolysis\" amount=\"4.025\" />       </StatusEffect>"));

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(Item), typeof(ContentXElement) })]
        public static void PostCtor(ItemComponent __instance, Item item)
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
            void LoadStatusEffect(ContentXElement ele, string tag, FastSE.FuncCond cond)
            {
                ele.SetAttributeValue("tags", tag);
                ele.SetAttributeValue("type", "OnSuccess");
                var se = new FastSE(ele, item.Name, cond);
                addSE(se, tag);
                ele.SetAttributeValue("type", "OnFailure");
                se = new FastSE(ele, item.Name, cond);
                addSE(se, tag);
            }
            if (item.HasTag("syringe"))
            {
                static bool findStrg(FastSE se, IReadOnlyList<ISerializableEntity> targets)
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
            else if (item.HasTag("raw_bloodpack"))
            {
                bool findStrg(FastSE _, IReadOnlyList<ISerializableEntity> targets)
                {
                    Character c = Unsafe.As<Character>(targets[0]);
                    foreach (var aff in c.CharacterHealth.afflictions.Keys)
                        if (aff is Affs.BloodType)
                            return !Utils.bloodTypeCompat(item.Prefab.Identifier.Value.Remove(0, 9), aff.Identifier.Value.Remove(0, 5));
                    return true;
                }
                LoadStatusEffect(RawHemolysisStatus, "raw_bp_type", findStrg);
            }
            else if (item.HasTag("proc_bloodpack"))
            {
                bool findStrg(FastSE _, IReadOnlyList<ISerializableEntity> targets)
                {
                    Character c = Unsafe.As<Character>(targets[0]);
                    foreach (var aff in c.CharacterHealth.afflictions.Keys)
                        if (aff is Affs.BloodType)
                            return !Utils.bloodTypeCompat(item.Prefab.Identifier.Value.Remove(0, 10), aff.Identifier.Value.Remove(0, 5));
                    return true;
                }
                LoadStatusEffect(ProcHemolysisStatus, "proc_bp_type", findStrg);
            }
        }
    }
}