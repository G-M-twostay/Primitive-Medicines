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
                ProcHemolysisStatus = new(PMMod.CntPkg, XElement.Parse($"<StatusEffect tags=\"proc_bp_type\" type=\"OnUse\" target=\"UseTarget\"  duration=\"5\" >          <Affliction identifier=\"hemolysis\" amount=\"4.025\" />       </StatusEffect>"));
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(Item), typeof(ContentXElement) })]
        public static void PostCtor(ItemComponent __instance, Item item)
        {
            if (item.HasTag("raw_bloodpack"))
            {
                bool findStrg(FastSE _0, Entity _1, IReadOnlyList<ISerializableEntity> targets)
                {
                    var ch = Unsafe.As<Character>(targets[0]).CharacterHealth;
                    return !Utils.bloodTypeCompat(item.Prefab.Identifier.Value.Remove(0, 9), Utils.FindBloodType(ch.afflictions).Code);
                }
                __instance.statusEffectLists.addSE(new FastSE(RawHemolysisStatus, item.Name, findStrg), "raw_bp_type");
            }
            else if (item.HasTag("proc_bloodpack"))
            {
                bool findStrg(FastSE _0, Entity _1, IReadOnlyList<ISerializableEntity> targets)
                {
                    var ch = Unsafe.As<Character>(targets[0]).CharacterHealth;
                    return !Utils.bloodTypeCompat(item.Prefab.Identifier.Value.Remove(0, 10), Utils.FindBloodType(ch.afflictions).Code);
                }
                __instance.statusEffectLists.addSE(new FastSE(ProcHemolysisStatus, item.Name, findStrg), "proc_bp_type");
            }
        }
    }
}
