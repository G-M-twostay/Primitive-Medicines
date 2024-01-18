using Barotrauma;
namespace PrimMed.Affs
{
    class LungDmg : Affliction
    {
        private const float O2_DRAIN_SPEED=12f;
        public LungDmg(AfflictionPrefab prefab, float strength) : base(prefab, strength) { }
        public override void Update(CharacterHealth ch, Limb targetLimb, float deltaTime)
        {
            
            base.Update(ch, targetLimb, deltaTime);

            ch.OxygenAmount -= Strength / Prefab.MaxStrength*O2_DRAIN_SPEED*deltaTime;
        }

    }
}