using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(Limb))]
    static class _Limb
    {
        [HarmonyPostfix]
        [HarmonyPatch("AddDamage", new Type[] { typeof(Vector2), typeof(IEnumerable<Affliction>), typeof(bool), typeof(float), typeof(float), typeof(Character) })]
        public static void _AddDamage(Limb __instance, in AttackResult __result, Character attacker)
        {
            var chr = __instance.character;
            if (chr.IsHuman/*&&!(chr.Unkillable || chr.GodMode)*/)
            {
                static float rmvBdgProb(in Identifier id) => id.Value switch
                {
                    "lacerations" => 0.7f,
                    "bitewounds" => 0.9f,
                    "blunttrauma" => 0.2f,
                    "gunshotwound" => 0.9f,
                    "explosiondamage" => 0.8f,
                    "burn" => 0.6f,
                    "acidburn" => 0.95f,
                    "pierce" => 0.1f,
                    _ => 0f
                };
                const float STEP = 4f;

                var ch = chr.CharacterHealth;
                float r = Rand.Value(Rand.RandSync.ServerAndClient);
                byte dec = 0;
                foreach (var aff in __result.Afflictions)
                    if (r < rmvBdgProb(aff.Identifier))
                    {
                        ++dec;
#if CLIENT
                        if(aff.Identifier== "blunttrauma")
                            SoundPlayer.PlaySound("bonebreak");
#endif
                    }

                if (dec > 0)
                    foreach (var (aff, lh) in ch.afflictions)
                        if (lh == ch.limbHealths[__instance.HealthIndex] && aff is Affs.Bandaged)
                        {
                            float t = Math.Min(aff.Prefab.MaxStrength / STEP * dec, aff.Strength);
                            aff.SetStrength(aff.Strength - t);
                            ch.addLimbAffFast(lh, Utils.SCAR_PFB, t, attacker, true, true);
                            break;
                        }
            }
        }
    }
}