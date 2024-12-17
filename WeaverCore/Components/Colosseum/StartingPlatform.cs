using System;
using System.Collections;
using UnityEngine;
using WeaverCore.Components;

namespace WeaverCore.Components.Colosseum
{
    public class StartingPlatform : MonoBehaviour
    {
        [SerializeField, Tooltip("Duration for fading the platform to white.")]
        float fadeToWhiteTime = 1.25f;

        [SerializeField, Tooltip("Delay between different phases of fading.")]
        float interDelay = 0.5f;

        [SerializeField, Tooltip("Duration for fading the platform to blank.")]
        float fadeToBlankTime = 1.25f;

        [SerializeField, Tooltip("Particle system used during the fade effect.")]
        ParticleSystem fadeParticles;


        Coroutine _fadeRoutine;

        Color baseColor;

        [NonSerialized]
        SpriteRenderer _renderer;
        public SpriteRenderer Renderer => _renderer ??= GetComponent<SpriteRenderer>();

        [NonSerialized]
        SpriteFlasher _flasher;
        public SpriteFlasher Flasher => _flasher ??= GetComponent<SpriteFlasher>();

        private void Awake() 
        {
            baseColor = Renderer.color;
            //StartCoroutine(Test());
        }

        IEnumerator Test()
        {
            while (true)
            {
                FadeOut(0);
                yield return new WaitForSeconds(fadeToWhiteTime + interDelay + fadeToBlankTime);
                yield return new WaitForSeconds(interDelay);

                FadeIn(0);
                yield return new WaitForSeconds(fadeToWhiteTime + interDelay + fadeToBlankTime);
                yield return new WaitForSeconds(interDelay);
            }
        }

        public void FadeOut(float delay)
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            _fadeRoutine = StartCoroutine(FadeRoutine(false, delay));
        }

        public void FadeIn(float delay)
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            _fadeRoutine = StartCoroutine(FadeRoutine(true, delay));
        }

        IEnumerator FadeRoutine(bool fadeIn, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            if (!fadeIn)
            {
                if (fadeParticles != null)
                {
                    fadeParticles.Play();
                }
                Flasher.FlashColor = Color.white;
                for (float t = 0; t < fadeToWhiteTime; t += Time.deltaTime)
                {
                    Flasher.FlashIntensity = t / fadeToWhiteTime;
                    yield return null;
                }

                Flasher.FlashIntensity = 1f;

                yield return new WaitForSeconds(interDelay / 2f);

                foreach (var c in GetComponentsInChildren<Collider2D>())
                {
                    c.enabled = false;
                }

                if (fadeParticles != null)
                {
                    fadeParticles.Stop();
                }

                yield return new WaitForSeconds(interDelay / 2f);

                //var destColor = new Color(1,1,1,0f);
                //var oldColor = Renderer.color;
                for (float t = 0; t < fadeToBlankTime; t += Time.deltaTime)
                {
                    Flasher.FlashColor = Color.Lerp(Color.white, new Color(1,1,1,0), t / fadeToBlankTime);
                    //Renderer.color = Color.Lerp(oldColor, destColor, t / fadeToBlankTime);
                    //Flasher.FlashIntensity = 1 - (t / fadeToBlankTime);
                    yield return null;
                }

                Flasher.FlashIntensity = 0f;
                Flasher.FlashColor = Color.white;
                Renderer.color = default;
            }
            else
            {
                if (fadeParticles != null)
                {
                    fadeParticles.Play();
                }
                var destColor = baseColor;
                //var oldColor = new Color(1,1,1,0f);
                Flasher.FlashColor = Color.white;
                Flasher.FlashIntensity = 1f;
                for (float t = 0; t < fadeToBlankTime; t += Time.deltaTime)
                {
                    Renderer.color = Color.Lerp(default, destColor, t / fadeToBlankTime);
                    yield return null;
                }
                Renderer.color = destColor;
                Flasher.FlashIntensity = 1f;

                yield return new WaitForSeconds(interDelay / 2f);

                foreach (var c in GetComponentsInChildren<Collider2D>())
                {
                    c.enabled = true;
                }
                if (fadeParticles != null)
                {
                    fadeParticles.Stop();
                }

                yield return new WaitForSeconds(interDelay / 2f);

                for (float t = 0; t < fadeToWhiteTime; t += Time.deltaTime)
                {
                    Flasher.FlashIntensity = 1f - (t / fadeToWhiteTime);
                    yield return null;
                }
                
                Flasher.FlashIntensity = 0f;
            }

            _fadeRoutine = null;
        }
    }
}
