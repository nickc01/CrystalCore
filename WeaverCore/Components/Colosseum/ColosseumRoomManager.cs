using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using WeaverCore;
using WeaverCore.Attributes;
using WeaverCore.Components;
using WeaverCore.Interfaces;
using WeaverCore.Internal;
using WeaverCore.Settings;
using WeaverCore.Utilities;

namespace WeaverCore.Components.Colosseum
{
    public class ColosseumRoomManager : MonoBehaviour, IColosseumIdentifier
    {
        #if UNITY_EDITOR
        [field: SerializeField, Tooltip("Placeholder text mesh used for preloading in the editor.")]
        public TextMeshPro PreloadPlaceHolder;
        #endif

        [HideInInspector]
        [SerializeField]
        bool _spawnLocationSpriteLabelsInternal = true;

        [Space]
        [Header("Labels")]
        [Tooltip("Enable or disable spawn location sprite labels.")]
        public bool SpawnLocationSpriteLabels = true;

        [Tooltip("Enable or disable spawn location labels.")]
        public bool SpawnLocationLabels = true;

        [Tooltip("Enable or disable enemy spawner labels.")]
        public bool EnemySpawnerLabels = true;

        [Tooltip("Enable or disable spike labels.")]
        public bool SpikeLabels = true;

        [Tooltip("Enable or disable enemy spawn point labels.")]
        public bool EnemySpawnPointsLabels = true;

        [Tooltip("Displays colors in the hiearchy view")]
        public bool ColorLabels = true;

        [Space]
        [Header("General")]
        [Tooltip("List of GameObject prefabs to use in the challenge.")]
        public List<GameObject> enemyPrefabs = new List<GameObject>();

        [Tooltip("List of enemy preloads used for the challenge.")]
        public List<ColosseumEnemyPreloads> preloadedEnemies = new List<ColosseumEnemyPreloads>();

        [Tooltip("List of spawn locations for the challenge.")]
        public List<ColosseumEnemySpawner> spawnLocations = new List<ColosseumEnemySpawner>();

        [SerializeField, Tooltip("Gates that players can exit. These gates will be closed when the Challenge Room Manager loads.")]
        List<ColosseumGate> exitGates = new List<ColosseumGate>();

        [SerializeField, Tooltip("Save-specific settings for the room.")]
        SaveSpecificSettings saveSettings;

        [SerializeField, SaveSpecificFieldName(typeof(bool), nameof(saveSettings))]
        [Tooltip("Field name for tracking whether the challenge is completed.")]
        string challengeCompletedFieldName;

        [SerializeField, Tooltip("Sound played when the challenge starts.")]
        private AudioClip challengeSound;

        [SerializeField, Tooltip("Delay before playing the challenge start sound.")]
        private float challengeSoundDelay = 0.25f;

        [SerializeField, Tooltip("Cutscene to play at the beginning of the challenge.")]
        private Wave beginningCutscene;

        [SerializeField, Tooltip("Cutscene to play at the end of the challenge.")]
        private Wave endCutscene;

        [SerializeField, Tooltip("Enable or disable stopping music during the challenge.")]
        private bool stopMusic = true;

        [SerializeField, Tooltip("Silent music cue to apply when stopping music.")]
        private MusicCue silentCue;

        [SerializeField, Tooltip("Type of music snapshot to apply when stopping music.")]
        private Music.SnapshotType stopMusicSnapshot = Music.SnapshotType.Silent;

        [SerializeField, Tooltip("Duration of the music snapshot transition.")]
        private float stopMusicDuration = 0.25f;

        [SerializeField, Tooltip("Wait for all enemies to die before proceeding.")]
        private bool waitForAllEnemiesToDie = true;

        [HideInInspector]
        [SerializeField, Tooltip("Position range for ending the cutscene.")]
        private Rect endingCutscenePosRange;

        [SerializeField, Tooltip("Sound played when the challenge is completed.")]
        private AudioClip challengeCompleteSound;

        [SerializeField, Tooltip("Delay before playing the challenge completion sound.")]
        private float challengeCompleteSoundDelay = 0f;

        [SerializeField, Tooltip("Music cue to play during the challenge.")]
        private WeaverMusicCue challengeMusic;

        [SerializeField, Tooltip("Delay before starting the challenge music.")]
        private float challengeMusicDelay = 0.5f;

        [Tooltip("Event triggered when the challenge starts.")]
        public UnityEvent onChallengeStart;

        [Tooltip("Event triggered when the challenge ends.")]
        public UnityEvent onChallengeEnd;

        private List<Wave> waves = new List<Wave>();
        private int currentWaveIndex = 0;
        private bool challengeActive = false;

        string IColosseumIdentifier.Identifier => "Colosseum Room Managers";

        Color IColosseumIdentifier.Color => Color.green;

        public bool ShowShortcut => false;

        void Start()
        {
            //WeaverLog.Log("Awake");
            if (saveSettings.TryGetFieldValue<bool>(challengeCompletedFieldName, out var result))
            {
                if (result)
                {
                    foreach (var gate in exitGates)
                    {
                        if (gate != null)
                        {
                            //WeaverLog.Log("OPENING GATE = " + gate);
                            gate.Open();
                        }
                    }
                }
                else
                {
                    WeaverLog.Log("PLAYING CUE DELAYED");
                    StartCoroutine(PlaySilentCueDelayed());
                    foreach (var gate in exitGates)
                    {
                        if (gate != null)
                        {
                            //WeaverLog.Log("CLOSING GATE = " + gate);
                            gate.Close();
                        }
                    }
                }
            }
        }

        IEnumerator PlaySilentCueDelayed()
        {
            yield return null;
            yield return null;
            Music.PlayMusicCue(silentCue,0f,0f);
        }

        public void PlayMusic()
        {
            if (challengeMusic != null)
            {
                Music.PlayMusicCue(challengeMusic, challengeMusicDelay);
            }
        }

        //void Start()
        //{
            // Initialize waves
            /*foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf)
                {
                    EnemyWave wave = child.GetComponent<EnemyWave>();
                    if (wave != null)
                    {
                        waves.Add(wave);
                    }
                }
            }*/
        //}

        public void StartChallenge()
        {
            if (!challengeActive)
            {
                challengeActive = true;
                onChallengeStart.Invoke();
                StartCoroutine(RunChallenge());
            }
        }

        private IEnumerator RunChallenge()
        {
            waves.Clear();
            waves.AddRange(GetComponentsInChildren<Wave>().Where(w => w.AutoRun).OrderBy(w => w.transform.GetSiblingIndex()));

            while (currentWaveIndex < waves.Count)
            {
                var wave = waves[currentWaveIndex];
                WeaverLog.Log("RUNNING WAVE = " + wave);
                yield return StartCoroutine(wave.RunWave(this));
                currentWaveIndex++;
            }
            saveSettings.TrySetFieldValue(challengeCompletedFieldName, true);
            onChallengeEnd.Invoke();
        }

        public void OnDrawGizmosSelected()
        {
            /*if (SpawnLocationLabels)
            {
                foreach (var spawnLocation in spawnLocations)
                {
                    if (spawnLocation != null)
                    {
                        Gizmos.color = spawnLocation.EditorColor;
                        Gizmos.DrawSphere(spawnLocation.transform.position, 0.5f);
                        Gizmos.DrawWireSphere(spawnLocation.transform.position, 0.5f);
                        Gizmos.DrawLine(transform.position, spawnLocation.transform.position);
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawCube(spawnLocation.transform.position, new Vector3(0.5f, 0.5f, 0.5f));
                    }
                }
            }*/
            
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.05f);
            var bl = transform.TransformPoint(endingCutscenePosRange.min);
            var tr = transform.TransformPoint(endingCutscenePosRange.max);
            var center = Vector3.Lerp(bl, tr, 0.5f);
            var size = tr - bl;
            Gizmos.DrawCube(center, size);
        }

        public void BeginInteraction()
        {
            StartCoroutine(InteractionRoutine());
        }

        IEnumerator InteractionRoutine()
        {
            PlayerData.instance.SetBool("disablePause", true);
            HeroController.instance.RelinquishControl();
            HeroController.instance.StopAnimationControl();
            //PlayMakerUtilities.SetFsmBool(HeroController.instance.gameObject, "Dream Nail", "Dream Convo", true);
            //GetComponent<Collider2D>().enabled = false;

            /*var heroX = Player.Player1.transform.position.x;
            var selfX = transform.position.x;
            if (heroX <= selfX)
            {
                //HERO ON LEFT
                HeroController.instance.FaceLeft();
            }
            else
            {
                //HERO ON RIGHT
                HeroController.instance.FaceRight();
            }*/

            if (challengeSound != null)
            {
                //WeaverAudio.PlayAtPoint(challengeSound, Player.Player1.transform.position);
                //StartCoroutine(PlaySoundRoutine(challengeSound, challengeSoundDelay));
                WeaverAudio.PlayAtPointDelayed(challengeSoundDelay, challengeSound, Player.Player1.transform.position);
            }

            HeroUtilities.PlayPlayerClip("Challenge Start");

            if (silentCue != null)
            {
                Music.PlayMusicCue(silentCue,0f,0f);   
            }

            yield return new WaitForSeconds(0.25f);

            if (beginningCutscene != null)
            {
                yield return beginningCutscene.RunWave(this);
            }

            PlayerData.instance.SetBool("disablePause", false);
            HeroController.instance.StartAnimationControl();
            HeroController.instance.RegainControl();

            StartChallenge();



            //DREAM ENTER EVENT

            //AUDIO PLAY SIMPLE??

            /*PlayerData.instance.SetString("dreamReturnScene", "GG_Workshop");

            if (GG_Preloads.dreamAreaEffect != null)
            {
                GameObject.Instantiate(GG_Preloads.dreamAreaEffect, Vector3.zero, Quaternion.identity);
            }

            foreach (Transform item in transform)
            {
                item.gameObject.SetActive(true);
            }

            EventManager.BroadcastEvent("BOX DOWN DREAM", gameObject);

            EventManager.BroadcastEvent("CONVO CANCEL", gameObject);*/
        }

        public void PlayEndingCutscene()
        {
            StartCoroutine(EndingCutsceneRoutine());
        }

        IEnumerator EndingCutsceneRoutine()
        {
            if (waitForAllEnemiesToDie)
            {
                var sceneManager = GameObject.FindObjectOfType<WeaverSceneManager>();

                Rect sceneBounds = default;

                if (sceneManager != null)
                {
                    sceneBounds = sceneManager.SceneDimensions;
                }

                Dictionary<MonoBehaviour, (Vector3, float)> lastPositions = new Dictionary<MonoBehaviour, (Vector3, float)>();

                /*foreach (var obj in GameObject.FindObjectsOfType<EntityHealth>())
                {
                    lastPositions.Add(obj, )
                }*/

                

                //lastPositions.Add(hComponent, (hComponent.transform.position, Time.time));

                bool IsAlive(MonoBehaviour e)
                {
                    bool isAlive = true;
                    if (e == null || e.gameObject == null)
                    {
                        isAlive = false;
                    }

                    if (isAlive && e.TryGetComponent<PoolableObject>(out var poolableObject))
                    {
                        isAlive = !poolableObject.InPool;
                    }

                    if (!lastPositions.ContainsKey(e))
                    {
                        lastPositions.Add(e, (e.transform.position, Time.time));
                    }

                    if (isAlive && lastPositions.TryGetValue(e, out var pair))
                    {
                        if (Vector2.Distance(e.transform.position, pair.Item1) > 0.1)
                        {
                            pair = (e.transform.position, Time.time);
                            lastPositions[e] = pair;
                        }

                        if (Time.time - pair.Item2 >= 3f)
                        {
                            isAlive = false;
                        }
                    }

                    if (isAlive && sceneManager != null)
                    {
                        isAlive = sceneBounds.IsWithin(e.transform.position);
                    }

                    if (isAlive && HealthUtilities.TryGetHealth(e, out var health))
                    {
                        isAlive = health > 0;
                        /*if (e.TryGetComponent<PoolableObject>(out var pool))
                        {
                            //return health > 0 && !pool.InPool;
                            isAlive = !(health <= 0 || pool.InPool);
                        }
                        else
                        {
                            
                        }*/
                    }

                    return isAlive;
                }

                while (true)
                {
                    if (!GameObject.FindObjectsOfType<EntityHealth>().Any(e => IsAlive(e))) 
                    {
                        break;
                    }
                    /*if (GameObject.FindObjectsOfType<EntityHealth>().Length == 0)
                    {
                        break;
                    }*/
                    yield return new WaitForSeconds(0.5f);
                }
            }

            /*var bl = transform.TransformPoint(endingCutscenePosRange.min);
            var tr = transform.TransformPoint(endingCutscenePosRange.max);

            var worldRect = new Rect
            {
                min = bl,
                max = tr
            };

            yield return new WaitUntil(() => worldRect.IsWithin(Player.Player1.transform.position));*/

            var completionZones = GetComponentsInChildren<CompletionZone>();

            yield return new WaitUntil(() => completionZones.Any(c => c.PlayerIsInZone()));

            if (stopMusic)
            {
                Music.ApplyMusicSnapshot(stopMusicSnapshot, 0f, stopMusicDuration);
            }

            if (challengeCompleteSound != null)
            {
                WeaverAudio.PlayAtPointDelayed(challengeCompleteSoundDelay, challengeCompleteSound, Player.Player1.transform.position, channel: WeaverCore.Enums.AudioChannel.Music);
            }

            PlayerData.instance.SetBool("disablePause", true);
            HeroController.instance.RelinquishControl();
            HeroController.instance.StopAnimationControl();

            if (silentCue != null)
            {
                Music.PlayMusicCue(silentCue,0f, 0.5f);
            }

            if (endCutscene != null)
            {
                yield return endCutscene.RunWave(this);
            }

            PlayerData.instance.SetBool("disablePause", false);
            HeroController.instance.StartAnimationControl();
            HeroController.instance.RegainControl();

            yield break;
        }

        public void ReenablePlayerAnim() 
        {
            HeroController.instance.StartAnimationControl();
        }

        /// <summary>
		/// Begins an in-game cutscene that freezes the player
		/// </summary>
		/// <param name="playSound">If true, a cutscene sound effect is played</param>
		public static void BeginInGameCutsceneBasic(bool playSound = true)
		{
			EventManager.SendEventToGameObject("FSM CANCEL", Player.Player1.gameObject);

			var hudCanvas = CameraUtilities.GetHudCanvas();

			if (hudCanvas != null)
			{
				EventManager.SendEventToGameObject("OUT", hudCanvas, null);
			}

			//HeroController.instance.RelinquishControl();
			//HeroController.instance.StartAnimationControl();
			//PlayerData.instance.SetBool("disablePause", true);
			//HeroController.instance.GetComponent<Rigidbody2D>().velocity = default;
			//HeroController.instance.AffectedByGravity(true);

			if (playSound)
			{
                /*if (cutsceneBeginSound == null)
                {
                    cutsceneBeginSound = WeaverAssets.LoadWeaverAsset<AudioClip>("dream_ghost_appear");
                }*/

				WeaverAudio.PlayAtPoint(WeaverAssets.LoadWeaverAsset<AudioClip>("dream_ghost_appear"), Player.Player1.transform.position);
            }
        }

		/// <summary>
		/// Ends an in-game cutscene and unfreezes the player
		/// </summary>
		public static void EndInGameCutsceneBasic()
		{
			//HeroController.instance.RegainControl();
			//HeroController.instance.StartAnimationControl();

            var hudCanvas = CameraUtilities.GetHudCanvas();

            if (hudCanvas != null)
            {
                EventManager.SendEventToGameObject("IN", hudCanvas, null);
            }
            PlayerData.instance.SetBool("disablePause", false);
        }


        private void OnValidate() 
        {
            if (_spawnLocationSpriteLabelsInternal != SpawnLocationSpriteLabels)
            {
                _spawnLocationSpriteLabelsInternal = SpawnLocationSpriteLabels;
                foreach (var spawner in GetComponentsInChildren<ColosseumEnemySpawner>())
                {
                    if (spawner is CageColosseumSpawner cageSpawn)
                    {
                        cageSpawn.OnValidate();
                    }
                }
            }

            gameObject.tag = "Colosseum Manager";
        }
    }
}
