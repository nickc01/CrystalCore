using UnityEngine;
using WeaverCore.Enums;
using WeaverCore.Interfaces;
using WeaverCore.Utilities;

namespace WeaverCore.Components
{
    /// <summary>
    /// Causes any nail hits on the object to be deflected. Useful for spikes
    /// </summary>
    public class NailDeflect : MonoBehaviour, IHittable
    {
        [field: SerializeField]
        public float deflectTimer { get; private set; } = 0f;

        [field: SerializeField]
        public float repeatDelay { get; private set; } = 0.25f;

		[field: SerializeField]
		public bool ApplyCameraShake {get; private set;} = true;


        public bool Hit(HitInfo hit)
        {
			if (hit.AttackType == AttackType.Nail || hit.AttackType == AttackType.NailBeam || hit.AttackType == AttackType.Generic)
			{
                if (deflectTimer <= 0f)
                {
                    deflectTimer = repeatDelay;
                    OnDeflect(hit);
                    return true;
                }
            }
            return false;
        }

        private void Update()
        {
            if (deflectTimer > 0f)
            {
                deflectTimer -= Time.deltaTime;
            }
        }

        /*private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!(collision.tag == "Nail Attack") || Time.time < deflectTimer)
            {
                return;
            }
            if (deflectTimer <= 0f)
            {
                deflectTimer = repeatDelay;

				var direction = collision.transform.position - collision.transform.position;

                OnDeflect(new HitInfo
				{
					Damage = 1,
					Direction = Mathf.Atan2(direction.y, direction.x),
					Attacker = collision.gameObject,
					AttackStrength = 1f,
					AttackType = AttackType.Nail,
					IgnoreInvincible = false
				});
            }
        }*/

        public static void PlayDeflectEffects(Vector3 playerPosition, GameObject deflectedObject, CardinalDirection hitDirection, bool applyRecoil = true, bool applyCameraShake = true)
        {

			CameraShaker.Instance.Shake(ShakeType.EnemyKillShake);
			Vector3 vector = new Vector3(0f, 0f, 0f);
			Vector3 euler = new Vector3(0f, 0f, 0f);
			Vector3 position = HeroController.instance.transform.position;
			Vector3 position2 = deflectedObject.transform.position;

			var boxCollider = deflectedObject.GetComponent<Collider2D>();

			/*bool doEffectsAtObject = boxCollider != null;
			if (useNailPosition)
			{
				doEffectsAtObject = false;
			}*/
			bool doEffectsAtObject = false;
			Vector2 vector2 = Vector2.zero;
			float num = 0f;
			float num2 = 0f;
			if (doEffectsAtObject)
			{
				vector2 = deflectedObject.transform.TransformPoint(boxCollider.offset);
				num = boxCollider.bounds.size.x * 0.5f;
				num2 = boxCollider.bounds.size.y * 0.5f;
			}
			switch (hitDirection)
			{
				case CardinalDirection.Right:
                    if (applyRecoil)
                    {
						HeroController.instance.RecoilLeft();
					}
					vector = ((!doEffectsAtObject) ? new Vector3(position.x + 2f, position.y, 0.002f) : new Vector3(vector2.x - num, position2.y, 0.002f));
					break;
				case CardinalDirection.Up:
					if (applyRecoil)
                    {
						HeroController.instance.RecoilDown();
					}
					vector = ((!doEffectsAtObject) ? new Vector3(position.x, position.y + 2f, 0.002f) : new Vector3(position2.x, Mathf.Max(vector2.y - num2, position2.y), 0.002f));
					euler = new Vector3(0f, 0f, 90f);
					break;
				case CardinalDirection.Left:
                    if (applyRecoil)
                    {
						HeroController.instance.RecoilRight();
					}
					vector = ((!doEffectsAtObject) ? new Vector3(position.x - 2f, position.y, 0.002f) : new Vector3(vector2.x + num, position2.y, 0.002f));
					euler = new Vector3(0f, 0f, 180f);
					break;
				default:
					vector = ((!doEffectsAtObject) ? new Vector3(position.x, position.y - 2f, 0.002f) : new Vector3(position2.x, Mathf.Min(vector2.y + num2, position2.y), 0.002f));
					euler = new Vector3(0f, 0f, 270f);
					break;
			}
			var blockEffect = Pooling.Instantiate(Assets.EffectAssets.BlockedHitPrefab, vector, Quaternion.Euler(euler));

			var clip = WeaverAudio.PlayAtPoint(Assets.AudioAssets.SwordCling, blockEffect.transform.position);
			clip.AudioSource.pitch = Random.Range(0.85f, 1.15f);
			//blockEffect.Spawn(vector, Quaternion.Euler(euler)).GetComponent<AudioSource>().pitch = Random.Range(0.85f, 1.15f);
			/*if (sendFSMEvent)
			{
				fsm.SendEvent(FSMEvent);
			}*/
		}

        protected virtual void OnDeflect(HitInfo hit)
        {
            PlayDeflectEffects(Player.Player1.transform.position, gameObject, DirectionUtilities.DegreesToDirection(hit.Direction), applyCameraShake: ApplyCameraShake);
        }
    }
}