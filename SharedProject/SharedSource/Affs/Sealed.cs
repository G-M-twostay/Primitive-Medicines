using System;
using Barotrauma;
namespace PrimMed.Affs{
    class Sealed : Affliction
    {
        public Sealed(AfflictionPrefab prefab, float strength) : 
            base(prefab, strength)
        {
        }
        private const float BLOODLOSS_SPEED=0.35f;
        public override void Update(CharacterHealth characterHealth, Limb targetLimb, float deltaTime)
        {
            float bloodlossResistance = GetResistance(characterHealth.BloodlossAffliction.Identifier);
            characterHealth.BloodlossAmount += BLOODLOSS_SPEED * (1.0f - bloodlossResistance) * deltaTime;
            characterHealth.BloodlossAffliction.Source = Source;
        }
    }
}