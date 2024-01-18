using System;
using Barotrauma;
namespace PrimMed.Affs
{
    class Bacterial : Affliction
    {
        private const byte INTV = 240, STRG_INTV = 60;
        private const float SPREAD_TH = 40f, SPREAD_RESULT = 10f;
        private byte elapsed = 0;
        public Bacterial(AfflictionPrefab prefab, float strength) : base(prefab, strength) { }
        private void updateStrg(in CharacterHealth ch)
        {
            float r = Rand.Value(Rand.RandSync.ServerAndClient);
            if (object.ReferenceEquals(Prefab, Utils.BACTERIAL0_PFB))
            {
                if (Strength < 20f)
                {
                    float durationMultiplier = 1f / (1f + ch.Character.GetStatValue(StatTypes.DebuffDurationMultiplier));
                    if (r < 0.1f)
                        SetStrength(Math.Max(Strength - 1 * durationMultiplier * StrengthDiminishMultiplier.Value, 0f));
                }
                else if (30f <= Strength && Strength < 70f)
                {
                    if (r < 0.05f)
                        SetStrength(Strength + 1f * (1f - ch.GetResistance(Prefab)));
                }
                else if (70 <= Strength)
                {
                    if (r < 0.1f)
                        SetStrength(Math.Min(Strength + 1 * (1f - ch.GetResistance(Prefab)), Prefab.MaxStrength));
                }
            }
            else
            {
                if (Strength < 20f)
                {
                    float durationMultiplier = 1f / (1f + ch.Character.GetStatValue(StatTypes.DebuffDurationMultiplier));
                    if (r < 0.1f)
                        SetStrength(Math.Max(Strength - 1 * durationMultiplier * StrengthDiminishMultiplier.Value, 0f));
                }
                else if (30f <= Strength && Strength < 70f)
                {
                    if (r < 0.1f)
                        SetStrength(Strength + 1f * (1f - ch.GetResistance(Prefab)));
                }
                else if (70 <= Strength)
                {
                    if (r < 0.15f)
                        SetStrength(Math.Min(Strength + 1 * (1f - ch.GetResistance(Prefab)), Prefab.MaxStrength));
                }
            }
        }
        public override void Update(CharacterHealth ch, Limb targetLimb, float deltaTime)
        {
            base.Update(ch, targetLimb, deltaTime);

            if (Utils.IsHost())
            {
                if (elapsed % STRG_INTV == 0)
                    updateStrg(ch);

                if (elapsed-- < 1)
                {
                    elapsed = INTV;

                    LimbType[] spreads;
                    switch (targetLimb.type)
                    {
                        case LimbType.LeftFoot:
                        case LimbType.RightFoot:
                        case LimbType.LeftForearm:
                        case LimbType.RightForearm:
                            spreads = new LimbType[] { LimbType.Torso };
                            break;
                        case LimbType.Waist:
                            spreads = new LimbType[] { LimbType.LeftLeg, LimbType.RightLeg, LimbType.LeftArm, LimbType.RightArm };
                            break;
                        case LimbType.Head:
                            spreads = new LimbType[] { LimbType.Torso };
                            break;
                        default:
                            spreads = new LimbType[0];
                            break;
                    }
                    foreach (LimbType d in spreads)
                        if (Rand.Value(Rand.RandSync.ServerAndClient) < (Strength - SPREAD_TH) / Prefab.MaxStrength)
                            ch.addLimbAffFast(ch.limbHealths[ch.Character.AnimController.GetLimb(d).HealthIndex], new Bacterial(Utils.BACTERIAL1_PFB, SPREAD_RESULT),true,true);
                            
                }


            }
        }
    }

}