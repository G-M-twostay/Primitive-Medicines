using Barotrauma;
using Barotrauma.Items.Components;
using System.Runtime.CompilerServices;
namespace PrimMed.Affs
{
    class Mortal : Affliction
    {
        private const byte INTV = 90;
        private const float MIN_INFECTION_PROB = 0.05f, DRY_INC = 1f, WET_INC = 1.5f, TEMP_REGR = 1.375f, HYPERTHERMIA_TH = 28f, HYPOTHERMIA_TH = -28f, HUSK_HYPERTHERMIA_TH = 20f, HUSK_HYPOTHERMIA_TH = -2000f;
        private static readonly (LimbType, float)[] LIMB_MODS = new (LimbType, float)[]{
            (LimbType.Head,1.125f),
            (LimbType.Torso,1f),
            (LimbType.LeftArm,0.875f),
            (LimbType.RightArm,0.875f),
            (LimbType.LeftLeg,0.875f),
            (LimbType.RightLeg,0.875f)
            };
        private static readonly AfflictionPrefab HYPERTHERMIA_PFB = AfflictionPrefab.Prefabs["hyperthermia"], HYPOTHERMIA_PFB = AfflictionPrefab.Prefabs["hypothermia"];
        private static (float, sbyte) AffPainMod(in Identifier id) => id.Value switch
        {
            "lacerations" => (1f, -1),
            "blunttrauma" => (0.875f, -1),
            "bitewounds" => (1.25f, -1),
            "gunshotwound" => (1.125f, -1),
            "explosiondamage" => (1.125f, -1),
            "organdamage" => (0.875f, 1),
            "bleeding" => (0.125f, -1),
            "burn" => (1f, -1),
            "acidburn" => (1.5f, -1),
            "psychosis" => (Rand.Value(Rand.RandSync.ServerAndClient) * 2f, (sbyte)Rand.Int(LIMB_MODS.Length, Rand.RandSync.ServerAndClient)),
            "hallucinating" => (Rand.Value(Rand.RandSync.ServerAndClient) + 0.25f, 0),
            "hyperthermia" => (1f, 0),
            "pressure" => (1f, 1),
            "pierce" => (0.75f, -1),
            "trauma" => (1f, -1),
            "incision" => (1f, -1),
            "sutural" => (0.625f, -1),
            _ => (0, -1),
        };
        private static float AffInfectMod(in Identifier id) => id.Value switch
        {
            "lacerations" => 1f,
            "blunttrauma" => 0.7f,
            "bitewounds" => 1.2f,
            "gunshotwound" => 1.1f,
            "explosiondamage" => 1f,
            "bleeding" => 1f,
            "burn" => 1f,
            "acidburn" => 1.15f,
            "pierce" => 0.9f,
            "scar" => 0.85f,
            "incision" => 0.95f,
            "sutural" => 0.8f,
            _ => 0f,
        };
        private byte elapsed = 1;
        public float temp = 0f;
        public Mortal(AfflictionPrefab prefab, float strength) : base(prefab, strength) { }
        public override void Update(CharacterHealth ch, Limb targetLimb, float deltaTime)
        {
            if (Utils.IsHost() && --elapsed < 1)
            {
                elapsed = INTV;

                //this affliction has no default behavior, nor is it shown in game, so no need to call base.
                //base.Update(ch, targetLimb, deltaTime);

                var affs = ch.afflictions;
                var lhs = ch.limbHealths;

                Limb[] limbs = new Limb[LIMB_MODS.Length];
                for (byte i = 0; i < LIMB_MODS.Length; ++i)
                    limbs[i] = ch.Character.AnimController.GetLimb(LIMB_MODS[i].Item1);

                Span<float> limbStrengths = stackalloc float[LIMB_MODS.Length];
                limbStrengths.Clear();
                //pains
                if (!(ch.IsUnconscious || ch.Stun > 0f))//unconscious or stunned characters don't feel pain
                    foreach ((Affliction aff, CharacterHealth.LimbHealth lh) in affs)
                    {
                        (float aff_mod, sbyte limbId) = AffPainMod(aff.Identifier);
                        if (aff_mod > 0)
                            if (lh is null)
                                limbStrengths[limbId] += aff_mod * aff.Strength / aff.Prefab.MaxStrength;
                            else
                                for (byte i = 0; i < LIMB_MODS.Length; ++i)
                                    if (lh == lhs[limbs[i].HealthIndex])
                                    {
                                        limbStrengths[i] += aff_mod * LIMB_MODS[i].Item2 * aff.Strength / aff.Prefab.MaxStrength;
                                        break;
                                    }

                    }
                for (byte i = 0; i < LIMB_MODS.Length; ++i)
                    ch.addLimbAffFast(lhs[limbs[i].HealthIndex], new Pain(Utils.PAIN_PFB, limbStrengths[i] * Utils.PAIN_PFB.MaxStrength), false, false);
                limbStrengths.Clear();

                var item = ch.Character.Inventory.GetItemInLimbSlot(InvSlotType.OuterClothes);
                bool hasSuit = item is not null && Unsafe.As<HashSet<Identifier>>(item.GetTags()).IsSupersetOf(new Identifier[] { "deepdiving".ToIdentifier(), "provocative".ToIdentifier() });
                {//infections
                    Span<float> bdgOffset = stackalloc float[LIMB_MODS.Length];
                    bdgOffset.Clear();
                    foreach ((Affliction aff, CharacterHealth.LimbHealth lh) in affs)
                        if (aff is Bandaged)
                            for (byte i = 0; i < LIMB_MODS.Length; ++i)
                                if (lh == lhs[limbs[i].HealthIndex])
                                {
                                    bdgOffset[i] = aff.Strength / (100f * 2);
                                    break;
                                }
                    foreach ((Affliction aff, CharacterHealth.LimbHealth lh) in affs)
                    {
                        float infMod = AffInfectMod(aff.Identifier);
                        if (infMod > 0f)
                            for (byte i = 0; i < LIMB_MODS.Length; ++i)
                                if (lh == lhs[limbs[i].HealthIndex])
                                {
                                    float waterMod = limbs[i].InWater && !hasSuit ? WET_INC : DRY_INC;
                                    limbStrengths[i] += (MIN_INFECTION_PROB + aff.Strength / aff.Prefab.MaxStrength * infMod - bdgOffset[i]) * waterMod;
                                    break;
                                }
                    }

                    for (byte i = 0; i < LIMB_MODS.Length; ++i)
                        ch.addLimbAffFast(lhs[limbs[i].HealthIndex], new Bacterial(Utils.BACTERIAL0_PFB, limbStrengths[i]), true, false);
                }
                //temperatures
                ref float dec = ref limbStrengths[0], inc = ref limbStrengths[1];
                dec = inc = 0f;

                {//from environment
                    bool hasLargeSuit = hasSuit && item.HasTag("deepdivinglarge");
                    if (!hasLargeSuit)
                    {//check if player is wearing exosuit
                        for (byte i = 0; i < LIMB_MODS.Length; ++i)
                            if (limbs[i].InWater)
                                dec += LIMB_MODS[i].Item2;
                        if (hasSuit)//prevents cooling only to the degree that temperatures don't drop.
                            dec/*dec=0 before this*/*= TEMP_REGR / 5.625f/*sum of all limb weights.*/;
                    }
                }

                bool hasHusk = false;
                ref float vul = ref limbStrengths[3];
                //from afflictions. overrides changes from environment.
                foreach (var aff in affs.Keys)
                    if (aff is Bacterial)//starts to cause fever at 0.45=90/200.
                        inc += aff.Strength / aff.Prefab.MaxStrength * TEMP_REGR / 0.45f;
                    else if (aff is AfflictionBleeding)
                        dec += aff.Strength / aff.Prefab.MaxStrength;
                    else if (aff is AfflictionHusk)
                        hasHusk = true;
                    else if (ReferenceEquals(aff.Prefab, Utils.BLOODLOSS_PFB))
                        vul = aff.Strength / aff.Prefab.MaxStrength;
                    else if (aff is Hemolysis)//starts to cause fever at 0.1=10/100.
                        inc += aff.Strength / aff.Prefab.MaxStrength * TEMP_REGR / 0.1f;
                    else if (aff is Rejection)//starts to cause fever at 0.2=20/100.
                        inc += aff.Strength / aff.Prefab.MaxStrength * TEMP_REGR / 0.2f;
                    else if (aff.Identifier == "Phenothiazinepoisoning")
                    {
                        float r = aff.Strength / aff.Prefab.MaxStrength;
                        float wavg = Math.Min(r, 0.5f) * 0.25f + Math.Max(0f, r - 0.5f) * 0.75f;//value strengths above 0.5 more.
                        dec += wavg * 2f * TEMP_REGR;//cause -2 temperature at max strength.
                    }


                //from clothes. overrides above.
                //check if wearing outpost deweller cloth.
                item = ch.Character.Inventory.GetItemInLimbSlot(InvSlotType.InnerClothes);
                if (item is not null)
                {
                    if (item.Prefab.Identifier == "heatclothes")
                    {
                        var contained = item.GetComponent<ItemContainer>().Inventory.FirstOrDefault();
                        if (contained is not null)
                            if (dec > 0f)
                            {
                                dec = 0f;
                                contained.Condition -= 0.1f * INTV / 60;
                            }
                    }
                    else if (item.Prefab.Identifier == "minerclothes" || item.Prefab.Identifier.StartsWith("commonerclothes"))
                        dec -= 0.125f;
                }

                //from treatments. overrides above.
                foreach ((Affliction aff, CharacterHealth.LimbHealth lh) in affs)
                    if (object.ReferenceEquals(Utils.ICED_PFB, aff.Prefab))
                    {
                        if (lh == lhs[limbs[0].HealthIndex])/*only ice pack on head reduce temperature*/
                        {
                            dec += aff.Strength / aff.Prefab.MaxStrength * TEMP_REGR;
                            break;
                        }
                    }
                    else if (object.ReferenceEquals(Utils.HEATED_PFB, aff.Prefab))
                        if (lh == lhs[limbs[1].HealthIndex])/*only heat pack on torso raise temperature*/
                        {
                            inc += aff.Strength / aff.Prefab.MaxStrength * TEMP_REGR;
                            break;
                        }

                ref float deltaTemp = ref limbStrengths[4];
                deltaTemp = inc - Math.Max(0f, dec);
                deltaTemp *= 1f + vul;

                temp += deltaTemp;
                temp -= TEMP_REGR * Math.Sign(temp);


                ref float hyperthermiaStrg = ref limbStrengths[0], hypothermiaStrg = ref limbStrengths[1];
                if (hasHusk)
                {
                    hyperthermiaStrg = temp - HUSK_HYPERTHERMIA_TH;
                    hypothermiaStrg = HUSK_HYPOTHERMIA_TH - temp;
                }
                else
                {
                    hyperthermiaStrg = temp - HYPERTHERMIA_TH;
                    hypothermiaStrg = HYPOTHERMIA_TH - temp;
                }
                ch.addLimbAffFast(null, HYPERTHERMIA_PFB, hyperthermiaStrg, null, false, false);
                ch.addLimbAffFast(null, HYPOTHERMIA_PFB, hypothermiaStrg, null, false, true);


                //when performing surgery, the lack of bleeding indicates vessels are sealed.
                limbStrengths.Clear();
                Character[] inciSrcs = new Character[LIMB_MODS.Length];
                foreach (var (aff, lh) in affs)
                    if (aff is AfflictionBleeding || object.ReferenceEquals(aff.Prefab, Utils.CNCT_PFB))
                    {
                        for (byte i = 0; i < LIMB_MODS.Length; ++i)
                            if (lh == lhs[limbs[i].HealthIndex])
                            {
                                limbStrengths[i] += 1f;
                                break;
                            }
                    }
                    else if (object.ReferenceEquals(aff.Prefab, Utils.INCISION_PFB))
                        for (byte i = 0; i < LIMB_MODS.Length; ++i)
                            if (lh == lhs[limbs[i].HealthIndex])
                            {
                                limbStrengths[i] -= 1f;
                                inciSrcs[i] = aff.Source;
                                break;
                            }
                for (byte i = 0; i < LIMB_MODS.Length; ++i)
                    if (limbStrengths[i] < 0f)
                    {
                        ch.addLimbAffFast(lhs[limbs[i].HealthIndex], new Sealed(Utils.SEALED_PFB, 1f)
                        {
                            Source = inciSrcs[i]
                        }, false);
                    }
            }

        }
    }
}