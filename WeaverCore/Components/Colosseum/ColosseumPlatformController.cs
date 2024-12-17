using System;
using UnityEngine;

namespace WeaverCore.Components.Colosseum
{
    public class ColosseumPlatformController : MonoBehaviour
    {
        [Tooltip("The starting position of the platform.")]
        public Vector3 PlatformStartPos = new Vector3(0f, -18f, 0.007f);

        [Tooltip("The ending position of the platform after expansion.")]
        public Vector3 PlatformEndPos = new Vector3(0f, 0f, 0.007f);

        [Tooltip("The active position of the platform when fully expanded.")]
        public Vector3 PlatformActivePos = new Vector3(0f, 0f, 0.001f);


        [NonSerialized]
        WeaverAnimationPlayer _animator;
        public WeaverAnimationPlayer Animator => _animator ??= GetComponent<WeaverAnimationPlayer>();
    }
}