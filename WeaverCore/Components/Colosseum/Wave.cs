using UnityEngine;
using System.Collections;

namespace WeaverCore.Components.Colosseum
{
    public abstract class Wave : MonoBehaviour
    {
        public virtual bool AutoRun => gameObject.activeInHierarchy && enabled;
        public abstract IEnumerator RunWave(ColosseumRoomManager challenge);
    }
}
