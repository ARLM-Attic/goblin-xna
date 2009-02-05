/************************************************************************************ 
 *
 * Microsoft XNA Community Game Platform
 * Copyright (C) Microsoft Corporation. All rights reserved.
 * 
 * ===================================================================================
 * Modified by: Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace GoblinXNA.Graphics.ParticleEffects
{
    /// <summary>
    /// Custom vertex structure for drawing point sprite particles.
    /// </summary>
    public struct ParticleVertex
    {
        /// <summary>
        /// Stores the starting position of the particle.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Stores the starting velocity of the particle.
        /// </summary>
        public Vector3 Velocity;

        /// <summary>
        /// Four random values, used to make each particle look slightly different.
        /// </summary>
        public Color Random;

        /// <summary>
        /// The time (in seconds) at that this particle was created.
        /// </summary>
        public float Time;


        /// <summary>
        /// Describe the layout of this vertex structure.
        /// </summary>
        public static readonly VertexElement[] VertexElements =
        {
            new VertexElement(0, 0, VertexElementFormat.Vector3,
                                    VertexElementMethod.Default,
                                    VertexElementUsage.Position, 0),

            new VertexElement(0, 12, VertexElementFormat.Vector3,
                                     VertexElementMethod.Default,
                                     VertexElementUsage.Normal, 0),

            new VertexElement(0, 24, VertexElementFormat.Color,
                                     VertexElementMethod.Default,
                                     VertexElementUsage.Color, 0),

            new VertexElement(0, 28, VertexElementFormat.Single,
                                     VertexElementMethod.Default,
                                     VertexElementUsage.TextureCoordinate, 0),
        };


        /// <summary>
        /// Describe the size of this vertex structure.
        /// </summary>
        public const int SizeInBytes = 32;
    }
}

