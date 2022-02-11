﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SolastaCommunityExpansion.Multiclass.Models
{
    internal static class InOutRulesContext
    {
        internal static void EnumerateHeroAllowedClassDefinitions(RulesetCharacterHero hero, List<CharacterClassDefinition> allowedClasses, ref int selectedClass)
        {
            var currentClass = hero.ClassesHistory[hero.ClassesHistory.Count - 1];

            allowedClasses.Clear();

            // only allows to leave a class if it is a supported one with required In/Out attributes
            if (!IsSupported(currentClass) || (Main.Settings.EnableMinInOutAttributes && !ApproveMultiClassInOut(hero, currentClass)))
            {
                allowedClasses.Add(currentClass);
            }

            // only allows existing classes with required In/Out attributes
            else if (hero.ClassesAndLevels.Count >= Main.Settings.MaxAllowedClasses)
            {
                foreach (var characterClassDefinition in hero.ClassesAndLevels.Keys)
                {
                    if (!Main.Settings.EnableMinInOutAttributes || ApproveMultiClassInOut(hero, characterClassDefinition))
                    {
                        allowedClasses.Add(characterClassDefinition);
                    }
                }
            }

            // only allows supported classes with required In/Out attributes
            else
            {
                foreach (var classDefinition in DatabaseRepository.GetDatabase<CharacterClassDefinition>())
                {
                    if (IsSupported(classDefinition) && (!Main.Settings.EnableMinInOutAttributes || ApproveMultiClassInOut(hero, classDefinition)))
                    {
                        allowedClasses.Add(classDefinition);
                    }
                }
            }

            allowedClasses.Sort((a, b) => a.FormatTitle().CompareTo(b.FormatTitle()));
            selectedClass = allowedClasses.IndexOf(hero.ClassesHistory[hero.ClassesHistory.Count - 1]);
        }

        private static readonly string[] CoreAttributes = new string[]
        {
            AttributeDefinitions.Strength,
            AttributeDefinitions.Dexterity,
            AttributeDefinitions.Intelligence,
            AttributeDefinitions.Wisdom,
            AttributeDefinitions.Charisma
        };

        private static int MyGetAttribute(RulesetCharacterHero hero, string attributeName)
        {
            var attribute = hero.GetAttribute(attributeName);
            var activeModifiers = attribute.ActiveModifiers;
            var currentValue = attribute.BaseValue;
            var minValue = int.MinValue;

            foreach (var activeModifier in activeModifiers)
            {
                currentValue = activeModifier.ApplyOnValue(currentValue);

                if (activeModifier.Operation == FeatureDefinitionAttributeModifier.AttributeModifierOperation.Additive)
                {
                    currentValue += UnityEngine.Mathf.FloorToInt(activeModifier.Value);
                }
            }

            return UnityEngine.Mathf.Clamp(currentValue, minValue, attribute.MaxEditableValue > 0 ? attribute.MaxEditableValue : attribute.MaxValue);
        }

        private static void EnumerateItemsAttributeModifiers(RulesetCharacterHero hero, Dictionary<string, int> attributeModifiers)
        {
            var items = new List<RulesetItem>();

            foreach (var attributeName in CoreAttributes)
            {
                attributeModifiers.Add(attributeName, 0);
            }

            hero.CharacterInventory.EnumerateAllItems(items, considerContainers: false);

            foreach (var featureDefinitionAttributeModifier in items
                .SelectMany(x => x.ItemDefinition.StaticProperties
                    .Select(y => y.FeatureDefinition)
                    .OfType<FeatureDefinitionAttributeModifier>()
                    .Where(z => System.Array.IndexOf(CoreAttributes, z.ModifiedAttribute) >= 0 && z.ModifierType == FeatureDefinitionAttributeModifier.AttributeModifierOperation.Additive)))
            {
                var modifiedAttribute = featureDefinitionAttributeModifier.ModifiedAttribute;
                var modifierValue = featureDefinitionAttributeModifier.ModifierValue;

                attributeModifiers[modifiedAttribute] += modifierValue;
            };
        }

        [SuppressMessage("Convert switch statement to expression", "IDE0066")]
        internal static bool ApproveMultiClassInOut(RulesetCharacterHero hero, CharacterClassDefinition classDefinition)
        {
            var itemsAttributeModifiers = new Dictionary<string, int>();

            EnumerateItemsAttributeModifiers(hero, itemsAttributeModifiers);

            var strength = MyGetAttribute(hero, AttributeDefinitions.Strength) - itemsAttributeModifiers[AttributeDefinitions.Strength];
            var dexterity = MyGetAttribute(hero, AttributeDefinitions.Dexterity) - itemsAttributeModifiers[AttributeDefinitions.Dexterity];
            var intelligence = MyGetAttribute(hero, AttributeDefinitions.Intelligence) - itemsAttributeModifiers[AttributeDefinitions.Intelligence];
            var wisdom = MyGetAttribute(hero, AttributeDefinitions.Wisdom) - itemsAttributeModifiers[AttributeDefinitions.Wisdom];
            var charisma = MyGetAttribute(hero, AttributeDefinitions.Charisma) - itemsAttributeModifiers[AttributeDefinitions.Charisma];

            if (classDefinition.GuiPresentation.Hidden)
            {
                return false;
            }

            switch (classDefinition.Name)
            {
                case RuleDefinitions.BarbarianClass:
                case IntegrationContext.CLASS_WARDEN:
                    return strength >= 13;

                case IntegrationContext.CLASS_BARD:
                case RuleDefinitions.SorcererClass:
                case IntegrationContext.CLASS_WARLOCK:
                case IntegrationContext.CLASS_WITCH:
                    return charisma >= 13;

                case RuleDefinitions.ClericClass:
                case RuleDefinitions.DruidClass:
                    return wisdom >= 13;

                case RuleDefinitions.FighterClass:
                    return strength >= 13 || dexterity >= 13;

                case IntegrationContext.CLASS_MONK:
                case RuleDefinitions.RangerClass:
                    return dexterity >= 13 && wisdom >= 13;

                case RuleDefinitions.PaladinClass:
                    return strength >= 13 && charisma >= 13;

                case RuleDefinitions.RogueClass:
                    return dexterity >= 13;

                case RuleDefinitions.WizardClass:
                case IntegrationContext.CLASS_TINKERER:
                case IntegrationContext.CLASS_ALCHEMIST:
                    return intelligence >= 13;

                default:
                    return false;
            }
        }

        [SuppressMessage("Convert switch statement to expression", "IDE0066")]
        internal static bool IsSupported(CharacterClassDefinition classDefinition)
        {
            if (classDefinition.GuiPresentation.Hidden)
            {
                return false;
            }

            switch (classDefinition.Name)
            {
                case RuleDefinitions.BarbarianClass:
                case RuleDefinitions.ClericClass:
                case RuleDefinitions.DruidClass:
                case RuleDefinitions.FighterClass:
                case RuleDefinitions.PaladinClass:
                case RuleDefinitions.RangerClass:
                case RuleDefinitions.RogueClass:
                case RuleDefinitions.SorcererClass:
                case RuleDefinitions.WizardClass:
                case IntegrationContext.CLASS_TINKERER:
                case IntegrationContext.CLASS_WARDEN:
                case IntegrationContext.CLASS_WITCH:
                case IntegrationContext.CLASS_ALCHEMIST:
                case IntegrationContext.CLASS_BARD:
                case IntegrationContext.CLASS_MONK:
                case IntegrationContext.CLASS_WARLOCK:
                    return true;

                default:
                    return false;
            }
        }
    }
}
