﻿/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace JetBrains.Interop.WinApi
{
	[Flags]
	public enum OFN : uint
	{
		OFN_READONLY = 0x00000001,
		OFN_OVERWRITEPROMPT = 0x00000002,
		OFN_HIDEREADONLY = 0x00000004,
		OFN_NOCHANGEDIR = 0x00000008,
		OFN_SHOWHELP = 0x00000010,
		OFN_ENABLEHOOK = 0x00000020,
		OFN_ENABLETEMPLATE = 0x00000040,
		OFN_ENABLETEMPLATEHANDLE = 0x00000080,
		OFN_NOVALIDATE = 0x00000100,
		OFN_ALLOWMULTISELECT = 0x00000200,
		OFN_EXTENSIONDIFFERENT = 0x00000400,
		OFN_PATHMUSTEXIST = 0x00000800,
		OFN_FILEMUSTEXIST = 0x00001000,
		OFN_CREATEPROMPT = 0x00002000,
		OFN_SHAREAWARE = 0x00004000,
		OFN_NOREADONLYRETURN = 0x00008000,
		OFN_NOTESTFILECREATE = 0x00010000,
		OFN_NONETWORKBUTTON = 0x00020000,
		OFN_NOLONGNAMES = 0x00040000,

		OFN_EXPLORER = 0x00080000,
		OFN_NODEREFERENCELINKS = 0x00100000,
		OFN_LONGNAMES = 0x00200000,

		OFN_ENABLEINCLUDENOTIFY = 0x00400000,
		OFN_ENABLESIZING = 0x00800000,

		OFN_DONTADDTORECENT = 0x02000000,
		OFN_FORCESHOWHIDDEN = 0x10000000,
	}
}