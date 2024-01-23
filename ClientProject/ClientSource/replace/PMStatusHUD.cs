using Barotrauma;
using FarseerPhysics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace PrimMed.Replace
{
    partial class PMStatusHUD
    {
        public override void DrawHUD(SpriteBatch spriteBatch, Character character)
        {
            if (character == null)
            {
                return;
            }

            if (OverlayColor.A > 0)
            {
                GUIStyle.UIGlow.Draw(spriteBatch, new Rectangle(0, 0, GameMain.GraphicsWidth, GameMain.GraphicsHeight),
                    OverlayColor);
            }

            if (ShowTexts)
            {
                Character closestCharacter = null;
                float closestDist = float.PositiveInfinity;
                foreach (Character c in visibleCharacters)
                {
                    if (c == character || !c.Enabled || c.Removed)
                    {
                        continue;
                    }

                    float dist = Vector2.DistanceSquared(GameMain.GameScreen.Cam.ScreenToWorld(PlayerInput.MousePosition), c.WorldPosition);
                    if (dist < closestDist)
                    {
                        closestCharacter = c;
                        closestDist = dist;
                    }
                }

                if (closestCharacter != null)
                {
                    float dist = Vector2.Distance(GameMain.GameScreen.Cam.ScreenToWorld(PlayerInput.MousePosition), closestCharacter.WorldPosition);
                    DrawCharacterInfo(spriteBatch, closestCharacter, 1.0f - MathHelper.Max((dist - (Range - FadeOutRange)) / FadeOutRange, 0.0f));
                }
            }

            if (ThermalGoggles)
            {
                spriteBatch.End();
                GameMain.LightManager.SolidColorEffect.Parameters["color"].SetValue(Color.Red.ToVector4() * (0.3f + MathF.Sin(thermalEffectState) * 0.05f));
                GameMain.LightManager.SolidColorEffect.CurrentTechnique = GameMain.LightManager.SolidColorEffect.Techniques["SolidColorBlur"];
                GameMain.LightManager.SolidColorEffect.Parameters["blurDistance"].SetValue(0.01f + MathF.Sin(thermalEffectState) * 0.005f);
                GameMain.LightManager.SolidColorEffect.CurrentTechnique.Passes[0].Apply();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, transformMatrix: Screen.Selected.Cam.Transform, effect: GameMain.LightManager.SolidColorEffect);

                Entity refEntity = equipper;
                if (!isEquippable || refEntity == null)
                {
                    refEntity = item;
                }

                foreach (Character c in Character.CharacterList)
                {
                    if (c == character || !c.Enabled || c.Removed || c.Params.HideInThermalGoggles)
                    {
                        continue;
                    }
                    if (!ShowDeadCharacters && c.IsDead)
                    {
                        continue;
                    }

                    float dist = Vector2.DistanceSquared(refEntity.WorldPosition, c.WorldPosition);
                    if (dist > Range * Range)
                    {
                        continue;
                    }

                    const float hideInThermalGogglesTemp = -60f;
                    if (c.IsHuman)
                        foreach (var aff in c.CharacterHealth.afflictions.Keys)
                            if (aff is Affs.Mortal m)
                                if (m.temp <= hideInThermalGogglesTemp)
                                    goto END;

                    Sprite pingCircle = GUIStyle.UIThermalGlow.Value.Sprite;
                    foreach (Limb limb in c.AnimController.Limbs)
                    {
                        if (limb.Mass < 0.5f && limb != c.AnimController.MainLimb)
                        {
                            continue;
                        }
                        float noise1 = PerlinNoise.GetPerlin((thermalEffectState + limb.Params.ID + c.ID) * 0.01f, (thermalEffectState + limb.Params.ID + c.ID) * 0.02f);
                        float noise2 = PerlinNoise.GetPerlin((thermalEffectState + limb.Params.ID + c.ID) * 0.01f, (thermalEffectState + limb.Params.ID + c.ID) * 0.008f);
                        Vector2 spriteScale = ConvertUnits.ToDisplayUnits(limb.body.GetSize()) / pingCircle.size * (noise1 * 0.5f + 2f);
                        Vector2 drawPos = new Vector2(limb.body.DrawPosition.X + (noise1 - 0.5f) * 100, -limb.body.DrawPosition.Y + (noise2 - 0.5f) * 100);
                        pingCircle.Draw(spriteBatch, drawPos, 0.0f, scale: Math.Max(spriteScale.X, spriteScale.Y));
                    }
                END:
                    ;
                }

                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            }
        }

        new private void DrawCharacterInfo(SpriteBatch spriteBatch, Character target, float alpha = 1.0f)
        {
            Vector2 hudPos = GameMain.GameScreen.Cam.WorldToScreen(target.DrawPosition);
            hudPos += Vector2.UnitX * 50.0f;

            List<LocalizedString> texts = new List<LocalizedString>();
            List<Color> textColors = new List<Color>();
            texts.Add(target.Info == null ? target.DisplayName : target.Info.DisplayName);
            Color nameColor = GUIStyle.TextColorNormal;
            if (Character.Controlled != null && target.TeamID != Character.Controlled.TeamID)
            {
                nameColor = target.TeamID == CharacterTeamType.FriendlyNPC ? Color.SkyBlue : GUIStyle.Red;
            }
            textColors.Add(nameColor);

            if (target.IsDead)
            {
                texts.Add(TextManager.Get("Deceased"));
                textColors.Add(GUIStyle.Red);
                if (target.CauseOfDeath != null)
                {
                    texts.Add(
                        target.CauseOfDeath.Affliction?.CauseOfDeathDescription ??
                        TextManager.AddPunctuation(':', TextManager.Get("CauseOfDeath"), TextManager.Get("CauseOfDeath." + target.CauseOfDeath.Type.ToString())));
                    textColors.Add(GUIStyle.Red);
                }
            }
            else
            {
                if (!target.CustomInteractHUDText.IsNullOrEmpty() && target.AllowCustomInteract)
                {
                    texts.Add(target.CustomInteractHUDText);
                    textColors.Add(GUIStyle.Green);
                }
                if (!target.IsIncapacitated && target.IsPet)
                {
                    texts.Add(CharacterHUD.GetCachedHudText("PlayHint", InputType.Use));
                    textColors.Add(GUIStyle.Green);
                }
                if (target.CharacterHealth.UseHealthWindow && !target.DisableHealthWindow && equipper?.FocusedCharacter == target && equipper.CanInteractWith(target, 160f, false))
                {
                    texts.Add(CharacterHUD.GetCachedHudText("HealHint", InputType.Health));
                    textColors.Add(GUIStyle.Green);
                }
                if (target.CanBeDragged)
                {
                    texts.Add(CharacterHUD.GetCachedHudText("GrabHint", InputType.Grab));
                    textColors.Add(GUIStyle.Green);
                }

                if (target.IsUnconscious)
                {
                    texts.Add(TextManager.Get("Unconscious"));
                    textColors.Add(GUIStyle.Orange);
                }
                if (target.Stun > 0.01f)
                {
                    texts.Add(TextManager.Get("Stunned"));
                    textColors.Add(GUIStyle.Orange);
                }

                int oxygenTextIndex = MathHelper.Clamp((int)Math.Floor((1.0f - (target.Oxygen / 100.0f)) * OxygenTexts.Length), 0, OxygenTexts.Length - 1);
                texts.Add(OxygenTexts[oxygenTextIndex]);
                textColors.Add(Color.Lerp(GUIStyle.Red, GUIStyle.Green, target.Oxygen / 100.0f));

                if (target.Bleeding > 0.0f)
                {
                    int bleedingTextIndex = MathHelper.Clamp((int)Math.Floor(target.Bleeding / 100.0f * BleedingTexts.Length), 0, BleedingTexts.Length - 1);
                    texts.Add(BleedingTexts[bleedingTextIndex]);
                    textColors.Add(Color.Lerp(GUIStyle.Orange, GUIStyle.Red, target.Bleeding / 100.0f));
                }

                var allAfflictions = target.CharacterHealth.GetAllAfflictions();
                Dictionary<AfflictionPrefab, float> combinedAfflictionStrengths = new Dictionary<AfflictionPrefab, float>();
                foreach (Affliction affliction in allAfflictions)
                {
                    if (affliction.Strength <= 0f)
                        continue;

                    const float maxSeeSkill = 170f;//170 is dr.sub+medical expertise+self care+medic officer cloth.
                    if (affliction.Prefab.AfflictionType == AfflictionPrefab.PoisonType || affliction.Prefab.AfflictionType == AfflictionPrefab.ParalysisType)
                    {
                        if (target.IsHuman || target.IsOnPlayerTeam)
                        {//poison types on monsters should always be displayed.
                            float chance = Math.Min((affliction.Strength - affliction.Prefab.ShowInHealthScannerThreshold) / affliction.Prefab.MaxStrength, 1f);
                            chance *= equipper.GetSkillLevel("medical") / maxSeeSkill;
                            if (equipper.HasTalent("whatastench"))
                                chance *= 1.125f;
                            if (Rand.Value(Rand.RandSync.Unsynced) > chance)
                                continue;
                        }
                    }
                    else if (affliction.Strength < affliction.Prefab.ShowInHealthScannerThreshold)
                        continue;

                    if (combinedAfflictionStrengths.ContainsKey(affliction.Prefab))
                    {
                        combinedAfflictionStrengths[affliction.Prefab] += affliction.Strength;
                    }
                    else
                    {
                        combinedAfflictionStrengths[affliction.Prefab] = affliction.Strength;
                    }
                }

                foreach (AfflictionPrefab affliction in combinedAfflictionStrengths.Keys)
                {
                    texts.Add(TextManager.AddPunctuation(':', affliction.Name, Math.Max((int)combinedAfflictionStrengths[affliction], 1).ToString() + " %"));
                    textColors.Add(Color.Lerp(GUIStyle.Orange, GUIStyle.Red, combinedAfflictionStrengths[affliction] / affliction.MaxStrength));
                }
            }

            GUI.DrawString(spriteBatch, hudPos, texts[0].Value, textColors[0] * alpha, Color.Black * 0.7f * alpha, 2, GUIStyle.SubHeadingFont, ForceUpperCase.No);
            hudPos.X += 5.0f * GUI.Scale;
            hudPos.Y += GUIStyle.SubHeadingFont.MeasureString(texts[0].Value).Y;

            hudPos.X = (int)hudPos.X;
            hudPos.Y = (int)hudPos.Y;

            for (int i = 1; i < texts.Count; i++)
            {
                GUI.DrawString(spriteBatch, hudPos, texts[i], textColors[i] * alpha, Color.Black * 0.7f * alpha, 2, GUIStyle.SmallFont);
                hudPos.Y += (int)(GUIStyle.SubHeadingFont.MeasureString(texts[i].Value).Y);
            }
        }
    }
}
