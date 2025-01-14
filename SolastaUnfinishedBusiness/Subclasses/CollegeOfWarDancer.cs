﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.Extensions;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomBehaviors;
using SolastaUnfinishedBusiness.CustomInterfaces;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;

namespace SolastaUnfinishedBusiness.Subclasses;

internal sealed class CollegeOfWarDancer : AbstractSubclass
{
    private const string PowerWarDanceName = "PowerWarDancerWarDance";

    private static readonly FeatureDefinition ImproveWarDance = FeatureDefinitionBuilder
        .Create("FeatureCollegeOfWarDancerImprovedWarDance")
        .SetGuiPresentation(Category.Feature)
        .AddToDB();

    internal CollegeOfWarDancer()
    {
        var warDance = FeatureDefinitionPowerBuilder
            .Create(PowerWarDanceName)
            .SetCustomSubFeatures(ValidatorsCharacter.HasMeleeWeaponInMainHand)
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.BardicInspiration)
            .SetGuiPresentation(Category.Feature, SpellDefinitions.MagicWeapon)
            .SetEffectDescription(EffectDescriptionBuilder
                .Create()
                .SetTargetingData(Side.All, RangeType.Self, 0, TargetType.Self)
                .SetDurationData(DurationType.Round, 1, TurnOccurenceType.StartOfTurn)
                .SetParticleEffectParameters(FeatureDefinitionPowers.PowerBardGiveBardicInspiration)
                .SetEffectForms(EffectFormBuilder
                        .Create()
                        .SetConditionForm(ConditionWarDance, ConditionForm.ConditionOperation.Add)
                        .Build(),
                    EffectFormBuilder
                        .Create()
                        .SetConditionForm(ConditionDefinitions.ConditionBardicInspiration,
                            ConditionForm.ConditionOperation.Add)
                        .Build()
                )
                .Build())
            .SetCustomSubFeatures(ValidatorsPowerUse.HasNoCondition(ConditionWarDance.Name),
                new WarDanceRefundOneAttackOfMainAction())
            .AddToDB();

        Subclass = CharacterSubclassDefinitionBuilder
            .Create("CollegeOfWarDancer")
            .SetOrUpdateGuiPresentation(Category.Subclass, CharacterSubclassDefinitions.RangerSwiftBlade)
            .AddFeaturesAtLevel(3, warDance, CommonBuilders.FeatureSetCasterFightingProficiency)
            .AddFeaturesAtLevel(6, ImproveWarDance)
            .AddToDB();
    }

    internal override CharacterSubclassDefinition Subclass { get; }

    private static readonly ConditionDefinition ConditionWarDance = BuildConditionWarDance();

    private static ConditionDefinition BuildConditionWarDance()
    {
        return ConditionDefinitionBuilder
            .Create("ConditionWarDance")
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionRaging)
            .AddFeatures(FeatureDefinitionMovementAffinityBuilder
                    .Create("MovementAffinityConditionWarDanceExtraMovement3")
                    .SetGuiPresentationNoContent(true)
                    .SetBaseSpeedAdditiveModifier(3)
                    .SetImmunities(false, false, true)
                    .AddToDB(),
                FeatureDefinitionBuilder
                    .Create("FeatureConditionWarDanceCustom")
                    .SetGuiPresentationNoContent(true)
                    .SetCustomSubFeatures(new WarDanceFlurryAttack(),
                        new ExtendedWarDanceDurationOnKill())
                    .AddToDB(),
                FeatureDefinitionBuilder
                    .Create("FeatureConditionWarDanceSwitchWeaponFreely")
                    .SetGuiPresentationNoContent(true)
                    .SetCustomSubFeatures(new SwitchWeaponFreely())
                    .AddToDB()
            )
            .SetCustomSubFeatures(new RemoveOnAttackMissOrAttackWithNonMeleeWeapon())
            .AddToDB();
    }

    private sealed class RemoveOnAttackMissOrAttackWithNonMeleeWeapon
    {
    }

    internal override FeatureDefinitionSubclassChoice SubclassChoice =>
        FeatureDefinitionSubclassChoices.SubclassChoiceBardColleges;

    private sealed class WarDanceFlurryAttack : IAttackFinished
    {
        public IEnumerator OnAttackFinished(GameLocationBattleManager battleManager, CharacterAction action,
            GameLocationCharacter attacker, GameLocationCharacter defender, RulesetAttackMode attackerAttackMode,
            RollOutcome attackRollOutcome, int damageAmount)
        {
            if (attackerAttackMode == null || !attacker.RulesetCharacter.HasConditionOfType(ConditionWarDance))
            {
                yield break;
            }

            if (attackRollOutcome is not (RollOutcome.Failure or RollOutcome.CriticalFailure) &&
                ValidatorsWeapon.IsMelee(attackerAttackMode))
            {
                yield break;
            }

            RemoveConditionOnAttackMissOrAttackWithNonMeleeWeapon(attacker.RulesetCharacter);
        }

        internal static void RemoveConditionOnAttackMissOrAttackWithNonMeleeWeapon(RulesetCharacter attacker)
        {
            var conditionsToRemove = new List<RulesetCondition>();

            conditionsToRemove.AddRange(
                attacker.ConditionsByCategory
                    .SelectMany(x => x.Value)
                    .Where(x => x.ConditionDefinition
                                    .HasSubFeatureOfType<RemoveOnAttackMissOrAttackWithNonMeleeWeapon>() ||
                                x.conditionDefinition == ConditionDefinitions.ConditionBardicInspiration));

            foreach (var conditionToRemove in conditionsToRemove)
            {
                attacker.RemoveCondition(conditionToRemove);
            }
        }
    }

    private static readonly DamageDieProvider UpgradeDice = (character, _) => GetMomentumDice(character);

    private static DieType GetMomentumDice(RulesetCharacter character)
    {
        var slotsByName = character.CharacterInventory.InventorySlotsByName;
        var item = slotsByName[EquipmentDefinitions.SlotTypeMainHand].EquipedItem;

        if (item == null || !item.itemDefinition.isWeapon)
        {
            return DieType.D1;
        }

        var isLight = item.itemDefinition.WeaponDescription.WeaponTags.Contains(TagsDefinitions.WeaponTagLight);

        if (isLight)
        {
            return DieType.D6;
        }

        var isHeavy = item.itemDefinition.WeaponDescription.WeaponTags.Contains(TagsDefinitions.WeaponTagHeavy);

        return isHeavy ? DieType.D10 : DieType.D8;
    }

    private static readonly DieNumProvider UpgradeDieNum = (character, _) => GetMomentumDiceNum(character);

    private static int GetMomentumDiceNum(RulesetCharacter character)
    {
        const int BASE_VALUE = 2;

        var momentum = character.ConditionsByCategory
            .SelectMany(x => x.Value)
            .Count(x => x.ConditionDefinition == WarDanceMomentum);

        if (momentum <= 1)
        {
            return BASE_VALUE;
        }

        var slotsByName = character.CharacterInventory.InventorySlotsByName;
        var item = slotsByName[EquipmentDefinitions.SlotTypeMainHand].EquipedItem;

        if (item == null || !item.itemDefinition.isWeapon)
        {
            return BASE_VALUE;
        }

        var isLight = item.itemDefinition.WeaponDescription.WeaponTags.Contains(TagsDefinitions.WeaponTagLight);

        if (isLight)
        {
            return BASE_VALUE + ((momentum - 1) / 2);
        }

        var isHeavy = item.itemDefinition.WeaponDescription.WeaponTags.Contains(TagsDefinitions.WeaponTagHeavy);

        if (isHeavy)
        {
            return BASE_VALUE + (2 * (momentum - 1));
        }

        return BASE_VALUE + (momentum - 1);
    }

    private static readonly ConditionDefinition WarDanceMomentum = ConditionDefinitionBuilder
        .Create("ConditionWarDanceMomentum")
        .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionHeraldOfBattle)
        .AllowMultipleInstances()
        .SetSilent(Silent.WhenAddedOrRemoved)
        .SetFeatures(FeatureDefinitionAdditionalDamageBuilder
            .Create("AdditionalDamageWarDanceMomentum")
            .SetGuiPresentationNoContent(true)
            .SetAdditionalDamageType(AdditionalDamageType.SameAsBaseDamage)
            .SetDamageDice(DieType.D6, 2)
            .SetRequiredProperty(RestrictedContextRequiredProperty.MeleeWeapon)
            .SetAttackModeOnly()
            .SetTriggerCondition(AdditionalDamageTriggerCondition.AlwaysActive)
            .SetNotificationTag("Momentum")
            .SetCustomSubFeatures(UpgradeDice, UpgradeDieNum)
            .AddToDB())
        .SetSpecialInterruptions(ConditionInterruption.AnyBattleTurnEnd)
        .SetCustomSubFeatures(new RemoveOnAttackMissOrAttackWithNonMeleeWeapon())
        .AddToDB();

    private sealed class WarDanceRefundOneAttackOfMainAction : IMightRefundOneAttackOfMainAction
    {
        private static readonly ConditionDefinition WarDanceMomentumExtraAction = ConditionDefinitionBuilder
            .Create("ConditionWarDanceMomentumExtraAction")
            .SetGuiPresentationNoContent(true)
            .SetFeatures(
                FeatureDefinitionActionAffinityBuilder
                    .Create("ActionAffinityWarDanceExtraAction")
                    .SetGuiPresentationNoContent(true)
                    .SetDefaultAllowedActionTypes()
                    .SetMaxAttackNumber(1)
                    .SetForbiddenActions(ActionDefinitions.Id.CastMain, ActionDefinitions.Id.PowerMain,
                        ActionDefinitions.Id.UseItemMain, ActionDefinitions.Id.HideMain, ActionDefinitions.Id.Ready)
                    .SetCustomSubFeatures(new WarDanceFlurryAttackModifier())
                    .AddToDB())
            .AddToDB();

        private static readonly ConditionDefinition ImprovedWarDanceMomentumExtraAction = ConditionDefinitionBuilder
            .Create("ConditionImprovedWarDanceMomentumExtraAction")
            .SetGuiPresentationNoContent(true)
            .SetFeatures(
                FeatureDefinitionActionAffinityBuilder
                    .Create("ActionAffinityImprovedWarDanceExtraAction")
                    .SetGuiPresentationNoContent(true)
                    .SetDefaultAllowedActionTypes()
                    .SetMaxAttackNumber(1)
                    .SetForbiddenActions(ActionDefinitions.Id.PowerMain,
                        ActionDefinitions.Id.UseItemMain, ActionDefinitions.Id.HideMain, ActionDefinitions.Id.Ready)
                    .SetCustomSubFeatures(new WarDanceFlurryAttackModifier())
                    .AddToDB())
            .AddToDB();

        public bool MightRefundOneAttackOfMainAction(GameLocationCharacter hero, CharacterActionParams actionParams)
        {
            var flag = true;
            if (actionParams.actionDefinition.Id != ActionDefinitions.Id.AttackMain)
            {
                if (actionParams.actionDefinition.Id != ActionDefinitions.Id.CastMain)
                {
                    flag = false;
                }

                // blade cantrips are allowed
                if (actionParams.RulesetEffect is not RulesetEffectSpell spellEffect ||
                    spellEffect.spellDefinition.spellLevel > 0 || !spellEffect.SpellDefinition
                        .HasSubFeatureOfType<IPerformAttackAfterMagicEffectUse>())
                {
                    flag = false;
                }
            }

            // Only apply if the main action is spent
            if (hero.usedMainAttacks != 0 || !hero.RulesetCharacter.HasConditionOfType(ConditionWarDance))
            {
                flag = false;
            }

            if (!flag)
            {
                var conditionsToRemove = new List<RulesetCondition>();

                conditionsToRemove.AddRange(
                    hero.RulesetCharacter.ConditionsByCategory
                        .SelectMany(x => x.Value)
                        .Where(x => x.conditionDefinition == WarDanceMomentumExtraAction ||
                                    x.conditionDefinition == ImprovedWarDanceMomentumExtraAction));
                foreach (var cond in conditionsToRemove)
                {
                    hero.RulesetCharacter.RemoveCondition(cond);
                }

                return false;
            }

            // apply action affinity
            hero.UsedMainSpell = true;
            hero.UsedMainCantrip = false;
            ApplyActionAffinity(hero.RulesetCharacter);

            // apply momentum
            GrantWarDanceMomentum(hero);
            return true;
        }

        private static void ApplyActionAffinity(RulesetCharacter character)
        {
            if (character is not RulesetCharacterHero)
            {
                return;
            }

            var condition = WarDanceMomentumExtraAction;

            if (character.HasAnyFeature(ImproveWarDance))
            {
                condition = ImprovedWarDanceMomentumExtraAction;
            }

            if (character.HasConditionOfType(condition))
            {
                return;
            }

            var actionAffinity = RulesetCondition.CreateActiveCondition(
                character.Guid,
                condition,
                DurationType.Round,
                1,
                TurnOccurenceType.EndOfTurn,
                character.Guid,
                character.CurrentFaction.Name);

            character.AddConditionOfCategory(AttributeDefinitions.TagCombat, actionAffinity);
        }

        private static void GrantWarDanceMomentum(IControllableCharacter hero)
        {
            // required for wildshape scenarios
            if (hero.RulesetCharacter is not RulesetCharacterHero)
            {
                return;
            }

            var slotsByName = hero.RulesetCharacter.CharacterInventory.InventorySlotsByName;
            var item = slotsByName[EquipmentDefinitions.SlotTypeMainHand].EquipedItem;

            if (item == null || !item.itemDefinition.isWeapon)
            {
                return;
            }

            var attackModifier = RulesetCondition.CreateActiveCondition(
                hero.RulesetCharacter.Guid,
                WarDanceMomentum,
                DurationType.Round,
                1,
                TurnOccurenceType.EndOfTurn,
                hero.RulesetCharacter.Guid,
                hero.RulesetCharacter.CurrentFaction.Name);

            hero.RulesetCharacter.AddConditionOfCategory(AttributeDefinitions.TagCombat, attackModifier);
        }
    }

    internal static void OnItemEquipped([NotNull] RulesetCharacter character)
    {
        if (character is RulesetCharacterHero hero && !ValidatorsWeapon.IsMelee(hero.GetMainWeapon()))
        {
            WarDanceFlurryAttack.RemoveConditionOnAttackMissOrAttackWithNonMeleeWeapon(character);
        }
    }

    private sealed class ExtendedWarDanceDurationOnKill : ITargetReducedToZeroHp
    {
        public IEnumerator HandleCharacterReducedToZeroHp(
            GameLocationCharacter attacker,
            GameLocationCharacter downedCreature,
            RulesetAttackMode attackMode, RulesetEffect activeEffect)
        {
            // activeEffect != null means a magical attack
            if (attackMode == null || activeEffect != null ||
                !attacker.RulesetCharacter.HasConditionOfType(ConditionWarDance) ||
                !ValidatorsWeapon.IsMelee(attackMode))
            {
                yield break;
            }

            if (!attacker.RulesetCharacter.TryGetConditionOfCategoryAndType(AttributeDefinitions.TagEffect,
                    ConditionWarDance.Name, out var activeCondition))
            {
                yield break;
            }

            activeCondition.remainingRounds += 1;
            activeCondition.endOccurence = TurnOccurenceType.EndOfTurn;

            foreach (var power in attacker.RulesetCharacter.powersUsedByMe.Where(power =>
                         power.Name == PowerWarDanceName))
            {
                power.remainingRounds += 1;
                break;
            }
        }
    }

    private sealed class WarDanceFlurryAttackModifier : IModifyAttackModeForWeapon
    {
        private const int LightMomentumModifier = -2;
        private const int MomentumModifier = -3;
        private const int HeavyMomentumModifier = -4;

        public void ModifyAttackMode(RulesetCharacter character, RulesetAttackMode attackMode)
        {
            if (attackMode == null || ValidatorsWeapon.IsRanged(attackMode))
            {
                return;
            }

            var momentum = character.ConditionsByCategory
                .SelectMany(x => x.Value)
                .Count(x => x.ConditionDefinition == WarDanceMomentum);

            if (momentum == 0)
            {
                return;
            }

            ApplyMomentum(momentum, attackMode);
        }

        private static void ApplyMomentum(int momentum, RulesetAttackMode attackMode)
        {
            if (attackMode == null)
            {
                return;
            }

            //this.sourceDefinition is ItemDefinition sourceDefinition && sourceDefinition.IsWeapon && sourceDefinition.WeaponDescription.WeaponTags.Contains("Light");
            var item = attackMode.sourceDefinition as ItemDefinition;

            if (item == null || !item.IsWeapon)
            {
                return;
            }

            var isLight = item.WeaponDescription.WeaponTags.Contains("Light");
            var isHeavy = item.WeaponDescription.WeaponTags.Contains("Heavy");
            var toHit = -1;

            if (isLight)
            {
                toHit += LightMomentumModifier * momentum;
            }
            else if (isHeavy)
            {
                toHit += HeavyMomentumModifier * momentum;
            }
            else
            {
                toHit += MomentumModifier * momentum;
            }

            attackMode.toHitBonus += toHit;

            var trendInfo = new TrendInfo(toHit, FeatureSourceType.CharacterFeature,
                Gui.Localize("Feedback/&AdditionalDamageMomentumFormat"), null);
            var index = attackMode.ToHitBonusTrends.IndexOf(trendInfo);

            if (index == -1)
            {
                attackMode.ToHitBonusTrends.Add(trendInfo);
            }
            else
            {
                attackMode.ToHitBonusTrends[index] = trendInfo;
            }
        }
    }

    private sealed class SwitchWeaponFreely : IUnlimitedFreeAction
    {
    }
}
