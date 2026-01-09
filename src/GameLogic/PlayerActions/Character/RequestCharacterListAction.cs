// <copyright file="RequestCharacterListAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Character;

using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic.Views.Character;

/// <summary>
/// Action to request the character list.
/// </summary>
public class RequestCharacterListAction
{
    /// <summary>
    /// Requests the character list and advances the player state to <see cref="PlayerState.CharacterSelection"/>.
    /// </summary>
    /// <param name="player">The player who requests the character list.</param>
    public async ValueTask RequestCharacterListAsync(Player player)
    {
        if (player.Logger.IsEnabled(LogLevel.Information))
        {
            player.Logger.LogInformation("Character list requested. CurrentState: {state}", player.PlayerState.CurrentState);
        }

        var advanced = await player.PlayerState.TryAdvanceToAsync(PlayerState.CharacterSelection).ConfigureAwait(false);
        if (!advanced)
        {
            player.Logger.LogWarning("Character list request rejected. CurrentState: {state}", player.PlayerState.CurrentState);
            return;
        }

        if (player.Logger.IsEnabled(LogLevel.Information))
        {
            player.Logger.LogInformation("Character list request accepted. Sending list.");
        }

        await player.InvokeViewPlugInAsync<IShowCharacterListPlugIn>(p => p.ShowCharacterListAsync()).ConfigureAwait(false);
    }
}
