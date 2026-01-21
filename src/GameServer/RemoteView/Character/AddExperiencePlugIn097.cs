// <copyright file="AddExperiencePlugIn097.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Character;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Experience update plugin for 0.97 clients.
/// </summary>
[PlugIn(nameof(AddExperiencePlugIn097), "Experience update plugin for 0.97 clients.")]
[Guid("742A2D34-3B1C-4A8C-88AB-9F4F9D5F6B58")]
[MinimumClient(0, 97, ClientLanguage.Invariant)]
[MaximumClient(0, 97, ClientLanguage.Invariant)]
public class AddExperiencePlugIn097 : IAddExperiencePlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddExperiencePlugIn097"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public AddExperiencePlugIn097(RemotePlayer player) => this._player = player;

    /// <inheritdoc/>
    public async ValueTask AddExperienceAsync(int exp, IAttackable? obj, ExperienceType experienceType)
    {
        var connection = this._player.Connection;
        var attributes = this._player.Attributes;
        var selectedCharacter = this._player.SelectedCharacter;
        if (connection is null || attributes is null || selectedCharacter is null)
        {
            return;
        }

        ushort damage = 0;
        if (obj is not null && obj.Id != obj.LastDeath?.KillerId)
        {
            damage = (ushort)Math.Min(obj.LastDeath?.FinalHit.HealthDamage ?? 0, ushort.MaxValue);
        }

        var id = (ushort)(obj?.GetId(this._player) ?? 0);
        if (id != 0)
        {
            id |= 0x8000;
        }

        var currentExperience = selectedCharacter.Experience;
        var level = (int)attributes[Stats.Level];
        var (viewExperience, _) = Version097ExperienceViewHelper.GetViewExperience(this._player, currentExperience, level);
        var previousExperience = Math.Max(0L, currentExperience - exp);
        var (previousViewExperience, _) = Version097ExperienceViewHelper.GetViewExperience(this._player, previousExperience, level);
        var viewExperienceDelta = viewExperience >= previousViewExperience
            ? viewExperience - previousViewExperience
            : 0u;
        var remainingViewExperience = viewExperienceDelta;

        var sentOnce = false;
        while (remainingViewExperience > 0 || !sentOnce)
        {
            ushort sendExp = remainingViewExperience > ushort.MaxValue
                ? ushort.MaxValue
                : (ushort)remainingViewExperience;
            await connection.SendExperienceGainedAsync(id, sendExp, damage).ConfigureAwait(false);
            damage = 0;
            remainingViewExperience = remainingViewExperience > sendExp ? remainingViewExperience - sendExp : 0;
            sentOnce = true;
        }
    }
}
