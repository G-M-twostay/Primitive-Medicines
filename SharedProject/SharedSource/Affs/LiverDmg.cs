using Barotrauma;
namespace PrimMed.Affs
{
    class LiverDmg : Affliction
    {
        private const byte INTV = 64;
        private const float IN_DMG_SPEED=0.5f;
        private byte elapsed = 1;
        public LiverDmg(AfflictionPrefab prefab, float strength) : base(prefab, strength) { }
        public override void Update(CharacterHealth ch, Limb targetLimb, float deltaTime)
        {
            base.Update(ch, targetLimb, deltaTime);
            if (Utils.IsHost()&&--elapsed % INTV == 0)
                ch.addLimbAffFast(null,Utils.IN_DMG_PFB,Strength / Prefab.MaxStrength * IN_DMG_SPEED,Source,true,true);
            
        }

    }
}