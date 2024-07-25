﻿using System;
using UnityEngine;
using WeaverCore.Utilities;

namespace WeaverCore.Components
{
    /// <summary>
    /// Manages rock particle effects.
    /// </summary>
    public class RockParticles : MonoBehaviour
    {
        /// <summary>
        /// The prefab instance of RockParticles.
        /// </summary>
        static CachedPrefab<RockParticles> _prefab = new CachedPrefab<RockParticles>();

        /// <summary>
        /// Gets the prefab instance of RockParticles.
        /// </summary>
        public static RockParticles Prefab
        {
            get
            {
                if (_prefab.Value == null)
                {
                    _prefab.Value = WeaverAssets.LoadWeaverAsset<GameObject>("Rock Particles").GetComponent<RockParticles>();
                }
                return _prefab.Value;
            }
        }

        [NonSerialized]
        ParticleSystem _particles;

        /// <summary>
        /// Gets the ParticleSystem component.
        /// </summary>
        public ParticleSystem Particles
        {
            get
            {
                if (_particles == null)
                {
                    _particles = GetComponent<ParticleSystem>();
                }
                return _particles;
            }
        }

        /// <summary>
        /// Spawns directional rock particles.
        /// </summary>
        /// <param name="position">The position to spawn the particles.</param>
        /// <param name="rotation">The rotation of the particles.</param>
        /// <param name="intensity">The intensity of the particles.</param>
        /// <param name="duration">The duration of the particle effect.</param>
        /// <param name="particleSize">The size of the particles.</param>
        /// <param name="playImmediately">Whether to play the particles immediately.</param>
        /// <returns>The instance of RockParticles.</returns>
        public static RockParticles SpawnDirectional(Vector3 position, Quaternion rotation, float intensity = 100, float duration = 0.1f, float particleSize = 1f, bool playImmediately = true, RockParticles prefab = null)
        {
            return Spawn(position, rotation, 0f, intensity, duration, particleSize, playImmediately, prefab);
        }

        /// <summary>
        /// Spawns non-directional rock particles.
        /// </summary>
        /// <param name="position">The position to spawn the particles.</param>
        /// <param name="intensity">The intensity of the particles.</param>
        /// <param name="duration">The duration of the particle effect.</param>
        /// <param name="particleSize">The size of the particles.</param>
        /// <param name="playImmediately">Whether to play the particles immediately.</param>
        /// <returns>The instance of RockParticles.</returns>
        public static RockParticles SpawnNonDirectional(Vector3 position, float intensity = 100, float duration = 0.1f, float particleSize = 1f, bool playImmediately = true, RockParticles prefab = null)
        {
            return Spawn(position, Quaternion.identity, 1f, intensity, duration, particleSize, playImmediately, prefab);
        }

        /// <summary>
        /// Spawns rock particles.
        /// </summary>
        /// <param name="position">The position to spawn the particles.</param>
        /// <param name="rotation">The rotation of the particles.</param>
        /// <param name="directionRandomness">The randomness of the particle direction.</param>
        /// <param name="intensity">The intensity of the particles.</param>
        /// <param name="duration">The duration of the particle effect.</param>
        /// <param name="particleSize">The size of the particles.</param>
        /// <param name="playImmediately">Whether to play the particles immediately.</param>
        /// <returns>The instance of RockParticles.</returns>
        public static RockParticles Spawn(Vector3 position, Quaternion rotation, float directionRandomness, float intensity = 100, float duration = 0.1f, float particleSize = 1f, bool playImmediately = true, RockParticles prefab = null)
        {
            if (prefab == null)
            {
                prefab = Prefab;
            }
            var instance = Pooling.Instantiate(prefab, position, rotation);

            var emit = instance.Particles.emission;
            var main = instance.Particles.main;
            var shape = instance.Particles.shape;
            var size = instance.Particles.sizeOverLifetime;

            emit.rateOverTimeMultiplier = intensity;
            size.sizeMultiplier = particleSize;
            shape.randomDirectionAmount = directionRandomness;
            main.duration = duration;

            if (playImmediately)
            {
                instance.Particles.Play();
            }

            return instance;
        }
    }
}
