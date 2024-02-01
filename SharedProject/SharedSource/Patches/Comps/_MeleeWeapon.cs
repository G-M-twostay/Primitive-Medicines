using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using PrimMed.Replace;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(MeleeWeapon))]
    static class _MeleeWeapon
    {
        private static float getLimbBdgStrg(CharacterHealth ch, Limb l)
        {
            foreach (var (aff, lh) in ch.afflictions)
                if (lh == ch.limbHealths[l.HealthIndex] && aff is Affs.Bandaged)
                    return aff.Strength;
            return 0f;
        }
        private static readonly ContentXElement bdgSETemp = new(PMMod.CntPkg, XElement.Parse("<StatusEffect tags=\"medical\" type=\"\" target=\"Limb\" disabledeltatime=\"true\">          <Affliction identifier=\"bandaged\" amount=\"10\" />        </StatusEffect>"));
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(Item), typeof(ContentXElement) })]
        public static void PostCtor(ItemComponent __instance, Item item)
        {
            void LoadStatusEffect(ContentXElement ele, in float strg, FastSE.FuncCond cond)
            {
                ele.SetAttributeValue("type", "OnSuccess");
                var se = new FastSE(ele, item.Name, cond);
                se.Afflictions[0].SetStrength(strg);
                __instance.statusEffectLists.addSE(se);

                ele.SetAttributeValue("type", "OnFailure");
                se = new FastSE(ele, item.Name, cond);
                se.Afflictions[0].SetStrength(strg / 2f);
                __instance.statusEffectLists.addSE(se);
            }
            if (item.Prefab.Identifier.StartsWith("antibleeding"))
            {
                static float calcMod(Character u) => u.HasTalent("medicalexpertise") ? Affs.Bandaged.TALENT_MOD : 1f;
                switch (item.Prefab.Identifier[12])
                {
                    case '1':
                        {
                            static bool belowTH(FastSE se, Entity _, IReadOnlyList<ISerializableEntity> targets)
                            {
                                Limb l = Unsafe.As<Limb>(targets[0]);
                                return getLimbBdgStrg(l.character.CharacterHealth, l) < 6f * calcMod(Unsafe.As<Character>(se.user));
                            }
                            LoadStatusEffect(bdgSETemp, 2f, belowTH);
                        }
                        break;
                    case '2':
                        {
                            static bool belowTH(FastSE se, Entity _, IReadOnlyList<ISerializableEntity> targets)
                            {
                                Limb l = Unsafe.As<Limb>(targets[0]);
                                return getLimbBdgStrg(l.character.CharacterHealth, l) < 12f * calcMod(Unsafe.As<Character>(se.user));
                            }
                            LoadStatusEffect(bdgSETemp, 4f, belowTH);
                        }
                        break;
                    case '3':
                        {
                            static bool belowTH(FastSE se, Entity _, IReadOnlyList<ISerializableEntity> targets)
                            {
                                Limb l = Unsafe.As<Limb>(targets[0]);
                                return getLimbBdgStrg(l.character.CharacterHealth, l) < 24f * calcMod(Unsafe.As<Character>(se.user));
                            }
                            LoadStatusEffect(bdgSETemp, 8f, belowTH);
                        }
                        break;
                }
            }
        }
    }
}
