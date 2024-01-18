using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using Microsoft.Xna.Framework;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(Item))]
    static class _Item
    {
        private static bool sulturable(in Identifier id) => id.Value switch
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
        [HarmonyPrefix]
        [HarmonyPatch("ApplyTreatment", new Type[] { typeof(Character), typeof(Character), typeof(Limb) })]
        public static bool _ApplyTreatment(Item __instance, Character user, Character character, Limb targetLimb)
        {
            if (!character.IsDead)
            {
                string id = __instance.Prefab.Identifier.Value;
                #region DEBUG
                if (character is null)
                {
                    DebugConsole.AddWarning("Target is null when applying: " + id);
                    return true;
                }
                if (user is null)
                {
                    DebugConsole.AddWarning("User is null when applying: " + id);
                    return true;
                }
                if (targetLimb is null)
                {
                    DebugConsole.AddWarning("Limb is null when applying: " + id);
                    return true;
                }
                #endregion
                const float SURGERY_TH_NORM = 100f, SURGERY_TH_TALENT = 75f, SCALPEL_SKILL = 135f, LAC_MOD_NORM = 1f, LAC_MOD_TALENT = 0.875f, BLEED_MOD_NORM = 1f, BLEED_MOD_TALENT = 0.75f, ASSISTANCE_MOD = 0.9375f;

                var ch = character.CharacterHealth;
                var lhs = ch.limbHealths;
                var affs = ch.afflictions;
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

                if (id == "bloodextractor")
                {
                    var contained = __instance.GetComponent<ItemContainer>().Inventory.FirstOrDefault();
                    if (contained is not null)
                    {
                        Entity.Spawner.AddItemToRemoveQueue(contained);
                        foreach (Affliction aff in affs.Keys)
                            if (aff is Affs.BloodType)
                            {
                                Entity.Spawner.AddItemToSpawnQueue(ItemPrefab.Prefabs["raw_" + aff.Identifier], user.Inventory);
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.PIERCE_PFB, 2f, user);
                                var tempAff = new AfflictionBleeding(BLEEDING_PFB, 2f);
                                tempAff.Source = user;
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], tempAff);
                                ch.addLimbAffFast(null, Utils.BLOODLOSS_PFB, user.HasTalent("bloodybusiness") ? 25f : 40f, user, true, true);
                                break;
                            }
                    }
                }
                else if (id.StartsWith("raw_"))
                {
                    if (id.EndsWith("_empty"))//applying empty packs means removing exisitng packs.
                    {
                        byte packCounts = 0;
                        foreach (var (aff, lh) in affs)
                            if (lh == lhs[targetLimb.HealthIndex] && object.ReferenceEquals(aff.Prefab, Utils.HEATED_PFB))
                            {
                                packCounts += (byte)Math.Ceiling(aff.Strength);
                                affs.Remove(aff);
                                break;
                            }
                        foreach (var (aff, lh) in affs)
                            if (lh == lhs[targetLimb.HealthIndex] && object.ReferenceEquals(aff.Prefab, Utils.ICED_PFB))
                            {
                                packCounts += (byte)Math.Ceiling(aff.Strength);
                                affs.Remove(aff);
                                break;
                            }
#if CLIENT
                        if(packCounts==0)
                            GUI.AddMessage(TextManager.Get("packs.not_equipped"), GUIStyle.Blue);  
#endif
                        while (packCounts-- > 0)
                            Entity.Spawner.AddItemToSpawnQueue(Utils.RAW_EMPTY_PFB, user.Inventory);
                    }
                    else
                    {
                        foreach (Affliction aff in affs.Keys)
                            if (aff is Affs.BloodType)
                            {
                                float hemoStrg = Utils.bloodTypeCompat(id.Remove(0, 9), aff.Identifier.Value.Remove(0, 5)) ? 5f : 36f;
                                var tempAff = new Affs.Hemolysis(Utils.HEMOLYSIS_PFB, hemoStrg);
                                tempAff.Source = user;
                                ch.addLimbAffFast(null, tempAff, true, true);
                                break;
                            }
                        Entity.Spawner.AddItemToSpawnQueue(Utils.RAW_EMPTY_PFB, user.Inventory);
                    }
                }
                else if (id.StartsWith("proc_"))
                {
                    foreach (Affliction aff in affs.Keys)
                        if (aff is Affs.BloodType)
                        {
                            if (!Utils.bloodTypeCompat(id.Remove(0, 10), aff.Identifier.Value.Remove(0, 5)))
                            {
                                var tempAff = new Affs.Hemolysis(Utils.HEMOLYSIS_PFB, 20f);
                                tempAff.Source = user;
                                ch.addLimbAffFast(null, tempAff, true, true);
                            }
                            break;
                        }
                    Entity.Spawner.AddItemToSpawnQueue(Utils.RAW_EMPTY_PFB, user.Inventory);
                }
                else if (id.StartsWith("antibleeding"))
                {
                    float currBdgStrg = 0f;
                    foreach (var (aff, lh) in affs)
                        if (lh == lhs[targetLimb.HealthIndex] && aff is Affs.Bandaged)
                        {
                            currBdgStrg = aff.Strength;
                            break;
                        }
                    float mod = user.HasTalent("medicalexpertise") ? Affs.Bandaged.TALENT_MOD : 1f;//this talent increases the threshold for all bandages.
                    switch (id[12])
                    {
                        case '1':
                            if (currBdgStrg >= 6f * mod)
                                return false;
                            break;
                        case '2':
                            if (currBdgStrg >= 12f * mod)
                                return false;
                            break;
                        case '3':
                            if (currBdgStrg >= 24f * mod)
                                return false;
                            break;
                    }
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
                    foreach (Affliction aff in affs.Keys)
                        if (aff is Affs.BloodType)
                        {
                            ic.TextChanged(textBox, aff.Name);
#if CLIENT
                                    ic.UpdateSignalsProjSpecific();
#endif
                            break;
                        }

                }
                else if (id == "scalpel")
                {
                    var action = __instance.GetComponent<Replace.Choose>().customInterfaceElementList[0].Signal;
                    float docPain;
                    if (object.ReferenceEquals(character.Inventory.GetItemInLimbSlot(InvSlotType.RightHand), __instance))
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
                    float minInci = user.HasTalent("deliverysystem") ? SURGERY_TH_TALENT : SURGERY_TH_NORM;
                    switch (action)
                    {
                        case "scalpel.incise":
                            {
                                float assMod = user.CharacterHealth.GetAffliction("medicalassistance", false) is null ? 1f : ASSISTANCE_MOD;
                                float inciMod = 1 + MathF.Pow(Math.Max(1 - r, 0f) / 2, 2) * assMod;
                                foreach (var (aff, lh) in affs)
                                    if (lh == lhs[targetLimb.HealthIndex] && aff is Affs.Bandaged)
                                    {
                                        affs.Remove(aff);
                                        break;
                                    }
                                var tempAff = new AfflictionBleeding(BLEEDING_PFB, 15f * bleedMod * qualMod * patientPainMod * assMod);
                                tempAff.Source = user;
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], tempAff);
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], LAC_PFB, 10f * lacMod * qualMod * patientPainMod * assMod, user);
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.INCISION_PFB, Math.Abs(surgeryReady(minInci)) * inciMod * patientPainMod, user, true, true);
                                break;
                            }
                        case "scalpel.lung":
                            if (targetLimb.type == LimbType.Torso && surgeryReady(minInci) >= 0f && organMiss(LUNG_M_PFB) is null)
                            {
                                var o = organCond(Utils.LUNG_DMG_PFB);
                                Entity.Spawner.AddItemToSpawnQueue(ItemPrefab.Prefabs["lung"], user.Inventory, o is null ? 100f : Utils.LUNG_DMG_PFB.MaxStrength - o.Strength);

                                Affliction tempAff = new AfflictionBleeding(BLEEDING_PFB, 6f * bleedMod * qualMod * patientPainMod);
                                tempAff.Source = user;
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], tempAff);
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], LAC_PFB, 4f * lacMod * qualMod * patientPainMod, user);
                                tempAff = new Affs.LungDmg(LUNG_M_PFB, 1f);
                                tempAff.Source = user;
                                ch.addLimbAffFast(null, tempAff, false, true);
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

                                Affliction tempAff = new AfflictionBleeding(BLEEDING_PFB, 5f * bleedMod * qualMod * patientPainMod);
                                tempAff.Source = user;
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], tempAff);
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], LAC_PFB, 3f * lacMod * qualMod * patientPainMod, user);
                                tempAff = new Affs.LiverDmg(LIVER_M_PFB, 1f);
                                tempAff.Source = user;
                                ch.addLimbAffFast(null, tempAff, false, true);
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

                                Affliction tempAff = new AfflictionBleeding(BLEEDING_PFB, 7f * bleedMod * qualMod * patientPainMod);
                                tempAff.Source = user;
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], tempAff);
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], LAC_PFB, 4f * lacMod * qualMod * patientPainMod, user);
                                tempAff = new Affs.HeartDmg(HEART_M_PFB, 1f);
                                tempAff.Source = user;
                                ch.addLimbAffFast(null, tempAff, false, true);
                            }
#if CLIENT
                            else
                                GUI.AddMessage(TextManager.Get("surgery.cannot"), GUIStyle.Red);
#endif
                            break;
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
                    if (object.ReferenceEquals(character.Inventory.GetItemInLimbSlot(InvSlotType.RightHand), __instance))
                        docPain = Utils.getLimbPain(user.AnimController.GetLimb(LimbType.RightArm), user.CharacterHealth);
                    else
                        docPain = Utils.getLimbPain(user.AnimController.GetLimb(LimbType.LeftArm), user.CharacterHealth);
                    docPain = (docPain * 0.75f + Utils.getLimbPain(character.AnimController.GetLimb(LimbType.Head), user.CharacterHealth) * 0.25f) / Utils.PAIN_PFB.MaxStrength;
                    float r = user.GetSkillLevel("medical") / SCALPEL_SKILL * (1 - docPain);
                    float qualMod = 1f - (__instance.Quality / Quality.MaxQuality) * 0.12f;
                    float patientPainMod = 1f;
                    float rv = Rand.Value(Rand.RandSync.ServerAndClient);
                    if (!character.IsIncapacitated)
                        patientPainMod += rv;
                    var holding = user.Inventory.GetItemInLimbSlot(InvSlotType.RightHand);
                    if (holding is null || !holding.HasTag("organ"))
                        holding = user.Inventory.GetItemInLimbSlot(InvSlotType.LeftHand);
                    if (holding is not null && holding.HasTag("organ"))
                    {
                        var contained = __instance.GetComponent<ItemContainer>().Inventory.FirstOrDefault();
                        if (contained is not null)
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
                                    ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.PIERCE_PFB, 10f * qualMod * pierceMod, user);
                                    var tempAff = new Affs.Rejection(REJECT_PFB, rv * rejectMod * REJECT_PFB.MaxStrength);
                                    tempAff.Source = user;
                                    ch.addLimbAffFast(null, tempAff, true, true);

                                    Entity.Spawner.AddItemToRemoveQueue(contained);
                                    Entity.Spawner.AddItemToRemoveQueue(holding);
                                    affs.Remove(toRmv);
                                }
                            }
#if CLIENT
                                else
                                    GUI.AddMessage(TextManager.Get("surgery.cannot"), GUIStyle.Red);
#endif
                        }
                    }
                    else
                    {
                        var contained = __instance.GetComponent<ItemContainer>().Inventory.FirstOrDefault();
                        float assMod = user.CharacterHealth.GetAffliction("medicalassistance", false) is null ? 1f : ASSISTANCE_MOD;
                        if (contained is not null)
                        {
                            if (holding is not null && holding.Prefab.Identifier == "antibleeding2")
                            {
                                float strg = 0f;
                                foreach (var (aff, lh) in affs)
                                    if (lh == lhs[targetLimb.HealthIndex] && sulturable(aff.Identifier))
                                    {
                                        strg += aff.Strength / aff.Prefab.MaxStrength;
                                        affs.Remove(aff);
                                    }
                                Entity.Spawner.AddItemToRemoveQueue(contained);
                                Entity.Spawner.AddItemToRemoveQueue(holding);
                                float pierceMod = rv < r ? 1f : patientPainMod;
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.PIERCE_PFB, strg / 12.5f * Utils.PIERCE_PFB.MaxStrength * qualMod * pierceMod * assMod, user);
                                ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.SULTURAL_PFB, strg * Utils.SULTURAL_PFB.MaxStrength * assMod, user, true, true);
                            }
                            else
                            {
                                foreach (var (aff, lh) in affs)
                                    if (lh == lhs[targetLimb.HealthIndex] && ReferenceEquals(aff.Prefab, Utils.INCISION_PFB))
                                    {
                                        float pierceMod = rv < r ? 1f : patientPainMod;
                                        Entity.Spawner.AddItemToRemoveQueue(contained);
                                        ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.PIERCE_PFB, aff.Strength / 12.5f * qualMod * pierceMod * assMod, user);
                                        ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.SULTURAL_PFB, aff.Strength * assMod, user, true, true);
                                        affs.Remove(aff);
                                        break;
                                    }
                            }

                        }
                        else
                        {
                            foreach (var (aff, lh) in affs)
                                if (lh == lhs[targetLimb.HealthIndex] && ReferenceEquals(aff.Prefab, Utils.SULTURAL_PFB))
                                {
                                    float bleedMod = (user.HasTalent("bloodybusiness") ? BLEED_MOD_TALENT : BLEED_MOD_NORM) * (rv < r ? 1f : 1.125f);
                                    float lacMod = (user.HasTalent("drsubmarine") ? LAC_MOD_TALENT : LAC_MOD_NORM) * (rv < r ? 1f : 1.125f);
                                    ch.addLimbAffFast(lh, Utils.SCAR_PFB, 5f * lacMod * qualMod * assMod, user);
                                    var tempAff = new AfflictionBleeding(BLEEDING_PFB, 7.5f * bleedMod * qualMod * assMod);
                                    tempAff.Source = user;
                                    ch.addLimbAffFast(lh, tempAff);
                                    ch.addLimbAffFast(lh, Utils.INCISION_PFB, aff.Strength, user, true, true);
                                    affs.Remove(aff);
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
                        var tempAff = new AfflictionBleeding(BLEEDING_PFB, 2f);
                        tempAff.Source = user;
                        ch.addLimbAffFast(lhs[targetLimb.HealthIndex], tempAff);
                        ch.addLimbAffFast(lhs[targetLimb.HealthIndex], Utils.PIERCE_PFB, 2f, user, true, true);
                    }
                }
            }
            return true;
        }
        [HarmonyPrefix]//auto injector headset
        [HarmonyPatch("ApplyStatusEffects", new Type[] { typeof(ActionType), typeof(float), typeof(Character), typeof(Limb), typeof(Entity), typeof(bool), typeof(Vector2?) })]
        public static void _ApplyStatusEffects(Item __instance, ActionType type, in float deltaTime, Character character, Limb limb, Entity useTarget, in bool isNetworkEvent, in Vector2? worldPosition)
        {
            if (type == ActionType.OnSuccess && useTarget is Character target)
            {
                string id = __instance.Prefab.Identifier.Value;
                var ch = target.CharacterHealth;
                var affs = ch.afflictions;
                if (id.StartsWith("raw_"))
                {
                    foreach (Affliction aff in affs.Keys)
                        if (aff is Affs.BloodType)
                        {
                            float hemoStrg = Utils.bloodTypeCompat(id.Remove(0, 9), aff.Identifier.Value.Remove(0, 5)) ? 5f : 36f;
                            var tempAff = new Affs.Hemolysis(Utils.HEMOLYSIS_PFB, hemoStrg);
                            tempAff.Source = character;
                            ch.addLimbAffFast(null, tempAff, true, true);
                            break;
                        }
                    Entity.Spawner.AddItemToSpawnQueue(Utils.RAW_EMPTY_PFB, character.Inventory);
                }
                else if (id.StartsWith("proc_"))
                {
                    foreach (Affliction aff in affs.Keys)
                        if (aff is Affs.BloodType)
                        {
                            if (!Utils.bloodTypeCompat(id.Remove(0, 10), aff.Identifier.Value.Remove(0, 5)))
                            {
                                var tempAff = new Affs.Hemolysis(Utils.HEMOLYSIS_PFB, 20f);
                                tempAff.Source = character;
                                ch.addLimbAffFast(null, tempAff, true, true);
                            }
                            break;
                        }
                    Entity.Spawner.AddItemToSpawnQueue(Utils.RAW_EMPTY_PFB, character.Inventory);
                }
            }
        }
    }
}