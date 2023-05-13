using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using FistVR;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace PlayerFootsteps
{
    [BepInPlugin("h3vr.kodeman.playerfootsteps", "Player Footsteps", "1.0.0")]
    [BepInProcess("h3vr.exe")]
    public class PlayerFootsteps : BaseUnityPlugin
    {
        private ConfigEntry<bool> AIDetectsSounds;
        private ConfigEntry<bool> MeatyFeet;
        private ConfigEntry<bool> QuietWalking;
        private ConfigEntry<float> AIDetectionSoundsMultiplier;
        private ConfigEntry<float> PlayerHeight;
        private ConfigEntry<float> PlayerCrouchHeight;
        private ConfigEntry<float> SoundVolume;
        private ConfigEntry<float> QuietWalkingSpeed;
        
        private Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();

        private Vector3 _lastPlayerPos;
        private AudioSource _audioSource;

        private bool _initialized;
        private float _lastHitDistFromHead;
        private float _stepTimer;

        // Prefab needed to access meaty footsteps sounds
        private Sosig _sosigPrefab;

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.dopplerLevel = 0;
            _audioSource.playOnAwake = false;

            DontDestroyOnLoad(gameObject);
            
            AIDetectsSounds = Config.Bind("FootstepsSounds", "AIDetectsSounds", true, "If enabled, AI will detect player footsteps based on ground material and the player speed.");
            MeatyFeet = Config.Bind("FootstepsSounds", "MeatyFeet", false, "If enabled, replaces footsteps sounds with meaty footstep sounds.");
            AIDetectionSoundsMultiplier = Config.Bind("FootstepsSounds", "AIDetectionSoundsMultiplier", 1f, "Multiplier for AI detection range based on player speed.");
            PlayerHeight = Config.Bind("FootstepsSounds", "PlayerHeight", 1.8f, "Player height in meters. If head is higher than this value, the player is considered flying and will not produce footsteps sounds.");
            PlayerCrouchHeight = Config.Bind("FootstepsSounds", "PlayerCrouchHeight", 1.25f, "If player head is lower than this value, the player is considered crouching and will not be detected by AI.");
            SoundVolume = Config.Bind("FootstepsSounds", "SoundVolume", 1f, "Volume of footsteps sounds. Value range is 0-1");
            QuietWalking = Config.Bind("FootstepsSounds", "QuietWalking", false, "If enabled, player will not alert sosigs when walking slowly without crouching.");
            QuietWalkingSpeed = Config.Bind("FootstepsSounds", "QuietWalkingSpeed", 2f, "If QuietWalking is enabled, walking slower than this value will not alert sosigs.");
            
            SoundVolume.Value = Mathf.Clamp(SoundVolume.Value, 0f, 1f);
            
            Logger.LogInfo("Player Footsteps mod loaded");
        }

        private IEnumerator Start()
        {
            // Make sure everything is initialized
            yield return new WaitForSeconds(1f);
            
            _lastPlayerPos = GM.CurrentPlayerBody.Head.transform.position;

            LoadAudioClips();
            
            if (MeatyFeet.Value)
                _sosigPrefab = IM.Instance.odicSosigObjsByID[SosigEnemyID.M_Swat_Heavy].SosigPrefabs[0].GetGameObject().GetComponent<Sosig>();

            _initialized = true;
        }
        
        private void LoadAudioClips()
        {
            _audioClips.Clear();

            _audioClips["Concrete.wav"] = null;
            _audioClips["Grass.wav"] = null;
            _audioClips["Gravel.wav"] = null;
            _audioClips["Metal.wav"] = null;
            _audioClips["Mud.wav"] = null;
            _audioClips["SoftMaterial.wav"] = null;
            _audioClips["Water.wav"] = null;
            _audioClips["Wood.wav"] = null;
            _audioClips["Sand.wav"] = null;
            _audioClips["Brick.wav"] = null;
            _audioClips["Rock.wav"] = null;
            _audioClips["Ice.wav"] = null;
            _audioClips["Glass.wav"] = null;
            _audioClips["SnowNorthestDakota.wav"] = null;
            
            string pathToSounds = Paths.PluginPath + @"\Kodeman-PlayerFootsteps\";
            
            foreach(KeyValuePair<string, AudioClip> clip in _audioClips)
            {
                StartCoroutine(GetAudioClip(pathToSounds + clip.Key, clip.Key));
            }
        }

        private void Update()
        {
            if (!_initialized)
                return;
            
            // Throttle footsteps, otherwise it will sound like player has very short legs.
            _stepTimer += Time.deltaTime;
            if (_stepTimer < .23f)
                return;
            if (MeatyFeet.Value && _stepTimer < 0.3)
                return;
            
            // Ignore Y position when calculating distance
            Vector3 playerPos = GM.CurrentPlayerBody.Head.transform.position;
            playerPos.y = 0;
            
            Vector3 lastPlayerPos = _lastPlayerPos;
            lastPlayerPos.y = 0;

            if (Vector3.Distance(lastPlayerPos, playerPos) > 1f)
            {
                _lastPlayerPos = playerPos;

                if (Physics.Raycast(GM.CurrentPlayerBody.Head.position, Vector3.down, out RaycastHit hit, PlayerHeight.Value, 1 << 19 | 1 << 12)) // environment layer and navblock
                {
                    _stepTimer = 0;
                    _lastHitDistFromHead = hit.distance;
                    
                    if (hit.collider.GetComponent<PMat>())
                    {
                        BulletImpactSoundType soundType = hit.collider.GetComponent<PMat>().MatDef.BulletImpactSound;
                        PlayFootstepSound(soundType);
                    }
                    else // Play generic sound if no PMat is found
                    {
                        PlayFootstepSound(BulletImpactSoundType.Generic, true);
                    }
                }
            }
        }
	
        private void PlayFootstepSound(BulletImpactSoundType soundType, bool noPMat = false)
        {
            float playerSpeed = GM.CurrentMovementManager.GetTopSpeedInLastSecond();
            float speedVolumeAdd = RemapClamped(playerSpeed, 3f, 7f, 0f, 0.35f);

            float volume = Random.Range(0.5f, 0.7f) + speedVolumeAdd;
            float pitch = Random.Range(0.75f, 1.15f);

            // Maximize volume if player is running
            if (playerSpeed > 4.25f)
                volume = 1;
            
            bool isPlayerWalkingSlowly = QuietWalking.Value && playerSpeed < QuietWalkingSpeed.Value;
            
            // Reduce volume if player is crouching
            if (_lastHitDistFromHead <= PlayerCrouchHeight.Value)
                volume -= 0.4f;
            // Reduce volume if player is walking slowly
            else if (isPlayerWalkingSlowly)
                volume -= 0.25f;

            volume *= SoundVolume.Value;

            _audioSource.volume = volume;
            _audioSource.pitch = pitch;
            
            HandleAIDetection();

            // Replace footsteps sounds with meaty footsteps sounds
            if (MeatyFeet.Value)
            {
                _audioSource.pitch = Random.Range(0.95f, 1.05f);
                _audioSource.volume *= 0.6f;
                
                int random = Random.Range(0, _sosigPrefab.AudEvent_FootSteps.Clips.Count);
                _audioSource.PlayOneShot(_sosigPrefab.AudEvent_FootSteps.Clips[random]);
 
                return;
            }
            
            if (noPMat && GM.TNH_Manager && GM.TNH_Manager.LevelName == "NorthestDakota")
            {
                _audioSource.PlayOneShot(_audioClips["SnowNorthestDakota.wav"]);
                return;
            }

            switch (soundType)
            {
                case BulletImpactSoundType.Grass:
                    _audioSource.PlayOneShot(_audioClips["Grass.wav"]);
                    break;
                case BulletImpactSoundType.WoodHeavy:
                case BulletImpactSoundType.WoodLight:
                case BulletImpactSoundType.WoodProp:
                    _audioSource.PlayOneShot(_audioClips["Wood.wav"]);
                    break;
                case BulletImpactSoundType.Gravel:
                    _audioSource.PlayOneShot(_audioClips["Gravel.wav"]);
                    break;
                case BulletImpactSoundType.Mud:
                case BulletImpactSoundType.Meat:
                    _audioSource.PlayOneShot(_audioClips["Mud.wav"]);
                    break;
                case BulletImpactSoundType.Water:
                    _audioSource.PlayOneShot(_audioClips["Water.wav"]);
                    break;
                case BulletImpactSoundType.SoftMaterial:
                    _audioSource.PlayOneShot(_audioClips["SoftMaterial.wav"]);
                    break;
                case BulletImpactSoundType.Sand:
                case BulletImpactSoundType.Sandbag:
                    _audioSource.PlayOneShot(_audioClips["Sand.wav"]);
                    break;
                case BulletImpactSoundType.MetalRicochetee:
                case BulletImpactSoundType.MetalThick:
                case BulletImpactSoundType.MetalThin:
                case BulletImpactSoundType.ArmorHard:
                case BulletImpactSoundType.ArmorSoft:
                    _audioSource.PlayOneShot(_audioClips["Metal.wav"]);
                    break;
                case BulletImpactSoundType.Brick:
                    _audioSource.PlayOneShot(_audioClips["Brick.wav"]);
                    break;
                case BulletImpactSoundType.Rock:
                    _audioSource.PlayOneShot(_audioClips["Rock.wav"]);
                    break;
                case BulletImpactSoundType.Ice:
                    _audioSource.PlayOneShot(_audioClips["Ice.wav"]);
                    break;
                case BulletImpactSoundType.Glass:
                case BulletImpactSoundType.GlassBulletProof:
                case BulletImpactSoundType.GlassShattery:
                case BulletImpactSoundType.GlassWindshield:
                    _audioSource.PlayOneShot(_audioClips["Glass.wav"]);
                    break;
                case BulletImpactSoundType.None:
                    break;
                case BulletImpactSoundType.Concrete:
                case BulletImpactSoundType.Generic:
                case BulletImpactSoundType.Plaster:
                case BulletImpactSoundType.Plastic:
                case BulletImpactSoundType.ZWhooshes:
                default:
                    _audioSource.PlayOneShot(_audioClips["Concrete.wav"]);
                    break;
            }
        }

        private void HandleAIDetection()
        {
            if (!AIDetectsSounds.Value)
                return;
            
            float playerSpeed = GM.CurrentMovementManager.GetTopSpeedInLastSecond();
            
            bool isPlayerRunning = playerSpeed > 4.25f;
            bool isPlayerCrouching = _lastHitDistFromHead <= 1.25f;
            bool isPlayerWalkingSlowly = QuietWalking.Value && playerSpeed < QuietWalkingSpeed.Value;
            
            Vector3 playerPos = GM.CurrentPlayerBody.Head.transform.position;
            int playerIff = GM.CurrentPlayerBody.GetPlayerIFF();

            // If player is running, increase the loudness of the sound
            // If player is crouching or slow, disable the sound
            // If player is running while crouched, make sound as if player was walking
            float maxDistanceHeard = 5;
            if (isPlayerRunning && !isPlayerCrouching)
            {
                maxDistanceHeard = 10;
            }

            maxDistanceHeard *= AIDetectionSoundsMultiplier.Value;

            
            if ((!isPlayerCrouching || isPlayerRunning) && !isPlayerWalkingSlowly && GM.CurrentAIManager)
                GM.CurrentAIManager.SonicEvent(GM.CurrentSceneSettings.BaseLoudness, maxDistanceHeard, playerPos, playerIff);
        }

        private IEnumerator GetAudioClip(string path, string clipName)
        {
            using (UnityWebRequest www =
                   UnityWebRequest.GetAudioClip(path, AudioType.WAV))
            {
                yield return www.Send();
            
                if (www.isError)
                {
                    Debug.Log("There was an error loading the audio clip for path: " + path + " \n " + www.error);
                }
                else
                {
                    _audioClips[clipName] = DownloadHandlerAudioClip.GetContent(www);
                }
            }
        }

        // For example input: 1 - 10 and output 0.1f - 1.0f, You enter x=5 and receive 0.5f
        private float RemapClamped(float aValue, float aIn1, float aIn2, float aOut1, float aOut2)
        {
            float t = (aValue - aIn1) / (aIn2 - aIn1);
            if (t > 1f)
                return aOut2;
            if(t < 0f)
                return aOut1;
            return aOut1 + (aOut2 - aOut1) * t;
        }
    }
}