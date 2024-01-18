using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Barotrauma.Networking;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(ItemComponent))]
    static class _ItemComponent
    {
        [HarmonyPostfix]
        [HarmonyPatch("ApplyStatusEffects", new Type[] { typeof(ActionType), typeof(float), typeof(Character), typeof(Limb), typeof(Entity), typeof(Character), typeof(Vector2), typeof(float) })]
        public static void _ApplyStatusEffects(ItemComponent __instance, in ActionType type, in float deltaTime, Character character, Limb targetLimb, Entity useTarget, Character user, in Vector2? worldPosition, in float afflictionMultiplier)
        {
            if (__instance.Item.HasTag("syringe"))
                switch (type)
                {
                    case ActionType.OnFailure:
                    case ActionType.OnSuccess:
                        if (character is not null)
                        {
                            float pierceAmount;
                            if (object.ReferenceEquals(user, character))
                                pierceAmount = user.HasTalent("selfcare") ? 0f : 1.25f;
                            else
                                pierceAmount = user is not null && user.HasTalent("deliverysystem") ? 0.5f : 1.25f;
                            var lhs = character.CharacterHealth.limbHealths;
                            character.CharacterHealth.addLimbAffFast(lhs[(targetLimb ?? character.AnimController.MainLimb).HealthIndex], Utils.PIERCE_PFB, pierceAmount,user,true,true);
                        }
                        break;
                }
        }
    }
}