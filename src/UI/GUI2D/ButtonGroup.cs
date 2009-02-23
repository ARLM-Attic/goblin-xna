/************************************************************************************ 
 * Copyright (c) 2008-2009, Columbia University
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

using GoblinXNA.UI.Events;

namespace GoblinXNA.UI.UI2D
{
    /// <summary>
    /// This class is used to create a multiple-exclusion scope for a set of buttons. Creating a
    /// set of buttons with the same ButtonGroup object means that turning "on" one of those buttons
    /// turns "off" all other buttons in the group. Typically, a button group contains G2DRadioButton.
    /// 
    /// Initially, all buttons in the group are unselected. Once any button is selected, one button is
    /// always selected in the group.
    /// </summary>
    public class ButtonGroup
    {
        #region Member Fields
        protected List<ToggleButton> group;
        protected GroupManager manager;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a button group.
        /// </summary>
        public ButtonGroup() 
        {
            group = new List<ToggleButton>();
            manager = new GroupManager(group);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets a list of buttons in this group.
        /// </summary>
        public virtual List<ToggleButton> Buttons
        {
            get { return group; }
        }
        #endregion

        #region Action Methods
        /// <summary>
        /// Adds a two-state button to this group.
        /// </summary>
        /// <param name="button">The button to be added</param>
        public virtual void Add(ToggleButton button)
        {
            if (button != null && !group.Contains(button))
            {
                group.Add(button);
                button.AddActionListener(manager);
            }
        }

        /// <summary>
        /// Removes a two-state button from this group.
        /// </summary>
        /// <param name="button">The button to be removed</param>
        /// <returns></returns>
        public virtual bool Remove(ToggleButton button)
        {
            bool removed = group.Remove(button);
            if (removed)
                button.RemoveActionListener(manager);

            return removed;
        }
        #endregion

        #region Inner Classes
        /// <summary>
        /// A manager class that manages the "on"/-"off" state of all of the added buttons.
        /// </summary>
        protected class GroupManager : ActionListener
        {
            private List<ToggleButton> group;

            /// <summary>
            /// Creates a group manager.
            /// </summary>
            /// <param name="group"></param>
            public GroupManager(List<ToggleButton> group)
            {
                this.group = group;
            }

            public void ActionPerformed(ActionEvent evt)
            {
                ToggleButton src = (ToggleButton) evt.Source;

                // If the radio button is already selected, then perform no action
                if (!src.Selected)
                {
                    src.Selected = true;
                    return;
                }

                foreach (ToggleButton button in group)
                {
                    if (!button.Equals(src))
                        button.Selected = false;
                }
            }
        }
        #endregion
    }
}
