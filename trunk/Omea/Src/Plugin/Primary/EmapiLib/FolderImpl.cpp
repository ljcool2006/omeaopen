﻿/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#include "folderimpl.h"
#include "messageimpl.h"
#include "etableimpl.h"
#using <mscorlib.dll>
#include "EntryID.h"
#include "ETable.h"
#include "EMAPIFolder.h"
#include "EMessage.h"
#include "Messages.h"
#include "Guard.h"
#include "StringConvertion.h"
#include "temp.h"

EMAPILib::FolderImpl::FolderImpl( const EMAPIFolderSPtr& eFolder ) : MAPIPropImpl( eFolder.get() )
{
    if ( eFolder.IsNull() )
    {
        Guard::ThrowArgumentNullException( "eFolder" );
    }
    _eFolder = eFolder.CloneOnHeap();
}
EMAPILib::FolderImpl::~FolderImpl()
{
    _eFolder = NULL;
}
void EMAPILib::FolderImpl::Empty()
{
    CheckDisposed();
    (*_eFolder)->Empty();
}
void EMAPILib::FolderImpl::SetReadFlags( String* entryID, bool unread )
{
    CheckDisposed();
    EntryIDSPtr entry;
    if ( entryID != NULL )
    {
        entry = Helper::HexToEntryID( entryID );
    }
    (*_eFolder)->SetReadFlags( entry, unread );
}
void EMAPILib::FolderImpl::CopyTo( IEFolder* destFolder )
{
    CheckDisposed( );
    MAPIPropImpl::CopyTo( &IID_IMAPIFolder, destFolder );
}
void EMAPILib::FolderImpl::SetMessageStatus( String* entryID, int newStatus, int newStatusMask )
{
    CheckDisposed();
    if ( entryID == NULL ) 
    {
        Guard::ThrowArgumentNullException( "entryID" );
    }
    EntryIDSPtr entry = Helper::HexToEntryID( entryID );
    (*_eFolder)->SetMessageStatus( entry, newStatus, newStatusMask );
}
int EMAPILib::FolderImpl::GetMessageStatus( String* entryID )
{
    CheckDisposed();
    if ( entryID == NULL ) 
    {
        Guard::ThrowArgumentNullException( "entryID" );
    }
    EntryIDSPtr entry = Helper::HexToEntryID( entryID );
    return (*_eFolder)->GetMessageStatus( entry );
}

void EMAPILib::FolderImpl::Dispose()
{
    Disposable::DisposeImpl();
    TypeFactory::Delete( _eFolder );
    _eFolder = NULL;
}
String* EMAPILib::FolderImpl::GetFolderID()
{
    CheckDisposed();
    return GetBinProp( (int)PR_PARENT_ENTRYID );
}
EMAPILib::IEFolder* EMAPILib::FolderImpl::CreateSubFolder( String* name )
{
    CheckDisposed();
    if ( name == NULL ) 
    {
        Guard::ThrowArgumentNullException( "name" );
    }
    EMAPIFolderSPtr folder = (*_eFolder)->CreateSubFolder( Temp::GetANSIString( name )->GetChars() );
    if ( !folder.IsNull() )
    {
        return new FolderImpl( folder );
    }
    return NULL;
}
EMAPILib::IETable* EMAPILib::FolderImpl::GetEnumTableForOwnEmail( )
{
    CheckDisposed();
    ETableSPtr table = (*_eFolder)->GetTable();
    if ( !table.IsNull() )
    {
        SizedSPropTagArray( 3, atProps ) = 
        { 3, (int)PR_SENDER_EMAIL_ADDRESS, (int)PR_SENDER_NAME, (int)PR_MESSAGE_DELIVERY_TIME };
        table->SetColumns( (LPSPropTagArray)&atProps );
        return new ETableImpl( table );
    }
    return NULL;
}
int EMAPILib::FolderImpl::GetTag()
{
    System::Guid set1( "{00062008-0000-0000-C000-000000000046}" );
    return GetIDsFromNames( &set1, 0x8578, PT_LONG );
}

EMAPILib::IETable* EMAPILib::FolderImpl::GetEnumTable( DateTime dt )
{
    CheckDisposed();
    ETableSPtr table = (*_eFolder)->GetTable();
    if ( !table.IsNull() )
    {

        static int tag = GetTag();

        const SizedSPropTagArray( 8, atProps ) = 
        { 8, (int)PR_ENTRYID, (int)0x66700102, (int)PR_LAST_MODIFICATION_TIME, (int)PR_MESSAGE_CLASS, 
            (int)PR_MESSAGE_FLAGS, (int)PR_MESSAGE_DELIVERY_TIME, tag, PR_BODY };

        /*
        const SizedSPropTagArray( 5, atProps ) = 
        { 5, (int)PR_ENTRYID, (int)PR_TRANSPORT_MESSAGE_HEADERS, (int)PR_MESSAGE_DELIVERY_TIME, 0x801D0003, 0x80240003 };
        */
        HRESULT hr = table->SetColumns( (LPSPropTagArray)&atProps );
        if ( hr == S_OK )
        {
            if ( dt != DateTime::MinValue )
            {
                FILETIME ft;
                Guard::SetFILETIME( &ft, dt.ToFileTime() );

                SPropValue prop;
                prop.ulPropTag = (int)PR_MESSAGE_DELIVERY_TIME;
                prop.Value.ft = ft;

                LPSRestriction pRest;
                hr = MAPIAllocateBuffer( sizeof(SRestriction), (LPVOID *)&pRest );
                MAPIBuffer mapiBuffer( hr, pRest );
                if ( hr == S_OK )
                {
                    pRest->rt = (int)RES_PROPERTY;
                    pRest->res.resProperty.relop = (int)RELOP_GE;
                    pRest->res.resProperty.ulPropTag = (int)PR_MESSAGE_DELIVERY_TIME;
                    pRest->res.resProperty.lpProp = &prop;
                    table->SetRestriction( pRest );
                }
            }
            return new ETableImpl( table );
        }
    }
    return NULL;
}
EMAPILib::IETable* EMAPILib::FolderImpl::GetEnumTableForRecordKey( String* recordKey )
{
    CheckDisposed();
    if ( recordKey == NULL ) return NULL;
    ETableSPtr table = (*_eFolder)->GetTable();
    if ( !table.IsNull() )
    {
        const SizedSPropTagArray( 3, atProps ) = 
            { 3, (int)PR_ENTRYID, (int)0x66700102, (int)PR_RECORD_KEY };
        table->SetColumns( (LPSPropTagArray)&atProps );
        EntryIDSPtr entry = Helper::HexToEntryID( recordKey );
        if ( entry.IsNull() ) return NULL;

        SPropValue prop;
        prop.ulPropTag = (int)PR_RECORD_KEY;
        prop.Value.bin.cb = entry->GetLength();
        prop.Value.bin.lpb = (LPBYTE)entry->getLPENTRYID();

        LPSRestriction pRest;
        HRESULT hr = MAPIAllocateBuffer( sizeof(SRestriction), (LPVOID *)&pRest );
        Guard::CheckHR( hr );
        MAPIBuffer mapiBuffer( hr, pRest );
        pRest->rt = (int)RES_PROPERTY;
        pRest->res.resProperty.relop = (int)RELOP_EQ;
        pRest->res.resProperty.ulPropTag = (int)PR_RECORD_KEY;
        pRest->res.resProperty.lpProp = &prop;
        table->SetRestriction( pRest );
        return new ETableImpl( table );
    }
    return NULL;
}

EMAPILib::IEFolders* EMAPILib::FolderImpl::GetFolders()
{
    CheckDisposed();
    EMAPIFoldersSPtr folders = (*_eFolder)->GetFolders();
    if ( !folders.IsNull() )
    {
        return new FoldersImpl( folders );
    }
    return NULL;
}
EMAPILib::IEMessages* EMAPILib::FolderImpl::GetMessages()
{
    CheckDisposed();
    MessagesSPtr messages = (*_eFolder)->GetMessages();
    if ( !messages.IsNull() )
    {
        return new EMAPILib::MessagesImpl( messages );
    }
    return NULL;
}
EMAPILib::IEMessage* EMAPILib::FolderImpl::OpenMessage( String* entryID )
{
    CheckDisposed();
    if ( entryID == NULL )
    {
        Guard::ThrowArgumentNullException( "entryID" );
    }
    if ( entryID->get_Length() == 0 ) 
        throw new System::ArgumentException( "entryID shold not be empty" );
    EntryIDSPtr entry = Helper::HexToEntryID( entryID );
    EMessageSPtr message = (*_eFolder)->OpenMessage( entry );
    if ( !message.IsNull() )
    {
        return new MessageImpl( message );
    }
    return NULL;
}

EMAPILib::IEMessage* EMAPILib::FolderImpl::CreateMessage( String* messageClass )
{
    CheckDisposed();
    EMessageSPtr message = (*_eFolder)->CreateMessage();
    if ( !message.IsNull() )
    {
        message->setStringProp( (int)PR_MESSAGE_CLASS, Temp::GetANSIString( messageClass )->GetChars() );
        return new MessageImpl( message );
    }
    return NULL;
}
void EMAPILib::FolderImpl::MoveFolder( String* entryID, IEFolder* destFolder )
{
    CheckDisposed();
    CopyFolder( entryID, destFolder, (int)FOLDER_MOVE );
}
void EMAPILib::FolderImpl::CopyFolder( String* entryID, IEFolder* destFolder )
{
    CheckDisposed();
    CopyFolder( entryID, destFolder, 0 );
}
void EMAPILib::FolderImpl::CopyFolder( String* entryID, IEFolder* destFolder, int flags )
{
    CheckDisposed();
    FolderImpl* destFolderImpl = dynamic_cast<FolderImpl*>(destFolder);
    EntryIDSPtr entry = Helper::HexToEntryID( entryID );
    (*_eFolder)->CopyFolder( entry, *(destFolderImpl->_eFolder), flags );
}

void EMAPILib::FolderImpl::MoveMessage( String* entryID, IEFolder* destFolder )
{
    CheckDisposed();
    CopyMessage( entryID, destFolder, (int)MESSAGE_MOVE );
}
void EMAPILib::FolderImpl::CopyMessage( String* entryID, IEFolder* destFolder )
{
    CheckDisposed();
    CopyMessage( entryID, destFolder, 0 );
}
void EMAPILib::FolderImpl::CopyMessage( String* entryID, IEFolder* destFolder, int flags )
{
    CheckDisposed();
    FolderImpl* destFolderImpl = dynamic_cast<FolderImpl*>(destFolder);
    if ( destFolderImpl == NULL )
    {
        return;
    }
    EntryIDSPtr entry = Helper::HexToEntryID( entryID );
    if ( !entry.IsNull() )
    {
        (*_eFolder)->CopyMessage( entry, *(destFolderImpl->_eFolder ), (int)flags );
    }
}

EMAPILib::FoldersImpl::FoldersImpl( const EMAPIFoldersSPtr& eFolders )
{
    if ( eFolders.IsNull() )
    {
        Guard::ThrowArgumentNullException( "eFolders" );
    }
    _eFolders = eFolders.CloneOnHeap();
}
EMAPILib::FoldersImpl::~FoldersImpl()
{
}

void EMAPILib::FoldersImpl::Dispose()
{
    Disposable::DisposeImpl();
    TypeFactory::Delete( _eFolders );
    _eFolders = NULL;
}
int EMAPILib::FoldersImpl::GetCount()
{
    CheckDisposed();
    return (*_eFolders)->GetCount();
}
String* EMAPILib::FoldersImpl::GetEntryId( int rowNum )
{
    CheckDisposed();
    LPSPropValue lpProp = (*_eFolders)->GetProp( 1, rowNum );
    if ( lpProp != NULL )
    {
        return Helper::EntryIDToHex( lpProp->Value.bin.lpb, lpProp->Value.bin.cb );
    }
    return NULL;
}

EMAPILib::IEFolder* EMAPILib::FoldersImpl::OpenFolder( int rowNum )
{
    CheckDisposed();
    EMAPIFolderSPtr folder = (*_eFolders)->GetFolder( rowNum );
    if ( !folder.IsNull() )
    {
        return new FolderImpl( folder );
    }
    return NULL;
}
