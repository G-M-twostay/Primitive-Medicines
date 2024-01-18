using Barotrauma;
namespace PrimMed.Affs
{
    class HeartDmg : Affliction
    {
        private const float BLOODLOSS_SPEED=1.75f;
        public HeartDmg(AfflictionPrefab prefab, float strength) : base(prefab, strength) { }
        public override void Update(CharacterHealth ch, Limb targetLimb, float deltaTime)
        {
            base.Update(ch, targetLimb, deltaTime);

            float bloodlossResistance = GetResistance(ch.BloodlossAffliction.Identifier);
            ch.BloodlossAmount += Strength/Prefab.MaxStrength*BLOODLOSS_SPEED * (1.0f - bloodlossResistance)*deltaTime;
        }

    }
}