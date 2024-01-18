using Barotrauma;
namespace PrimMed.Affs
{
    class Pain : Affliction
    {
        private static readonly AfflictionPrefab TRAUMA_PFB = AfflictionPrefab.Prefabs["trauma"];
        private const int MIN_INTV = 128, MAX_INTV = 256;
        private const float TRAUMA_TH = 0.5f;
        private byte elapsed = 1;
        public Pain(AfflictionPrefab prefab, float strength) : base(prefab, strength) { }
        public override void Update(CharacterHealth ch, Limb targetLimb, float deltaTime)
        {
            base.Update(ch, targetLimb, deltaTime);

            if (Utils.IsHost() && --elapsed < 1)
            {
                elapsed = (byte)Rand.Range(MIN_INTV, MAX_INTV, Rand.RandSync.ServerAndClient);

                float relativeStrg = Strength / Prefab.MaxStrength;
                float traumaChance = relativeStrg - TRAUMA_TH;
                float r = Rand.Value(Rand.RandSync.ServerAndClient);
                if (r < traumaChance / 2)
                    ch.addLimbAffFast(ch.limbHealths[targetLimb.HealthIndex], TRAUMA_PFB, traumaChance * TRAUMA_PFB.MaxStrength);
                if (r < relativeStrg / 5)
                    switch (targetLimb.type)
                    {
                        case LimbType.LeftFoot:
                        case LimbType.RightFoot:
                            ch.Character.Stun = 0.5f;
                            /*                            ((HumanoidAnimController)ch.Character.AnimController).Crouching = false;
                                                        ch.Character.AnimController.IgnorePlatforms = true;
                                                        ch.Character.AnimController.ResetPullJoints();
                                                        ch.Character.IsForceRagdolled = true;*/
                            break;
                        case LimbType.Waist:
                            ch.Character.Stun = 0.25f;
                            break;
                        case LimbType.LeftForearm:
                            {
                                Item item = ch.Character.Inventory.GetItemInLimbSlot(InvSlotType.LeftHand);
                                item?.Drop(ch.Character);
                                break;
                            }
                        case LimbType.RightForearm:
                            {
                                Item item = ch.Character.Inventory.GetItemInLimbSlot(InvSlotType.RightHand);
                                item?.Drop(ch.Character);
                                break;
                            }
                        case LimbType.Head:
                            ch.Character.Stun = relativeStrg * 2;
                            break;
                    }
            }
        }
    }

}