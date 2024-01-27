using Barotrauma;
namespace PrimMed.Affs
{
    class Hemolysis : Affliction
    {
        private const byte INTV = 64;
        private byte elapsed = 1;
        public Hemolysis(AfflictionPrefab prefab, float strength) : base(prefab, strength) { }
        public override void Update(CharacterHealth ch, Limb targetLimb, float deltaTime)
        {
            base.Update(ch, targetLimb, deltaTime);
            if (Utils.IsHost() && --elapsed % INTV == 0)
            {
                float r = Rand.Value(Rand.RandSync.ServerAndClient), s = Strength / Prefab.MaxStrength;
                Affliction aff;
                if (r < 0.75f)
                    aff = new LungDmg(Utils.LUNG_DMG_PFB, s);
                else if (r < 0.875f)
                    aff = new LiverDmg(Utils.LIVER_DMG_PFB, s);
                else
                    aff = new HeartDmg(Utils.HEART_DMG_PFB, s);
                aff.Source = Source;
                ch.addLimbAffFast(null, aff, true, true);
            }
        }

    }
}