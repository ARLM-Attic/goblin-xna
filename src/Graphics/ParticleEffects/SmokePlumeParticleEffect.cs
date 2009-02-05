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
    /// A particle system that simulates a smoke effect.
    /// </summary>
    public class SmokePlumeParticleEffect : ParticleEffect
    {
        /// <summary>
        /// Creates a smoke particle system.
        /// </summary>
        public SmokePlumeParticleEffect()
            : base()
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            textureName = "smoke";

            maxParticles = 600;
            particles = new ParticleVertex[maxParticles];

            duration = TimeSpan.FromSeconds(10);

            minHorizontalVelocity = 0;
            maxHorizontalVelocity = 15;

            minVerticalVelocity = 10;
            maxVerticalVelocity = 20;

            // Create a wind effect by tilting the gravity vector sideways.
            gravity = new Vector3(-20, -5, 0);

            endVelocity = 0.75f;

            minRotateSpeed = -1;
            maxRotateSpeed = 1;

            minStartSize = 5;
            maxStartSize = 10;

            minEndSize = 50;
            maxEndSize = 200;
        }
    }
}
