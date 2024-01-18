using Barotrauma;
namespace PrimMed.Affs
{
    class Rejection : Affliction
    {
        private const byte INTV = 96;
        private byte elapsed = 1;
        public Rejection(AfflictionPrefab prefab, float strength) : base(prefab, strength) { }
        public override void Update(CharacterHealth ch, Limb targetLimb, float deltaTime)
        {
            base.Update(ch, targetLimb, deltaTime);
            if (Utils.IsHost()&&--elapsed % INTV == 0)
            {
                float r=Rand.Value(Rand.RandSync.ServerAndClient),relativeStrg=Strength / Prefab.MaxStrength;
                Affliction tempAff;
                if(r<0.2f)
                    tempAff=new Hemolysis(Utils.HEMOLYSIS_PFB,relativeStrg);
                else if(r<0.4f)
                    tempAff=Utils.IN_DMG_PFB.Instantiate(relativeStrg);
                else if(r<0.6f)
                    tempAff=new LungDmg(Utils.LUNG_DMG_PFB,relativeStrg);
                else if(r<0.8f)
                    tempAff=new LiverDmg(Utils.LIVER_DMG_PFB,relativeStrg);
                else
                    tempAff=new HeartDmg(Utils.HEART_DMG_PFB,relativeStrg);
                
                tempAff.Source=Source;
                ch.addLimbAffFast(null,tempAff,true,true);
            }
        }

    }
}