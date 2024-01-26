using Barotrauma;
using Microsoft.Xna.Framework;
namespace PrimMed.Replace
{
    partial class FastSE : StatusEffect
    {
        public delegate float CondAffStrg(Character user, IReadOnlyList<ISerializableEntity> targets);
        private readonly CondAffStrg affCond;
        public FastSE(ContentXElement element, string parentDebugName, CondAffStrg userCond) : base(element, parentDebugName)
        {
            this.affCond = userCond;
        }
        public override void Apply(ActionType type, float deltaTime, Entity entity, ISerializableEntity target, Vector2? worldPosition = null)
        {
            if (Disabled)
            {
                return;
            }
            if (this.type != type /*|| !HasRequiredItems(entity)*/)
            {
                return;
            }

            if (!IsValidTarget(target))
            {

                return;
            }

            /*            if (Duration > 0.0f && !Stackable)
                        {
                            //ignore if not stackable and there's already an identical statuseffect
                            DurationListElement existingEffect = DurationList.Find(d => d.Parent == this && d.Targets.FirstOrDefault() == target);
                            if (existingEffect != null)
                            {
                                existingEffect.Reset(Math.Max(existingEffect.Timer, Duration), user);
                                return;
                            }
                        }*/

            currentTargets.Clear();
            currentTargets.Add(target);
            /*            if (!HasRequiredConditions(currentTargets))
                        {
                            return;
                        }*/
            float m = affCond(user, currentTargets);
            /*            if (m < 0)
                            return;*/
            foreach (var aff in Afflictions)
                aff.SetStrength(m);
            Apply(deltaTime, entity, currentTargets, worldPosition);
        }
        public override void Apply(ActionType type, float deltaTime, Entity entity, IReadOnlyList<ISerializableEntity> targets, Vector2? worldPosition = null)
        {
            if (Disabled)
            {
                return;
            }
            if (this.type != type)
            {
                return;
            }
            if (ShouldWaitForInterval(entity, deltaTime))
            {
                return;
            }

            currentTargets.Clear();
            foreach (ISerializableEntity target in targets)
            {
                if (!IsValidTarget(target))
                {
                    continue;
                }
                currentTargets.Add(target);
            }

            if (TargetIdentifiers != null && currentTargets.Count == 0)
            {
                return;
            }

            /*            bool hasRequiredItems = HasRequiredItems(entity);
                        if (!hasRequiredItems || !HasRequiredConditions(currentTargets))
                        {
            #if CLIENT
                            if (!hasRequiredItems && playSoundOnRequiredItemFailure)
                            {
                                PlaySound(entity, GetHull(entity), GetPosition(entity, targets, worldPosition));
                            }
            #endif
                            return;
                        }*/

            /*            if (Duration > 0.0f && !Stackable)
                        {
                            //ignore if not stackable and there's already an identical statuseffect
                            DurationListElement existingEffect = DurationList.Find(d => d.Parent == this && d.Targets.SequenceEqual(currentTargets));
                            if (existingEffect != null)
                            {
                                existingEffect?.Reset(Math.Max(existingEffect.Timer, Duration), user);
                                return;
                            }
                        }*/
            float m = affCond(user, currentTargets);
            /*            if (m < 0)
                            return;*/
            foreach (var aff in Afflictions)
                aff.SetStrength(m);
            Apply(deltaTime, entity, currentTargets, worldPosition);
        }
    }
}