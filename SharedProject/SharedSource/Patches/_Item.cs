using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(Item))]
    static class _Item
    {
        private static bool Sulturable(in Identifier id) => id.Value switch
        {
            "lacerations" => true,
            "bitewounds" => true,
            "gunshotwound" => true,
            "explosiondamage" => true,
            "burn" => true,
            "acidburn" => true,
            "incision" => true,
            _ => false,
        };
        private static readonly AfflictionPrefab BLEEDING_PFB = AfflictionPrefab.Prefabs["bleeding"], LAC_PFB = AfflictionPrefab.Prefabs["lacerations"], REJECT_PFB = AfflictionPrefab.Prefabs["immunereject"], LUNG_M_PFB = AfflictionPrefab.Prefabs["lungmissing"], LIVER_M_PFB = AfflictionPrefab.Prefabs["livermissing"], HEART_M_PFB = AfflictionPrefab.Prefabs["heartmissing"];
        private static readonly ItemPrefab ICEPACK_PFB = ItemPrefab.Prefabs["icepack"], HEATPACK_PFB = ItemPrefab.Prefabs["heatpack"], INCENDIUMPACK_PFB = ItemPrefab.Prefabs["incendiumpack"];
        [HarmonyPrefix]
        [HarmonyPatch("ApplyTreatment", new Type[] { typeof(Character), typeof(Character), typeof(Limb) })]
        public static bool PreApplyTreatment(Item __instance, Character user, Character character, Limb targetLimb)
        {
            if (!character.IsDead)
            {
                string id = __instance.Prefab.Identifier.Value;
                if (targetLimb is null)//this can happen when bots are applying treatments, for some unknown reasons.
                {
                    targetLimb = character.AnimController.MainLimb;
                    DebugConsole.AddWarning($"Limb is null when applying {id} by {user.Name} to {character.Name}.");
                }
                const float SURGERY_TH_NORM = 100f, SURGERY_TH_TALENT = 75f, SCALPEL_SKILL = 135f, LAC_MOD_NORM = 1f, LAC_MOD_TALENT = 0.875f, BLEED_MOD_NORM = 1f, BLEED_MOD_TALENT = 0.75f, ASSISTANCE_MOD = 0.9375f, normalPackDuration = 40f, incendPackDuration = 60f;

                var ch = character.CharacterHealth;
                var lhs = ch.limbHealths;
                var affs = ch.afflictions;
                sbyte posture()
                {
                    var t = character.SelectedSecondaryItem?.Prefab;
                    if (t is not null)
                    {
                        if (t.Identifier.Contains("chair"))
                            return 1;
                        if (t.Identifier.Contains("hospitalbed"))
                            return -1;
                        if (t.Identifier.Contains("bed") || t.Identifier.Contains("bunk"))
                            return 0;
                    }
                    return 2;
                }
                Affliction organMiss(AfflictionPrefab t)
                {
                    foreach (var aff in affs.Keys)
                        if (ReferenceEquals(aff.Prefab, t))
                            return aff;
                    return null;
                }
                Affliction organCond(AfflictionPrefab t)//automatically deletes the affliction
                {
                    foreach (var aff in affs.Keys)
                        if (ReferenceEquals(aff.Prefab, t))
                        {
                            affs.Remove(aff);
                            return aff;
                        }
                    return null;
                }
                float surgeryReady(in float th)
                {
                    foreach (var (aff, lh) in affs)
                        if (lh == lhs[targetLimb.HealthIndex] && ReferenceEquals(aff.Prefab, Utils.INCISION_PFB))
                            return aff.Strength - th;
                    return -th;
                }

                void rmv(AfflictionPrefab t, ItemPrefab o, in float singleDuration)
                {
                    byte packCounts = 0;
                    float remain = 0f;
                    foreach (var (aff, lh) in affs)
                        if (lh == lhs[targetLimb.HealthIndex] && ReferenceEquals(aff.Prefab, t))
                        {
                            packCounts = (byte)Math.Ceiling(aff.Strength);
                            remain = aff.Duration;
                            affs.Remove(aff);
                            break;
                        }
                    for (byte i = 0; i < packCounts; ++i)
                        Entity.Spawner.AddItemToSpawnQueue(o, user.Inventory, remain / (singleDuration * packCounts) * o.Health);
                }

                if (id == "transfusionset")
                {
                    var contained = __instance.GetComponent<ItemContainer>().Inventory.FirstOrDefault();
                    if (contained is not null)
                    {
                        float posMod = 1 + 0.05f * (posture() - 1);
                        ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.PIERCE_PFB, (user.HasTalent("deliverysystem") ? 1f : 2f) * posMod, user);
                        ch.addLimbAffFast(lhs[targetLimb.HealthIndex], new AfflictionBleeding(BLEEDING_PFB, user.HasTalent("bloodybusiness") ? 1.5f : 2f)
                        {
                            Source = user
                        });
                        if (ReferenceEquals(contained.Prefab, Utils.LIQUIDBAG_PFB))
                        {
                            Entity.Spawner.AddItemToRemoveQueue(contained);

                            Entity.Spawner.AddItemToSpawnQueue(ItemPrefab.Prefabs["raw_" + Utils.FindBloodType(affs).Identifier], user.Inventory);
                            ch.addLimbAffFast(null, Utils.BLOODLOSS_PFB, user.HasTalent("bloodybusiness") ? 25f : 40f, user, true, true);
#if CLIENT
                            SoundPlayer.PlaySound("squelch");
#endif                        
                        }
                        else
                        {
                            contained.ApplyStatusEffects(contained.GetComponent<Holdable>().DegreeOfSuccess(user) < Rand.Value(Rand.RandSync.ServerAndClient) ? ActionType.OnSuccess : ActionType.OnFailure, 1f, user, useTarget: character);
                            contained.ApplyStatusEffects(ActionType.OnUse, 1f, user, useTarget: character);
                            contained.ApplyStatusEffects(ActionType.OnBroken, 1f, user, useTarget: character);
                        }
                    }
                }
                else if (id.StartsWith("raw_") || id.StartsWith("proc_") || id.StartsWith("antibloodloss"))
                {
                    if (user.IsPlayer)
                    {
#if CLIENT
                        GUI.AddMessage(TextManager.Get("packs.cannot"), GUIStyle.Red);
#endif
                        return false;
                    }

                }
                else if (ReferenceEquals(__instance.Prefab, Utils.LIQUIDBAG_PFB))//applying empty packs means removing exisitng packs.
                {
                    rmv(Utils.HEATED_PFB, HEATPACK_PFB, normalPackDuration);
                    rmv(Utils.ICED_PFB, ICEPACK_PFB, normalPackDuration);
                    rmv(Utils.INCENDIUM_PFB, INCENDIUMPACK_PFB, incendPackDuration);
                }
                else if (ReferenceEquals(__instance.Prefab, ICEPACK_PFB))//wrote packs here because 1 it won't cause any compatibility issue and 2 writing it in SE is too complicated and have lots of boilerplate code.
                {
                    ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.ICED_PFB, 1f, Utils.ICED_PFB.Duration * __instance.Condition / __instance.MaxCondition, user);
                }
                else if (ReferenceEquals(__instance.Prefab, HEATPACK_PFB))
                {
                    rmv(Utils.INCENDIUM_PFB, INCENDIUMPACK_PFB, incendPackDuration);//only one type of heat pack can exists
                    ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.HEATED_PFB, 1f, Utils.HEATED_PFB.Duration * __instance.Condition / __instance.MaxCondition, user);
                }
                else if (ReferenceEquals(__instance.Prefab, INCENDIUMPACK_PFB))
                {
                    rmv(Utils.HEATED_PFB, HEATPACK_PFB, normalPackDuration);
                    ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.INCENDIUM_PFB, 1f, Utils.INCENDIUM_PFB.Duration * __instance.Condition / __instance.MaxCondition, user);
                }
                else if (id == "bloodsampler")
                {
                    foreach (var (aff, lh) in affs)
                        if (aff is AfflictionBleeding && lh == lhs[targetLimb.HealthIndex])
                            goto isBleeding;
#if CLIENT
                    GUI.AddMessage(TextManager.Get("bloodsampler.cannot"), GUIStyle.Red);
#endif
                    return false;
                isBleeding:
                    var ic = __instance.GetComponent<CustomInterface>();
                    var textBox = ic.customInterfaceElementList[0];
                    ic.TextChanged(textBox, Utils.FindBloodType(affs).Name);
#if CLIENT
                    ic.UpdateSignalsProjSpecific();
#endif
                }
                else if (id == "scalpel")
                {
                    var action = __instance.GetComponent<Replace.Choose>().customInterfaceElementList[0].Signal;
                    float docPain;
                    if (ReferenceEquals(character.Inventory.GetItemInLimbSlot(InvSlotType.RightHand), __instance))
                        docPain = Utils.getLimbPain(user.AnimController.GetLimb(LimbType.RightArm), user.CharacterHealth);
                    else
                        docPain = Utils.getLimbPain(user.AnimController.GetLimb(LimbType.LeftArm), user.CharacterHealth);
                    docPain = (docPain * 0.75f + Utils.getLimbPain(character.AnimController.GetLimb(LimbType.Head), user.CharacterHealth) * 0.25f) / Utils.PAIN_PFB.MaxStrength;
                    float r = user.GetSkillLevel("medical") / SCALPEL_SKILL * (1 - docPain);
                    float rv = Rand.Value(Rand.RandSync.ServerAndClient);
                    float patientPainMod = 1f;
                    if (!character.IsIncapacitated)
                        patientPainMod += rv;

                    float lacMod = (user.HasTalent("drsubmarine") ? LAC_MOD_TALENT : LAC_MOD_NORM) * (rv < r ? 1f : 1.125f);
                    float bleedMod = (user.HasTalent("bloodybusiness") ? BLEED_MOD_TALENT : BLEED_MOD_NORM) * (rv < r ? 1f : 1.125f);
                    float qualMod = 1f - (__instance.Quality / Quality.MaxQuality) * 0.12f;
                    float assMod = user.CharacterHealth.GetAffliction("medicalassistance", false) is null ? 1f : ASSISTANCE_MOD;
                    float posMod = 1 + rv / 8f * posture();
                    float minInci = user.HasTalent("deliverysystem") ? SURGERY_TH_TALENT : SURGERY_TH_NORM;
                    switch (action)
                    {
                        case "scalpel.incise":
                            {
                                float inciMod = 1 + MathF.Pow(Math.Max(1 - r, 0f) / 2, 2) * assMod;
                                foreach (var (aff, lh) in affs)
                                    if (lh == lhs[targetLimb.HealthIndex] && aff is Affs.Bandaged)
                                    {
                                        affs.Remove(aff);
                                        break;
                                    }
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], new AfflictionBleeding(BLEEDING_PFB, 15f * bleedMod * qualMod * patientPainMod * assMod)
                                {
                                    Source = user
                                });
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], LAC_PFB, 10f * lacMod * qualMod * patientPainMod * assMod * posMod, user);
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.INCISION_PFB, Math.Abs(surgeryReady(minInci)) * inciMod * patientPainMod, user, true, true);
#if CLIENT
                                SoundPlayer.PlaySound("incise");
                                if (patientPainMod > 1f)
                                    SoundPlayer.PlaySound(character.IsMale ? "male_scream" : "female_scream");
#endif
                                break;
                            }
                        case "scalpel.lung":
                            if (targetLimb.type == LimbType.Torso && surgeryReady(minInci) >= 0f && organMiss(LUNG_M_PFB) is null)
                            {
                                var o = organCond(Utils.LUNG_DMG_PFB);
                                Entity.Spawner.AddItemToSpawnQueue(ItemPrefab.Prefabs["lung"], user.Inventory, o is null ? 100f : Utils.LUNG_DMG_PFB.MaxStrength - o.Strength);

                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], new AfflictionBleeding(BLEEDING_PFB, 6f * bleedMod * qualMod * patientPainMod * posMod)
                                {
                                    Source = user
                                });
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], LAC_PFB, 4f * lacMod * qualMod * patientPainMod * posMod, user);
                                ch.addLimbAffFast(null, new Affs.LungDmg(LUNG_M_PFB, 1f)
                                {
                                    Source = user
                                }, false, true);
#if CLIENT
                                SoundPlayer.PlaySound("severed");
                                if (patientPainMod > 1f)
                                    SoundPlayer.PlaySound(character.IsMale ? "male_scream" : "female_scream");
#endif
                            }
#if CLIENT
                            else
                                GUI.AddMessage(TextManager.Get("surgery.cannot"), GUIStyle.Red);
#endif
                            break;
                        case "scalpel.liver":
                            if (targetLimb.type == LimbType.Torso && surgeryReady(minInci) >= 0f && organMiss(LIVER_M_PFB) is null)
                            {
                                var o = organCond(Utils.LIVER_DMG_PFB);
                                Entity.Spawner.AddItemToSpawnQueue(ItemPrefab.Prefabs["liver"], user.Inventory, o is null ? 100f : Utils.LIVER_DMG_PFB.MaxStrength - o.Strength);

                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], new AfflictionBleeding(BLEEDING_PFB, 5f * bleedMod * qualMod * patientPainMod * posMod)
                                {
                                    Source = user
                                });
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], LAC_PFB, 3f * lacMod * qualMod * patientPainMod * posMod, user);
                                ch.addLimbAffFast(null, new Affs.LiverDmg(LIVER_M_PFB, 1f)
                                {
                                    Source = user
                                }, false, true);
#if CLIENT
                                SoundPlayer.PlaySound("severed");
                                if (patientPainMod != 1f)
                                    SoundPlayer.PlaySound(character.IsMale ? "male_scream" : "female_scream");
#endif
                            }
#if CLIENT
                            else
                                GUI.AddMessage(TextManager.Get("surgery.cannot"), GUIStyle.Red);
#endif
                            break;
                        case "scalpel.heart":
                            if (targetLimb.type == LimbType.Torso && surgeryReady(minInci) >= 0f && organMiss(HEART_M_PFB) is null)
                            {
                                var o = organCond(Utils.HEART_DMG_PFB);
                                Entity.Spawner.AddItemToSpawnQueue(ItemPrefab.Prefabs["heart"], user.Inventory, o is null ? 100f : Utils.HEART_DMG_PFB.MaxStrength - o.Strength);

                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], new AfflictionBleeding(BLEEDING_PFB, 7f * bleedMod * qualMod * patientPainMod * posMod)
                                {
                                    Source = user
                                });
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], LAC_PFB, 4f * lacMod * qualMod * patientPainMod * posMod, user);
                                ch.addLimbAffFast(null, new Affs.HeartDmg(HEART_M_PFB, 1f)
                                {
                                    Source = user
                                }, false, true);
#if CLIENT
                                SoundPlayer.PlaySound("severed");
                                if (patientPainMod != 1f)
                                    SoundPlayer.PlaySound(character.IsMale ? "male_scream" : "female_scream");
#endif
                            }
#if CLIENT
                            else
                                GUI.AddMessage(TextManager.Get("surgery.cannot"), GUIStyle.Red);
#endif
                            break;
                        case "scalpel.appendage":
                            {
                                const float HuskAppendageTH = 75f, CutStrg = 21f;
                                foreach (var aff in affs.Keys)
                                    if (aff is AfflictionHusk && aff.Identifier.StartsWith("husk")/*spineline, mudraptor gene also is afflictionhusk*/&& targetLimb.type == LimbType.Head)
                                        if (aff.Strength >= HuskAppendageTH)
                                        {
                                            Entity.Spawner.AddItemToSpawnQueue(ItemPrefab.Prefabs["huskstinger"], user.Inventory);
                                            aff.SetStrength(CutStrg);
                                            ch.addLimbAffFast(lhs[targetLimb.HealthIndex], new AfflictionBleeding(BLEEDING_PFB, 15f * bleedMod * qualMod * patientPainMod * assMod * posMod)
                                            {
                                                Source = user
                                            });
                                            ch.addLimbAffFast(lhs[targetLimb.HealthIndex], LAC_PFB, 10f * lacMod * qualMod * patientPainMod * assMod * posMod, user);
#if CLIENT
                                            SoundPlayer.PlaySound("severed");
#endif
                                            goto DONE;
                                        }
#if CLIENT
                                GUI.AddMessage(TextManager.Get("surgery.cannot"), GUIStyle.Red);
#endif
                                    DONE:
                                ;
                                break;
                            }
                        default:
#if CLIENT
                            GUI.AddMessage(TextManager.Get("scalpel.unknown"), GUIStyle.Red);
#endif
                            break;
                    }

                }
                else if (id == "scissors")
                {
                    float docPain;
                    if (ReferenceEquals(character.Inventory.GetItemInLimbSlot(InvSlotType.RightHand), __instance))
                        docPain = Utils.getLimbPain(user.AnimController.GetLimb(LimbType.RightArm), user.CharacterHealth);
                    else
                        docPain = Utils.getLimbPain(user.AnimController.GetLimb(LimbType.LeftArm), user.CharacterHealth);
                    docPain = (docPain * 0.75f + Utils.getLimbPain(character.AnimController.GetLimb(LimbType.Head), user.CharacterHealth) * 0.25f) / Utils.PAIN_PFB.MaxStrength;
                    float r = user.GetSkillLevel("medical") / SCALPEL_SKILL * (1 - docPain);
                    float qualMod = 1f - (__instance.Quality / Quality.MaxQuality) * 0.12f;
                    float patientPainMod = 1f;
                    float rv = Rand.Value(Rand.RandSync.ServerAndClient);
                    float posMod = 1 + rv / 8f * posture();
                    if (!character.IsIncapacitated)
                        patientPainMod += rv;
                    var holding = user.Inventory.GetItemInLimbSlot(InvSlotType.RightHand);
                    if (holding is null || !holding.HasTag("organ"))
                        holding = user.Inventory.GetItemInLimbSlot(InvSlotType.LeftHand);
                    if (holding is not null && holding.HasTag("organ"))//holding an organ on the other hand
                    {
                        var contained = __instance.GetComponent<ItemContainer>().Inventory.FirstOrDefault();
                        if (contained is not null)//check if sulture is loaded, if so stitch the organ in.
                        {
                            if (targetLimb.type == LimbType.Torso && surgeryReady(user.HasTalent("deliverysystem") ? SURGERY_TH_TALENT : SURGERY_TH_NORM) >= 0f)
                            {
                                var organType = AfflictionPrefab.Prefabs[holding.Prefab.Identifier + "missing"];
                                Affliction toRmv = organMiss(organType);
                                if (toRmv is not null)
                                {
                                    float pierceMod = rv < r ? 1f : patientPainMod;
                                    float rejectMod = 1f;
                                    if (user.HasTalent("genetampering") && r > 1f)
                                        rejectMod /= r;

                                    ch.addLimbAffFast(null, AfflictionPrefab.Prefabs[holding.Prefab.Identifier + "dmg"], holding.Condition, user, false);
                                    ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.PIERCE_PFB, 10f * qualMod * pierceMod * posMod, user);
                                    ch.addLimbAffFast(null, new Affs.Rejection(REJECT_PFB, rv * rejectMod * REJECT_PFB.MaxStrength)
                                    {
                                        Source = user
                                    }, true, true);

                                    Entity.Spawner.AddItemToRemoveQueue(contained);
                                    Entity.Spawner.AddItemToRemoveQueue(holding);
                                    affs.Remove(toRmv);
#if CLIENT
                                    SoundPlayer.PlaySound("suture");
                                    if (patientPainMod != 1f)
                                        SoundPlayer.PlaySound(character.IsMale ? "male_scream" : "female_scream");
#endif
                                }
                            }
#if CLIENT
                            else//if not, give a warning.
                                GUI.AddMessage(TextManager.Get("surgery.cannot"), GUIStyle.Red);
#endif
                        }
                    }
                    else//not holding any organ
                    {
                        var contained = __instance.GetComponent<ItemContainer>().Inventory.FirstOrDefault();
                        float assMod = user.CharacterHealth.GetAffliction("medicalassistance", false) is null ? 1f : ASSISTANCE_MOD;
                        if (contained is not null)//loaded with sulture
                        {
                            if (holding is not null && holding.Prefab.Identifier == "antibleeding2")//use platiseal to seal wounds
                            {
                                float strg = 0f;
                                foreach (var (aff, lh) in affs)
                                    if (lh == lhs[targetLimb.HealthIndex] && Sulturable(aff.Identifier))
                                    {
                                        strg += aff.Strength / aff.Prefab.MaxStrength;
                                        affs.Remove(aff);
                                    }
                                Entity.Spawner.AddItemToRemoveQueue(contained);
                                Entity.Spawner.AddItemToRemoveQueue(holding);
                                float pierceMod = rv < r ? 1f : patientPainMod;
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.PIERCE_PFB, strg / 12.5f * Utils.PIERCE_PFB.MaxStrength * qualMod * pierceMod * assMod * posMod, user);
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.SULTURAL_PFB, strg * Utils.SULTURAL_PFB.MaxStrength * assMod, user, true, true);
                            }
                            else//not holding platiseal, only seal incision
                            {
                                foreach (var (aff, lh) in affs)
                                    if (lh == lhs[targetLimb.HealthIndex] && ReferenceEquals(aff.Prefab, Utils.INCISION_PFB))
                                    {
                                        float pierceMod = rv < r ? 1f : patientPainMod;
                                        Entity.Spawner.AddItemToRemoveQueue(contained);
                                        ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.PIERCE_PFB, aff.Strength / 12.5f * qualMod * pierceMod * assMod * posMod, user);
                                        ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.SULTURAL_PFB, aff.Strength * assMod, user, true, true);
                                        affs.Remove(aff);

                                        break;
                                    }
                            }
#if CLIENT
                            SoundPlayer.PlaySound("suture");
                            if (patientPainMod != 1f)
                                SoundPlayer.PlaySound(character.IsMale ? "male_scream" : "female_scream");
#endif
                        }
                        else//not loaded with sulture, reopen incision.
                        {
                            foreach (var (aff, lh) in affs)
                                if (lh == lhs[targetLimb.HealthIndex] && ReferenceEquals(aff.Prefab, Utils.SULTURAL_PFB))
                                {
                                    float bleedMod = (user.HasTalent("bloodybusiness") ? BLEED_MOD_TALENT : BLEED_MOD_NORM) * (rv < r ? 1f : 1.125f);
                                    float lacMod = (user.HasTalent("drsubmarine") ? LAC_MOD_TALENT : LAC_MOD_NORM) * (rv < r ? 1f : 1.125f);
                                    ch.addLimbAffFast(lh, Utils.SCAR_PFB, 5f * lacMod * qualMod * assMod, user);
                                    ch.addLimbAffFast(lh, new AfflictionBleeding(BLEEDING_PFB, 7.5f * bleedMod * qualMod * assMod)
                                    {
                                        Source = user
                                    });
                                    ch.addLimbAffFast(lh, Utils.INCISION_PFB, aff.Strength * posMod, user, true, true);
                                    affs.Remove(aff);
#if CLIENT
                                    SoundPlayer.PlaySound("severed");
#endif
                                    break;
                                }
                        }

                    }


                }
                else if (id == "stapler")
                {
                    var contained = __instance.GetComponent<ItemContainer>().Inventory.FirstOrDefault();
                    if (surgeryReady(user.HasTalent("deliverysystem") ? SURGERY_TH_TALENT : SURGERY_TH_NORM) >= 0f && contained is not null)
                    {
                        foreach (var (aff, lh) in affs)
                            if (lh == lhs[targetLimb.HealthIndex] && aff is Affs.Sealed)
                            {
                                affs.Remove(aff);
                                break;
                            }
                        ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.CNCT_PFB, 20f, user, false, false);
                        Entity.Spawner.AddItemToRemoveQueue(contained);
                        foreach (var (aff, lh) in affs)
                            if (lh == lhs[targetLimb.HealthIndex] && aff is AfflictionBleeding)
                            {
                                affs.Remove(aff);
                                break;
                            }
                    }
                    else
                    {
                        ch.addLimbAffFast(lhs[targetLimb.HealthIndex], new AfflictionBleeding(BLEEDING_PFB, 2f)
                        {
                            Source = user
                        });
                        ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.PIERCE_PFB, 2f, user, true, true);
#if CLIENT
                        SoundPlayer.PlaySound("poke");
#endif
                    }
                }
            }
            return true;
        }
    }
}