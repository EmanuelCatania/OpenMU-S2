// <copyright file="Version097ExperienceViewHelper.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Character;

using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;

internal static class Version097ExperienceViewHelper
{
    private const int ClientMaxLevel = 1000;
    private const double MaxExperienceFactor = 0.95;

    public static (uint Current, uint Next) GetViewExperience(RemotePlayer player)
    {
        var attributes = player.Attributes;
        var selectedCharacter = player.SelectedCharacter;
        if (attributes is null || selectedCharacter is null)
        {
            return (0, 0);
        }

        return GetViewExperience(player, selectedCharacter.Experience, (int)attributes[Stats.Level]);
    }

    public static (uint Current, uint Next) GetViewExperience(RemotePlayer player, long experience, int level)
    {
        var expTable = player.GameServerContext.ExperienceTable;
        if (expTable.Length == 0)
        {
            return (0, 0);
        }

        if (level < 0)
        {
            return (0, 0);
        }

        var clampedLevel = Math.Min(level, expTable.Length - 1);
        var nextLevelIndex = Math.Min(level + 1, expTable.Length - 1);
        var expForCurrentLevel = expTable[clampedLevel];
        var expForNextLevel = expTable[nextLevelIndex];

        var progress = 0.0;
        if (expForNextLevel > expForCurrentLevel)
        {
            progress = (experience - expForCurrentLevel) / (double)(expForNextLevel - expForCurrentLevel);
        }

        progress = Math.Clamp(progress, 0.0, 1.0);

        var maxLevel = Math.Max(ClientMaxLevel, player.GameServerContext.Configuration.MaximumLevel);
        var scaleFactor = uint.MaxValue * MaxExperienceFactor / Math.Pow(maxLevel, 3);
        var previousLevel = Math.Clamp(level - 1, 0, maxLevel);
        var currentLevel = Math.Clamp(level, 0, maxLevel);
        var viewPrevious = scaleFactor * Math.Pow(previousLevel, 3);
        var viewNext = scaleFactor * Math.Pow(currentLevel, 3);
        var viewCurrent = viewPrevious + ((viewNext - viewPrevious) * progress);

        return (ClampToUInt32(viewCurrent), ClampToUInt32(viewNext));
    }

    private static uint ClampToUInt32(double value)
    {
        if (value <= 0)
        {
            return 0;
        }

        if (value >= uint.MaxValue)
        {
            return uint.MaxValue;
        }

        return (uint)value;
    }
}
