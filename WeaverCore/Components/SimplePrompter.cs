using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using WeaverCore;
using WeaverCore.Assets.Components;

namespace WeaverCore.Components
{
    public class SimplePrompter : MonoBehaviour
    {
        [SerializeField]
        WeaverArrowPrompt prompt;

        public UnityEvent OnPromptActivated;

        bool playerInRange = false;

        public bool PromptEnabled
        {
            get => prompt.gameObject.activeSelf;
            set => prompt.gameObject.SetActive(true);
        }

        void Awake()
        {
            StartCoroutine(StartRoutine());
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.name == "HeroBox" || collision.GetComponent<HeroController>() != null)
            {
                playerInRange = true;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.name == "HeroBox" || collision.GetComponent<HeroController>() != null)
            {
                playerInRange = false;
            }
        }

        IEnumerator StartRoutine()
        {
            prompt.HideInstant();
            while(true)
            {
                yield return new WaitUntil(() => playerInRange && PromptEnabled);
                //while (!playerInRange || !PromptEnabled)
                 //   yield return null;
                prompt.Show();
                while (playerInRange && PromptEnabled)
                {
                    if (HeroController.instance.CanTalk() && (PlayerInput.up.WasPressed || PlayerInput.down.WasPressed))
                    {
                        prompt.Hide();
                        OnPromptActivated?.Invoke();
                        PromptEnabled = false;
                        yield break;
                    }
                    yield return null;
                }
                prompt.Hide();
            }
        }
    }
}

