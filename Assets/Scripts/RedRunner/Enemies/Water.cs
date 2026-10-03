using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RedRunner.Characters;

namespace RedRunner.Enemies
{

	public class Water : Enemy
	{

		[SerializeField]
		private Collider2D m_Collider2D;

		public override Collider2D Collider2D {
			get {
				return m_Collider2D;
			}
		}

		void OnTriggerEnter2D (Collider2D other)
		{
			Character character = other.GetComponent<Character> ();
			if (character != null && character.DecidesDeath) {
				Kill (character);
			}
		}

		public override void Kill (Character target)
		{
			target.Die (DeathCause.Water);
		}

	}

}