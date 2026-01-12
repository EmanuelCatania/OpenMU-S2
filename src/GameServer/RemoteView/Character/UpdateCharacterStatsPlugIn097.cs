// <copyright file="UpdateCharacterStatsPlugIn097.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Character;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The default implementation of the <see cref="IUpdateCharacterStatsPlugIn"/> which is forwarding everything to the game client with specific data packets.
/// </summary>
[PlugIn(nameof(UpdateCharacterStatsPlugIn097), "The default implementation of the IUpdateCharacterStatsPlugIn which is forwarding everything to the game client with specific data packets.")]
[Guid("8ACD9D6B-6FA7-42C3-8C07-E137655CB92F")]
[MinimumClient(0, 97, ClientLanguage.Invariant)]
public class UpdateCharacterStatsPlugIn097 : IUpdateCharacterStatsPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCharacterStatsPlugIn097"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public UpdateCharacterStatsPlugIn097(RemotePlayer player) => this._player = player;

    /// <inheritdoc/>
    public async ValueTask UpdateCharacterStatsAsync()
    {
        var connection = this._player.Connection;
        var selectedCharacter = this._player.SelectedCharacter;
        var attributes = this._player.Attributes;
        if (connection is null || this._player.Account is null || selectedCharacter?.CurrentMap is null || attributes is null)
        {
            return;
        }

        var (viewExperience, viewNextExperience) = Version097ExperienceViewHelper.GetViewExperience(this._player);
        if (this._player.PlayerState.CurrentState == PlayerState.CharacterSelection)
        {
            await connection.SendAsync(WriteCharacterInformationPacket).ConfigureAwait(false);
        }

        await connection.SendAsync(WriteNewCharacterInfoPacket).ConfigureAwait(false);
        await connection.SendAsync(WriteNewCharacterCalcPacket).ConfigureAwait(false);

        await this._player.InvokeViewPlugInAsync<IApplyKeyConfigurationPlugIn>(p => p.ApplyKeyConfigurationAsync()).ConfigureAwait(false);

        int WriteCharacterInformationPacket()
        {
            const int packetLength = 48;
            var span = connection.Output.GetSpan(packetLength)[..packetLength];
            span[0] = 0xC3;
            span[1] = (byte)packetLength;
            span[2] = 0xF3;
            span[3] = 0x03;
            span[4] = this._player.Position.X;
            span[5] = this._player.Position.Y;
            span[6] = (byte)selectedCharacter.CurrentMap!.Number;
            span[7] = this._player.Rotation.ToPacketByte();

            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8, 4), viewExperience);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(12, 4), viewNextExperience);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(16, 2), (ushort)Math.Max(selectedCharacter.LevelUpPoints, 0));
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(18, 2), (ushort)attributes[Stats.BaseStrength]);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(20, 2), (ushort)attributes[Stats.BaseAgility]);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(22, 2), (ushort)attributes[Stats.BaseVitality]);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(24, 2), (ushort)attributes[Stats.BaseEnergy]);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(26, 2), (ushort)attributes[Stats.CurrentHealth]);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(28, 2), (ushort)attributes[Stats.MaximumHealth]);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(30, 2), (ushort)attributes[Stats.CurrentMana]);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(32, 2), (ushort)attributes[Stats.MaximumMana]);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(34, 2), (ushort)attributes[Stats.CurrentAbility]);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(36, 2), (ushort)attributes[Stats.MaximumAbility]);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(38, 4), (uint)this._player.Money);
            span[42] = (byte)selectedCharacter.State.Convert();
            span[43] = (byte)selectedCharacter.CharacterStatus.Convert();
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(44, 2), (ushort)selectedCharacter.UsedFruitPoints);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(46, 2), selectedCharacter.GetMaximumFruitPoints());
            PacketLogHelper.LogPacket(this._player.Logger, "F3:03 CharacterInformation097", span, packetLength);
            return packetLength;
        }

        int WriteNewCharacterInfoPacket()
        {
            const int packetLength = 76;
            var span = connection.Output.GetSpan(packetLength)[..packetLength];
            span[0] = 0xC1;
            span[1] = (byte)packetLength;
            span[2] = 0xF3;
            span[3] = 0xE0;

            var offset = 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.Level]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(selectedCharacter.LevelUpPoints));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), viewExperience);
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), viewNextExperience);
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.BaseStrength]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.BaseAgility]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.BaseVitality]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.BaseEnergy]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.CurrentHealth]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.MaximumHealth]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.CurrentMana]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.MaximumMana]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.CurrentAbility]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.MaximumAbility]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(selectedCharacter.UsedFruitPoints));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(selectedCharacter.GetMaximumFruitPoints()));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.Resets]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), 0);

            PacketLogHelper.LogPacket(this._player.Logger, "F3:E0 NewCharacterInfo", span, packetLength);
            return packetLength;
        }

        int WriteNewCharacterCalcPacket()
        {
            const int packetLength = 72;
            var span = connection.Output.GetSpan(packetLength)[..packetLength];
            span[0] = 0xC1;
            span[1] = (byte)packetLength;
            span[2] = 0xF3;
            span[3] = 0xE1;

            var wizardryIncrease = attributes[Stats.WizardryAttackDamageIncrease];
            var magicDamageRate = wizardryIncrease > 1f ? ClampToUInt32((wizardryIncrease - 1f) * 100f) : 0;

            var offset = 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.CurrentHealth]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.MaximumHealth]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.CurrentMana]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.MaximumMana]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.CurrentAbility]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.MaximumAbility]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.AttackSpeed]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.MagicSpeed]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.MinimumPhysBaseDmg]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.MaximumPhysBaseDmg]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.MinimumWizBaseDmg]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.MaximumWizBaseDmg]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), magicDamageRate);
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.AttackRatePvm]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), 0);
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.DefenseFinal]));
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(offset, 4), ClampToUInt32(attributes[Stats.DefenseRatePvm]));

            PacketLogHelper.LogPacket(this._player.Logger, "F3:E1 NewCharacterCalc", span, packetLength);
            return packetLength;
        }
    }

    private static uint ClampToUInt32(float value)
    {
        if (value <= 0f)
        {
            return 0;
        }

        if (value >= uint.MaxValue)
        {
            return uint.MaxValue;
        }

        return (uint)value;
    }

    private static uint ClampToUInt32(long value)
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
