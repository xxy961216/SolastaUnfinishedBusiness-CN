﻿using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using SolastaCommunityExpansion.Models;
using UnityEngine;

namespace SolastaCommunityExpansion.Patches.Tools.DefaultParty
{
    [HarmonyPatch(typeof(NewAdventurePanel), "Refresh")]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    internal static class NewAdventurePanel_Refresh
    {
        internal static bool ShouldAssignDefaultParty { get; set; }

        internal static void Postfix(NewAdventurePanel __instance, RectTransform ___characterSessionPlatesTable)
        {
            if (!Main.Settings.EnableTogglesToOverwriteDefaultTestParty
                || !ShouldAssignDefaultParty
                || Global.IsMultiplayer)
            {
                return;
            }

            var characterPoolService = ServiceRepository.GetService<ICharacterPoolService>();
            var max = Math.Min(Main.Settings.DefaultPartyHeroes.Count,
                ___characterSessionPlatesTable.childCount);

            for (var i = 0; i < max; i++)
            {
                var characterPlateSession =
                    ___characterSessionPlatesTable.GetChild(i).GetComponent<CharacterPlateSession>();

                if (characterPlateSession.gameObject.activeSelf)
                {
                    var heroname = Main.Settings.DefaultPartyHeroes[i];
                    var filename = characterPoolService.BuildCharacterFilename(heroname);

                    characterPlateSession.BindCharacter(filename, false);
                    __instance.AutotestSelectCharacter(i, heroname);
                }
            }

            ShouldAssignDefaultParty = false;
        }
    }
}
