﻿using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using WeaverCore.Attributes;
using WeaverCore.Features;
using WeaverCore.Implementations;
using WeaverCore.Inventory;
using WeaverCore.Utilities;

namespace WeaverCore.Game.Implementations
{
    public class G_InventoryNavigator_I : InventoryNavigator_I
	{
        static PlayMakerFSM inventoryFSM;

        PlayMakerFSM uiFSM;
        PlayMakerFSM cursorUpdaterFSM;
        PlayMakerFSM cursorFSM;

        Transform cursorBL;
        Transform cursorBR;
        Transform cursorTL;
        Transform cursorTR;

        FsmState lArrowState;
		FsmState rArrowState;
		FsmState mainInputState;
        Transform background;
		//ArrowElement.ArrowState currentArrow;

        InventoryElement startupElement;

        EventManager manager;

        InventoryElement elementTargetOverride = null;

        bool firstStartup = false;

        public override Vector3 GetCursorPosition()
        {
            return transform.TransformPoint(cursorUpdaterFSM.GetVector3Variable("Item Pos").Value);
        }

        /*public override GameObject GetHighlightedObject()
        {
            return cursorFSM.GetGameObjectVariable("Item").Value;
        }

        public override void UpdateHighlightedObject(GameObject obj)
        {
            cursorFSM.GetGameObjectVariable("Item").Value = obj;
            cursorFSM.SendEvent("UPDATE CURSOR");

            //var renderer = obj.GetComponent<Renderer>();
        }*/

        static bool initialized = false;

        public override bool CanCloseInventory
        {
            get
            {
                return !inventoryFSM.GetBoolVariable("Do Not Close").Value;
            }
            set
            {
                inventoryFSM.GetBoolVariable("Do Not Close").Value = !value;
            }
        }

        InventoryElement highlightedElement;
        public override InventoryElement HighlightedElement => highlightedElement;

        [OnPlayerInit]
        static void OnInit(Player player)
        {
            if (player == Player.Player1Raw)
            {
                if (inventoryFSM == null)
                {
                    var gameCamera = GameObject.FindObjectOfType<GameCameras>();

                    //WeaverLog.Log("Game Camera = " + gameCamera?.name ?? "null");

                    inventoryFSM = gameCamera.transform.Find("HudCamera").Find("Inventory").GetComponents<PlayMakerFSM>().FirstOrDefault(c => c.FsmName == "Inventory Control");

                    //WeaverLog.Log("INVENTORY FSM = " + inventoryFSM?.name ?? "null");

                    if (!initialized)
                    {
                        initialized = true;
                        EventManager.OnEventTriggered += EventManager_OnEventTriggered;
                    }

                    AddAction("Tween Panes", () =>
                    {
                        var currentPane = inventoryFSM.GetGameObjectVariable("Current Pane").Value;
                        var prevPane = inventoryFSM.GetGameObjectVariable("Prev Pane").Value;

                        if (currentPane != null)
                        {
                            PaneOpenBegin(currentPane);
                        }

                        if (prevPane != null)
                        {
                            PaneCloseBegin(prevPane);
                        }
                    });

                    AddAction("Opened", () =>
                    {
                        var currentPane = inventoryFSM.GetGameObjectVariable("Current Pane").Value;
                        var prevPane = inventoryFSM.GetGameObjectVariable("Prev Pane").Value;

                        if (currentPane != null)
                        {
                            PaneOpenEnd(currentPane);
                        }

                        if (prevPane != null)
                        {
                            PaneCloseEnd(prevPane);
                        }
                    });

                    AddAction("Open Current Pane", () =>
                    {
                        var currentPane = inventoryFSM.GetGameObjectVariable("Current Pane").Value;

                        if (currentPane != null)
                        {
                            PaneOpenBegin(currentPane);
                        }
                    });

                    AddAction("Close", () =>
                    {
                        var currentPane = inventoryFSM.GetGameObjectVariable("Current Pane").Value;

                        if (currentPane != null)
                        {
                            PaneCloseBegin(currentPane);
                        }
                    });

                    AddAction("Regain Control", () =>
                    {
                        var currentPane = inventoryFSM.GetGameObjectVariable("Current Pane").Value;

                        if (currentPane != null)
                        {
                            PaneCloseEnd(currentPane);
                        }
                    }, 0);

                    AddAction("R Lock Close", () =>
                    {
                        var currentPane = inventoryFSM.GetGameObjectVariable("Current Pane").Value;

                        if (currentPane != null)
                        {
                            PaneCloseBegin(currentPane);
                        }
                    });

                    AddAction("Regain Control 2", () =>
                    {
                        var currentPane = inventoryFSM.GetGameObjectVariable("Current Pane").Value;

                        if (currentPane != null)
                        {
                            PaneCloseEnd(currentPane);
                        }
                    }, 0);

                    AddAction("Open", () =>
                    {
                        inventoryFSM.GetGameObjectVariable("Current Pane").Value = null;
                        inventoryFSM.GetGameObjectVariable("Prev Pane").Value = null;
                    });

                    /*AddActionIfUnique("OPEN_CLEAR_ACTION", "Open", () =>
                    {
                        inventoryFSM.GetGameObjectVariable("Current Pane").Value = null;
                        inventoryFSM.GetGameObjectVariable("Prev Pane").Value = null;
                    });


                    AddActionIfUnique("PANE_FINAL_POS_PREV_CLOSE_ACTION", "Pane Final Pos", () =>
                    {
                        var prevPane = inventoryFSM.GetGameObjectVariable("Prev Pane").Value;

                        if (prevPane != null)
                        {
                            PaneCloseEnd(prevPane);
                        }
                    });

                    AddActionIfUnique("TWEEN_PANES_PREV_PANE_CLOSE_ACTION", "Tween Panes", () =>
                    {
                        var prevPane = inventoryFSM.GetGameObjectVariable("Prev Pane").Value;

                        foreach (var action in inventoryFSM.FsmStates.First(s => s.Name == inventoryFSM.ActiveStateName).Actions)
                        {
                            WeaverLog.Log("Action = " + action.Name + " - " + action.GetType().FullName);
                        }

                        if (prevPane != null)
                        {
                            WeaverLog.Log("CLOSE A");
                            PaneCloseBegin(prevPane);
                        }
                    });

                    AddActionIfUnique("R_LOCK_CLOSE_CUR_PANE_CLOSE_ACTION", "R Lock Close", () =>
                    {
                        var currentPane = inventoryFSM.GetGameObjectVariable("Current Pane").Value;

                        if (currentPane != null)
                        {
                            WeaverLog.Log("CLOSE B");
                            PaneCloseBegin(currentPane);
                        }
                    });

                    AddActionIfUnique("REGAIN_CONTROL_2_CUR_PANE_CLOSE_ACTION", "Regain Control 2", () =>
                    {
                        var currentPane = inventoryFSM.GetGameObjectVariable("Current Pane").Value;

                        if (currentPane != null)
                        {
                            PaneCloseEnd(currentPane);
                        }
                    }, 0);

                    AddActionIfUnique("DAMAGE_CLOSE_CUR_PANE_CLOSE_ACTION", "Damage Close", () =>
                    {
                        var currentPane = inventoryFSM.GetGameObjectVariable("Current Pane").Value;

                        if (currentPane != null)
                        {
                            PaneCloseEnd(currentPane);
                        }
                    }, 0);

                    AddActionIfUnique("CLOSE_CUR_PANE_CLOSE_ACTION", "Close", () =>
                    {
                        WeaverLog.Log("Current State = " + inventoryFSM.ActiveStateName);
                        foreach (var action in inventoryFSM.FsmStates.First(s => s.Name == inventoryFSM.ActiveStateName).Actions)
                        {
                            WeaverLog.Log("Action = " + action.Name + " - " + action.GetType().FullName);
                        }
                        var currentPane = inventoryFSM.GetGameObjectVariable("Current Pane").Value;

                        if (currentPane != null)
                        {
                            WeaverLog.Log("CLOSE C");
                            PaneCloseBegin(currentPane);
                        }
                    });

                    AddActionIfUnique("REGAIN_CONTROL_CUR_PANE_CLOSE_ACTION", "Regain Control", () =>
                    {
                        var currentPane = inventoryFSM.GetGameObjectVariable("Current Pane").Value;

                        if (currentPane != null)
                        {
                            PaneCloseEnd(currentPane);
                        }
                    }, 0);*/
                }
            }
        }

        static IEnumerator OnInitRoutine()
        {
            yield return null;
            //yield return new WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Menu_Title");
            //WeaverLog.Log("FULLY INIT");
            try
            {
                
            }
            catch (Exception e)
            {
                WeaverLog.LogException(e);
                throw;
            }
        }

        static void AddActionIfUnique(string name, string state, Action action, int index = -1)
        {
            if (!inventoryFSM.FindActionsByType<MethodAction>(state).Any(a => a.ActionName == name))
            {
                //WeaverLog.Log("ACTIONS OF METHOD = " + inventoryFSM.FindActionsByType<MethodAction>(state).Count());
                var addedAction = AddAction(state, action, index);
                addedAction.ActionName = name;
            }
        }


        static MethodAction AddAction(string state, Action action, int index = -1)
        {
            var stateInstance = inventoryFSM.GetState(state);

            stateInstance.Fsm = inventoryFSM.Fsm;

            //stateInstance.AddMethod(action);

            //WeaverLog.Log("ADDING ACTION IN STATE = " + state);

            if (index < 0)
            {
                return stateInstance.AddMethod(() =>
                {
                    try
                    {
                        //WeaverLog.Log("RUNNING ACTION IN STATE = " + state);
                        action?.Invoke();
                    }
                    catch (Exception e)
                    {
                        WeaverLog.LogException(e);
                    }
                });
            }
            else
            {
                return stateInstance.InsertMethod(() =>
                {
                    try
                    {
                        //WeaverLog.Log("RUNNING ACTION IN STATE = " + state);
                        action?.Invoke();
                    }
                    catch (Exception e)
                    {
                        WeaverLog.LogException(e);
                    }
                }, index);
            }
        }

        bool awoken = false;

        void Awake()
		{
            if (awoken)
            {
                return;
            }
            awoken = true;
            uiFSM = transform.parent.GetComponents<PlayMakerFSM>().FirstOrDefault(c => c.FsmName == "Empty UI");
            cursorUpdaterFSM = transform.parent.GetComponents<PlayMakerFSM>().FirstOrDefault(c => c.FsmName == "Update Cursor");
            cursorFSM = transform.parent.Find("Cursor").GetComponent<PlayMakerFSM>();

            cursorBL = cursorFSM.transform.Find("BL");
            cursorBR = cursorFSM.transform.Find("BR");
            cursorTL = cursorFSM.transform.Find("TL");
            cursorTR = cursorFSM.transform.Find("TR");


            if (manager == null)
            {
                manager = transform.parent.GetComponent<EventManager>();
            }

            background = transform.Find("Background");

            //EventManager.OnEventTriggered += EventManager_OnEventTriggered;

            //manager.AddReceiverForEvent("INVENTORY OPENED", InvOpened);
            //manager.AddReceiverForEvent("INVENTORY CLOSED", InvClosed);
            //manager.AddReceiverForEvent("PANE RESET", PaneClosed);
            //manager.AddReceiverForEvent("ACTIVATE", PaneOpened);

            //WeaverLog.Log("CURSOR FSM = " + cursorFSM);

            lArrowState = uiFSM.FsmStates.FirstOrDefault(s => s.Name == "L Arrow");
            rArrowState = uiFSM.FsmStates.FirstOrDefault(s => s.Name == "R Arrow");

            mainInputState = uiFSM.AddState("Main Input");

			var heartPieceState = uiFSM.GetState("Init Heart Piece");

			heartPieceState.ChangeFsmTransition("FINISHED", mainInputState.Name);

            lArrowState.ChangeFsmTransition("UI RIGHT", mainInputState.Name);
            rArrowState.ChangeFsmTransition("UI LEFT", mainInputState.Name);

            mainInputState.AddMethod(MainState);

            mainInputState.AddTransition("GOTO LEFT ARROW", lArrowState.Name);
            mainInputState.AddTransition("GOTO RIGHT ARROW", rArrowState.Name);

            InputManager.OnLeftEvent += InputManager_OnLeftEvent;
            InputManager.OnRightEvent += InputManager_OnRightEvent;
            InputManager.OnUpEvent += InputManager_OnUpEvent;
            InputManager.OnDownEvent += InputManager_OnDownEvent;
            InputManager.OnSelectEvent += InputManager_OnSelectEvent;


            var updateState = cursorUpdaterFSM.GetState("Update");

            //var setVectAction = (SetFsmVector3)updateState.Actions[3];

            //var actualCursor = setVectAction.gameObject.GameObject.Value;

            var actualCursor = transform.parent.Find("Cursor").gameObject;

            //cursorFSM.RemoveAction("Update", 7);
            //cursorFSM.RemoveAction("Update", 6);
            //cursorFSM.RemoveAction("Update", 5);
            //cursorFSM.RemoveAction("Update", 4);

            var cursorActivateState = cursorFSM.GetState("Cursor Activate");

            /*cursorActivateState.InsertMethod(() =>
            {
                if (startupElement == null)
                {
                    startupElement = MainPanel.RightArrow;
                }
                WeaverLog.Log("STARTUP ELEMENT = " + startupElement.name);
                firstStartup = true;
                WeaverLog.Log("CURSOR FSM BEFORE POS = " + cursorFSM.transform.localPosition);
                cursorFSM.transform.localPosition = GetCursorPosForElement(startupElement);
                WeaverLog.Log("CURSOR FSM AFTER POS = " + cursorFSM.transform.localPosition);
                highlightedElement = startupElement;
            }, 0);*/

            //cursorFSM.RemoveAction("Update", 3);
            cursorUpdaterFSM.RemoveAction("Update", 2);
            cursorUpdaterFSM.RemoveAction("Update", 1);
            cursorUpdaterFSM.RemoveAction("Update", 0);

            cursorUpdaterFSM.InsertMethod("Update", () =>
            {
                //WeaverLog.Log("GETTING ITEM POS");
                var item = cursorUpdaterFSM.GetGameObjectVariable("Item");
                if (item.Value != null)
                {
                    Vector3 itemPos;
                    Vector2 itemBounds;
                    Vector2 itemOffset;
                    if (item.Value.TryGetComponent<InventoryElement>(out var element))
                    {
                        //WeaverLog.Log("ITEM ELEMENT = " + element.name);
                        itemPos = GetCursorPosForElement(element);
                        itemBounds = GetCursorBoundsForElement(element);
                        itemOffset = GetCursorOffsetForElement(element);
                    }
                    else
                    {
                        //WeaverLog.Log("ITEM ELEMENT NON EXISTENT");
                        itemPos = item.Value.transform.localPosition;
                        itemBounds = item.Value.GetComponent<Collider2D>().bounds.size;
                        itemOffset = item.Value.GetComponent<Collider2D>().offset;
                    }
                    //WeaverLog.Log("GETTING ITEM POS = " + itemPos);
                    //WeaverLog.Log("GETTING ITEM Bounds = " + itemBounds);
                    //WeaverLog.Log("GETTING ITEM Offset = " + itemOffset);
                    cursorUpdaterFSM.GetFsmVector3Variable("Item Pos").Value = itemPos;
                    cursorUpdaterFSM.GetFsmVector2Variable("Box Bounds").Value = itemBounds;
                    cursorUpdaterFSM.GetFloatVariable("Box Offset X").Value = itemOffset.x;
                    cursorUpdaterFSM.GetFloatVariable("Box Offset Y").Value = itemOffset.y;
                    //PlayMakerUtilities.SetFsmVector3(actualCursor, "Cursor Movement", "MoveToPos", itemPos);
                    //PlayMakerUtilities.SetFsmVector2(actualCursor, "Cursor Movement", "ColliderBounds", itemBounds);
                    //PlayMakerUtilities.SetFsmFloat(actualCursor, "Cursor Movement", "Box Offset X", itemOffset.x);
                    //PlayMakerUtilities.SetFsmFloat(actualCursor, "Cursor Movement", "Box Offset Y", itemOffset.y);
                    //EventManager.SendEventToGameObject("CURSOR MOVE", actualCursor);
                }
            }, 0);

            /*foreach (var action in updateState.Actions)
            {
                WeaverLog.Log("Update Action = " + action.GetType().FullName);
            }*/

        }

        private void InputManager_OnSelectEvent()
        {
            if (uiFSM.ActiveStateName == mainInputState.Name)
            {
                if (highlightedElement != null && highlightedElement.Selectable)
                {
                    highlightedElement.OnClick();
                }
                //HighlightElement(FindNextElement(highlightedElement, InventoryElement.MoveDirection.Down));
            }
        }

        private void InputManager_OnDownEvent()
        {
            if (uiFSM.ActiveStateName == mainInputState.Name)
            {
                HighlightElement(FindNextElement(highlightedElement, InventoryElement.MoveDirection.Down));
            }
        }

        private void InputManager_OnUpEvent()
        {
            if (uiFSM.ActiveStateName == mainInputState.Name)
            {
                HighlightElement(FindNextElement(highlightedElement, InventoryElement.MoveDirection.Up));
            }
        }

        private void InputManager_OnRightEvent()
        {
            if (uiFSM.ActiveStateName == mainInputState.Name)
            {
                HighlightElement(FindNextElement(highlightedElement, InventoryElement.MoveDirection.Right));
            }
        }

        private void InputManager_OnLeftEvent()
        {
            if (uiFSM.ActiveStateName == mainInputState.Name)
            {
                HighlightElement(FindNextElement(highlightedElement, InventoryElement.MoveDirection.Left));
            }
        }

        private static void EventManager_OnEventTriggered(string eventName, GameObject source, GameObject destination, EventManager.EventType eventType)
        {
            /*if (source == inventoryFSM.gameObject)
            {
                WeaverLog.Log("SOURCE EVENT NAME = " + eventName);
                WeaverLog.Log("DEST = " + destination?.gameObject);
            }*/

            //WeaverLog.Log("G SOURCE = " + source?.name);
            if (eventType == EventManager.EventType.Broadcast)
            {
                if (eventName == "INVENTORY OPENED")
                {
                    InvOpened();
                }
                else if (eventName == "INVENTORY CLOSED")
                {
                    InvClosed();
                }
            }
            else if (eventType == EventManager.EventType.Message)
            {
                if (eventName == "UP" && source == inventoryFSM.gameObject)
                {
                    //WeaverLog.Log("A DEST = " + destination?.name);
                    //PaneOpenBegin(destination);
                }

                /*if (eventName == "PANE RESET" && source == inventoryFSM.gameObject)
                {
                    //WeaverLog.Log("B DEST = " + destination?.name);
                    PaneCloseBegin(destination);
                }*/

                if (eventName == "ACTIVATE" && source == inventoryFSM.gameObject)
                {
                    //WeaverLog.Log("C DEST = " + destination?.name);
                    //PaneOpenEnd(destination);
                    //WeaverLog.Log("PANE_A");
                    /*var prevPane = inventoryFSM.GetGameObjectVariable("Prev Pane").Value;
                    //WeaverLog.Log("PANE_B = " + prevPane);

                    if (prevPane != null)
                    {
                        //WeaverLog.Log("PANE_C");
                        PaneCloseEnd(prevPane);
                    }*/
                }
            }
        }

        /*private void EventManager_OnEventTriggered(string eventName, GameObject source, GameObject destination, EventManager.EventType eventType)
        {
            //TODO

        }*/

        static void InvOpened()
        {
            //WeaverLog.Log("INV OPENED");
            InventoryOpenEvent?.Invoke();
        }

        static void InvClosed()
        {
            //WeaverLog.Log("INV CLOSED");
            InventoryCloseEvent?.Invoke();
        }

        static void PaneOpenBegin(GameObject pane)
        {
            //WeaverLog.Log("PANE OPEN BEGIN = " + pane.gameObject.name);
            var nav = pane.GetComponentInChildren<G_InventoryNavigator_I>();
            if (nav != null)
            {
                nav.Internal_OnPaneOpenBegin();
            }
            PaneOpenBeginEvent?.Invoke(pane.gameObject.name);
        }

        static void PaneOpenEnd(GameObject pane)
        {
            //WeaverLog.Log("PANE OPEN END = " + pane.gameObject.name);
            var nav = pane.GetComponentInChildren<G_InventoryNavigator_I>();
            if (nav != null)
            {
                nav.Internal_OnPaneOpenEnd();
            }
            PaneOpenEndEvent?.Invoke(pane.gameObject.name);
        }

        static void PaneCloseBegin(GameObject pane)
        {
            //WeaverLog.Log("PANE CLOSE BEGIN = " + pane.gameObject.name);
            //WeaverLog.Log(new System.Diagnostics.StackTrace());
            var nav = pane.GetComponentInChildren<G_InventoryNavigator_I>();
            if (nav != null)
            {
                nav.Internal_OnPaneCloseBegin();
            }
            PaneCloseBeginEvent?.Invoke(pane.gameObject.name);
        }

        static void PaneCloseEnd(GameObject pane)
        {
            //WeaverLog.Log("PANE CLOSE END = " + pane.gameObject.name);
            //WeaverLog.Log(new System.Diagnostics.StackTrace());
            var nav = pane.GetComponentInChildren<G_InventoryNavigator_I>();
            if (nav != null)
            {
                nav.Internal_OnPaneCloseEnd();
            }
            PaneCloseEndEvent?.Invoke(pane.gameObject.name);
        }

        public override void InitPanel(InventoryPanel panel)
        {
            Awake();

            var fadeGroup = panel.transform.parent.GetComponent<FadeGroup>();

            var spriteRenderers = new List<SpriteRenderer>(fadeGroup.spriteRenderers);
            spriteRenderers.AddRange(panel.GetComponentsInChildren<SpriteRenderer>());
            fadeGroup.spriteRenderers = spriteRenderers.Distinct().ToArray();

            var texts = fadeGroup.texts.ToList();
            texts.AddRange(panel.GetComponentsInChildren<TextMeshPro>());
            fadeGroup.texts = texts.Distinct().ToArray();

            var animators = fadeGroup.animators.ToList();
            animators.AddRange(panel.GetComponentsInChildren<InvAnimateUpAndDown>());
            fadeGroup.animators = animators.Distinct().ToArray();
        }

        GameObject actualRightArrow => transform.parent.parent.Find("Border").Find("Arrow Right").gameObject;
        GameObject actualLeftArrow => transform.parent.parent.Find("Border").Find("Arrow Left").gameObject;

        void MainState()
        {
            /*if (firstStartup)
            {
                firstStartup = false;
                return;
            }*/

            if (highlightedElement == null)
            {
                if (startupElement == null)
                {
                    startupElement = MainPanel.RightArrow;
                }
                //WeaverLog.Log("STARTUP ELEMENT = " + startupElement.name);
                firstStartup = true;
                //WeaverLog.Log("CURSOR FSM BEFORE POS = " + cursorFSM.transform.localPosition);
                cursorFSM.transform.localPosition = GetCursorPosForElement(startupElement);
                //WeaverLog.Log("CURSOR FSM AFTER POS = " + cursorFSM.transform.localPosition);
                highlightedElement = startupElement;

                var size = GetCursorBoundsForElement(startupElement);
                var offset = GetCursorOffsetForElement(startupElement);

                cursorBL.localPosition = new Vector3((-size.x / 2f) + offset.x, (-size.y / 2f) + offset.y, 0f);
                cursorBR.localPosition = new Vector3((size.x / 2f) + offset.x, (-size.y / 2f) + offset.y, 0f);
                cursorTL.localPosition = new Vector3((-size.x / 2f) + offset.x, (size.y / 2f) + offset.y, 0f);
                cursorTR.localPosition = new Vector3((size.x / 2f) + offset.x, (size.y / 2f) + offset.y, 0f);

                HighlightElement(highlightedElement);

                return;
            }

            if (elementTargetOverride != null)
            {
                HighlightElement(elementTargetOverride);
                elementTargetOverride = null;
                return;
            }
            if (highlightedElement != null && highlightedElement is LeftArrowElement lArrowElement)
            {
                HighlightElement(FindNextElement(lArrowElement, InventoryElement.MoveDirection.Right));
            }
            else if (highlightedElement != null && highlightedElement is RightArrowElement rArrowElement)
            {
                HighlightElement(FindNextElement(rArrowElement, InventoryElement.MoveDirection.Left));
            }
            else
            {
                HighlightElement(startupElement);
            }
        }

        public override void HighlightElement(InventoryElement element)
        {
            if (element == highlightedElement)
            {
                return;
            }

            /*if (element == null)
            {
                WeaverLog.Log("TRYING TO HIGHLIGHT ELEMENT = " + "null");
            }
            else
            {
                WeaverLog.Log("TRYING TO HIGHLIGHT ELEMENT = " + element.name);
            }*/

            if (inventoryFSM.ActiveStateName != mainInputState.Name)
            {
                elementTargetOverride = element;
                inventoryFSM.SetState(mainInputState.Name);
                return;
            }

            if (element is LeftArrowElement)
            {
                var prevHighlightedElement = highlightedElement;
                highlightedElement = element;

                if (prevHighlightedElement != null)
                {
                    prevHighlightedElement.OnUnHighlight();
                }

                highlightedElement.OnHighlight();

                //uiFSM.SetState(lArrowState.Name);
                //uiFSM.SendEvent("GOTO LEFT ARROW");
                uiFSM.Fsm.DelayedEvent(FsmEvent.GetFsmEvent("GOTO LEFT ARROW"), 1f / 60f);
                //StartCoroutine(SendEventAfterDelay(uiFSM, "GOTO LEFT ARROW"));
                //cursorFSM.GetGameObjectVariable("Item").Value = actualLeftArrow;
                //cursorFSM.SendEvent("UPDATE CURSOR");
            }
            else if (element is RightArrowElement)
            {
                var prevHighlightedElement = highlightedElement;
                highlightedElement = element;

                if (prevHighlightedElement != null)
                {
                    prevHighlightedElement.OnUnHighlight();
                }

                highlightedElement.OnHighlight();

                //uiFSM.SetState(rArrowState.Name);
                //uiFSM.SendEvent("GOTO RIGHT ARROW");
                uiFSM.Fsm.DelayedEvent(FsmEvent.GetFsmEvent("GOTO RIGHT ARROW"), 1f / 60f);
                //StartCoroutine(SendEventAfterDelay(uiFSM, "GOTO RIGHT ARROW"));
                //cursorFSM.GetGameObjectVariable("Item").Value = actualRightArrow;
                //cursorFSM.SendEvent("UPDATE CURSOR");
            }
            else
            {
                var prevHighlightedElement = highlightedElement;
                highlightedElement = element;

                if (prevHighlightedElement != null)
                {
                    prevHighlightedElement.OnUnHighlight();
                }

                highlightedElement.OnHighlight();

                cursorUpdaterFSM.GetGameObjectVariable("Item").Value = highlightedElement.gameObject;
                cursorUpdaterFSM.SendEvent("UPDATE CURSOR");
            }
        }

        public override void SetStartupElement(InventoryElement element)
        {
            startupElement = element;

            /*var heartPieceState = uiFSM.GetState("Init Heart Piece");
            var posAction = (GetPosition)heartPieceState.Actions[0];
            posAction.gameObject.GameObject.Value = startupElement.gameObject;*/
        }

        void Internal_OnPaneOpenBegin()
        {

        }

        void Internal_OnPaneOpenEnd()
        {

        }

        void Internal_OnPaneCloseBegin()
        {

        }

        void Internal_OnPaneCloseEnd()
        {
            highlightedElement = null;
        }

        public override Vector3 GetCursorPosForElement(InventoryElement element)
        {
            //WeaverLog.Log("GETTING POSITION FOR ELEMENT = " + element.name);
            //return element.CursorPos - background.transform.position;
            //return element.CursorPos;
            //return element.CursorPos - element.transform.position;

            return transform.InverseTransformPoint(element.CursorPos);
        }

        public override Vector2 GetCursorBoundsForElement(InventoryElement element)
        {
            return element.CursorSize;
        }

        public override Vector2 GetCursorOffsetForElement(InventoryElement element)
        {
            return element.CursorOffset;
        }

        /*void OnEnable()
        {
            WeaverLog.Log("UI ENABLED");
            StartCoroutine(StartRoutine());
        }

        void OnDisable()
        {
            WeaverLog.Log("UI DISABLED");
            StopAllCoroutines();
        }*/

        //IEnumerator StartRoutine()
        //{
        //yield return new WaitUntil(() => uiFSM.ActiveStateName == lArrowState.Name || uiFSM.ActiveStateName == rArrowState.Name || uiFSM.ActiveStateName == mainInputState.Name);
        /*var highlightedObject = GetHighlightedObject();
        WeaverLog.Log("HIGHLIGHTED OBJ = " + highlightedObject);

        while (true)
        {
            highlightedObject = GetHighlightedObject();
            WeaverLog.Log("HIGHLIGHTED OBJ = " + highlightedObject);
            yield return new WaitForSeconds(0.5f);
        }*/
        //WeaverLog.Log("BEGINNING STATE = " + uiFSM.ActiveStateName);
        //}
    }
}
