#region File Description
//-----------------------------------------------------------------------------
// LineManager3D.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using directives
using System;
using System.Collections;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System.Collections.Generic;

using GoblinXNA.Shaders;

#endregion

namespace GoblinXNA.Graphics
{
    /// <summary>
    /// Helper class for game for rendering lines.
    /// This class will collect all line calls, then build a new vertex buffer
    /// if any line has changed or the line number changed and finally will
    /// render all lines in the vertex buffer at the end of the frame (so this
    /// class is obviously only for 2D lines directly on screen, no z buffer
    /// and no stuff will be in front of the lines, because everything is
    /// rendered at the end of the frame).
    /// </summary>
    public class LineManager3D : IDisposable
    {
        #region Line struct
        /// <summary>
        /// Struct for a line, instances of this class will be added to lines.
        /// </summary>
        public struct Line
        {
            // Positions
            public Vector3 startPoint, endPoint;
            // Colors
            public Color startColor, endColor;

            /// <summary>
            /// Constructor
            /// </summary>
            public Line(
                Vector3 setStartPoint, Color setStartColor,
                Vector3 setEndPoint, Color setEndColor)
            {
                startPoint = setStartPoint;
                startColor = setStartColor;
                endPoint = setEndPoint;
                endColor = setEndColor;
            }

            public Line(
                Vector3 setStartPoint, Vector3 setEndPoint,
                Color color)
                : this(setStartPoint, color, setEndPoint, color)
            { }

            /// <summary>
            /// Are these two Lines equal?
            /// </summary>
            public static bool operator ==(Line a, Line b)
            {
                return
                    a.startPoint == b.startPoint &&
                    a.endPoint == b.endPoint &&
                    a.startColor == b.startColor &&
                    a.endColor == b.endColor;
            }

            /// <summary>
            /// Are these two Lines not equal?
            /// </summary>
            public static bool operator !=(Line a, Line b)
            {
                return
                    a.startPoint != b.startPoint ||
                    a.endPoint != b.endPoint ||
                    a.startColor != b.startColor ||
                    a.endColor != b.endColor;
            }

            /// <summary>
            /// Support Equals(.) to keep the compiler happy
            /// (because we used == and !=)
            /// </summary>
            public override bool Equals(object a)
            {
                if (a is Line)
                    return (Line)a == this;

                return false;
            }

            /// <summary>
            /// Support GetHashCode() to keep the compiler happy
            /// (because we used == and !=)
            /// </summary>
            public override int GetHashCode()
            {
                return 0; // Not supported or nessescary
            }
        }
        #endregion

        #region Variables
        /// <summary>
        /// Number of lines used this frame, will be set to 0 when rendering.
        /// </summary>
        private int numOfLines = 0;

        /// <summary>
        /// The actual list for all the lines, it will NOT be reseted each
        /// frame like numOfLines! We will remember the last lines and
        /// only change this list when anything changes (new line, old
        /// line missing, changing line data).
        /// When this happens buildVertexBuffer will be set to true.
        /// </summary>
        private List<Line> lines = new List<Line>();

        /// <summary>
        /// Build vertex buffer this frame because the line list was changed?
        /// </summary>
        private bool buildVertexBuffer = false;

        /// <summary>
        /// Vertex buffer for all lines
        /// </summary>
        VertexPositionColor[] lineVertices =
            new VertexPositionColor[MaxNumOfLines * 2];

        /// <summary>
        /// Real number of primitives currently used.
        /// </summary>
        private int numOfPrimitives = 0;

        /// <summary>
        /// Max. number of lines allowed to prevent to big buffer, will never
        /// be reached, but in case something goes wrong or numOfLines is not
        /// reseted each frame, we won't add unlimited lines (all new lines
        /// will be ignored if this max. number is reached).
        /// </summary>
        protected const int MaxNumOfLines =
            4096 * 20;

        /// <summary>
        /// Vertex declaration for our lines.
        /// </summary>
        VertexDeclaration decl = null;

        IShader lineShader;
        string shaderTechnique;

        #endregion

        #region Initialization
        /// <summary>
        /// Init LineManager
        /// </summary>
        public LineManager3D()
        {
            if (State.Device == null)
                throw new ArgumentNullException(
                    "XNA device is not initialized, can't init line manager.");

            decl = new VertexDeclaration(
                State.Device, VertexPositionColor.VertexElements);
        }
        #endregion

        #region Properties

        public IShader LineShader
        {
            get { return lineShader; }
            set { lineShader = value; }
        }

        public String ShaderTechnique
        {
            get { return shaderTechnique; }
            set { shaderTechnique = value; }
        }

        #endregion

        #region Dispose
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                decl.Dispose();
            }
        }
        #endregion

        #region AddLine
        public void AddLine(Line line)
        {
            // Don't add new lines if limit is reached
            if (numOfLines >= MaxNumOfLines)
            {
                /*ignore
                Log.Write("Too many lines requested in LineManager3D. " +
                    "Max lines = " + MaxNumOfLines);
                 */
                return;
            }

            // Check if this exact line exists at the current lines position.
            if (lines.Count > numOfLines)
            {
                if ((Line)lines[numOfLines] != line)
                {
                    // overwrite old line, otherwise just increase numOfLines
                    lines[numOfLines] = line;
                    // Remember to build vertex buffer in Render()
                    buildVertexBuffer = true;
                }
            }
            else
            {
                // Then just add new line
                lines.Add(line);
                // Remember to build vertex buffer in Render()
                buildVertexBuffer = true;
            }

            // nextUpValue line
            numOfLines++;
        }

        /// <summary>
        /// Add line
        /// </summary>
        public void AddLine(
            Vector3 startPoint, Color startColor,
            Vector3 endPoint, Color endColor)
        {
            // Build line
            Line line = new Line(startPoint, startColor, endPoint, endColor);

            AddLine(line);
        }

        /// <summary>
        /// Add line (only 1 color for start and end version)
        /// </summary>
        public void AddLine(Vector3 startPoint, Vector3 endPoint,
            Color color)
        {
            AddLine(startPoint, color, endPoint, color);
        }
        #endregion

        #region Update vertex buffer
        protected void UpdateVertexBuffer()
        {
            // Don't do anything if we got no lines.
            if (numOfLines == 0 ||
                // Or if some data is invalid
                lines.Count < numOfLines)
            {
                numOfPrimitives = 0;
                return;
            }

            // Set all lines
            for (int lineNum = 0; lineNum < numOfLines; lineNum++)
            {
                Line line = (Line)lines[lineNum];
                lineVertices[lineNum * 2 + 0] = new VertexPositionColor(
                    line.startPoint, line.startColor);
                lineVertices[lineNum * 2 + 1] = new VertexPositionColor(
                    line.endPoint, line.endColor);
            }
            numOfPrimitives = numOfLines;

            // Vertex buffer was build
            buildVertexBuffer = false;
        }
        #endregion

        #region Render
        /// <summary>
        /// Render all lines added this frame
        /// </summary>
        public void Render()
        {
            // Need to build vertex buffer?
            if (buildVertexBuffer ||
                numOfPrimitives != numOfLines)
            {
                UpdateVertexBuffer();
            }

            // Render lines if we got any lines to render
            if (numOfPrimitives > 0 && lineShader != null)
            {
                lineShader.Render(
                    Matrix.Identity,
                    shaderTechnique,
                    delegate
                    {
                        State.Device.VertexDeclaration = decl;
                        State.Device.DrawUserPrimitives<VertexPositionColor>(
                            PrimitiveType.LineList, lineVertices, 0, numOfPrimitives);
                    });
            }

            // Ok, finally reset numOfLines for next frame
            numOfLines = 0;
        }
        #endregion
    }
}
