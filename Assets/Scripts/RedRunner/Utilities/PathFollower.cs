using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RedRunner.Characters;

namespace RedRunner.Utilities
{

	public class PathFollower : MonoBehaviour
	{

		[SerializeField]
		protected PathDefinition m_PathDefinition;
		[SerializeField]
		protected float m_Step = 0.1f;
		[SerializeField]
		protected bool m_FollowSpeeds = true;
		[SerializeField]
		protected float m_Speed = 1f;
		[SerializeField]
		protected bool m_FollowDelays = true;
		[SerializeField]
		protected float m_Delay = 0f;
		[SerializeField]
		protected bool m_Smart = false;
		[SerializeField]
		protected Vector3 m_RangeSize = new Vector3 (4f, 4f, 4f);
		[SerializeField]
		protected Vector3 m_RangeOffset = new Vector3 (0f, 0f, 0f);

		protected bool m_Stopped = false;
		protected IEnumerator<PathPoint> m_CurrentPoint;
		protected bool m_IsMovingNext = false;
		protected Vector3 m_LastPosition;
		protected Vector3 m_Velocity;
		protected Vector3 m_SmoothVelocity;
		protected float m_OverTimeSpeed = 0f;
		protected List<System.Numerics.Vector3> m_Cycle;
		protected float m_CycleTickDelta;
		protected Rigidbody2D m_Rigidbody2D;
		protected bool m_Driven = false;

		public virtual bool Stopped {
			get {
				return m_Stopped;
			}
			set {
				m_Stopped = value;
			}
		}

		public virtual Vector3 Velocity {
			get {
				return m_Velocity;
			}
		}

		public virtual int CycleLength {
			get {
				return m_Cycle == null ? 0 : m_Cycle.Count;
			}
		}

		void Awake ()
		{
			m_Stopped = m_Smart;
		}

		void Start ()
		{
			if (m_PathDefinition == null) {
				return;
			}

			m_CurrentPoint = m_PathDefinition.GetPathEnumerator ();
			m_CurrentPoint.MoveNext ();

			if (m_CurrentPoint.Current == null)
				return;

			transform.position = m_CurrentPoint.Current.transform.position;
			StartCoroutine (CalcVelocity ());
		}

		void OnDrawGizmos ()
		{
			if (m_Smart) {
				Gizmos.DrawWireCube (transform.position + m_RangeOffset, m_RangeSize);
			}
		}

		void Update ()
		{
			if (m_Driven) {
				return;
			}
			if (m_Smart) {
				Collider2D[] colliders = Physics2D.OverlapBoxAll (transform.position + m_RangeOffset, m_RangeSize, 0f, LayerMask.GetMask ("Characters"));
				for (int i = 0; i < colliders.Length; i++) {
					Character character = colliders [i].GetComponent<Character> ();
					if (character != null) {
						m_Stopped = false;
					}
				}
			} else {
				m_Stopped = false;
			}

			if (m_CurrentPoint == null || m_CurrentPoint.Current == null || m_Stopped || !GameManager.Singleton.gameRunning) {
				return;
			}

			float speed = Time.deltaTime * m_CurrentPoint.Current.speed;
			if (m_CurrentPoint.Current.moveType == PathPoint.MoveType.MoveTowards) {
				transform.position = Vector3.MoveTowards (transform.position, m_CurrentPoint.Current.transform.position, speed);
			} else if (m_CurrentPoint.Current.moveType == PathPoint.MoveType.Lerp) {
				transform.position = Vector3.Lerp (transform.position, m_CurrentPoint.Current.transform.position, speed);
			} else if (m_CurrentPoint.Current.moveType == PathPoint.MoveType.SmoothDamp) {
				transform.position = Vector3.SmoothDamp (transform.position, m_CurrentPoint.Current.transform.position, ref m_SmoothVelocity, m_CurrentPoint.Current.smoothTime);
			} else if (m_CurrentPoint.Current.moveType == PathPoint.MoveType.Acceleration) {
				Vector3 direction = (m_CurrentPoint.Current.transform.position - transform.position).normalized;
				transform.position = Vector3.MoveTowards (transform.position, m_CurrentPoint.Current.transform.position, m_OverTimeSpeed);
				m_OverTimeSpeed += m_CurrentPoint.Current.acceleration;
				if (m_OverTimeSpeed > m_CurrentPoint.Current.maxSpeed) {
					m_OverTimeSpeed = m_CurrentPoint.Current.maxSpeed;
				}
			}

			var distanceSquared = (transform.position - m_CurrentPoint.Current.transform.position).sqrMagnitude;
			if (distanceSquared < m_Step * m_Step) {
				m_Stopped = true;
				if (!m_IsMovingNext) {
					StartCoroutine (MoveNext ());
				}
			}
		}

		public virtual Vector3 PositionAt (uint tick)
		{
			System.Numerics.Vector3 position = RedRunner.Simulation.PathCycle.PositionAt (m_Cycle, tick);
			return new Vector3 (position.X, position.Y, position.Z);
		}

		public virtual void FollowTick (uint tick, float tickDelta)
		{
			if (m_PathDefinition == null) {
				return;
			}
			if (m_Cycle == null || m_CycleTickDelta != tickDelta) {
				BuildCycle (tickDelta);
			}
			Vector3 position = PositionAt (tick);
			m_Velocity = (PositionAt (tick - 1) - position) / tickDelta;
			if (!m_Driven) {
				m_Driven = true;
				StopAllCoroutines ();
				m_Rigidbody2D = GetComponent<Rigidbody2D> ();
				transform.position = position;
				if (m_Rigidbody2D != null) {
					m_Rigidbody2D.position = position;
				}
			} else if (m_Rigidbody2D != null) {
				m_Rigidbody2D.MovePosition (position);
			} else {
				transform.position = position;
			}
		}

		protected virtual void BuildCycle (float tickDelta)
		{
			m_CycleTickDelta = tickDelta;
			var stops = new List<RedRunner.Simulation.PathStop> ();
			var points = new List<PathPoint> ();
			foreach (PathPoint point in m_PathDefinition.Points) {
				if (point == null) {
					break;
				}
				Vector3 position = point.transform.position;
				stops.Add (new RedRunner.Simulation.PathStop (new System.Numerics.Vector3 (position.x, position.y, position.z), (RedRunner.Simulation.PathMove)point.moveType, point.speed, point.smoothTime, point.acceleration, point.maxSpeed, point.delay));
				points.Add (point);
			}
			if (stops.Count == 0) {
				Vector3 here = transform.position;
				m_Cycle = new List<System.Numerics.Vector3> { new System.Numerics.Vector3 (here.x, here.y, here.z) };
				return;
			}
			m_Cycle = RedRunner.Simulation.PathCycle.Build (stops, m_PathDefinition.ContinueToStart, m_Step, tickDelta, index => DelayAt (points [index]));
		}

		protected virtual float DelayAt (PathPoint point)
		{
			if (m_FollowDelays && m_PathDefinition.UseGlobalDelay) {
				return m_PathDefinition.GlobalDelay;
			}
			if (!m_FollowDelays) {
				return m_Delay;
			}
			return point.delay;
		}

		IEnumerator CalcVelocity ()
		{
			while (Application.isPlaying) {
				m_LastPosition = transform.position;
				yield return new WaitForEndOfFrame ();
				m_Velocity = (m_LastPosition - transform.position) / Time.deltaTime;
			}
		}

		IEnumerator MoveNext ()
		{
			m_IsMovingNext = true;
			float delay = m_CurrentPoint.Current.delay;
			if (m_FollowDelays && m_PathDefinition.UseGlobalDelay) {
				delay = m_PathDefinition.GlobalDelay;
			} else if (!m_FollowDelays) {
				delay = m_Delay;
			}
			yield return new WaitForSeconds (delay);
			m_OverTimeSpeed = 0f;
			m_CurrentPoint.MoveNext ();
			m_IsMovingNext = false;
		}

	}

}