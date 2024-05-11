using Barotrauma;
namespace PrimMed.Affs
{
    class Infection : Affliction
    {
        private const byte INTV = 240, STRG_INTV = 60;
        private const float SPREAD_TH = 40f, SPREAD_RESULT = 8f;
        private byte elapsed = 0;
        public Infection(AfflictionPrefab prefab, float strength) : base(prefab, strength) { }
        private void updateStrg(in CharacterHealth ch)
        {
            float r = Rand.Value(Rand.RandSync.ServerAndClient);
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

                    LimbType[] spreads = targetLimb.type switch
                    {
                        LimbType.LeftFoot or LimbType.RightFoot or LimbType.LeftForearm or LimbType.RightForearm => new LimbType[] { LimbType.Torso },
                        LimbType.Waist => new LimbType[] { LimbType.LeftLeg, LimbType.RightLeg, LimbType.LeftArm, LimbType.RightArm },
                        LimbType.Head => new LimbType[] { LimbType.Torso },
                        _ => Array.Empty<LimbType>(),
                    };
                    var r = Rand.Value(Rand.RandSync.ServerAndClient);
                    foreach (LimbType d in spreads)
                        if (r < (Strength - SPREAD_TH * Utils.hostilityMod(WorldHostilityOption.High, -0.125f)) / Prefab.MaxStrength)
                            ch.addLimbAffFast(ch.limbHealths[ch.Character.AnimController.GetLimb(d).HealthIndex], new Bacterial1(Utils.BACTERIAL1_PFB, SPREAD_RESULT), true, true);

                }
            }
        }
    }

}