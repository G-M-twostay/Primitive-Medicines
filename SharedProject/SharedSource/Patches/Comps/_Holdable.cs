using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using PrimMed.Replace;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(Holdable))]
    static class _Holdable
    {
        private static readonly ContentXElement RawHemolysisStatus = new(PMMod.CntPkg, XElement.Parse($"<StatusEffect tags=\"raw_bp_type\" type=\"OnUse\" target=\"UseTarget\"  duration=\"9\" >          <Affliction identifier=\"hemolysis\" amount=\"4.025\" />        </StatusEffect>")),
                ProcHemolysisStatus = new(PMMod.CntPkg, XElement.Parse($"<StatusEffect tags=\"proc_bp_type\" type=\"OnUse\" target=\"UseTarget\"  duration=\"5\" >          <Affliction identifier=\"hemolysis\" amount=\"4.025\" />       </StatusEffect>")),
            PierceStatus = new(PMMod.CntPkg, XElement.Parse($"<StatusEffect tags=\"\" type=\"\" target=\"Limb\"  disabledeltatime=\"true\" stack=\"false\">          <Affliction identifier=\"pierce\" amount=\"1\" />        </StatusEffect>"));
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(Item), typeof(ContentXElement) })]
        public static void PostCtor(ItemComponent __instance, Item item)
        {
            if (item.HasTag("raw_bloodpack"))
            {
                bool findHemoStrg(FastSE _0, Entity _1, IReadOnlyList<ISerializableEntity> targets)
                {
                    var ch = Unsafe.As<Character>(targets[0]).CharacterHealth;
                    return !Utils.bloodTypeCompat(item.Prefab.Identifier.Value.Remove(0, 9), Utils.FindBloodType(ch.afflictions).Code);
                }
                __instance.statusEffectLists.addSE(new FastSE(RawHemolysisStatus, item.Name, findHemoStrg), "raw_bp_type");
            }
            else if (item.HasTag("proc_bloodpack"))
            {
                bool findHemoStrg(FastSE _0, Entity _1, IReadOnlyList<ISerializableEntity> targets)
                {
                    var ch = Unsafe.As<Character>(targets[0]).CharacterHealth;
                    return !Utils.bloodTypeCompat(item.Prefab.Identifier.Value.Remove(0, 10), Utils.FindBloodType(ch.afflictions).Code);
                }
                __instance.statusEffectLists.addSE(new FastSE(ProcHemolysisStatus, item.Name, findHemoStrg), "proc_bp_type");
            }
            else if (item.HasTag("syringe"))
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
                static bool findPierceStrg(FastSE se, Entity _, IReadOnlyList<ISerializableEntity> targets)
                {
                    float strg = 1.25f;
                    if (se.user is not null)
                    {
                        if (ReferenceEquals(se.user, Unsafe.As<Limb>(targets.FirstOrDefault())?.character))
                            if (se.user.HasTalent("selfcare"))
                                return false;
                        if (se.user.HasTalent("deliverysystem"))
                            strg = 0.5f;
                    }
                    se.Afflictions[0].SetStrength(strg);
                    return true;
                }
                LoadStatusEffect(PierceStatus, "syringe_pierce", findPierceStrg);
            }
        }
    }
}
