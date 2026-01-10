// <copyright file="PipelinedAutoHackCheckDecryptor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Network.HackCheck;

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using MUnique.OpenMU.Network;

internal sealed class PipelinedAutoHackCheckDecryptor : IPipelinedDecryptor
{
    private const int MaxPlausiblePacketLength = 0x2000;

    private readonly Pipe _pipe = new();
    private readonly PipeReader _source;
    private readonly HackCheckKeys _keys;
    private readonly HackCheckUsageState _state;
    private byte[] _pending = new byte[64];
    private int _pendingLength;

    public PipelinedAutoHackCheckDecryptor(PipeReader source, HackCheckKeys keys, HackCheckUsageState state)
    {
        this._source = source;
        this._keys = keys;
        this._state = state;
        _ = this.ReadSourceAsync().ConfigureAwait(false);
    }

    public PipeReader Reader => this._pipe.Reader;

    private async Task ReadSourceAsync()
    {
        Exception? error = null;

        try
        {
            while (true)
            {
                var result = await this._source.ReadAsync().ConfigureAwait(false);
                var buffer = result.Buffer;

                if (!buffer.IsEmpty)
                {
                    if (!this._state.TryGet(out var useHackCheck))
                    {
                        this.AppendPending(buffer);
                        if (this.TryDetectHackCheck(out useHackCheck))
                        {
                            this._state.SetIfUnknown(useHackCheck);
                            this.WritePending(useHackCheck);
                            var flushResult = await this._pipe.Writer.FlushAsync().ConfigureAwait(false);
                            if (flushResult.IsCompleted || flushResult.IsCanceled)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        this.WriteProcessed(buffer, useHackCheck);
                        var flushResult = await this._pipe.Writer.FlushAsync().ConfigureAwait(false);
                        if (flushResult.IsCompleted || flushResult.IsCanceled)
                        {
                            break;
                        }
                    }

                    this._source.AdvanceTo(buffer.End);
                }
                else
                {
                    this._source.AdvanceTo(buffer.End);
                }

                if (result.IsCompleted || result.IsCanceled)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            error = ex;
        }

        if (this._pendingLength > 0 && !this._state.TryGet(out var decided))
        {
            decided = false;
            this._state.SetIfUnknown(false);
            this.WritePending(decided);
            await this._pipe.Writer.FlushAsync().ConfigureAwait(false);
        }

        await this._pipe.Writer.CompleteAsync(error).ConfigureAwait(false);
        await this._source.CompleteAsync(error).ConfigureAwait(false);
    }

    private void AppendPending(ReadOnlySequence<byte> buffer)
    {
        foreach (var segment in buffer)
        {
            this.EnsurePendingCapacity(this._pendingLength + segment.Length);
            segment.Span.CopyTo(this._pending.AsSpan(this._pendingLength));
            this._pendingLength += segment.Length;
        }
    }

    private void EnsurePendingCapacity(int length)
    {
        if (this._pending.Length >= length)
        {
            return;
        }

        var newSize = Math.Max(length, this._pending.Length * 2);
        Array.Resize(ref this._pending, newSize);
    }

    private bool TryDetectHackCheck(out bool useHackCheck)
    {
        if (this._pendingLength < 2)
        {
            useHackCheck = false;
            return false;
        }

        var header = this._pending[0];
        if (header == 0xC1 || header == 0xC3)
        {
            var length = this._pending[1];
            if (this.IsPlausibleLength(length, 3))
            {
                useHackCheck = false;
                return true;
            }

            useHackCheck = true;
            return true;
        }

        if (header == 0xC2 || header == 0xC4)
        {
            if (this._pendingLength < 3)
            {
                useHackCheck = false;
                return false;
            }

            var length = (this._pending[1] << 8) | this._pending[2];
            if (this.IsPlausibleLength(length, 4))
            {
                useHackCheck = false;
                return true;
            }

            useHackCheck = true;
            return true;
        }

        useHackCheck = true;
        return true;
    }

    private bool IsPlausibleLength(int length, int minimumLength)
    {
        return length >= minimumLength && length <= MaxPlausiblePacketLength;
    }

    private void WritePending(bool useHackCheck)
    {
        if (this._pendingLength <= 0)
        {
            return;
        }

        var span = this._pipe.Writer.GetSpan(this._pendingLength);
        var target = span.Slice(0, this._pendingLength);
        this._pending.AsSpan(0, this._pendingLength).CopyTo(target);
        if (useHackCheck)
        {
            HackCheckCrypto.Decrypt(target, this._keys);
        }

        this._pipe.Writer.Advance(this._pendingLength);
        this._pendingLength = 0;
    }

    private void WriteProcessed(ReadOnlySequence<byte> buffer, bool useHackCheck)
    {
        foreach (var segment in buffer)
        {
            var span = this._pipe.Writer.GetSpan(segment.Length);
            var destination = span.Slice(0, segment.Length);
            segment.Span.CopyTo(destination);
            if (useHackCheck)
            {
                HackCheckCrypto.Decrypt(destination, this._keys);
            }

            this._pipe.Writer.Advance(segment.Length);
        }
    }
}
