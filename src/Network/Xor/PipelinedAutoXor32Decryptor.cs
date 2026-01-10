// <copyright file="PipelinedAutoXor32Decryptor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Network.Xor;

using System.Buffers;
using System.IO.Pipelines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.Network;

/// <summary>
/// A decryptor which auto-detects the correct xor32 key based on the first packet.
/// </summary>
public sealed class PipelinedAutoXor32Decryptor : PacketPipeReaderBase, IPipelinedDecryptor
{
    private const int MinimumLoginLength = 48;
    private static readonly byte[] ConnectServerSubCodes = { 0x02, 0x03, 0x06 };

    private readonly Pipe _pipe = new();
    private readonly byte[] _primaryKey;
    private readonly byte[] _fallbackKey;
    private readonly ILogger _logger;
    private byte[]? _selectedKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelinedAutoXor32Decryptor"/> class.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="primaryKey">The primary xor32 key.</param>
    /// <param name="fallbackKey">The fallback xor32 key.</param>
    /// <param name="logger">The logger.</param>
    public PipelinedAutoXor32Decryptor(PipeReader source, byte[] primaryKey, byte[] fallbackKey, ILogger? logger = null)
    {
        if (primaryKey.Length != 32)
        {
            throw new ArgumentException($"primaryKey must have a size of 32 bytes, but is {primaryKey.Length} bytes long.");
        }

        if (fallbackKey.Length != 32)
        {
            throw new ArgumentException($"fallbackKey must have a size of 32 bytes, but is {fallbackKey.Length} bytes long.");
        }

        this.Source = source;
        this._primaryKey = primaryKey;
        this._fallbackKey = fallbackKey;
        this._logger = logger ?? NullLogger.Instance;
        _ = this.ReadSourceAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public PipeReader Reader => this._pipe.Reader;

    /// <inheritdoc />
    protected override ValueTask OnCompleteAsync(Exception? exception)
    {
        return this._pipe.Writer.CompleteAsync(exception);
    }

    /// <inheritdoc />
    protected override async ValueTask<bool> ReadPacketAsync(ReadOnlySequence<byte> packet)
    {
        if (this._selectedKey is null)
        {
            this._selectedKey = this.SelectKey(packet);
        }

        this.DecryptAndWrite(packet, this._selectedKey ?? this._primaryKey);
        return await this.TryFlushWriterAsync(this._pipe.Writer).ConfigureAwait(false);
    }

    private byte[] SelectKey(ReadOnlySequence<byte> packet)
    {
        var primaryMatch = this.TryMatchPacket(packet, this._primaryKey);
        var fallbackMatch = this.TryMatchPacket(packet, this._fallbackKey);

        if (primaryMatch && !fallbackMatch)
        {
            this.LogSelection("primary", packet);
            return this._primaryKey;
        }

        if (fallbackMatch && !primaryMatch)
        {
            this.LogSelection("fallback", packet);
            return this._fallbackKey;
        }

        if (primaryMatch && fallbackMatch)
        {
            this.LogSelection("primary (both matched)", packet);
            return this._primaryKey;
        }

        this.LogSelection("primary (no match)", packet);
        return this._primaryKey;
    }

    private void LogSelection(string selection, ReadOnlySequence<byte> packet)
    {
        if (!this._logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        var previewLength = (int)Math.Min(8, packet.Length);
        Span<byte> buffer = stackalloc byte[8];
        packet.Slice(0, previewLength).CopyTo(buffer);
        var hex = BitConverter.ToString(buffer.Slice(0, previewLength).ToArray());
        this._logger.LogInformation("Selected xor32 key: {selection}. First packet bytes: {packetPreview}", selection, hex);
    }

    private bool TryMatchPacket(ReadOnlySequence<byte> packet, byte[] key)
    {
        if (packet.IsEmpty)
        {
            return false;
        }

        var buffer = packet.ToArray();
        var headerSize = ArrayExtensions.GetPacketHeaderSize(buffer);
        if (headerSize == 0 || buffer.Length <= headerSize)
        {
            return false;
        }

        for (var i = buffer.Length - 1; i > headerSize; i--)
        {
            buffer[i] = (byte)(buffer[i] ^ buffer[i - 1] ^ key[i % 32]);
        }

        var header = ArrayExtensions.NormalizePacketHeader(buffer[0]);
        var code = buffer[headerSize];
        var subIndex = headerSize + 1;
        var subCode = subIndex < buffer.Length ? buffer[subIndex] : (byte)0;

        if (header is 0xC3 or 0xC4 && code == 0xF1 && subCode == 0x01 && buffer.Length >= MinimumLoginLength)
        {
            return true;
        }

        if (header is 0xC1 or 0xC2 && code == 0xF4 && IsConnectServerSubCode(subCode))
        {
            return true;
        }

        return false;
    }

    private void DecryptAndWrite(ReadOnlySequence<byte> packet, byte[] key)
    {
        var span = this._pipe.Writer.GetSpan((int)packet.Length);
        var target = span.Slice(0, (int)packet.Length);
        packet.CopyTo(target);

        var headerSize = target.GetPacketHeaderSize();
        for (var i = target.Length - 1; i > headerSize; i--)
        {
            target[i] = (byte)(target[i] ^ target[i - 1] ^ key[i % 32]);
        }

        this._pipe.Writer.Advance(target.Length);
    }

    private static bool IsConnectServerSubCode(byte subCode)
    {
        for (var i = 0; i < ConnectServerSubCodes.Length; i++)
        {
            if (ConnectServerSubCodes[i] == subCode)
            {
                return true;
            }
        }

        return false;
    }
}
