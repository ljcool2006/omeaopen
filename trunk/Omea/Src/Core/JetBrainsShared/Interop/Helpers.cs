﻿using System;

/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

namespace JetBrains.UI.Interop
{
	/// <summary>
	/// Contains several interop helper functions
	/// </summary>
	public static class Helpers
	{
				/// <summary>
		/// Performs all the checks of the <see cref="JetBrains.UI.Avalon.Helpers.Glassify(IntPtr,bool)"/> function, but does not actually apply the effect.
		/// Allows to tell with a high probability whether the <see cref="JetBrains.UI.Avalon.Helpers.Glassify(IntPtr,bool)"/> function will succeed.
		/// </summary>
		public static bool CanGlassify(IntPtr handle)
		{
			if(handle == IntPtr.Zero)
				throw new ArgumentNullException("handle");

			// Is the glass effect available?
			if(!((Environment.OSVersion.Platform == PlatformID.Win32NT) && (Environment.OSVersion.Version >= new Version(6, 0)) && (Win32Declarations.DwmIsCompositionEnabled())))
				return false;

			return true;
		}

	}
}