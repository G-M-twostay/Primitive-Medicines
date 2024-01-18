using Barotrauma;
namespace PrimMed.Affs
{
    class Suppressed : Affliction
    {
        private const float SUPPRESSION_SPEED = 0.5f;
        private const byte INTV = 64;
        private byte elapsed = 1;
        private static readonly AfflictionPrefab REJECT_RESIS_PFB = AfflictionPrefab.Prefabs["reject_resistance"];
        public Suppressed(AfflictionPrefab prefab, float strength) : base(prefab, strength) { }
        public override void Update(CharacterHealth ch, Limb targetLimb, float deltaTime)
        {
            base.Update(ch, targetLimb, deltaTime);
            if (Utils.IsHost() && --elapsed % INTV == 0)
            {
                foreach (var aff in ch.afflictions.Keys)
                    if (aff is Rejection)
                    {
                        ch.matchingAfflictions.Clear();
                        ch.matchingAfflictions.Add(aff);
                        break;
                    }
                ch.reduceAffFast(Strength / Prefab.MaxStrength * SUPPRESSION_SPEED);
                ch.addLimbAffFast(null, REJECT_RESIS_PFB, Strength, Source, false);
            }
        }

    }
}