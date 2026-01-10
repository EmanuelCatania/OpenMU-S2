// <copyright file="PipelinedAutoHackCheckEncryptor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Network.HackCheck;

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;

internal sealed class PipelinedAutoHackCheckEncryptor : IPipelinedEncryptor
{
    private readonly Pipe _pipe = new();
    private readonly PipeWriter _target;
    private readonly HackCheckKeys _keys;
    private readonly HackCheckUsageState _state;

    public PipelinedAutoHackCheckEncryptor(PipeWriter target, HackCheckKeys keys, HackCheckUsageState state)
    {
        this._target = target;
        this._keys = keys;
        this._state = state;
        _ = this.ReadSourceAsync().ConfigureAwait(false);
    }

    public PipeWriter Writer => this._pipe.Writer;

    private async Task ReadSourceAsync()
    {
        var source = this._pipe.Reader;
        Exception? error = null;

        try
        {
            while (true)
            {
                var result = await source.ReadAsync().ConfigureAwait(false);
                var buffer = result.Buffer;

                if (!buffer.IsEmpty)
                {
                    var useHackCheck = await this.GetHackCheckUsageAsync().ConfigureAwait(false);
                    this.WriteProcessed(buffer, useHackCheck);
                    source.AdvanceTo(buffer.End);

                    var flushResult = await this._target.FlushAsync().ConfigureAwait(false);
                    if (flushResult.IsCompleted || flushResult.IsCanceled)
                    {
                        break;
                    }
                }
                else
                {
                    source.AdvanceTo(buffer.End);
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

        await this._target.CompleteAsync(error).ConfigureAwait(false);
        await source.CompleteAsync(error).ConfigureAwait(false);
    }

    private async Task<bool> GetHackCheckUsageAsync()
    {
        if (this._state.TryGet(out var useHackCheck))
        {
            return useHackCheck;
        }

        await this._state.WaitAsync().ConfigureAwait(false);
        this._state.TryGet(out useHackCheck);
        return useHackCheck;
    }

    private void WriteProcessed(ReadOnlySequence<byte> buffer, bool useHackCheck)
    {
        foreach (var segment in buffer)
        {
            var span = this._target.GetSpan(segment.Length);
            var destination = span.Slice(0, segment.Length);
            segment.Span.CopyTo(destination);
            if (useHackCheck)
            {
                HackCheckCrypto.Encrypt(destination, this._keys);
            }

            this._target.Advance(segment.Length);
        }
    }
}
