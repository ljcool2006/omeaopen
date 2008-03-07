﻿/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace JetBrains.Interop.WinApi
{
	[Flags]
	public enum AccessRights : uint
	{
		GENERIC_READ = 0x80000000,
		GENERIC_WRITE = 0x40000000,
		GENERIC_EXECUTE = 0x20000000,
		GENERIC_ALL = 0x10000000,

		ACCESS_SYSTEM_SECURITY = 0x01000000,

		DELETE = 0x00010000,
		READ_CONTROL = 0x00020000,
		WRITE_DAC = 0x00040000,
		WRITE_OWNER = 0x00080000,
		SYNCHRONIZE = 0x00100000,

		STANDARD_RIGHTS_READ = READ_CONTROL,
		STANDARD_RIGHTS_WRITE = READ_CONTROL,
		STANDARD_RIGHTS_EXECUTE = READ_CONTROL,

		STANDARD_RIGHTS_ALL = 0x001F0000,
		SPECIFIC_RIGHTS_ALL = 0x0000FFFF,
	}
}