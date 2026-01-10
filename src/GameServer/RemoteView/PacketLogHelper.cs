// <copyright file="PacketLogHelper.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System;
using Microsoft.Extensions.Logging;

/// <summary>
/// Helper for lightweight packet previews during debugging.
/// </summary>
internal static class PacketLogHelper
{
    private const int PreviewBytes = 16;

    public static void LogPacket(ILogger logger, string label, ReadOnlySpan<byte> data, int length)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        var previewLength = Math.Min(length, PreviewBytes);
        var hex = BitConverter.ToString(data.Slice(0, previewLength).ToArray());
        logger.LogInformation("[S->C] {Label} Len {Length} Head {Head} Bytes {Bytes}",
            label,
            length,
            data.Length > 0 ? $"0x{data[0]:X2}" : "n/a",
            hex);
    }
}
