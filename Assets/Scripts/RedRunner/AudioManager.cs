using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedRunner
{

	public class AudioManager : MonoBehaviour
	{

		#region Singleton

		private static AudioManager m_Singleton;

		public static AudioManager Singleton {
			get {
				return m_Singleton;
			}
		}

		#endregion

		#region Fields

		[Header ("Audio Sources")]
		[Space]
		[SerializeField]
		protected AudioSource m_MusicAudioSource;
		[SerializeField]
		protected AudioSource m_SoundAudioSource;
		[SerializeField]
		protected AudioSource m_CoinAudioSource;
		[SerializeField]
		protected AudioSource m_DieAudioSource;
		[SerializeField]
		protected AudioSource m_MaceSlamAudioSource;
		[SerializeField]
		protected AudioSource m_UIAudioSource;

		[Header ("Music Clips")]
		[Space]
		[SerializeField]
		protected AudioClip m_MusicClip;

		[Header ("Sound Clips")]
		[Space]
		[SerializeField]
		protected AudioClip m_CoinSound;
		[SerializeField]
		protected AudioClip m_ChestSound;
		[SerializeField]
		protected AudioClip m_WaterSplashSound;
		[SerializeField]
		protected AudioClip m_SpikeSound;
		[SerializeField]
		protected AudioClip[] m_GroundedSounds;
		[SerializeField]
		protected AudioClip m_JumpSound;
		[SerializeField]
		protected AudioClip[] m_FootstepSounds;
		[SerializeField]
		protected AudioClip m_MaceSlamSound;
		[SerializeField]
		protected AudioClip m_ButtonClickSound;

		[Header ("Hearing")]
		[Space]
		[SerializeField]
		protected float m_HearingDistance = 15f;

		#endregion

		#region MonoBehaviour Messages

		void Awake ()
		{
			m_Singleton = this;
			PlayMusic ();
		}

		#endregion

		#region Methods

		public void PlayMusic ()
		{
			m_MusicAudioSource.clip = m_MusicClip;
			m_MusicAudioSource.Play ();
		}

		public void PlaySoundAt (AudioClip clip, Vector3 position, float volume)
		{
			AudioSource.PlayClipAtPoint (clip, position, volume);
		}

		public void PlaySoundOn (AudioSource audio, AudioClip clip)
		{
			audio.clip = clip;
			audio.Play ();
		}

		public void PlaySoundOn (AudioSource audio, AudioClip clip, Vector3 position)
		{
			float volume = VolumeAt (position);
			if (volume > 0f && clip != null) {
				audio.PlayOneShot (clip, volume);
			}
		}

		public float VolumeAt (Vector3 position)
		{
			Camera listener = Camera.main;
			if (listener == null || m_HearingDistance <= 0f) {
				return 1f;
			}
			Vector2 offset = position - listener.transform.position;
			return Mathf.Clamp01 (1f - offset.magnitude / m_HearingDistance);
		}

		public void PlayChestSound (Vector3 position)
		{
			PlaySoundOn (m_CoinAudioSource, m_ChestSound, position);
		}

		public void PlayCoinSound (Vector3 position)
		{
			PlaySoundOn (m_CoinAudioSource, m_CoinSound, position);
		}

		public void PlayWaterSplashSound (Vector3 position)
		{
			PlaySoundOn (m_DieAudioSource, m_WaterSplashSound, position);
		}

		public void PlayMaceSlamSound (Vector3 position)
		{
			PlaySoundOn (m_MaceSlamAudioSource, m_MaceSlamSound, position);
		}

		public void PlaySpikeSound (Vector3 position)
		{
			PlaySoundOn (m_DieAudioSource, m_SpikeSound, position);
		}

		public void PlayGroundedSound (AudioSource audio)
		{
			if (m_GroundedSounds.Length > 0) {
				PlaySoundOn (audio, GetRandomClip (m_GroundedSounds), audio.transform.position);
			}
		}

		public void PlayJumpSound (AudioSource audio)
		{
			PlaySoundOn (audio, m_JumpSound, audio.transform.position);
		}

		public void PlayFootstepSound (AudioSource audio)
		{
			if (m_FootstepSounds.Length > 0) {
				PlaySoundOn (audio, GetRandomClip (m_FootstepSounds), audio.transform.position);
			}
		}

		public void PlayFootstepSound (AudioSource audio, ref int index)
		{
			if (m_FootstepSounds.Length > 0) {
				PlaySoundOn (audio, m_FootstepSounds [index], audio.transform.position);
				if (index < m_FootstepSounds.Length - 1) {
					index++;
				} else {
					index = 0;
				}
			}
		}

		public void PlayClickSound ()
		{
			PlaySoundOn (m_UIAudioSource, m_ButtonClickSound);
		}

		public AudioClip GetRandomClip (AudioClip[] clips)
		{
			if (clips.Length > 0) {
				return clips [Random.Range (0, clips.Length)];
			}
			return null;
		}

		#endregion

	}

}