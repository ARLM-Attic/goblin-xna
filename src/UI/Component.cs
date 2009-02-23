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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GoblinXNA.UI
{
    /// <summary>
    /// An abstract UI component. This class cannot be instantiated.
    /// </summary>
    abstract public class Component
    {
        #region Member Fields
        /// <summary>
        /// Default color of the background
        /// </summary>
        public static Color DEFAULT_COLOR = Color.LightGray;
        /// <summary>
        /// Default transparency value for any colors associated with this component
        /// </summary>
        public static float DEFAULT_ALPHA = 1.0f;

        /// <summary>
        /// Parent component of this component for scene-graph-based drawing
        /// </summary>
        protected Component parent;
        /// <summary>
        /// Background color of this component if enabled
        /// </summary>
        protected Color backgroundColor;
        /// <summary>
        /// Background color of this component if not enabled
        /// </summary>
        protected Color disabledColor;
        /// <summary>
        /// Border color of this component's background
        /// </summary>
        protected Color borderColor;
        /// <summary>
        /// Indicator of whether to paint the border
        /// </summary>
        protected bool drawBorder;
        /// <summary>
        /// Indicator of whether to paint the background
        /// </summary>
        protected bool drawBackground;
        /// <summary>
        /// Transparency value in the range [0 -- 255]
        /// </summary>
        protected byte alpha;
        /// <summary>
        /// Indicator of whether this component is visible
        /// </summary>
        protected bool visible;
        /// <summary>
        /// Indicator of whether this component is enabled
        /// </summary>
        protected bool enabled;
        /// <summary>
        /// Indicator of whether this component is focused . 
        /// NOTE: This variable is useful only for indicating that the component
        /// should receive key input
        /// </summary>
        protected bool focused;
        /// <summary>
        /// Name of this component. 
        /// NOTE: Mostly used only for debugging (See ToString() method)
        /// </summary>
        protected String name;
        /// <summary>
        /// Label/Text associated with this component
        /// </summary>
        protected String label;
        /// <summary>
        /// Color of the texture (it's always Color.White, but it contains alpha info as well)
        /// </summary>
        protected Color textureColor;

        /// <summary>
        /// Indicator of how label/text should be aligned horizontally
        /// </summary>
        protected GoblinEnums.HorizontalAlignment horizontalAlignment;
        /// <summary>
        /// Indicator of how label/text should be aligned vertically
        /// </summary>
        protected GoblinEnums.VerticalAlignment verticalAlignment;

        /// <summary>
        /// Label/Text color
        /// </summary>
        protected Color textColor;
        /// <summary>
        /// Transparency value of the label/text
        /// </summary>
        protected byte textAlpha;
        /// <summary>
        /// Indicator of whether a key is held down
        /// </summary>
        protected bool keyDown;

        #region Event Listeners Fields
        /// <summary>
        /// A list of container listeners
        /// </summary>
        protected List<ContainerListener> containerListeners;
        /// <summary>
        /// A list of focus listeners
        /// </summary>
        protected List<FocusListener> focusListeners;
        
        #endregion

        /// <summary>
        /// Indicator of whether any mouse button is held down. 
        /// NOTE: Mainly used for detecting mouse dragging event
        /// </summary>
        protected bool mouseDown;
        /// <summary>
        /// Indicator of whether the mouse pointer is hovering on this component
        /// </summary>
        protected bool within;
        /// <summary>
        /// Indicator of whether the mouse has entered the bound of this component. 
        /// NOTE: Mainly used for detecting mouse enter and exit event
        /// </summary>
        protected bool entered;

        /// <summary>
        /// An XNA class used for loading a texture for the background image
        /// </summary>
        protected Texture2D backTexture;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a UI component with the specified background color and transparency value.
        /// </summary>
        /// <param name="bgColor">Background color of this component</param>
        /// <param name="alpha">Transparency value of this component [0.0f - 1.0f]. 1.0f meaning
        /// totally opaque, and 0.0f meaning totally transparent</param>
        public Component(Color bgColor, float alpha)
        {
            this.alpha = (byte)(alpha * 255);
            this.backgroundColor= new Color(bgColor.R, bgColor.G, bgColor.B, this.alpha);

            containerListeners = new List<ContainerListener>();
            focusListeners = new List<FocusListener>();
         
            name = "Component";
            visible = true;
            enabled = true;
            parent = null;
            drawBorder = true;
            drawBackground = true;
            focused = false;
            borderColor = Color.Black;
            disabledColor = Color.Gray;
            textureColor = Color.White;

            label = "";
            horizontalAlignment = GoblinEnums.HorizontalAlignment.Left;
            verticalAlignment = GoblinEnums.VerticalAlignment.Top;

            textColor = Color.Black;
            textAlpha = (byte)255;
            keyDown = false;

            mouseDown = false;
            within = false;
            entered = false;
        }

        /// <summary>
        /// Creates a UI component with the specified background color and transparency of 1.0f.
        /// </summary>
        /// <param name="bgColor">Background color of this component</param>
        public Component(Color bgColor) :
            this(bgColor, DEFAULT_ALPHA) { }

        /// <summary>
        /// Creates a UI component with a light gray background color and transparency of 1.0f.
        /// </summary>
        public Component() :
            this(DEFAULT_COLOR, DEFAULT_ALPHA){ }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the background color of this component.
        /// </summary>
        public virtual Color BackgroundColor
        {
            get { return backgroundColor; }
            set
            {
                this.backgroundColor = new Color(value.R, value.G, value.B, alpha);
            }
        }

        /// <summary>
        /// Gets or sets the border color of the background of this component.
        /// </summary>
        public virtual Color BorderColor
        {
            get { return borderColor; }
            set
            {
                borderColor = new Color(value.R, value.G, value.B, alpha);
            }
        }

        /// <summary>
        /// Gets or sets the background color of this component when disabled.
        /// </summary>
        public virtual Color DisabledColor
        {
            get { return disabledColor; }
            set
            {
                disabledColor = new Color(value.R, value.G, value.B, alpha);
            }
        }

        /// <summary>
        /// Gets or sets the transparency value of this component.
        /// </summary>
        /// <remarks>
        /// A transparency value in the range [0.0f -- 1.0f]. 1.0f meaning
        /// totally opaque, and 0.0f meaning totally transparent
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Throws ArgumentException if alpha value is outside of the range [0.0f -- 1.0f]
        /// </exception>
        public virtual float Transparency
        {
            get { return alpha / 255f; }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentException("Invalid alpha value: " + value +
                        " Possible values: 0.0 - 1.0");

                alpha = (byte)(value * 255);

                backgroundColor = new Color(backgroundColor.R, backgroundColor.G, backgroundColor.B, alpha);
                borderColor = new Color(borderColor.R, borderColor.G, borderColor.B, alpha);
                disabledColor = new Color(disabledColor.R, disabledColor.G, disabledColor.B, alpha);
                textureColor = new Color(255, 255, 255, alpha);
            }
        }

        /// <summary>
        /// Gets or sets whether this component is visible.
        /// </summary>
        public virtual bool Visible
        {
            get { return visible; }
            set { visible = value; }
        }

        /// <summary>
        /// Gets or sets whether this component is enabled.
        /// </summary>
        public virtual bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        /// <summary>
        /// Gets whether this component is focused. 
        /// </summary>
        /// <remarks>
        /// Focused variable is mainly used for determining that the component
        /// should receive key input, since key input should be received by only one
        /// component at a time
        /// </remarks>
        public virtual bool Focused
        {
            get { return focused; }
            internal set
            {
                focused = value;

                foreach (FocusListener listener in focusListeners)
                {
                    FocusEvent evt = new FocusEvent(this);
                    if (focused)
                        listener.FocusGained(evt);
                    else
                        listener.FocusLost(evt);
                }
            }
        }

        /// <summary>
        /// Get or sets the background image with the given image texture.
        /// </summary>
        /// <remarks>
        /// This will automatically disable the border drawing, and enable the background drawing.
        /// </remarks>
        public virtual Texture2D Texture
        {
            get { return backTexture; }
            set 
            { 
                backTexture = value;
                drawBorder = false;
                drawBackground = true;
            }
        }

        /// <summary>
        /// Gets or sets whether the border should be painted.
        /// </summary>
        public virtual bool DrawBorder
        {
            get { return drawBorder; }
            set { drawBorder = value; }
        }

        /// <summary>
        /// Gets or sets whether the background should be painted.
        /// </summary>
        public virtual bool DrawBackground
        {
            get { return drawBackground; }
            set { drawBackground = value; }
        }

        /// <summary>
        /// Gets or sets the parent of this component.
        /// </summary>
        public virtual Component Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        /// <summary>
        /// Gets the root ancestor of this component.
        /// </summary>
        internal virtual Component RootParent
        {
            get
            {
                if (HasParent)
                    return parent.RootParent;
                else
                    return this;
            }
        }

        /// <summary>
        /// Gets whether this component has a parent.
        /// </summary>
        public virtual bool HasParent
        {
            get { return (parent != null); }
        }

        /// <summary>
        /// Gets or sets the name of this component. 
        /// </summary>
        /// <remarks>
        /// Name information is mainly used for debugging purpose only.
        /// </remarks>
        public virtual String Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Gets or sets the label/text associated with this component.
        /// </summary>
        public virtual String Text
        {
            get { return label; }
            set { label = value; }
        }

        /// <summary>
        /// Gets or sets the color of the label/text.
        /// </summary>
        public virtual Color TextColor
        {
            get { return textColor; }
            set
            {
                textColor = new Color(value.R, value.G, value.B, textAlpha);
            }
        }

        /// <summary>
        /// Gets or sets the transparency of the label/text.
        /// </summary>
        /// <remarks>
        /// A transparency value in the range [0.0f -- 1.0f]
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Throws ArgumentException if alpha value is outside of the range [0.0f -- 1.0f].
        /// </exception>
        public virtual float TextTransparency
        {
            get { return textAlpha / 255f; }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentException("Invalid alpha value: " + value +
                        " Possible values: 0.0 - 1.0");

                this.textAlpha = (byte)(value * 255);
                textColor = new Color(textColor.R, textColor.G, textColor.B, textAlpha);
            }
        }

        /// <summary>
        /// Gets or sets how label/text should be aligned horizontally.
        /// </summary>
        public virtual GoblinEnums.HorizontalAlignment HorizontalAlignment
        {
            get { return horizontalAlignment; }
            set { horizontalAlignment = value; }
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the label/text.
        /// </summary>
        public virtual GoblinEnums.VerticalAlignment VerticalAlignment
        {
            get { return verticalAlignment; }
            set { verticalAlignment = value; }
        }

        /// <summary>
        /// Gets a list of container listeners added to this component.
        /// </summary>
        public virtual List<ContainerListener> ContainerListeners
        {
            get { return containerListeners; }
        }

        /// <summary>
        /// Gets a list of focus listeners added to this component.
        /// </summary>
        public virtual List<FocusListener> FocusListeners
        {
            get { return focusListeners; }
        }
        #endregion

        #region Event Listener Methods
        /// <summary>
        /// Add a listener for container events (componentAdded, componentRemoved).
        /// </summary>
        /// <param name="listener"></param>
        public virtual void AddContainerListener(ContainerListener listener)
        {
            if (listener != null && !containerListeners.Contains(listener))
                containerListeners.Add(listener);
        }

        /// <summary>
        /// Add a listener for focus events (focusGained, focusLost).
        /// </summary>
        /// <param name="listener"></param>
        public virtual void AddFocusListener(FocusListener listener)
        {
            if (listener != null && !focusListeners.Contains(listener))
                focusListeners.Add(listener);
        }
        
        /// <summary>
        /// Remove a specific container listener if already added.
        /// </summary>
        /// <param name="listener"></param>
        public virtual void RemoveContainerListener(ContainerListener listener)
        {
            containerListeners.Remove(listener);
        }

        /// <summary>
        /// Remove a specific focus listener if already added.
        /// </summary>
        /// <param name="listener"></param>
        public virtual void RemoveFocusListener(FocusListener listener)
        {
            focusListeners.Remove(listener);
        }
        #endregion
    }
}
