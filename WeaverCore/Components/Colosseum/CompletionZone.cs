using System;
using UnityEngine;
using WeaverCore.Utilities;

namespace WeaverCore.Components.Colosseum
{
    public class CompletionZone : MonoBehaviour, IColosseumIdentifier
    {
        [NonSerialized]
        Collider2D _trigger;
        public Collider2D Trigger => _trigger ??= GetComponent<Collider2D>();

        string IColosseumIdentifier.Identifier => "Colosseum Zones";

        Color IColosseumIdentifier.Color => Color.black;

        bool IColosseumIdentifier.ShowShortcut => true;

        public bool PlayerIsInZone() {
            var bounds = Trigger.bounds;
            var rect = new Rect()
            {
                size = bounds.size,
                center = bounds.center
            };

            return RectUtilities.IsWithin(rect, Player.Player1.transform.position);
            //return Trigger.bounds.Contains(Player.Player1.transform.position);
        }
    }
}
