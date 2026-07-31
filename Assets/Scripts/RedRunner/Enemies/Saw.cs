using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RedRunner.Characters;

namespace RedRunner.Enemies
{

	public class Saw : Enemy
	{

		[SerializeField]
		private Collider2D m_Collider2D;
		[SerializeField]
		private Transform targetRotation;
		[SerializeField]
		private float m_Speed = 1f;
		[SerializeField]
		private bool m_RotateClockwise = false;


		public override Collider2D Collider2D {
			get {
				return m_Collider2D;
			}
		}

		void Start ()
		{
			if (targetRotation == null) {
				targetRotation = transform;
			}
		}

		void Update ()
		{
			Vector3 rotation = targetRotation.rotation.eulerAngles;
			if (!m_RotateClockwise) {
				rotation.z += m_Speed;
			} else {
				rotation.z -= m_Speed;
			}
			targetRotation.rotation = Quaternion.Euler (rotation);
		}

		void OnCollisionEnter2D (Collision2D collision2D)
		{
			Character character = collision2D.collider.GetComponent<Character> ();
			if (character != null) {
				Kill (character);
			}
			AudioManager.Singleton.PlaySawHitSound(transform.position);
			Kill(character);
		}



		public override void Kill (Character target)
		{
			target.Die (true);
		}

	}

}