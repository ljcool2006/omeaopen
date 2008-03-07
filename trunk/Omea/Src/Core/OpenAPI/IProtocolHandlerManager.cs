﻿/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Windows.Forms;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Interface for registering URL protocol handlers.
	/// </summary>
	/// <since>2.0</since>
    public interface IProtocolHandlerManager
    {
        /// <summary>
        /// Registers new URL protocol handler.
        /// </summary>
        /// <param name="protocol">URL protocol.</param>
        /// <param name="friendlyName">Friendly name of URL protocol.</param>
        /// <param name="handler">Delegate that uses as protocol handler for processing requested urls.</param>
        void RegisterProtocolHandler( string protocol, string friendlyName, ProtocolHandlerCallback handler );

        /// <summary>
        /// Registers new URL protocol handler.
        /// </summary>
        /// <param name="protocol">URL protocol.</param>
        /// <param name="friendlyName">Friendly name of URL protocol.</param>
        /// <param name="handler">Delegate that uses as protocol handler for processing requested urls.</param>
        /// <param name="makeDefaultHandler">Delegate that is invoked when protocol is set as default.</param>
        void RegisterProtocolHandler( string protocol, string friendlyName, ProtocolHandlerCallback handler, MethodInvoker makeDefaultHandler );
    }

    /// <summary>
    /// Delegate for the method which is invoked when an URL is opened through Omea.
    /// </summary>
    public delegate void ProtocolHandlerCallback( string url );
}
