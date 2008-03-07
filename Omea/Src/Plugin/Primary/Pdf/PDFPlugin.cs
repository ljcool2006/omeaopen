﻿/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>


using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using AxAcroPDFLib;

using JetBrains.Build.InstallationData;
using JetBrains.Omea.Base;
using JetBrains.Omea.Base.Install;
using JetBrains.Omea.COM;
using JetBrains.Omea.OpenAPI;
using Microsoft.Win32;

/// Supplementary tool.
[assembly : InstallFile("PdfToText", TargetRootXml.InstallDir, "", SourceRootXml.ProductHomeDir, "Lib/Distribution", "pdftotext.exe", "9ffc8af8-1cba-4963-910f-76ce1d79f385")]

namespace JetBrains.Omea.PDFPlugin
{
	[PluginDescriptionAttribute("JetBrains Inc.", "Adobe PDF files viewer and plain text extractor, for search capabilities")]
	public class PDFPlugin : IPlugin, IResourceTextProvider
	{
		private AcrobatOcxDisplayer _ocxDisplayer;
		private Acrobat7Displayer _acro7Displayer;

		public void Register()
		{
			RegisterTypes();
			Core.PluginLoader.RegisterResourceTextProvider( "PdfFile", this );

			if ( Acrobat7Installed() )
			{
				_acro7Displayer = new Acrobat7Displayer();
				Core.PluginLoader.RegisterResourceDisplayer( "PdfFile", _acro7Displayer );
			}
			else
			{
				_ocxDisplayer = new AcrobatOcxDisplayer();
				Core.PluginLoader.RegisterResourceDisplayer( "PdfFile", _ocxDisplayer );                
			}

			if( !File.Exists( Application.StartupPath + "\\pdftotext.exe" ) )
			{
				System.Windows.Forms.MessageBox.Show( "PDF plugin can not find [pdftotext.exe] supplementary file", "Error while loading PDFPlugin" );
			}
		}

		private bool Acrobat7Installed()
		{
			bool result = false;
			RegistryKey regKey = Registry.ClassesRoot.OpenSubKey( "AcroExch.Document" );
			if ( regKey != null )
			{
				RegistryKey curVerKey = regKey.OpenSubKey( "CurVer" );
				if ( curVerKey != null )
				{
					string defaultValue = (string) curVerKey.GetValue( "" );
					if ( defaultValue != null )
					{
						int pos = defaultValue.LastIndexOf( "." );
						if ( pos >= 0 )
						{
							string versionStr = defaultValue.Substring( pos + 1 );
							try
							{
								int version = Int32.Parse( versionStr );
								if ( version >= 7 )
								{
									result = true;
								}
							}
							catch( Exception )
							{
								// ignore
							}
						}
					}
					curVerKey.Close();
				}
				regKey.Close();
			}

			return result;
		}

		public void Startup()
		{
		}

		public void Shutdown()
		{
			if ( _ocxDisplayer != null )
			{
				_ocxDisplayer.Dispose();
			}
			if ( _acro7Displayer != null )
			{
				_acro7Displayer.Dispose();
			}
			Core.FileResourceManager.DeregisterFileResourceType( "PdfFile" );
		}

		bool  IResourceTextProvider.ProcessResourceText( IResource resource, IResourceTextConsumer consumer )
		{
			Debug.Assert( resource.Type == "PdfFile", "PDFPlugin doesn't process resources of type " + resource.Type );

			if( consumer.Purpose == TextRequestPurpose.ContextExtraction &&
				resource.GetIntProp( "Size" ) > MaxFileSize )
				return false;

			string  name = Core.FileResourceManager.GetSourceFile( resource );
			if ( name != null )
			{
				ProcessPDFFile( resource.Id, name, consumer );
				Core.FileResourceManager.CleanupSourceFile( resource, name );
			}
			return true;
		}

		//---------------------------------------------------------------------
		protected   void    RegisterTypes()
		{
			string exts = Core.SettingStore.ReadString( "FilePlugin", "PdfExts" );
			exts = ( exts.Length == 0 ) ? ".pdf" : exts + ",.pdf";
			string[] extsArray = exts.Split( ',' );
			for( int i = 0; i < extsArray.Length; ++i )
			{
				extsArray[ i ] = extsArray[ i ].Trim();
			}
			Core.FileResourceManager.RegisterFileResourceType(
				"PdfFile", "PDF File", "Name", 0, this, extsArray );
			Core.FileResourceManager.SetContentType( "PdfFile", "application/pdf" );
		}

		//---------------------------------------------------------------------
		protected   void    ProcessPDFFile( int ID, string FileName, IResourceTextConsumer consumer )
		{
			Process process = new Process();
			Debug.WriteLine( "Starting indexing: " + FileName );
			string workPath = Path.GetTempPath();
			string outFile = Path.Combine( workPath, "pdf2text.out" );
			try
			{
				process.StartInfo.FileName = "pdftotext.exe";
				process.StartInfo.Arguments = " -lowprio " + Utils.QuotedString( FileName ) + " " + outFile;
				process.StartInfo.WorkingDirectory = workPath;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				if( !process.Start() )
					throw( new Exception( "Aplication did not managed to call Start for the process with filename: " + FileName ));
				process.WaitForExit();

				StreamReader reader = new StreamReader( outFile );
				string  Buffer = Utils.StreamReaderReadToEnd( reader );
				reader.Close();
				File.Delete( outFile );

				consumer.AddDocumentFragment( ID, Buffer );
			}
			catch( Exception exc_ )
			{
				Debug.WriteLine( "Can not start process [" + process.StartInfo.FileName + process.StartInfo.Arguments + "] with reason " + exc_.Message );
			}
		}

		//---------------------------------------------------------------------
		private const int       MaxFileSize = 500000;
	}

	internal class AcrobatOcxDisplayer : IResourceDisplayer, IDisplayPane, IDisposable
	{
		private AxPdfLib.AxPdf _axPdf;

		public AcrobatOcxDisplayer()
		{
			_axPdf = new AxPdfLib.AxPdf();
			((System.ComponentModel.ISupportInitialize)_axPdf).BeginInit();
			_axPdf.Enabled = true;
			_axPdf.Name = "_axPdf";
		}

		public void Dispose()
		{
			if( _axPdf != null )
				COM_Object.Release( _axPdf.GetOcx() );
		}

		public IDisplayPane CreateDisplayPane( string resourceType )
		{
			return this;
		}

		Control IDisplayPane.GetControl()
		{
			return _axPdf;
		}
        
		void IDisplayPane.DisplayResource( IResource resource )
		{
			try
			{
				string  FileName = Core.FileResourceManager.GetSourceFile( resource );
				Debug.Assert(( FileName != null ) && ( FileName.Length > 0 ));
				_axPdf.LoadFile( FileName );
			}
			catch( Exception exc )
			{
				Core.ReportBackgroundException( exc );
			}
		}
        
		void IDisplayPane.EndDisplayResource( IResource resource )
		{
		}

		void IDisplayPane.HighlightWords( WordPtr[] words )
		{
		}

		void IDisplayPane.DisposePane()
		{
		}

		public string GetSelectedText( ref TextFormat format )
		{
			return null;
		}

		public string GetSelectedPlainText()
		{
			return null;
		}

		bool ICommandProcessor.CanExecuteCommand( string action )
		{
			return false;
		}

		void ICommandProcessor.ExecuteCommand( string action )
		{
		}
	}

	internal class Acrobat7Displayer: IResourceDisplayer, IDisplayPane, IDisposable
	{
		private AxAcroPDFLib.AxAcroPDF _axPdf;

		public Acrobat7Displayer()
		{
			_axPdf = new AxAcroPDF();
			_axPdf.Enabled = true;
			_axPdf.Name = "_axPdf";
		}

		public void Dispose()
		{
			if( _axPdf != null )
				COM_Object.Release( _axPdf.GetOcx() );
		}

		public IDisplayPane CreateDisplayPane( string resourceType )
		{
			return this;
		}

		Control IDisplayPane.GetControl()
		{
			return _axPdf;
		}
        
		void IDisplayPane.DisplayResource( IResource resource )
		{
			if( resource == null )
				throw new ArgumentNullException( "resource", "PDFPlugin -- Input resource is NULL in DisplayResource." );

			try	// Trap general failures, and report to background exception collector
			{
				try	// Filter out some known and expectd errors, and avoid from showing them to the end user
				{
					string  fileName = Core.FileResourceManager.GetSourceFile( resource );
					if( fileName == null )
						throw new ApplicationException( "PDFPlugin -- Can not restore PDF file from resource." );
					_axPdf.LoadFile( fileName );
				}
				catch(COMException ex)
				{
					if(ex.ErrorCode == -0x7FFFBFFB)	// E_FAIL (?) an integer representation for 0x80004005
						Trace.WriteLine("The Acro7 Control has told us that shit happens.", "PDF");
					else
						throw ex;	// Let it be processed the usual way
				}
			}
			catch( Exception exc )
			{
				Core.ReportBackgroundException( exc );
			}
		}
        
		void IDisplayPane.EndDisplayResource( IResource resource )
		{
		}

		void IDisplayPane.HighlightWords( WordPtr[] words )
		{
		}

		void IDisplayPane.DisposePane()
		{
		}

		public string GetSelectedText( ref TextFormat format )
		{
			return null;
		}

		public string GetSelectedPlainText()
		{
			return null;
		}

		bool ICommandProcessor.CanExecuteCommand( string action )
		{
			return false;
		}

		void ICommandProcessor.ExecuteCommand( string action )
		{
		}
	}

	/*
    internal class AcrobatWebDisplayer: IResourceDisplayer
    {
        #region IResourceDisplayer Members
        
        public IDisplayPane CreateDisplayPane( string resourceType )
        {
            return new BrowserDisplayPane( new DisplayResourceInBrowserDelegate( DisplayPdf ) );
        }

        private void DisplayPdf( IResource resource, AbstractWebBrowser browser, WordPtr[] wordsToHighlight )
        {
            string fileName = null;
            try
            {
                fileName = Core.FileResourceManager.GetSourceFile( resource );
            }
            catch( Exception e )
            {
                Utils.DisplayException( e, "Can't display PDF file" );
            }
            if ( fileName != null )
            {
                browser.NavigateInPlace( fileName );
            }
            else
            {
                browser.ShowHtml( "Failed to get source file for resource" );
            }
        }

        #endregion
    }
    */

}