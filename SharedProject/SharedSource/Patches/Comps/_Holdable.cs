using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using PrimMed.Replace;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(Holdable))]
    static class _Holdable
    {
        private static readonly AccessTools.FieldRef<AfflictionPrefab, float> AfflictionPrefab_MaxDuration_ = AccessTools.FieldRefAccess<AfflictionPrefab, float>(typeof(AfflictionPrefab).GetField("Duration", BindingFlags.Public | BindingFlags.SetField | BindingFlags.Instance | BindingFlags.ExactBinding));
        private static readonly ContentXElement RawHemolysisStatus = new(PMMod.CntPkg, XElement.Parse($"<StatusEffect tags=\"raw_bp_type\" type=\"OnUse\" target=\"UseTarget\"  duration=\"9\" >          <Affliction identifier=\"hemolysis\" amount=\"4.025\" />        </StatusEffect>")),
                ProcHemolysisStatus = new(PMMod.CntPkg, XElement.Parse($"<StatusEffect tags=\"proc_bp_type\" type=\"OnUse\" target=\"UseTarget\"  duration=\"5\" >          <Affliction identifier=\"hemolysis\" amount=\"4.025\" />       </StatusEffect>"));
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(Item), typeof(ContentXElement) })]
        public static void PreCtor(ItemComponent __instance, Item item)
        {
            if (item.HasTag("raw_bloodpack"))
            {
                bool findStrg(FastSE _0, Entity _1, IReadOnlyList<ISerializableEntity> targets)
                {
                    Character c = Unsafe.As<Character>(targets[0]);
                    foreach (var aff in c.CharacterHealth.afflictions.Keys)
                        if (aff is Affs.BloodType)
                            return !Utils.bloodTypeCompat(item.Prefab.Identifier.Value.Remove(0, 9), aff.Identifier.Value.Remove(0, 5));
                    return true;
                }
                __instance.statusEffectLists.addSE(new FastSE(RawHemolysisStatus, item.Name, findStrg), "raw_bp_type");
            }
            else if (item.HasTag("proc_bloodpack"))
            {
                bool findStrg(FastSE _0, Entity _1, IReadOnlyList<ISerializableEntity> targets)
                {
                    Character c = Unsafe.As<Character>(targets[0]);
                    foreach (var aff in c.CharacterHealth.afflictions.Keys)
                        if (aff is Affs.BloodType)
                            return !Utils.bloodTypeCompat(item.Prefab.Identifier.Value.Remove(0, 10), aff.Identifier.Value.Remove(0, 5));
                    return true;
                }
                __instance.statusEffectLists.addSE(new FastSE(ProcHemolysisStatus, item.Name, findStrg), "proc_bp_type");
            }
        }
    }
}
