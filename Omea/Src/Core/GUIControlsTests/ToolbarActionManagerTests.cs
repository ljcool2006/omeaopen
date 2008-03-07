﻿/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.PicoCore;
using NUnit.Framework;

namespace GUIControlsTests
{
	/// <summary>
	/// Tests for ToolbarActionManager.
	/// </summary>
	[TestFixture]
    public class ToolbarActionManagerTests
	{
        private TestCore _core;
        private ToolBar _toolBar;
        private ToolbarActionManager _actionManager;
        
        [SetUp] public void SetUp()
        {
            _core = new TestCore();
            _toolBar = new ToolBar();
            _actionManager = new ToolbarActionManager( _toolBar );
        }

        [TearDown] public void TearDown()
        {
            _toolBar.Dispose();
            _core.Dispose();
        }

        [Test] public void UnregisterAction()
        {
            MockAction action = new MockAction();
            _actionManager.RegisterAction( action, null, ListAnchor.Last, 
                (Image) null, "My Action", "", null, null );
            _actionManager.UpdateToolbarActions();

            Assert.AreEqual( 2, _toolBar.Buttons.Count );
            Assert.AreEqual( ToolBarButtonStyle.Separator, _toolBar.Buttons [1].Style );
            Assert.AreEqual( false, _toolBar.Buttons [1].Visible );

            _actionManager.UnregisterAction( action );
            Assert.AreEqual( 1, _toolBar.Buttons.Count );
            Assert.AreEqual( ToolBarButtonStyle.Separator, _toolBar.Buttons [0].Style );
        }

        private class MockAction: IAction
        {
            public void Execute( IActionContext context )
            {
            }

            public void Update( IActionContext context, ref ActionPresentation presentation )
            {
            }
        }
	}
}