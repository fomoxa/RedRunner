using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using RedRunner.Utilities;

namespace RedRunner.Characters
{

	public class RedCharacter : Character
	{
		#region Fields

		[Header ( "Character Details" )]
		[Space]
		[SerializeField]
		protected float m_MaxRunSpeed = 8f;
		[SerializeField]
		protected float m_RunSmoothTime = 5f;
		[SerializeField]
		protected float m_RunSpeed = 5f;
		[SerializeField]
		protected float m_WalkSpeed = 1.75f;
		[SerializeField]
		protected float m_JumpStrength = 10f;
		[SerializeField]
		protected string[] m_Actions = new string[0];
		[SerializeField]
		protected int m_CurrentActionIndex = 0;

		[Header ( "Character Reference" )]
		[Space]
		[SerializeField]
		protected Rigidbody2D m_Rigidbody2D;
		[SerializeField]
		protected Collider2D m_Collider2D;
		[SerializeField]
		protected Animator m_Animator;
		[SerializeField]
		protected GroundCheck m_GroundCheck;
		[SerializeField]
		protected ParticleSystem m_RunParticleSystem;
		[SerializeField]
		protected ParticleSystem m_JumpParticleSystem;
		[SerializeField]
		protected ParticleSystem m_WaterParticleSystem;
		[SerializeField]
		protected ParticleSystem m_BloodParticleSystem;
		[SerializeField]
		protected Skeleton m_Skeleton;
		[SerializeField]
		protected float m_RollForce = 10f;
		[SerializeField]
		protected float m_TeleportDistance = 2f;

		[Header ( "Character Audio" )]
		[Space]
		[SerializeField]
		protected AudioSource m_MainAudioSource;
		[SerializeField]
		protected AudioSource m_FootstepAudioSource;
		[SerializeField]
		protected AudioSource m_JumpAndGroundedAudioSource;

		#endregion

		#region Private Variables

		protected bool m_ClosingEye = false;
		protected bool m_Guard = false;
		protected bool m_Block = false;
		protected Vector2 m_Speed = Vector2.zero;
		protected float m_CurrentRunSpeed = 0f;
		protected float m_CurrentSmoothVelocity = 0f;
		protected readonly RedRunner.Simulation.RunnerMotion m_Motion = new RedRunner.Simulation.RunnerMotion ();
		protected int m_CurrentFootstepSoundIndex = 0;
		protected Vector3 m_InitialScale;
		protected Vector3 m_InitialPosition;
		protected Vector3 m_LastPosition;

		#endregion

		#region Properties

		public override float MaxRunSpeed
		{
			get
			{
				return m_MaxRunSpeed;
			}
		}

		public override float RunSmoothTime
		{
			get
			{
				return m_RunSmoothTime;
			}
		}

		public override float RunSpeed
		{
			get
			{
				return m_RunSpeed;
			}
		}

		public override float WalkSpeed
		{
			get
			{
				return m_WalkSpeed;
			}
		}

		public override float JumpStrength
		{
			get
			{
				return m_JumpStrength;
			}
		}

		public override Vector2 Speed
		{
			get
			{
				return m_Speed;
			}
		}

		public override string[] Actions
		{
			get
			{
				return m_Actions;
			}
		}

		public override string CurrentAction
		{
			get
			{
				return m_Actions [ m_CurrentActionIndex ];
			}
		}

		public override int CurrentActionIndex
		{
			get
			{
				return m_CurrentActionIndex;
			}
		}

		public override GroundCheck GroundCheck
		{
			get
			{
				return m_GroundCheck;
			}
		}

		public override Rigidbody2D Rigidbody2D
		{
			get
			{
				return m_Rigidbody2D;
			}
		}

		public override Collider2D Collider2D
		{
			get
			{
				return m_Collider2D;
			}
		}

		public override Animator Animator
		{
			get
			{
				return m_Animator;
			}
		}

		public override ParticleSystem RunParticleSystem
		{
			get
			{
				return m_RunParticleSystem;
			}
		}

		public override ParticleSystem JumpParticleSystem
		{
			get
			{
				return m_JumpParticleSystem;
			}
		}

		public override ParticleSystem WaterParticleSystem
		{
			get
			{
				return m_WaterParticleSystem;
			}
		}

		public override ParticleSystem BloodParticleSystem
		{
			get
			{
				return m_BloodParticleSystem;
			}
		}

		public override Skeleton Skeleton
		{
			get
			{
				return m_Skeleton;
			}
		}

        public override bool ClosingEye
		{
			get
			{
				return m_ClosingEye;
			}
		}

		public override bool Guard
		{
			get
			{
				return m_Guard;
			}
		}

		public override bool Block
		{
			get
			{
				return m_Block;
			}
		}

		public override AudioSource Audio
		{
			get
			{
				return m_MainAudioSource;
			}
		}

		public System.Func<Fomoxa.Networking.Simulation.PhysicsBody2D> BodySource { get; set; }

		protected Vector2 LinearVelocity
		{
			get
			{
				if ( BodySource != null )
				{
					Fomoxa.Networking.Simulation.PhysicsBody2D body = BodySource ();
					if ( body.IsValid )
					{
						return new Vector2 ( body.Velocity.X, body.Velocity.Y );
					}
				}
				return m_Rigidbody2D.linearVelocity;
			}
			set
			{
				if ( BodySource != null )
				{
					Fomoxa.Networking.Simulation.PhysicsBody2D body = BodySource ();
					if ( body.IsValid )
					{
						body.Velocity = new System.Numerics.Vector2 ( value.x, value.y );
						return;
					}
				}
				m_Rigidbody2D.linearVelocity = value;
			}
		}

		public virtual float RunSmoothVelocity
		{
			get
			{
				return m_Motion.RunSmoothVelocity;
			}
			set
			{
				m_Motion.RunSmoothVelocity = value;
			}
		}

		public RedRunner.Simulation.RunnerSettings Settings
		{
			get
			{
				return new RedRunner.Simulation.RunnerSettings ( m_RunSpeed, m_MaxRunSpeed, m_RunSmoothTime, m_JumpStrength, m_RollForce );
			}
		}

		#endregion

		#region MonoBehaviour Messages

		void Awake ()
		{
			m_InitialPosition = transform.position;
			m_LastPosition = transform.position;
			m_InitialScale = transform.localScale;
			m_GroundCheck.OnGrounded += GroundCheck_OnGrounded;
			m_Skeleton.OnActiveChanged += Skeleton_OnActiveChanged;
            IsDead = new Property<bool>(false);
			m_ClosingEye = false;
			m_Guard = false;
			m_Block = false;
			m_CurrentFootstepSoundIndex = 0;
			GameManager.OnReset += GameManager_OnReset;
		}

		void Update ()
		{
			if ( !GameManager.Singleton.gameStarted || !GameManager.Singleton.gameRunning )
			{
				return;
			}

			if ( IsDead.Value && !m_ClosingEye )
			{
				StartCoroutine ( CloseEye () );
			}
		}

		void LateUpdate ()
		{
			Vector2 velocity = m_Rigidbody2D.linearVelocity;
			Fomoxa.Networking.Simulation.PhysicsBody2D body = BodySource != null ? BodySource () : default;
			if ( body.IsValid )
			{
				velocity = new Vector2 ( body.Velocity.X, body.Velocity.Y );
			}
			else if ( m_Rigidbody2D.bodyType == RigidbodyType2D.Kinematic && Time.deltaTime > 0f )
			{
				Vector2 moved = transform.position - m_LastPosition;
				velocity = moved.magnitude > m_TeleportDistance ? Vector2.zero : Vector2.ClampMagnitude ( moved / Time.deltaTime, m_MaxRunSpeed + m_JumpStrength );
			}
			m_LastPosition = transform.position;
			m_Speed = new Vector2 ( Mathf.Abs ( velocity.x ), Mathf.Abs ( velocity.y ) );
			m_Animator.SetFloat ( "Speed", m_Speed.x );
			m_Animator.SetFloat ( "VelocityX", m_Speed.x );
			m_Animator.SetFloat ( "VelocityY", velocity.y );
			m_Animator.SetBool ( "IsGrounded", m_GroundCheck.IsGrounded );
			m_Animator.SetBool ( "IsDead", IsDead.Value );
			m_Animator.SetBool ( "Block", m_Block );
			m_Animator.SetBool ( "Guard", m_Guard );
		}

		//		void OnCollisionEnter2D ( Collision2D collision2D )
		//		{
		//			bool isGround = collision2D.collider.CompareTag ( GroundCheck.GROUND_TAG );
		//			if ( isGround && !m_IsDead )
		//			{
		//				bool isBottom = false;
		//				for ( int i = 0; i < collision2D.contacts.Length; i++ )
		//				{
		//					if ( !isBottom )
		//					{
		//						isBottom = collision2D.contacts [ i ].normal.y == 1;
		//					}
		//					else
		//					{
		//						break;
		//					}
		//				}
		//				if ( isBottom )
		//				{
		//					m_JumpParticleSystem.Play ();
		//				}
		//			}
		//		}

		#endregion

		#region Private Methods

		IEnumerator CloseEye ()
		{
			m_ClosingEye = true;
			yield return new WaitForSeconds ( 0.6f );
			while ( m_Skeleton.RightEye.localScale.y > 0f )
			{
				if ( m_Skeleton.RightEye.localScale.y > 0f )
				{
					Vector3 scale = m_Skeleton.RightEye.localScale;
					scale.y -= 0.1f;
					m_Skeleton.RightEye.localScale = scale;
				}
				if ( m_Skeleton.LeftEye.localScale.y > 0f )
				{
					Vector3 scale = m_Skeleton.LeftEye.localScale;
					scale.y -= 0.1f;
					m_Skeleton.LeftEye.localScale = scale;
				}
				yield return new WaitForSeconds ( 0.05f );
			}
		}

		#endregion

		#region Public Methods

		public virtual void PlayFootstepSound ()
		{
			if ( m_GroundCheck.IsGrounded )
			{
				AudioManager.Singleton.PlayFootstepSound ( m_FootstepAudioSource, ref m_CurrentFootstepSoundIndex );
			}
		}

		public override void Move ( float horizontalAxis )
		{
			if ( !IsDead.Value )
			{
				float speed = m_CurrentRunSpeed;
//				if ( CrossPlatformInputManager.GetButton ( "Walk" ) )
//				{
//					speed = m_WalkSpeed;
				//				}
				Vector2 velocity = LinearVelocity;
				velocity.x = speed * horizontalAxis;
				LinearVelocity = velocity;
				if ( horizontalAxis > 0f )
				{
					Vector3 scale = transform.localScale;
					scale.x = Mathf.Sign ( horizontalAxis );
					transform.localScale = scale;
				}
				else if ( horizontalAxis < 0f )
				{
					Vector3 scale = transform.localScale;
					scale.x = Mathf.Sign ( horizontalAxis );
					transform.localScale = scale;
				}
			}
		}

		public virtual void Simulate ( float horizontalAxis, bool jump, bool roll, float deltaTime, bool effects )
		{
			Fomoxa.Networking.Simulation.PhysicsBody2D body = BodySource != null ? BodySource () : default;
			if ( !body.IsValid )
			{
				return;
			}
			RedRunner.Simulation.RunnerActions actions = m_Motion.Simulate ( body, Settings, horizontalAxis, jump, roll, jump && m_GroundCheck.Check (), IsDead.Value, deltaTime );
			if ( !IsDead.Value )
			{
				Vector3 scale = transform.localScale;
				scale.x = m_Motion.Facing;
				transform.localScale = scale;
			}
			if ( effects && ( actions & RedRunner.Simulation.RunnerActions.Jumped ) != 0 )
			{
				m_Animator.ResetTrigger ( "Jump" );
				m_Animator.SetTrigger ( "Jump" );
				m_JumpParticleSystem.Play ();
				AudioManager.Singleton.PlayJumpSound ( m_JumpAndGroundedAudioSource );
			}
			if ( effects && ( actions & RedRunner.Simulation.RunnerActions.Rolled ) != 0 )
			{
				m_Animator.SetTrigger ( "Roll" );
			}
		}

		public override void Jump ()
		{
			Jump ( true );
		}

		public virtual void Jump ( bool effects )
		{
			if ( !IsDead.Value )
			{
				if ( m_GroundCheck.Check () )
				{
					Vector2 velocity = LinearVelocity;
					velocity.y = m_JumpStrength;
					LinearVelocity = velocity;
					if ( effects )
					{
						m_Animator.ResetTrigger ( "Jump" );
						m_Animator.SetTrigger ( "Jump" );
						m_JumpParticleSystem.Play ();
						AudioManager.Singleton.PlayJumpSound ( m_JumpAndGroundedAudioSource );
					}
				}
			}
		}

		public virtual void Roll ( bool effects )
		{
			if ( !IsDead.Value )
			{
				Fomoxa.Networking.Simulation.PhysicsBody2D rolled = BodySource != null ? BodySource () : default;
				if ( rolled.IsValid )
				{
					rolled.AddForce ( new System.Numerics.Vector2 ( Mathf.Sign ( transform.localScale.x ) * m_RollForce, 0f ) );
				}
				else
				{
					m_Rigidbody2D.AddForce ( new Vector2 ( Mathf.Sign ( transform.localScale.x ) * m_RollForce, 0f ) );
				}
				if ( effects )
				{
					m_Animator.SetTrigger ( "Roll" );
				}
			}
		}

		public override void Die ()
		{
			Die ( DeathCause.Fall );
		}

		public override void Die ( DeathCause cause )
		{
			if ( !IsDead.Value )
			{
				DeathCause = cause;
                IsDead.Value = true;
				m_Skeleton.SetActive ( true, m_Rigidbody2D.linearVelocity );
				if ( cause == DeathCause.Saw || cause == DeathCause.Spike || cause == DeathCause.Mace )
				{
					ParticleSystem particle = Instantiate<ParticleSystem> (
						                          m_BloodParticleSystem,
						                          transform.position,
						                          Quaternion.identity );
					Destroy ( particle.gameObject, particle.main.duration );
				}
				if ( cause == DeathCause.Spike || cause == DeathCause.Mace )
				{
					AudioManager.Singleton.PlaySpikeSound ( transform.position );
				}
				if ( cause == DeathCause.Water )
				{
					Vector3 spawnPosition = transform.position;
					spawnPosition.y += -1f;
					ParticleSystem particle = Instantiate<ParticleSystem> ( m_WaterParticleSystem, spawnPosition, Quaternion.identity );
					Destroy ( particle.gameObject, particle.main.duration );
					AudioManager.Singleton.PlayWaterSplashSound ( transform.position );
				}
				if ( CameraController.Singleton != null && CameraController.Singleton.Followee == transform )
				{
					CameraController.Singleton.fastMove = true;
				}
			}
		}

		public override void EmitRunParticle ()
		{
			if ( !IsDead.Value )
			{
				m_RunParticleSystem.Emit ( 1 );
			}
		}

		public override void Reset ()
		{
            IsDead.Value = false;
			m_ClosingEye = false;
			m_Guard = false;
			m_Block = false;
			m_CurrentFootstepSoundIndex = 0;
			transform.localScale = m_InitialScale;
			m_Motion.Facing = m_InitialScale.x < 0f ? -1 : 1;
			LinearVelocity = Vector2.zero;
			m_Skeleton.SetActive ( false, m_Rigidbody2D.linearVelocity );
		}

		#endregion

		#region Events

		void GameManager_OnReset ()
		{
			transform.position = m_InitialPosition;
			Reset ();
		}

		void Skeleton_OnActiveChanged ( bool active )
		{
			m_Animator.enabled = !active;
			m_Collider2D.enabled = !active;
			m_Rigidbody2D.simulated = !active;
		}

		void GroundCheck_OnGrounded ()
		{
			if ( !IsDead.Value )
			{
				m_JumpParticleSystem.Play ();
				AudioManager.Singleton.PlayGroundedSound ( m_JumpAndGroundedAudioSource );
			}
		}

		#endregion

		[System.Serializable]
		public class CharacterDeadEvent : UnityEvent
		{

		}

	}

}
