using Barotrauma;
using Barotrauma.Items.Components;
using Barotrauma.Networking;
using Microsoft.Xna.Framework;

namespace PrimMed.Replace
{
    partial class AltGun : RangedWeapon
    {
        public AltGun(Item item, ContentXElement element)
           : base(item, element)
        {
            //need to remove the original `RangedWeapon` component. The old components might still be there if we're overriding an item.
            var origIc = item.components.Find(static (ic) => ic.GetType() == typeof(RangedWeapon));
            item.components.Remove(origIc);
            item.updateableComponents.Remove(origIc);
            item.componentsByType[typeof(RangedWeapon)] = this;
        }

        public Projectile FindProjectile(byte slotIndex)
        {
            foreach (ItemContainer container in item.GetComponents<ItemContainer>())
                foreach (Item containedItem in container.Inventory.GetItemsAt(slotIndex))
                    if (containedItem is not null)
                    {
                        Projectile projectile = containedItem.GetComponent<Projectile>();
                        if (IsSuitableProjectile(projectile))
                            return projectile;

                    }
            return null;
        }

        public override bool Use(float deltaTime, Character character = null)
        {
            tryingToCharge = true;
            if (character == null || character.Removed)
            {
                return false;
            }
            if ((item.RequireAimToUse && !character.IsKeyDown(InputType.Aim)) || ReloadTimer > 0.0f)
            {
                return false;
            }
            if (currentChargeTime < MaxChargeTime)
            {
                return false;
            }

            IsActive = true;
            float baseReloadTime = reload;
            float weaponSkill = character.GetSkillLevel("weapons");
            bool applyReloadFailure = ReloadSkillRequirement > 0 && ReloadNoSkill > reload && weaponSkill < ReloadSkillRequirement;
            if (applyReloadFailure)
            {
                //Examples, assuming 40 weapon skill required: 1 - 40/40 = 0 ... 1 - 0/40 = 1 ... 1 - 20 / 40 = 0.5
                float reloadFailure = MathHelper.Clamp(1 - (weaponSkill / ReloadSkillRequirement), 0, 1);
                baseReloadTime = MathHelper.Lerp(reload, ReloadNoSkill, reloadFailure);
            }

            if (character.IsDualWieldingRangedWeapons())
            {
                baseReloadTime *= Math.Max(1f, ApplyDualWieldPenaltyReduction(character, DualWieldReloadTimePenaltyMultiplier, neutralValue: 1f));
            }

            ReloadTimer = baseReloadTime / (1 + character?.GetStatValue(StatTypes.RangedAttackSpeed) ?? 0f);
            ReloadTimer /= 1f + item.GetQualityModifier(Quality.StatType.FiringRateMultiplier);

            currentChargeTime = 0f;

            var abilityRangedWeapon = new AbilityRangedWeapon(item);
            character.CheckTalents(AbilityEffectType.OnUseRangedWeapon, abilityRangedWeapon);

            if (item.AiTarget != null)
            {
                item.AiTarget.SoundRange = item.AiTarget.MaxSoundRange;
                item.AiTarget.SightRange = item.AiTarget.MaxSightRange;
            }

            ignoredBodies.Clear();
            foreach (Limb l in character.AnimController.Limbs)
            {
                if (l.IsSevered)
                {
                    continue;
                }
                ignoredBodies.Add(l.body.FarseerBody);
            }

            foreach (Item heldItem in character.HeldItems)
            {
                var holdable = heldItem.GetComponent<Holdable>();
                if (holdable?.Pusher != null)
                {
                    ignoredBodies.Add(holdable.Pusher.FarseerBody);
                }
            }

            float degreeOfFailure = 1.0f - DegreeOfSuccess(character);
            degreeOfFailure *= degreeOfFailure;
            if (degreeOfFailure > Rand.Range(0.0f, 1.0f))
            {
                ApplyStatusEffects(ActionType.OnFailure, 1.0f, character);
            }

            for (byte i = 0; i < ProjectileCount; ++i)
            {
                Projectile projectile = FindProjectile(i);
                if (projectile != null)
                {
                    Vector2 barrelPos = TransformedBarrelPos + item.body.SimPosition;
                    float rotation = (Item.body.Dir == 1.0f) ? Item.body.Rotation : Item.body.Rotation - MathHelper.Pi;
                    float spread = GetSpread(character) * projectile.GetSpreadFromPool();
                    Patches._RangedWeapon._GetSpread(this, ref spread, character);

                    float damageMultiplier = (1f + item.GetQualityModifier(Quality.StatType.FirepowerMultiplier)) * WeaponDamageModifier;
                    projectile.Launcher = item;
                    projectile.Shoot(character, character.AnimController.AimSourceSimPos, barrelPos, rotation + spread, ignoredBodies: ignoredBodies.ToList(), createNetworkEvent: false, damageMultiplier, LaunchImpulse);
                    projectile.Item.GetComponent<Rope>()?.Attach(Item, projectile.Item);
                    if (projectile.Item.body != null)
                    {
                        if (i == 0)
                        {
                            Item.body.ApplyLinearImpulse(new Vector2((float)Math.Cos(projectile.Item.body.Rotation), (float)Math.Sin(projectile.Item.body.Rotation)) * Item.body.Mass * -50.0f, maxVelocity: NetConfig.MaxPhysicsBodyVelocity);
                        }
                        projectile.Item.body.ApplyTorque(projectile.Item.body.Mass * degreeOfFailure * 20.0f * projectile.GetSpreadFromPool());
                    }
                    Item.RemoveContained(projectile.Item);
                }
            }
#if CLIENT
            LaunchProjSpecific();
#endif

            return true;
        }
    }
}