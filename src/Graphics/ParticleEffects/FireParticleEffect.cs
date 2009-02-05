/************************************************************************************ 
 *
 * Microsoft XNA Community Game Platform
 * Copyright (C) Microsoft Corporation. All rights reserved.
 * 
 * ===================================================================================
 * Modified by: Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GoblinXNA.Graphics.ParticleEffects
{
    /// <summary>
    /// A particle system that simulates a fire effect.
    /// </summary>
    public class FireParticleEffect : ParticleEffect
    {
        #region Constructors
        /// <summary>
        /// Creates a fire particle system.
        /// </summary>
        public FireParticleEffect()
            : base()
        {
        }
        #endregion

        #region Override Methods
        protected override void Initialize()
        {
            base.Initialize();

            textureName = "fire";

            maxParticles = 2400;
            particles = new ParticleVertex[maxParticles];

            duration = TimeSpan.FromSeconds(2);
            durationRandomness = 1;

            minHorizontalVelocity = 0;
            maxHorizontalVelocity = 15;

            minVerticalVelocity = -10;
            maxVerticalVelocity = 10;

            // Set gravity upside down, so the flames will 'fall' upward.
            gravity = new Vector3(0, 15, 0);

            minColor = new Color(0, 0, 0, 10);
            maxColor = new Color(255, 255, 255, 40);

            minStartSize = 5;
            maxStartSize = 10;

            minEndSize = 10;
            maxEndSize = 40;

            sourceBlend = Blend.SourceAlpha;
            destinationBlend = Blend.One;
        }
        #endregion
    }
}
