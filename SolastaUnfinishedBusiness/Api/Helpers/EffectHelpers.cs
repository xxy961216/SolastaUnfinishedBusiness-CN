﻿using SolastaUnfinishedBusiness.Api.Extensions;

namespace SolastaUnfinishedBusiness.Api.Helpers;

internal static class EffectHelpers
{
    /**DC and magic attack bonus will be calculated based on the stats of the user, not from device itself*/
    public const int BasedOnUser = -1;

    /**DC and magic attack bonus will be calculated based on the stats of character who summoned item, not from device itself*/
    public const int BasedOnItemSummoner = -2;

    internal static int CalculateSaveDc(RulesetCharacter character, EffectDescription effectDescription,
        string className, int def = 10)
    {
        switch (effectDescription.DifficultyClassComputation)
        {
            case RuleDefinitions.EffectDifficultyClassComputation.SpellCastingFeature:
            {
                var rulesetSpellRepertoire = character.GetClassSpellRepertoire(className);

                if (rulesetSpellRepertoire != null)
                {
                    return rulesetSpellRepertoire.SaveDC;
                }

                break;
            }
            case RuleDefinitions.EffectDifficultyClassComputation.AbilityScoreAndProficiency:
                var attributeValue = character.TryGetAttributeValue(effectDescription.SavingThrowDifficultyAbility);
                var proficiencyBonus = character.TryGetAttributeValue(AttributeDefinitions.ProficiencyBonus);

                return RuleDefinitions.ComputeAbilityScoreBasedDC(attributeValue, proficiencyBonus);

            case RuleDefinitions.EffectDifficultyClassComputation.FixedValue:
                return effectDescription.FixedSavingThrowDifficultyClass;

            //TODO: implement missing computation methods (like Ki and Breath Weapon)
            case RuleDefinitions.EffectDifficultyClassComputation.Ki:
                break;
            case RuleDefinitions.EffectDifficultyClassComputation.BreathWeapon:
                break;
            case RuleDefinitions.EffectDifficultyClassComputation.CustomAbilityModifierAndProficiency:
                break;
        }

        return def;
    }

    internal static RulesetCharacter GetSummoner(RulesetCharacter summon)
    {
        if (summon.TryGetConditionOfCategoryAndType(AttributeDefinitions.TagConjure,
                RuleDefinitions.ConditionConjuredCreature, out var activeCondition))
        {
            return GetCharacterByGuid(activeCondition.SourceGuid);
        }

        return null;
    }
    
    internal static RulesetCharacter GetCharacterByGuid(ulong guid)
    {
        if (guid == 0) { return null; }

        if (!RulesetEntity.TryGetEntity<RulesetEntity>(guid, out var entity))
        {
            return null;
        }
        
        return entity as RulesetCharacter;
    }

    internal static RulesetCharacter GetCharacterByEffectGuid(ulong guid)
    {
        if (guid == 0) { return null; }

        if (!RulesetEntity.TryGetEntity<RulesetEffect>(guid, out var effect))
        {
            return null;
        }

        return effect switch
        {
            RulesetEffectSpell spell => spell.Caster,
            RulesetEffectPower power => power.User,
            _ => null
        };
    }

    internal static (RulesetCharacter, BaseDefinition) GetCharacterAndSourceDefinitionByEffectGuid(ulong guid)
    {
        if (guid == 0) { return (null, null); }

        if (!RulesetEntity.TryGetEntity<RulesetEffect>(guid, out var effect))
        {
            return (null, null);
        }

        return effect switch
        {
            RulesetEffectSpell spell => (spell.Caster, spell.SourceDefinition),
            RulesetEffectPower power => (power.User, power.PowerDefinition),
            _ => (null, null)
        };
    }
}
