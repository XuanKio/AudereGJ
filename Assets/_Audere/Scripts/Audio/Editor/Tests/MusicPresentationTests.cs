#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Audere.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Audere.Audio.Editor.Tests
{
    public sealed class MusicPresentationTests
    {
        private readonly List<Object> objects = new List<Object>();
        private MusicPresentationState state;
        private bool hadMusicPreference;
        private float musicPreference;

        [SetUp]
        public void SetUp()
        {
            state = new MusicPresentationState();
            hadMusicPreference = PlayerPrefs.HasKey(AudioService.MusicVolumePrefKey);
            musicPreference = PlayerPrefs.GetFloat(AudioService.MusicVolumePrefKey);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
            if (hadMusicPreference) PlayerPrefs.SetFloat(AudioService.MusicVolumePrefKey, musicPreference);
            else PlayerPrefs.DeleteKey(AudioService.MusicVolumePrefKey);
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            objects.Add(go);
            return go;
        }

        private CanvasGroup Cover(float alpha)
        {
            CanvasGroup cover = NewObject("QA Cover").AddComponent<CanvasGroup>();
            cover.alpha = alpha;
            state.TrackScreenFade(cover);
            return cover;
        }

        [TestCase(0f, 1f)]
        [TestCase(.25f, .75f)]
        [TestCase(.75f, .25f)]
        [TestCase(1f, 0f)]
        public void CoverGain_FollowsVisibleScene(float alpha, float gain)
        {
            Cover(alpha);
            Assert.AreEqual(gain, state.Gain, .0001f);
        }

        [Test]
        public void BlackHold_RemainsSilentBetweenSteps_AndRevealReleasesIt()
        {
            CanvasGroup cover = Cover(1f);
            for (int i = 0; i < 20; i++) Assert.AreEqual(0f, state.Gain);
            cover.alpha = 0f;
            Assert.AreEqual(1f, state.Gain);
        }

        [Test]
        public void OverlappingOwners_CannotResumeEachOther()
        {
            CanvasGroup cover = Cover(.8f);
            GameObject shader = NewObject("Shader");
            GameObject load = NewObject("Load");
            state.SetDuck(shader, .1f);
            state.SetDuck(load, 0f);
            state.Release(shader);
            Assert.AreEqual(0f, state.Gain);
            state.Release(load);
            Assert.AreEqual(.2f, state.Gain, .0001f);
            cover.alpha = 0f;
            Assert.AreEqual(1f, state.Gain);
        }

        [Test]
        public void DestroyedSceneObjects_DoNotLeaveMuteOrCombatState()
        {
            CanvasGroup cover = Cover(1f);
            GameObject owner = NewObject("Old Scene");
            state.SetDuck(owner, 0f);
            state.SetCombat(owner, true);
            Object.DestroyImmediate(cover.gameObject);
            Object.DestroyImmediate(owner);
            Assert.AreEqual(1f, state.Gain);
            Assert.IsFalse(state.IsCombat);
        }

        [Test]
        public void InactiveCover_DoesNotMuteVisibleScene()
        {
            CanvasGroup cover = Cover(1f);
            cover.gameObject.SetActive(false);
            Assert.AreEqual(1f, state.Gain);
            cover.gameObject.SetActive(true);
            Assert.AreEqual(0f, state.Gain);
        }

        [Test]
        public void CombatSessionEnding_DoesNotOverrideCombatPresentation()
        {
            GameObject world = NewObject("World Mode");
            GameObject session = NewObject("Combat Session");
            state.SetCombat(world, true);
            state.SetCombat(session, true);
            state.Release(session);
            state.Release(session);
            Assert.IsTrue(state.IsCombat);
            state.SetCombat(world, false);
            Assert.IsFalse(state.IsCombat);
        }

        [TestCase(0f, 1.1f, 1f)]
        [TestCase(.55f, 1.1f, .5f)]
        [TestCase(1.1f, 1.1f, 0f)]
        [TestCase(1.4f, 1.1f, 0f)]
        [TestCase(2f, 1.1f, 0f)]
        public void Fullscreen_DucksBySwap_AndHoldsUntilCompletion(float time, float swap, float expected)
        {
            Assert.AreEqual(expected, FullscreenTransitionController.EvaluateMusicGain(time, swap), .0001f);
        }

        [Test]
        public void Service_ReinitializeDoesNotDuplicateSources_OrRestartMusic()
        {
            AudioService service = Service(out _, out _);
            Tick(service, 1f);
            Tick(service, 1f);
            AudioSource source = service.MusicSource;
            service.Initialize();
            Assert.AreSame(source, service.MusicSource);
            Assert.AreEqual(2, service.GetComponentsInChildren<AudioSource>().Length);
            Assert.IsTrue(source.loop);
            Assert.IsFalse(source.playOnAwake);
            Assert.AreEqual(0f, source.spatialBlend);
            Assert.AreEqual(1f, service.MusicPresentationGain);
        }

        [Test]
        public void Service_BlackCover_AndVolumeSettingCannotUnmuteMusic_OrAffectSfx()
        {
            AudioService service = Service(out _, out _);
            Tick(service, 1f);
            Tick(service, 1f);
            CanvasGroup cover = Cover(1f);
            service.TrackScreenFade(cover);
            Tick(service, .01f);
            service.SetMusicVolume(.6f);
            Assert.AreEqual(0f, service.MusicSource.volume);
            AudioSource sfx = (AudioSource)Field("sfxSource").GetValue(service);
            Assert.AreEqual(service.SfxVolume, sfx.volume);
            cover.alpha = 0f;
            Tick(service, .1f);
            Assert.That(service.MusicSource.volume, Is.GreaterThan(0f).And.LessThan(.3f));
            Tick(service, 1f);
            Assert.AreEqual(.3f, service.MusicSource.volume, .0001f);
        }

        [Test]
        public void Service_EmptyCombatSlotMeansSilence_AndReturnRestoresNormalTrack()
        {
            AudioService service = Service(out AudioClip normal, out _);
            Tick(service, 1f);
            Tick(service, 1f);
            GameObject owner = NewObject("Combat");
            service.SetCombatMusicOwner(owner, true);
            Tick(service, 1f);
            Tick(service, 1f);
            Assert.AreEqual(AudioId.Music_Combat, service.CurrentMusicId);
            Assert.IsNull(service.MusicSource.clip);
            Assert.AreEqual(0f, service.MusicSource.volume);
            service.ReleaseMusicOwner(owner);
            Tick(service, .1f);
            Tick(service, 1f);
            Assert.AreSame(normal, service.MusicSource.clip);
            Assert.AreEqual(1f, service.MusicPresentationGain);
        }

        [Test]
        public void Service_AssignedCombatClipWaitsForTransitionRelease()
        {
            AudioService service = Service(out AudioClip normal, out AudioCatalog catalog);
            AudioClip combat = AudioClip.Create("QA Combat", 64, 1, 8000, false);
            objects.Add(combat);
            SetEntries(catalog, normal, combat);
            Tick(service, 1f);
            Tick(service, 1f);
            GameObject transition = NewObject("Fullscreen");
            GameObject owner = NewObject("World Combat");
            service.SetMusicDuck(transition, 0f);
            service.SetCombatMusicOwner(owner, true);
            Tick(service, 1f);
            Tick(service, 1f);
            Assert.AreSame(combat, service.MusicSource.clip);
            Assert.AreEqual(0f, service.MusicSource.volume);
            service.ReleaseMusicOwner(transition);
            Tick(service, .1f);
            Assert.Greater(service.MusicSource.volume, 0f);
            Assert.Less(service.MusicPresentationGain, 1f);
        }

        [Test]
        public void Service_CancelReplayAndDisable_ClearOwners()
        {
            AudioService service = Service(out _, out _);
            GameObject transition = NewObject("Fullscreen");
            Tick(service, 1f);
            Tick(service, 1f);
            service.SetMusicDuck(transition, 0f);
            Tick(service, 1f);
            service.ReleaseMusicOwner(transition);
            service.ReleaseMusicOwner(transition);
            Tick(service, 1f);
            Assert.AreEqual(1f, service.MusicPresentationGain);
            service.SetMusicDuck(transition, 0f);
            Tick(service, 1f);
            Assert.AreEqual(0f, service.MusicSource.volume);
            service.gameObject.SetActive(false);
            // Ordinary MonoBehaviour messages are not driven by EditMode tests.
            typeof(AudioService).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(service, null);
            service.gameObject.SetActive(true);
            Tick(service, 1f);
            Assert.AreEqual(1f, service.MusicPresentationGain);
        }

        [Test]
        public void ProductionCatalog_HasBgm_AndEditableCombatSlot()
        {
            AudioCatalog catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>("Assets/_Audere/Data/Audio/AudioCatalog.asset");
            Assert.IsTrue(catalog.TryGet(AudioId.Music_Exploration, out AudioEntry normal));
            Assert.AreEqual("Assets/_Audere/Audio/bgm.mp3", AssetDatabase.GetAssetPath(normal.clip));
            Assert.IsTrue(catalog.TryGet(AudioId.Music_Combat, out AudioEntry combat));
            Assert.That(combat.volume, Is.InRange(0f, 1f));
        }

        [Test]
        public void Bootstrap_HasDedicatedSerializedMusicSource()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Audere/Scenes/00_Bootstrap.unity");
            try
            {
                AudioService service = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                    if (service == null) service = root.GetComponentInChildren<AudioService>(true);
                Assert.IsNotNull(service);
                Assert.IsNotNull(service.MusicSource);
                Assert.AreNotSame(Field("sfxSource").GetValue(service), service.MusicSource);
                Assert.IsTrue(service.MusicSource.loop);
                Assert.IsFalse(service.MusicSource.playOnAwake);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }


        [Test]
        public void EncounterMusic_PriorityAndRetryKeepTheSelectedTrack()
        {
            var world = NewObject("World");
            var session = NewObject("Session");
            state.SetCombat(world, true, AudioId.Music_TimorCombat);
            state.SetCombat(session, true, AudioId.Music_TimorCombat, 1);
            Assert.AreEqual(AudioId.Music_TimorCombat, state.ResolveCombatTrack(AudioId.Music_Combat));
            state.Release(session);
            Assert.AreEqual(AudioId.Music_TimorCombat, state.ResolveCombatTrack(AudioId.Music_Combat));
            state.SetCombat(session, true, AudioId.Music_Combat, 1);
            state.SetCombat(world, true, AudioId.Music_TimorCombat);
            Assert.AreEqual(AudioId.Music_Combat, state.ResolveCombatTrack(AudioId.Music_Combat));
            Object.DestroyImmediate(session);
            Assert.AreEqual(AudioId.Music_TimorCombat, state.ResolveCombatTrack(AudioId.Music_Combat));
            state.Release(world);
            Assert.IsFalse(state.IsCombat);
            Assert.AreEqual(AudioId.Music_Combat, state.ResolveCombatTrack(AudioId.Music_Combat));
        }

        [Test]
        public void Service_TimorSelectionNeverFallsBackToRegularCombat()
        {
            var service = Service(out AudioClip normal, out AudioCatalog catalog);
            var combat = AudioClip.Create("Regular combat", 64, 1, 8000, false);
            var timor = AudioClip.Create("Timor combat", 64, 1, 8000, false);
            objects.Add(combat); objects.Add(timor);
            SetEntries(catalog, normal, combat);
            var entries = (List<AudioEntry>)typeof(AudioCatalog).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
            entries.Add(new AudioEntry { id = AudioId.Music_TimorCombat, clip = timor, volume = .4f });
            typeof(AudioCatalog).GetMethod("BuildLookup", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(catalog, null);
            var world = NewObject("Timor world");
            var session = NewObject("Timor session");
            service.SetCombatMusicOwner(world, true, AudioId.Music_TimorCombat);
            service.SetCombatMusicOwner(session, true, AudioId.Music_TimorCombat, 1);
            Tick(service, 1f); Tick(service, 1f);
            Assert.AreSame(timor, service.MusicSource.clip);
            service.ReleaseMusicOwner(session);
            Tick(service, 1f);
            Assert.AreSame(timor, service.MusicSource.clip);
            entries[2].clip = null;
            Tick(service, 1f); Tick(service, 1f);
            Assert.IsNull(service.MusicSource.clip);
            service.ReleaseMusicOwner(world);
            Tick(service, 1f); Tick(service, 1f);
            Assert.AreSame(normal, service.MusicSource.clip);
        }

        [Test]
        public void MusicBeatClock_AlignsActivationAndSkipsMissedBeats()
        {
            var clock = new Audere.Combat.CombatMusicBeatClock();
            double period = 60d / 110d * 2d, lead = 60d / 110d, offset = .013d;
            int emitted = 0;
            for (int frame = 0; frame < 600; frame++)
            {
                double now = frame / 60d;
                if (!clock.Tick(now, period, offset, lead)) continue;
                emitted++;
                double grid = (now + lead - offset) / period;
                double error = System.Math.Abs(grid - System.Math.Round(grid)) * period;
                Assert.LessOrEqual(error, 1d / 60d + .00001);
                Assert.IsFalse(clock.Tick(now, period, offset, lead), "No double emission in one frame.");
            }
            Assert.Greater(emitted, 5);
            Assert.IsFalse(clock.Tick(30.17, period, offset, lead), "Resume waits for a fresh telegraph.");
            Assert.IsFalse(clock.Tick(0, period, offset, lead), "Loop wrap re-aligns safely.");
            var shortPause = new Audere.Combat.CombatMusicBeatClock();
            Assert.IsFalse(shortPause.Tick(.5, period, offset, lead));
            Assert.IsFalse(shortPause.Tick(.9, period, offset, lead), "A short pause must not emit a stale beat.");
        }

        [Test]
        public void ProductionCombatMusic_UsesSeparateSlotsAndTimorRhythm()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>("Assets/_Audere/Data/Audio/AudioCatalog.asset");
            Assert.IsTrue(catalog.TryGet(AudioId.Music_Combat, out var regular));
            Assert.IsTrue(catalog.TryGet(AudioId.Music_TimorCombat, out var timor));
            Assert.AreEqual("Assets/_Audere/Audio/combat1_final (mp3cut.net).mp3", AssetDatabase.GetAssetPath(regular.clip));
            Assert.AreEqual("Assets/_Audere/Audio/bossfightfull.mp3", AssetDatabase.GetAssetPath(timor.clip));
            foreach (var guid in AssetDatabase.FindAssets("t:CombatEncounterData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var encounter = AssetDatabase.LoadAssetAtPath<Audere.Combat.CombatEncounterData>(path);
                Assert.AreEqual(path.Contains("/TimorNightPressure/") ? AudioId.Music_TimorCombat : AudioId.Music_Combat, encounter.Music, path);
            }
            var move = AssetDatabase.LoadAssetAtPath<Audere.Combat.NarrativePressurePatternMove>(
                "Assets/_Audere/Data/Combat/TimorNightPressure/Moves/Move_TimorNightPressure_02.asset");
            Assert.IsTrue(move.UsesMusicGrid);
            Assert.AreEqual(110f, move.RhythmBpm);
            Assert.GreaterOrEqual(move.TelegraphDuration, .45);
        }


        private AudioService AttackAudioService()
        {
            var service = Service(out AudioClip clip, out AudioCatalog catalog);
            var entries = (List<AudioEntry>)typeof(AudioCatalog).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(catalog);
            entries.Add(new AudioEntry { id = AudioId.Enemy_BulletVolley, clip = clip, volume = 1f });
            entries.Add(new AudioEntry { id = AudioId.Enemy_LaserVolley, clip = clip, volume = .2f });
            typeof(AudioCatalog).GetMethod("BuildLookup", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(catalog, null);
            return service;
        }

        [Test]
        public void VolleyAudio_ThreeBulletsEveryPoint35_ProducesThreeSounds()
        {
            AttackAudioService();
            var owner = NewObject("Volley owner");
            var sound = new Audere.Combat.CombatVolleyAudio(owner.transform);
            int played = 0;
            for (int beat = 0; beat < 3; beat++)
            {
                if (beat > 0) sound.Advance(.35f);
                for (int bullet = 0; bullet < 3; bullet++)
                    if (sound.PlayBullet(.25f)) played++;
            }
            Assert.AreEqual(3, played, "Nine projectiles must produce only three beats.");
            sound.Reset();
            played = 0;
            for (int bullet = 0; bullet < 5; bullet++)
            {
                if (sound.PlayBullet(.25f)) played++;
                sound.Advance(.07f);
            }
            Assert.AreEqual(2, played, "Rapid sequential bullets are grouped, not voiced individually.");
            Assert.AreEqual(1, owner.GetComponentsInChildren<AudioSource>().Length);
        }

        [Test]
        public void VolleyAudio_IndependentKinds_PauseCancelAndReuse()
        {
            AttackAudioService();
            var owner = NewObject("Volley owner");
            var sound = new Audere.Combat.CombatVolleyAudio(owner.transform);
            Assert.IsTrue(sound.PlayBullet(.25f));
            Assert.IsTrue(sound.PlayLaser(.12f));
            Assert.IsFalse(sound.PlayLaser(.12f));
            sound.SetPaused(true);
            sound.Advance(10f);
            Assert.IsFalse(sound.PlayBullet(.25f));
            sound.SetPaused(false);
            Assert.IsFalse(sound.PlayBullet(.25f), "Pause must not consume the beat cooldown.");
            sound.Advance(.25f);
            Assert.IsTrue(sound.PlayBullet(.25f));
            sound.Reset(); sound.Reset();
            Assert.IsTrue(sound.PlayBullet(.25f), "A fresh phase starts ready.");
            Assert.AreEqual(2, owner.GetComponentsInChildren<AudioSource>().Length);
            sound.Reset();
            Assert.IsTrue(sound.PlayBullet(.25f, 73, 2));
            sound.Reset(72, 1);
            Assert.IsFalse(sound.PlayBullet(.25f, 73, 2), "Stale phase cleanup must not reset the new beat.");
            sound.SetPaused(true);
            Assert.IsFalse(sound.PlayLaser(.12f, 74, 1), "A new owner cannot bypass pause.");
            sound.SetPaused(false);
            sound.Reset(73, 2);
            Assert.IsTrue(sound.PlayBullet(.25f, 73, 2));
            owner.SetActive(false);
            Assert.IsFalse(sound.PlayLaser(.12f));
            Object.DestroyImmediate(owner);
            Assert.DoesNotThrow(() => sound.Reset(), "Destroyed audio sources remain safe to clean up.");
        }

        [Test]
        public void BoardVolleyAudio_WaitsForActivation_AndClearsWithHazards()
        {
            AttackAudioService();
            var prefab = AssetDatabase.LoadAssetAtPath<Audere.Combat.CombatBoardView>("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab");
            var board = Object.Instantiate(prefab);
            objects.Add(board.gameObject);
            board.gameObject.SetActive(true);
            for (int i = 0; i < 5; i++)
                board.SpawnEnemyBullet(null, new Vector2(-90 + i * 12, 80), Vector2.down, 71, 1, .5f);
            for (int i = 0; i < 3; i++)
                board.SpawnEnemyLaser(new Vector2(-100 + i * 50, 0), new Vector2(-100 + i * 50, 0), new Vector2(10, 200), 0, .5f, 1f, 71, 1);
            Assert.AreEqual(0, board.GetComponentsInChildren<AudioSource>().Length);
            board.TickBullets(.2f, 999f);
            Assert.AreEqual(0, board.GetComponentsInChildren<AudioSource>().Length);
            board.TickBullets(.31f, 999f);
            Assert.AreEqual(2, board.GetComponentsInChildren<AudioSource>().Length, "One source per kind, not per projectile.");
            board.ClearRuntimeBullets(71, 1);
            foreach (var source in board.GetComponentsInChildren<AudioSource>()) Assert.IsFalse(source.isPlaying);
            board.SpawnEnemyLaser(Vector2.zero, Vector2.zero, new Vector2(10, 200), 0, .5f, 1f, 72, 1);
            board.ClearRuntimeBullets();
            board.TickBullets(1f, 999f);
            foreach (var source in board.GetComponentsInChildren<AudioSource>()) Assert.IsFalse(source.isPlaying, "Cancelled telegraphs cannot fire later.");
        }

        [Test]
        public void ProductionAttackAudio_UsesRequestedClips()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>("Assets/_Audere/Data/Audio/AudioCatalog.asset");
            Assert.IsTrue(catalog.TryGet(AudioId.Enemy_BulletVolley, out var bullet));
            Assert.IsTrue(catalog.TryGet(AudioId.Enemy_LaserVolley, out var laser));
            Assert.AreEqual("Assets/_Audere/Audio/dan.wav", AssetDatabase.GetAssetPath(bullet.clip));
            Assert.AreEqual("Assets/_Audere/Audio/laze.mp3", AssetDatabase.GetAssetPath(laser.clip));
        }

        private AudioService Service(out AudioClip normal, out AudioCatalog catalog)
        {
            Assert.IsTrue(AudioService.Instance == null, "Run these EditMode tests outside a live audio session.");
            normal = AudioClip.Create("QA Silent Clip", 64, 1, 8000, false);
            catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            objects.Add(normal);
            objects.Add(catalog);
            SetEntries(catalog, normal, null);
            AudioService service = NewObject("QA Audio").AddComponent<AudioService>();
            Field("catalog").SetValue(service, catalog);
            service.Initialize();
            return service;
        }

        private static void SetEntries(AudioCatalog catalog, AudioClip normal, AudioClip combat)
        {
            typeof(AudioCatalog).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(catalog,
                new List<AudioEntry>
                {
                    new AudioEntry { id = AudioId.Music_Exploration, clip = normal, volume = .5f },
                    new AudioEntry { id = AudioId.Music_Combat, clip = combat, volume = .5f },
                });
            typeof(AudioCatalog).GetMethod("BuildLookup", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(catalog, null);
        }

        private static FieldInfo Field(string name) => typeof(AudioService).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        private static void Tick(AudioService service, float seconds) =>
            typeof(AudioService).GetMethod("TickMusic", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(service, new object[] { seconds });
    }
}
#endif
