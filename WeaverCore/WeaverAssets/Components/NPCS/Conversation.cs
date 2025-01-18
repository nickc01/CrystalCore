using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using WeaverCore.Utilities;

namespace WeaverCore.Assets.Components
{
    /// <summary>
    /// The base class for all conversations with an NPC. You can setup a conversation by inheriting from this class, adding the component to an NPC object, and using <see cref="Speak(string)"/> and <see cref="PresentYesNoQuestion(string)"/> in <see cref="DoConversation"/> to talk with the player
    /// </summary>
    public abstract class Conversation : MonoBehaviour
    {
        /// <summary>
        /// The result of giving the player a yes/no question prompt
        /// </summary>
        public enum YesNoResult
        {
            /// <summary>
            /// Neither options were selected (shouldn't happen normally)
            /// </summary>
            Neither,
            /// <summary>
            /// The player selected "Yes"
            /// </summary>
            Yes,
            /// <summary>
            /// The player selected "No"
            /// </summary>
            No
        }

        bool talking = false;

        [field: SerializeField]
        public bool ShowBoxOnStart { get; set; } = true;

        /// <summary>
        /// The result of the yes/no dialog box
        /// </summary>
        public YesNoResult DialogBoxResult { get; private set; }

        EventManager eventReceiver;

        bool boxVisible = false;

        /// <summary>
        /// Override this method to customize the conversation with a player. Use <see cref="Speak(string)"/> to speak with the player, and use <see cref="PresentYesNoQuestion(string)"/> to prevent the player with a yes/no question
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerator DoConversation();

        /// <summary>
        /// Starts a conversation with the player
        /// </summary>
        public void StartConversation()
        {
            StartCoroutine(StartConversationRoutine());
        }

        public void StartConversation(bool makePlayerLookUp, Action onDone = null)
        {
            StartCoroutine(StartConversationRoutine(makePlayerLookUp, onDone));
        }

        private void EventReceiver_OnReceivedEvent(string eventName, GameObject source)
        {
            if (eventName == "YES")
            {
                DialogBoxResult = YesNoResult.Yes;
            }
            else if (eventName == "NO")
            {
                DialogBoxResult = YesNoResult.No;
            }
            else if (eventName == "CONVO_FINISH" && talking)
            {
                talking = false;
            }
        }

        /// <summary>
        /// Starts a conversation with the player
        /// </summary>
        /// <param name="makePlayerLookUp">Should the player look up when starting the convo?</param>
        public IEnumerator StartConversationRoutine(bool makePlayerLookUp = true, Action onDone = null)
        {
            if (eventReceiver == null)
            {
                if (!TryGetComponent(out eventReceiver))
                {
                    eventReceiver = gameObject.AddComponent<EventManager>();
                }
                eventReceiver.OnReceivedEvent += EventReceiver_OnReceivedEvent;
            }
            if (makePlayerLookUp)
            {
                HeroUtilities.PlayPlayerClip("LookUp");
            }

            /*var dialogManager = GameObject.Find("DialogueManager");
            if (dialogManager != null)
            {
                EventManager.SendEventToGameObject("BOX UP", dialogManager, gameObject);
            }*/
            boxVisible = false;
            if (ShowBoxOnStart)
            {
                ShowConversationBox();
            }
            yield return new WaitForSeconds(0.3f);
            yield return DoConversation();
            onDone?.Invoke();

        }

        /// <summary>
        /// Shows the conversation box
        /// </summary>
        public void ShowConversationBox()
        {
            if (!boxVisible)
            {
                boxVisible = true;
                var dialogManager = GameObject.Find("DialogueManager");
                if (dialogManager != null)
                {
                    EventManager.SendEventToGameObject("BOX UP", dialogManager, gameObject);
                }
            }
        }

        /// <summary>
        /// Hides the conversation box
        /// </summary>
        public void HideConversationBox()
        {
            if (boxVisible)
            {
                boxVisible = false;
                var dialogManager = GameObject.Find("DialogueManager");
                if (dialogManager != null)
                {
                    EventManager.SendEventToGameObject("BOX DOWN", dialogManager, gameObject);
                }
            }
        }



        /// <summary>
        /// Immediately reveals the text in the dialogue box, rather than having to wait for it to type out
        /// </summary>
        public void FinishTyping()
        {
            var dialogManager = GameObject.Find("DialogueManager");
            if (dialogManager != null)
            {
                var text = dialogManager.transform.Find("Text")?.gameObject;
                if (text != null)
                {
                    var dialogueBox = text.GetComponent("DialogueBox");

                    var textMesh = (TextMeshPro)dialogueBox.ReflectGetField("textMesh", BindingFlags.Instance | BindingFlags.NonPublic);

                    if (textMesh != null)
                    {
                        textMesh.maxVisibleCharacters = 99999;
                    }
                    /*talking = true;
                    EventManager.SendEventToGameObject("TALK START", gameObject, gameObject);
                    StartDialogBoxConversation(, message);
                    yield return new WaitUntil(() => !talking);
                    EventManager.SendEventToGameObject("TALK END", gameObject, gameObject);*/
                }
                /*else
                {
                    yield return new WaitForSeconds(0.5f);
                }*/
            }
        }

        protected IEnumerator SpeakImmediate(string message)
        {
            Coroutine speakImmediateCoroutine = null;
            IEnumerator SpeakImmediateRoutine(Func<bool> finished)
            {
                while (!finished())
                {
                    FinishTyping();
                    yield return new WaitForFixedUpdate();
                }
                speakImmediateCoroutine = null;
            }

            ShowConversationBox();
            var dialogManager = GameObject.Find("DialogueManager");
            if (dialogManager != null)
            {
                var text = dialogManager.transform.Find("Text")?.gameObject;
                if (text != null)
                {
                    talking = true;
                    EventManager.SendEventToGameObject("TALK START", gameObject, gameObject);
                    StartDialogBoxConversation(text.GetComponent("DialogueBox"), message);
                    speakImmediateCoroutine = StartCoroutine(SpeakImmediateRoutine(() => !talking));
                    yield return new WaitUntil(() => !talking);
                    EventManager.SendEventToGameObject("TALK END", gameObject, gameObject);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }

            if (speakImmediateCoroutine != null)
            {
                StopCoroutine(speakImmediateCoroutine);
                speakImmediateCoroutine = null;
            }
        }

        /// <summary>
        /// Speaks a message to the player
        /// </summary>
        /// <param name="message">The message to speak</param>
        protected IEnumerator Speak(string message)
        {
            ShowConversationBox();
            var dialogManager = GameObject.Find("DialogueManager");
            if (dialogManager != null)
            {
                var text = dialogManager.transform.Find("Text")?.gameObject;
                if (text != null)
                {
                    talking = true;
                    EventManager.SendEventToGameObject("TALK START", gameObject, gameObject);
                    StartDialogBoxConversation(text.GetComponent("DialogueBox"), message);
                    yield return new WaitUntil(() => !talking);
                    EventManager.SendEventToGameObject("TALK END", gameObject, gameObject);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        /// <summary>
        /// Speaks a series of messages to the player. Each message is split into seperate pages
        /// </summary>
        /// <param name="messages">The messages to speak</param>
        protected IEnumerator Speak(IEnumerable<string> messages)
        {
            /*string insert = "<page>";
            StringBuilder finalMessage = new StringBuilder();
            foreach (var msg in messages)
            {
                finalMessage.Append(msg);
                finalMessage.Append(insert);
            }
            finalMessage.Remove(finalMessage.Length - insert.Length, insert.Length);
            return Speak(finalMessage.ToString());*/
            return Speak(StringUtilities.Pagify(messages));
        }

        /// <summary>
        /// Speaks a series of messages to the player. Each message is split into seperate pages
        /// </summary>
        /// <param name="messages">The messages to speak</param>
        protected IEnumerator Speak(params string[] messages)
        {
            return Speak((IEnumerable<string>)messages);
        }

        /// <summary>
        /// Presents a yes/no question prompt to the player. The result is stored in <see cref="DialogBoxResult"/>
        /// </summary>
        /// <param name="message">The message to say in the prompt</param>
        protected IEnumerator PresentYesNoQuestion(string message)
        {
            return PresentYesNoQuestion(message, 0);
        }

        /// <summary>
        /// Presents a yes/no question prompt to the player. The result is stored in <see cref="DialogBoxResult"/>
        /// </summary>
        /// <param name="message">The message to speak</param>
        /// <param name="geoCost">How much geo the player need to pay to select "Yes"</param>
        protected IEnumerator PresentYesNoQuestion(string message, int geoCost)
        {
            HideConversationBox();
            var dialogManager = GameObject.Find("DialogueManager");
            if (dialogManager != null)
            {
                DialogBoxResult = YesNoResult.Neither;
                EventManager.SendEventToGameObject("BOX DOWN", dialogManager, gameObject);
                EventManager.SendEventToGameObject("TALK END", gameObject, gameObject);
                yield return new WaitForSeconds(0.3f);

                EventManager.SendEventToGameObject("BOX UP YN", dialogManager, gameObject);
                yield return new WaitForSeconds(0.25f);

                var ynPrompt = dialogManager.transform.Find("Text YN")?.gameObject;
                if (ynPrompt != null)
                {
                    PlayMakerUtilities.SetFsmInt(ynPrompt, "Dialogue Page Control", "Toll Cost", geoCost);
                    PlayMakerUtilities.SetFsmGameObject(ynPrompt, "Dialogue Page Control", "Requester", gameObject);
                    var dialogBox = ynPrompt.GetComponent("DialogueBox");
                    if (dialogBox != null)
                    {
                        StartDialogBoxConversation(dialogBox, message);

                        yield return new WaitUntil(() => DialogBoxResult != YesNoResult.Neither);
                        EventManager.SendEventToGameObject("BOX DOWN YN", dialogManager, gameObject);
                    }
                }
            }
        }

        static void StartDialogBoxConversation(object dialogBox, string text)
        {
            var type = dialogBox.GetType();
            type.GetField("currentConversation").SetValue(dialogBox, text.GetHashCode().ToString());
            type.GetField("currentPage").SetValue(dialogBox, 1);
            var textMesh = (TextMeshPro)type.GetField("textMesh",BindingFlags.NonPublic | BindingFlags.Instance).GetValue(dialogBox);
            textMesh.text = text;
            textMesh.ForceMeshUpdate();
            type.GetMethod("ShowPage").Invoke(dialogBox, new object[] { 1 });
        }

        /// <summary>
        /// Display an <see cref="WeaverBossTitle"/>. This is mainly used to display the name of the NPC the player is talking to
        /// </summary>
        /// <param name="title"></param>
        /// <param name="minorText"></param>
        protected void DisplayTitle(string title, string minorText = "")
        {
            WeaverBossTitle.Spawn(minorText,title);
        }
    }
}
