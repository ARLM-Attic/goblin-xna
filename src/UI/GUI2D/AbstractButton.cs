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

using Microsoft.Xna.Framework.Graphics;

using GoblinXNA.UI.Events;

namespace GoblinXNA.UI.UI2D
{
    /// <summary>
    /// An abstract class that defines a button. 
    /// </summary>
    /// <remarks>
    /// Any GoblinXNA 2D GUI button components should extend this class.
    /// </remarks>
    public class AbstractButton : G2DComponent
    {
        #region Member Fields
        /// <summary>
        /// A list of action listeners
        /// </summary>
        protected List<ActionListener> actionListeners;
        /// <summary>
        /// Color used to highlight the inner border when the mouse is over it
        /// </summary>
        protected Color highlightColor;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an abstract button with the specified label.
        /// </summary>
        /// <param name="label"></param>
        protected AbstractButton(String label) 
            : base()
        {
            actionListeners = new List<ActionListener>();
            Text = label;
            highlightColor = Color.Yellow;
        }
        /// <summary>
        /// Creates an abstract button with no text.
        /// </summary>
        protected AbstractButton() : this("") { }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the color used for highlighting the inner border when the mouse is over it.
        /// </summary>
        public virtual Color HighlightColor
        {
            get { return highlightColor; }
            set
            {
                highlightColor = new Color(value.R, value.G, value.B, (byte)(alpha * 255));
            }
        }
        /// <summary>
        /// Gets a list of action listeners added to this button
        /// </summary>
        public virtual List<ActionListener> ActionListeners
        {
            get { return actionListeners; }
        }
        #endregion

        #region Override Properties
        public override float Transparency
        {
            get
            {
                return base.Transparency;
            }
            set
            {
                base.Transparency = value;

                highlightColor = new Color(highlightColor.R, highlightColor.G, highlightColor.B, alpha);
            }
        }
        #endregion

        #region Event Listener Methods
        /// <summary>
        /// Add a listener for action events (actionPerformed)
        /// </summary>
        /// <param name="listener"></param>
        public virtual void AddActionListener(ActionListener listener)
        {
            if(listener != null && !actionListeners.Contains(listener))
                actionListeners.Add(listener);
        }
        /// <summary>
        /// Remove a specific action listener if already added
        /// </summary>
        /// <param name="listener"></param>
        public virtual void RemoveActionListener(ActionListener listener)
        {
            actionListeners.Remove(listener);
        }
        
        #endregion

        #region Action Methods
        /// <summary>
        /// Programmatically click the button
        /// </summary>
        public virtual void DoClick() {
            foreach (ActionListener listener in actionListeners)
            {
                ActionEvent evt = new ActionEvent(this);
                listener.ActionPerformed(evt);
            }
        }
        #endregion
    }
}
