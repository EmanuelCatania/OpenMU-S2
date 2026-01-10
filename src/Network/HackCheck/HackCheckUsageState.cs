// <copyright file="HackCheckUsageState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Network.HackCheck;

using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

internal sealed class HackCheckUsageState
{
    private int _useHackCheck = -1;
    private readonly AsyncManualResetEvent _decided = new(false);

    public bool TryGet(out bool useHackCheck)
    {
        var current = Volatile.Read(ref this._useHackCheck);
        if (current < 0)
        {
            useHackCheck = false;
            return false;
        }

        useHackCheck = current == 1;
        return true;
    }

    public bool SetIfUnknown(bool useHackCheck)
    {
        var newValue = useHackCheck ? 1 : 0;
        var original = Interlocked.CompareExchange(ref this._useHackCheck, newValue, -1);
        if (original == -1)
        {
            this._decided.Set();
            return true;
        }

        return false;
    }

    public Task WaitAsync() => this._decided.WaitAsync();
}
