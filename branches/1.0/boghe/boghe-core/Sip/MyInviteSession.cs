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

namespace BogheCore.Sip
{
    public abstract class MyInviteSession : MySipSession
    {
        protected MediaType mediaType;
        protected MediaSessionMgr mediaSessionMgr = null;
        protected InviteState state;
        

        public enum InviteState
        {
            NONE,
            INCOMING,
            INPROGRESS,
            REMOTE_RINGING,
            EARLY_MEDIA,
            INCALL,
            TERMINATING,
            TERMINATED,
        }

        protected MyInviteSession(MySipStack sipStack)
            :base(sipStack)
        {
            this.state = InviteState.NONE;
        }

        public MediaType MediaType
        {
            get { return this.mediaType; }
        }

        public InviteState State
        {
            get { return this.state; }
            set { this.state = value; }
        }

        public bool IsActive
        {
            get {
                return this.State != MyInviteSession.InviteState.NONE
                && this.State != MyInviteSession.InviteState.TERMINATING 
                && this.State != MyInviteSession.InviteState.TERMINATED; 
            }
        }

        public MediaSessionMgr MediaSessionMgr
        {
            get
            {
                if (this.mediaSessionMgr == null)
                {
                    this.mediaSessionMgr = (this.Session as InviteSession).getMediaMgr();
                }
                return this.mediaSessionMgr;
            }
        }
    }
}
