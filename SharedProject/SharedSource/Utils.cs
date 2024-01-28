using Barotrauma;
using HarmonyLib;
using System.Reflection;
namespace PrimMed
{
    class AffRegister
    {
        private readonly AccessTools.FieldRef<AfflictionPrefab, ConstructorInfo> AfflictionPrefab_constructor_ = AccessTools.FieldRefAccess<AfflictionPrefab, ConstructorInfo>(typeof(AfflictionPrefab).GetField("constructor", BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.Instance | BindingFlags.ExactBinding));
        internal void register(Type affType, in Identifier id)
        {
            ConstructorInfo cons = affType.GetConstructor(new Type[] { typeof(AfflictionPrefab), typeof(float) });
            AfflictionPrefab_constructor_(AfflictionPrefab.Prefabs[id]) = cons;
        }
    }
    static partial class Utils
    {
        internal static float getLimbPain(in Limb limb, in CharacterHealth ch)
        {
            foreach ((Affliction aff, CharacterHealth.LimbHealth lh) in ch.afflictions)
                if (aff is Affs.Pain)
                    if (lh == ch.limbHealths[limb.HealthIndex])
                        return aff.Strength;

            return 0f;
        }
        public static bool IsHost()
        {//is server or single player.
#if CLIENT
            return GameMain.IsSingleplayer;
#else
            return true;
#endif
        }

        internal static void addLimbAffFast(this CharacterHealth ch, CharacterHealth.LimbHealth limbh, AfflictionPrefab newPfb, in float strength, Character src = null, in bool stack = true, in bool canKill = false) => ch.addLimbAffFast(limbh, newPfb.Instantiate(strength, src), stack, canKill);

        internal static void addLimbAffFast(this CharacterHealth ch, CharacterHealth.LimbHealth limbh, Affliction newAff, in bool stack = true, in bool canKill = false)
        {
            //no need to check for stun, poison immunity as humans aren't immune to neither.

            Affliction existing = null;

            foreach ((Affliction aff, CharacterHealth.LimbHealth lh) in ch.afflictions)
                if (lh == limbh && object.ReferenceEquals(aff.Prefab, newAff.Prefab))
                {
                    existing = aff;
                    break;
                }

            if (existing is not null)
            {
                float newStrg = newAff.Strength * (100.0f / ch.MaxVitality) * (1f - ch.GetResistance(newAff.Prefab));
                if (stack)
                    newStrg += existing.Strength;

                existing.Strength = newStrg;
                if (existing == ch.stunAffliction)
                    ch.Character.SetStun(existing.Strength, true, true);

                existing.Duration = newAff.Prefab.Duration;
                existing.Source = newAff.Source;
            }
            else
            {
                if (newAff.Strength <= 0f)//when there is no existing ones and strength is 0 that means we don't do anything.
                    return;

                newAff.SetStrength(Math.Min(newAff.Prefab.MaxStrength, newAff.Strength * (100.0f / ch.MaxVitality) * (1f - ch.GetResistance(newAff.Prefab))));

                ch.afflictions.Add(newAff, limbh);
                ch.Character.HealthUpdateInterval = 0f;
                MedicalClinic.OnAfflictionCountChanged(ch.Character);
#if CLIENT
                if (CharacterHealth.OpenHealthWindow != ch && limbh is not null)
                    ch.selectedLimbIndex = -1;
#endif
            }
            if (canKill)
            {
                ch.CalculateVitality();
                ch.KillIfOutOfVitality();
            }
        }
        internal static void reduceAffFast(this CharacterHealth ch, in float totalAmount) => ch.reduceAffFast(ch.matchingAfflictions, totalAmount);

        internal static void reduceAffFast(this CharacterHealth ch, List<Affliction> matchingAfflictions, float totalAmount)
        {
            float amount = totalAmount / matchingAfflictions.Count;
            byte active = (byte)matchingAfflictions.Count;
            foreach (var aff in matchingAfflictions)
                if (aff.Strength < amount)
                {
                    totalAmount -= aff.Strength;
                    --active;
                    aff.SetStrength(0f);
                }

            if (active > 0)
            {
                amount = totalAmount / active;
                foreach (var aff in matchingAfflictions)
                    if (aff.Strength > 0f)
                        aff.SetStrength(aff.Strength - amount);
            }
            ch.CalculateVitality();
        }

        internal static bool bloodTypeCompat(string donor, string recip) => donor switch
        {
            "o1" => true,//O-
            "o0" => recip.EndsWith('0'),//O+
            "b1" => recip.Contains('b'),//B-
            "b0" => recip.EndsWith("b0"),//B+
            "a1" => recip.Contains('a'),//A-
            "a0" => recip.StartsWith('a') && recip.EndsWith('0'),//A+
            "ab1" => recip.StartsWith("ab"),//AB-
            "ab0" => recip == "ab0",//AB+
            _ => true
        };

        internal static bool addSE(this Dictionary<ActionType, List<StatusEffect>> all, StatusEffect se, string tag)
        {
            if (all.TryGetValue(se.type, out List<StatusEffect> l))
            {
                /*this method somehow gets called multiple times for the same item.
                 * I suspect that the reason is of follows:
                 * the first time is when the meleeweapon or holdable component gets created,
                 * the second time is when the projectile component copies everything from meleeweapon by having `inheritstatuseffectsfrom="MeleeWeapon"`.
                 */
                if (l.Find(i => i.HasTag(tag)) is null)
                {
                    l.Add(se);
                    return true;
                }
            }
            else
            {
                all[se.type] = new List<StatusEffect>() { se };
                return true;
            }
            return false;
        }

    }
}