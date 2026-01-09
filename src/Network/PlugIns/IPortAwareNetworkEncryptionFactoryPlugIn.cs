// <copyright file="IPortAwareNetworkEncryptionFactoryPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Network.PlugIns;

using System.IO.Pipelines;
using System.Runtime.InteropServices;
using MUnique.OpenMU.Network;

/// <summary>
/// A network encryption factory plugin which can select encryption based on the local port.
/// </summary>
[Guid("2E2B6C1E-2D6C-4E55-9A6C-8FD624A391C1")]
public interface IPortAwareNetworkEncryptionFactoryPlugIn : INetworkEncryptionFactoryPlugIn
{
    /// <summary>
    /// Creates a <see cref="IPipelinedDecryptor"/> for the specified source and port.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="direction">The direction of the data flow.</param>
    /// <param name="port">The local port.</param>
    /// <returns>The created decryptor.</returns>
    IPipelinedDecryptor? CreateDecryptor(PipeReader source, DataDirection direction, int port);

    /// <summary>
    /// Creates a <see cref="IPipelinedEncryptor"/> for the specified target and port.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="direction">The direction of the data flow.</param>
    /// <param name="port">The local port.</param>
    /// <returns>The created encryptor.</returns>
    IPipelinedEncryptor? CreateEncryptor(PipeWriter target, DataDirection direction, int port);
}
