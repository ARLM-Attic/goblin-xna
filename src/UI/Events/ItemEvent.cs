/************************************************************************************ 
 * Copyright (c) 2008, Columbia University
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Columbia University nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY COLUMBIA UNIVERSITY ''AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <copyright holder> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 * 
 * ===================================================================================
 * Author: Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/ 

using System;
using System.Collections.Generic;
using System.Text;

namespace GoblinXNA.UI.Events
{
    /// <summary>
    /// A semantic event that indicates that an item was selected or deselected.
    /// </summary>
    public class ItemEvent : Event
    {
        /// <summary>
        /// This state-change-value indicates that a selected item was selected
        /// </summary>
        public static int SELECTED = 0;
        /// <summary>
        /// This state-change-value indicates that a selected item was deselected
        /// </summary>
        public static int DESELECTED = 1;
        
        /// <summary>
        /// Item affected by the event
        /// </summary>
        protected Object item;
        /// <summary>
        /// Type of state change: either SELECTED or DESELECTED
        /// </summary>
        protected int stateChange;

        public ItemEvent(Object src, Object item, int stateChange)
            : base(src)
        {
            this.item = item;
            this.stateChange = stateChange;
        }

        /// <summary>
        /// Get the item affected by the event
        /// </summary>
        /// <returns>The item affected by the event</returns>
        public virtual Object GetItem()
        {
            return item;
        }
        /// <summary>
        /// Get the type of state change (selected or deselected)
        /// </summary>
        /// <returns>Type of state change: either SELECTED or DESELECTED</returns>
        public virtual int GetStateChange()
        {
            return stateChange;
        }
    }
}
