using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Barotrauma;
namespace PrimMed.Affs
{
    class Bandaged : Affliction
    {
        private const byte INTV = 120;
        internal const float TALENT_MOD = 1.2f;
        //30 is standard max strength, 30*1.2 is talent max strength. Numbers are set according to standard max strength, so for burn we want it to be 0.1 at 30.
        internal static readonly Dictionary<Identifier, float> Healable = new Dictionary<Identifier, float> { { "burn".ToIdentifier(), 0.1f * TALENT_MOD }, { "damage".ToIdentifier(), 0.1f * TALENT_MOD }, { "bleeding".ToIdentifier(), 2f * TALENT_MOD }, { "scar".ToIdentifier(), 0.1f * TALENT_MOD } };
        private byte elapsed = 1;
        private readonly Dictionary<Identifier, List<Affliction>> cache = new Dictionary<Identifier, List<Affliction>>(4);
        public Bandaged(AfflictionPrefab prefab, float strength) : base(prefab, strength)
        {
            foreach(Identifier typeName in Healable.Keys)
                cache.Add(typeName,new List<Affliction>(1));
        }
        public override void Update(CharacterHealth ch, Limb targetLimb, float deltaTime)
        {
            base.Update(ch, targetLimb, deltaTime);
            if (--elapsed < 1)
            {
                elapsed = INTV;

                var affs = ch.afflictions;

                foreach ((Affliction aff, CharacterHealth.LimbHealth lh) in affs)
                    if (lh == ch.limbHealths[targetLimb.HealthIndex])
                        if (cache.TryGetValue(aff.Prefab.AfflictionType, out List<Affliction> q))
                            q.Add(aff);
                
                bool healthy = true;
                foreach ((Identifier id, List<Affliction> q) in cache)
                    if (q.Count > 0)
                    {
                        ch.reduceAffFast(q,Strength / Prefab.MaxStrength * Healable[id] * INTV / 60);
                        q.Clear();
                        healthy = false;
                    }

                if (healthy)
                    affs.Remove(this);
            }
        }
    }
}