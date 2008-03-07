﻿/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.Nntp
{
	internal class ArticlePreviewPane: MessageDisplayPane
	{
        private const string _cPluginIniSection = "NNTP";
        private const string _cFontOverride = "NewsArticleFontOverride";
        private const string _cFont = "NewsArticleFont";
        private const string _cFontSize = "NewsArticleFontSize";

        private IResourceList _articleListener;
		private System.ComponentModel.Container components = null;
        private readonly IResourceBrowser _rbrowser = Core.ResourceBrowser;
        private JetLinkLabel _downloadLabel;
        private bool _articleIsRedisplayed;

        private static string  _font;
        private static int     _fontSize;

		/// <summary>
		/// The Web Security Context that displays the News Article preview by default, in the restricted environment.
		/// </summary>
		private readonly WebSecurityContext _ctxRestricted;

	    public ArticlePreviewPane()
		{
			InitializeComponent();            

			// Initialize the security context
			_ctxRestricted = WebSecurityContext.Restricted;
			_ctxRestricted.WorkOffline = false;	// Enable downloading of the referenced content

	        ReadNewsarticleFontAttributes();

            Core.UIManager.AddOptionsChangesListener( "Internet", "Newsgroups", ReadNewsarticleFontAttributesHandler);
            Core.UIManager.AddOptionsChangesListener( "Omea", "General", ReadNewsarticleFontAttributesHandler);
        }

        private static void ReadNewsarticleFontAttributesHandler( object sender, EventArgs args )
        {
            ReadNewsarticleFontAttributes();
            Core.ResourceBrowser.RedisplaySelectedResource();
        }

        private static void ReadNewsarticleFontAttributes()
        {
            //  Initialize font from local settings or from the Core.UIManager.
            _font = Core.UIManager.DefaultFontFace;
            _fontSize = (int)Core.UIManager.DefaultFontSize;

            bool overriden = Core.SettingStore.ReadBool( _cPluginIniSection, _cFontOverride, false );
            if( overriden )
            {
                _font = Core.SettingStore.ReadString( _cPluginIniSection, _cFont, Core.UIManager.DefaultFontFace );
                _fontSize = Core.SettingStore.ReadInt( _cPluginIniSection, _cFontSize, (int)Core.UIManager.DefaultFontSize );
            }
        }

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		private void InitializeComponent()
		{
            this.SuspendLayout();
            // 
            // ArticlePreviewPane
            // 
            this.Name = "ArticlePreviewPane";
            this.Size = new System.Drawing.Size(608, 280);
            this.ResumeLayout(false);

        }
		#endregion

		public override void DisplayResource( IResource article, WordPtr[] wordsToHighlight )
		{
            if( _downloadLabel == null )
            {
                _downloadLabel = new JetLinkLabel();
                _downloadLabel.Text = "Click here or press F5 to download the article";
                _downloadLabel.BackColor = Color.Transparent;
                _downloadLabel.ClickableLink = true;
                _downloadLabel.Click += _downloadLabel_Click;
                Controls.Add( _downloadLabel );
            }
            _downloadLabel.Visible = false;
            _ieBrowser.Visible = true;
		    bool redisplayed = _articleIsRedisplayed;
            _articleIsRedisplayed = false;

		    DisposeArticleListener();

			// Set the subject, highlight if needed
            ShowSubject( article.GetPropText( Core.Props.Subject ), wordsToHighlight );

            try
            {
                //  The content being indexed (plaintext) is not the same that is
                //  displayed (autogenerated HTML), so the offsets are updated by
                //  this method to correspond to the new formatting.
                string sFormattedArticle = ArticleBody2Html( article, ref wordsToHighlight );
                ShowHtml( sFormattedArticle, _ctxRestricted, wordsToHighlight );
            }
            catch( Exception e )
            {
                Utils.DisplayException( e, "Error" );
                return;
            }

            /**
             * set last selected article for article's owner
             */
            IResource owner = _rbrowser.OwnerResource;
            if( owner != null && ( owner.Type == NntpPlugin._newsGroup ||
                owner.Type == NntpPlugin._newsFolder || owner.Type == NntpPlugin._newsServer ) )
            {
                ResourceProxy proxy = new ResourceProxy( owner );
                proxy.AsyncPriority = JobPriority.AboveNormal;
                proxy.SetPropAsync( NntpPlugin._propLastSelectedArticle, article.Id );
            }

            _articleListener = article.ToResourceListLive();
            _articleListener.ResourceChanged += _articleListener_ResourceChanged;

		    bool hasNoBody = article.HasProp( NntpPlugin._propHasNoBody );
		    if( !hasNoBody )
            {
                Core.UIManager.GetStatusWriter( this, StatusPane.Network ).ClearStatus();
            }
            else
            {
                if( !Utils.IsNetworkConnectedLight() )
                {
                    ShowHtml( "<pre>" + NntpPlugin._networkUnavailable + ".</pre>", WebSecurityContext.Internet, null );
                    return;
                }
                IResourceList groups = article.GetLinksOfType( NntpPlugin._newsGroup, NntpPlugin._propTo );
                if( groups.Count > 0 )
                {
                    foreach( IResource groupRes in groups.ValidResources )
                    {
                        IResource server = new NewsgroupResource( groupRes ).Server;
                        if( server != null )
                        {
                            ServerResource serverRes = new ServerResource( server );
                            if( redisplayed || serverRes.DownloadBodiesOnDeliver || serverRes.DownloadBodyOnSelection )
                            {
                                Core.UIManager.GetStatusWriter( this, StatusPane.Network ).ShowStatus( "Downloading article..." );
                                NntpConnection articlesConnection = NntpConnectionPool.GetConnection( server, "foreground" );
                                NntpDownloadArticleUnit downloadUnit =
                                    new NntpDownloadArticleUnit( article, groupRes, JobPriority.Immediate, true );
                                downloadUnit.OnProgress += downloadUnit_OnProgress;
                                ShowHtml( "<pre>Downloading article: 0%</pre>", WebSecurityContext.Internet, null );
                                articlesConnection.StartUnit( Int32.MaxValue - 1, downloadUnit );
                                return;
                            }
                        }
                    }
                    if( !redisplayed )
                    {
                        Point location = _ieBrowser.Location;
                        _downloadLabel.Location = new Point( location.X + 8, location.Y + 8 );
                        _downloadLabel.Visible = true;
                        _ieBrowser.Visible = false;
                    }
                }
            }
        }

        public override void EndDisplayResource( IResource res )
        {
            _downloadLabel.Visible = false;
            _ieBrowser.Visible = true;
            DisposeArticleListener();
        }

        public override void DisposePane()
        {
            _downloadLabel.Visible = false;
            DisposeArticleListener();
        }

		/// <summary>
		/// Extracts the article body.
		/// If plaintext, optionally converts it to formatted HTML and maintains the offsets in the latter case.
		/// </summary>
        internal static string ArticleBody2Html( IResource article, ref WordPtr[] toHighlight )
        {
            string formattedText;
            if( article.HasProp( NntpPlugin._propHtmlContent ) )	// HTML, don't update offsets
            {
                formattedText = article.GetPropText( NntpPlugin._propHtmlContent );
                formattedText = Core.MessageFormatter.GetFormattedHtmlBody( article, formattedText, ref toHighlight );
            }
            else	// Plaintext, possibly formatted, update offsets
            {
                formattedText = Core.MessageFormatter.GetFormattedBody( article, Core.Props.LongBody, NntpPlugin._propReply,
                                                                        ref toHighlight, _font, _fontSize );
            }
            
            /**
             * Look for X-Newsreader header if formating is enabled
             */
            if( !article.HasProp( "NoFormat" ) )
            {
                string[] headers = article.GetPropText( NntpPlugin._propArticleHeaders ).Split( '\n' );
                foreach( string headerLine in headers )
                {
                    if( headerLine.StartsWith( "X-Newsreader: " ) && headerLine.IndexOf( Core.ProductName ) > 0 )
                    {
                        formattedText += "<p align=right><small>Posted by ";
                        formattedText += headerLine.Substring( "X-Newsreader: ".Length );
                        formattedText += "</small></p>";
                        break;
                    }
                }
            }

            /**
             * display inline attachments and subtitute embedded ones if any
             */
            if( Settings.DisplayAttachmentsInline )
            {
                IResourceList attachments = article.GetLinksOfType( null, NntpPlugin._propAttachment );
                foreach( IResource attachment in attachments )
                {
                    if( attachment.Type == "Picture" )
                    {
                        string filename = Core.FileResourceManager.GetSourceFile( attachment );
                        formattedText += "<hr><center><img src=\"";
                        formattedText += filename;
                        formattedText += "\"></center>";
                    }
                }
            }

            IResourceList embeddedList = article.GetLinksOfType( null, NntpPlugin._propEmbeddedContent );
            foreach( IResource embeddedContent in embeddedList )
            {
                string contentId = embeddedContent.GetPropText( CommonProps.ContentId );
                if( contentId.Length > 0 )
                {
                    string filename = Core.FileResourceManager.GetSourceFile( embeddedContent );
                    formattedText = formattedText.Replace( "cid:" + contentId, filename );
                }
            }

            return formattedText;
        }

        private void DisposeArticleListener()
        {
            if( _articleListener != null )
            {
                _articleListener.Dispose();
                _articleListener = null;
            }
        }

        private void _articleListener_ResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            IResource article = e.Resource;
            bool need2Redisplay = false;
            foreach( int prop in e.ChangeSet.GetChangedProperties() )
            {
                if( prop == Core.Props.IsUnread )
                {
                    if( article.HasProp( NntpPlugin._propHasNoBody ) && !article.HasProp( prop ) &&

                        ////////////////////////////////////////////////////////////////////////////////////////////////////////
                        /// that is the dirtiest hack i ever wrote:
                        /// we try to recognize by name of current resource job that unread status is changed
                        /// by ResourceBrowser's timer handler
                        ////////////////////////////////////////////////////////////////////////////////////////////////////////
                        Core.ResourceAP.CurrentJobName == "Marking resource read by timer" )
                    {
                        Core.ResourceAP.QueueJob(
                            JobPriority.Immediate, new ResourceDelegate( SetResourceUnread ), article );
                    }
                }
                if( prop == Core.Props.LongBody ||
                    prop == NntpPlugin._propHtmlContent ||
                    prop == Core.Props.Subject ||
                    prop == NntpPlugin._propHasNoBody ||
                    prop == Core.ContactManager.Props.LinkFrom ||
                    prop == Core.FileResourceManager.PropCharset )
                {
                    need2Redisplay = true;
                }
            }
            if( need2Redisplay )
            {
                RedisplayArticle( article );
            }
        }

        private static void SetResourceUnread( IResource res )
        {
            res.SetProp( Core.Props.IsUnread, true );
        }

        internal void RedisplayArticle( IResource res )
        {
            if( !Core.UserInterfaceAP.IsOwnerThread )
            {
                Core.UserInterfaceAP.QueueJob( new ResourceDelegate( RedisplayArticle ), res );
            }
            else
            {
                if( IsArticleDisplayed( res ) )
                {
                    _articleIsRedisplayed = true;
                    _rbrowser.RedisplaySelectedResource();
                }
            }
        }

        internal bool IsArticleDisplayed( IResource res )
        {
            try
            {
                if( _articleListener != null && !res.IsDeleted && _articleListener[ 0 ] == res )
                {
                    return true;
                }
            }
            catch( InvalidResourceIdException ) {}
            return false;
        }

        private void downloadUnit_OnProgress( NntpDownloadArticleUnit sender, string progress )
        {
            if( !Core.UserInterfaceAP.IsOwnerThread )
            {
                Core.UserInterfaceAP.QueueJob( new NntpDownloadArticleUnit.ProgressDelegate( downloadUnit_OnProgress ), sender, progress );
            }
            else
            {
                IResource article = null;
                try
                {
                    if( _articleListener != null )
                    {
                        article = _articleListener[ 0 ];
                    }
                    if( article != null && article == sender.Article && article.HasProp( NntpPlugin._propHasNoBody ) )
                    {
                        ShowHtml( "<pre>Downloading article: " + progress + "%</pre>", _ctxRestricted, null );
                    }
                }
                catch( InvalidResourceIdException ) {}
            }
        }

        private void _downloadLabel_Click(object sender, EventArgs e)
        {
            if( _articleListener != null )
            {
                IResource article = null;
                lock( _articleListener )
                {
                    if( _articleListener.Count > 0 )
                    {
                        article = _articleListener[ 0 ];
                    }
                }
                if( article != null && !article.IsDeleted )
                {
                    RefreshArticleAction.RefreshArticleImpl( article.ToResourceList() );
                }
            }
        }
    }
}
