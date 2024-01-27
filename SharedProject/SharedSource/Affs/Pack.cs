using Barotrauma;
using System;
namespace PrimMed.Affs
{
    class Pack : Affliction
    {
        public Pack(AfflictionPrefab prefab, float strength) : base(prefab, strength) { }
        public override void Update(CharacterHealth ch, Limb targetLimb, float deltaTime)
        {
            base.Update(ch, targetLimb, deltaTime);
            if(Duration<0.125f)
            {
                for(byte i=(byte)Math.Ceiling(Strength);i>0;--i)
                    Entity.Spawner.AddItemToSpawnQueue(Utils.LIQUIDBAG_PFB, ch.Character.Inventory);
                Duration=0f;
            }
        }

    }
}