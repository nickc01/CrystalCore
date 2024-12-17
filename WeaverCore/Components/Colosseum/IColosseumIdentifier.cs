using UnityEngine;
using UnityEngine.Events;

namespace WeaverCore.Components.Colosseum
{
    public interface IColosseumIdentifier
    {
        string Identifier { get; }
        Color Color { get; }
        bool ShowShortcut { get; }
    }

    public interface IColosseumIdentifierExtra
    {
        Color UnderlineColor { get; }
    }
}