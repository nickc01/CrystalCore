using System;
using UnityEngine;

namespace WeaverCore.Components
{
    public class ColosseumPlatformController : MonoBehaviour
    {
        public Vector3 PlatformStartPos = new Vector3(0f, -18f, 0.007f);
        public Vector3 PlatformEndPos = new Vector3(0f, 0f, 0.007f);
        public Vector3 PlatformActivePos = new Vector3(0f, 0f, 0.001f);

        [NonSerialized]
        WeaverAnimationPlayer _animator;
        public WeaverAnimationPlayer Animator => _animator ??= GetComponent<WeaverAnimationPlayer>();
    }
}