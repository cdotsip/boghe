﻿/*
* Boghe IMS/RCS Client - Copyright (C) 2010 Mamadou Diop.
*
* Contact: Mamadou Diop <diopmamadou(at)doubango.org>
*	
* This file is part of Boghe Project (http://code.google.com/p/boghe)
*
* Boghe is free software: you can redistribute it and/or modify it under the terms of 
* the GNU General Public License as published by the Free Software Foundation, either version 3 
* of the License, or (at your option) any later version.
*	
* Boghe is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
* without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  
* See the GNU General Public License for more details.
*	
* You should have received a copy of the GNU General Public License along 
* with this program; if not, write to the Free Software Foundation, Inc., 
* 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.doubango.tinyWRAP;
using BogheCore.Utils;
using BogheCore.Sip;
using System.Drawing;
using BogheControls.Utils;
using BogheCore.Model;
using BogheCore;

namespace BogheApp
{
    partial class SessionWindow
    {
        private bool MsrpFailureReport
        {
            get
            {
                return this.configurationService.Get(Configuration.ConfFolder.RCS, Configuration.ConfEntry.MSRP_FAILURE, Configuration.DEFAULT_RCS_MSRP_FAILURE);
            }
        }

        private bool MsrpSuccessReport
        {
            get
            {
                return this.configurationService.Get(Configuration.ConfFolder.RCS, Configuration.ConfEntry.MSRP_SUCCESS, Configuration.DEFAULT_RCS_MSRP_SUCCESS);
            }
        }

        private bool MsrpOmaFinalDeliveryReport
        {
            get
            {
                return this.configurationService.Get(Configuration.ConfFolder.RCS, Configuration.ConfEntry.OMAFDR, Configuration.DEFAULT_RCS_OMAFDR);
            }
        }

        private MyMsrpSession CreateOutgoingSession(MediaType mediaType)
        {
            MyMsrpSession msrpSession = MyMsrpSession.CreateOutgoingSession(this.sipService.SipStack, mediaType, this.remotePartyUri);
            msrpSession.SuccessReport = this.MsrpSuccessReport;
            msrpSession.FailureReport = this.MsrpFailureReport;
            msrpSession.OmaFinalDeliveryReport = this.MsrpOmaFinalDeliveryReport;

            return msrpSession;
        }

        private void SendFile(String filePath)
        {
            MyMsrpSession msrpSession = this.CreateOutgoingSession(MediaType.FileTransfer);
            lock (this.fileTransferSessions)
            {
                this.fileTransferSessions.Add(msrpSession);
            }
            msrpSession.onMsrpEvent += this.FileTransfer_onMsrpEvent;

            HistoryFileTransferEvent @event = new HistoryFileTransferEvent(this.remotePartyUri, filePath);
            @event.Status = HistoryEvent.StatusType.Outgoing;
            @event.MsrpSession = msrpSession;
            @event.SipSessionId = msrpSession.Id;
            this.AddMessagingEvent(@event);

            msrpSession.SendFile(filePath);
        }        

        private void AttachDisplays()
        {
            if (this.AVSession == null || this.AVSession.MediaSessionMgr == null)
            {
                return;
            }

            lock (this.AVSession)
            {
                // Remote
                this.AVSession.MediaSessionMgr.consumerSetInt64(twrap_media_type_t.twrap_media_video, "remote-hwnd", this.videoDisplayRemote.Handle.ToInt64());
                this.AVSession.MediaSessionMgr.producerSetInt64(twrap_media_type_t.twrap_media_video, "local-hwnd", this.videoDisplayLocal.Handle.ToInt64());

                if (this.AVSession.IsConnected)
                {
                    this.videoDisplayLocal.Visibility = System.Windows.Visibility.Visible;
                    this.videoDisplayRemote.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        private void AddMessagingEvent(HistoryEvent @event)
        {
            this.historyDataSource.Add(@event);

            if (this.chatHistoryEvent != null)
            {
                this.chatHistoryEvent.Messages.Add(@event as HistoryShortMessageEvent);
            }
            else
            {
                switch (@event.MediaType)
                {
                    case BogheCore.MediaType.ShortMessage:
                    case BogheCore.MediaType.SMS:
                        this.historyService.AddEvent(@event);
                        break;
                }
            }

            if (@event.Status == HistoryEvent.StatusType.Incoming)
            {
                this.soundService.PlayNewEvent();
            }
        }

        private void InitializeView()
        {
            if (this.AVSession != null)
            {
                lock (this.AVSession)
                {
                    this.Title = String.Format("Talking with {0}", this.AVSession.RemotePartyDisplayName);
                    this.UpdateControls();
                }
            }
            else
            {
                this.Title = String.Format("Talking with {0}", UriUtils.GetDisplayName(this.remotePartyUri));
            }
        }

        private void UpdateControls()
        {
            if (this.AVSession != null)
            {
                lock (this.AVSession)
                {
                    switch (this.AVSession.State)
                    {
                        case MyInviteSession.InviteState.INCOMING:
                            this.labelInfo.Content = String.Format("Incoming call from {0}", this.AVSession.RemotePartyDisplayName);
                            this.buttonHangUp.IsEnabled = true;
                            this.UpdateButtonCallOrAnswer(true, "Answer", Properties.Resources.phone_pick_up_32);
                            
                            this.MenuItemCall_MakeAudioCall.IsEnabled = false;
                            this.MenuItemCall_MakeVideoCall.IsEnabled = false;
                            this.MenuItemCall_HoldResume.IsEnabled = false;
                            this.MenuItemCall_MakeTransfer.IsEnabled = false;
                            this.MenuItemCall_HangUp.IsEnabled = true;
                            break;

                        case MyInviteSession.InviteState.INPROGRESS:
                            this.buttonHangUp.IsEnabled = true;
                            this.UpdateButtonCallOrAnswer(false, "Call", Properties.Resources.phone_pick_up_32);

                            this.MenuItemCall_MakeAudioCall.IsEnabled = false;
                            this.MenuItemCall_MakeVideoCall.IsEnabled = false;
                            this.MenuItemCall_HoldResume.IsEnabled = false;
                            this.MenuItemCall_MakeTransfer.IsEnabled = false;
                            this.MenuItemCall_HangUp.IsEnabled = true;
                            break;

                        case MyInviteSession.InviteState.INCALL:
                            this.buttonHangUp.IsEnabled = true;
                            this.UpdateButtonCallOrAnswer(false, "Call", Properties.Resources.phone_pick_up_32);

                            this.MenuItemCall_MakeAudioCall.IsEnabled = false;
                            this.MenuItemCall_MakeVideoCall.IsEnabled = false;
                            this.MenuItemCall_HoldResume.IsEnabled = true;
                            this.MenuItemCall_MakeTransfer.IsEnabled = false;
                            this.MenuItemCall_HangUp.IsEnabled = true;
                            break;

                        case MyInviteSession.InviteState.TERMINATED:
                        case MyInviteSession.InviteState.TERMINATING:
                            this.buttonHangUp.IsEnabled = false;
                            this.UpdateButtonCallOrAnswer(true, "Call", Properties.Resources.phone_pick_up_32);

                            this.MenuItemCall_MakeAudioCall.IsEnabled = true;
                            this.MenuItemCall_MakeVideoCall.IsEnabled = true;
                            this.MenuItemCall_HoldResume.IsEnabled = false;
                            this.MenuItemCall_MakeTransfer.IsEnabled = false;
                            this.MenuItemCall_HangUp.IsEnabled = false;
                            break;
                    }
                }
            }
        }

        private void UpdateButtonCallOrAnswer(bool enabled, String text, Bitmap image)
        {
            this.buttonCallOrAnswer.Tag = text;
            this.buttonCallOrAnswer.IsEnabled = enabled;
            this.buttonCallOrAnswerLabel.Content = text;
            this.buttonCallOrAnswerImage.Source = MyImageConverter.FromBitmap(image);
        }
    }
}
