using Barotrauma;
namespace PrimMed.Affs
{
    class Hemolysis : Affliction
    {
        private const byte INTV = 64;
        private byte elapsed = 1;
        private static readonly AfflictionPrefab ORGAN_DMG_PFB = AfflictionPrefab.Prefabs["organdamage"];
        public Hemolysis(AfflictionPrefab prefab, float strength) : base(prefab, strength) { }
        public override void Update(CharacterHealth ch, Limb targetLimb, float deltaTime)
        {
            base.Update(ch, targetLimb, deltaTime);
            if (Utils.IsHost() && --elapsed % INTV == 0)
            {
                float r = Rand.Value(Rand.RandSync.ServerAndClient), s = Strength / Prefab.MaxStrength;
                if (r < 0.5f)
                    ch.addLimbAffFast(null, ORGAN_DMG_PFB, s, Source, true, true);
                //ch.Character.dmgLimbFast(ch.Character.AnimController.MainLimb, new Affliction[] { ORGAN_DMG_PFB.Instantiate(s, Source) }, Source, sound: false);
                else if (r < 0.75f)
                    ch.addLimbAffFast(null, new LungDmg(Utils.LUNG_DMG_PFB, s + 0.025f) { Source = Source }, true, true);
                //ch.Character.dmgLimbFast(ch.Character.AnimController.MainLimb, new Affliction[] { new LungDmg(Utils.LUNG_DMG_PFB, s) { Source = Source } }, Source, sound: false);
                else
                    ch.addLimbAffFast(null, Utils.IN_DMG_PFB, s + 0.025f, Source, true, true);
                //ch.Character.dmgLimbFast(ch.Character.AnimController.MainLimb, new Affliction[] { Utils.IN_DMG_PFB.Instantiate(s, Source) }, Source, sound: false);
            }
        }

    }
}