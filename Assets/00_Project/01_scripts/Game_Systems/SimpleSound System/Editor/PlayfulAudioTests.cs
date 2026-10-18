using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class PlayfulAudioTests
{
    [Test]
    public void GameplayLibrary_HasVariationsAndCompleteUncutSounds()
    {
        var library = AssetDatabase.LoadAssetAtPath<GameplaySounds_SO>("Assets/00_Project/03_scriptableObjects/GameplaySounds.asset");
        Assert.NotNull(library);
        Assert.That(library.StepsSounds.Count, Is.GreaterThanOrEqualTo(4));
        Assert.That(library.CollectingSounds.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(library.ThrowingSounds.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(library.ShelfPlacementSounds.Count, Is.GreaterThanOrEqualTo(3));
        var all = library.StepsSounds.Concat(library.CollectingSounds).Concat(library.ThrowingSounds)
            .Concat(library.ShelfPlacementSounds).Concat(new[] { library.JumpSound, library.WrongSFX,
                library.WrongSectionSFX, library.CompleteShelfSFX, library.UIClick });
        foreach (var sound in all)
        {
            Assert.True(sound.IsValid);
            Assert.That(AssetDatabase.GetAssetPath(sound.Clip), Does.StartWith(PlayfulAudioBuilder.Root));
            Assert.That(sound.Pitch, Is.EqualTo(1));
            Assert.That(sound.Volume, Is.InRange(.01f, 1));
            Assert.That(sound.StartTime, Is.Zero);
            Assert.That(sound.EndTime, Is.EqualTo(sound.Clip.length).Within(.0001f));
            Assert.False(sound.EndsEarly);
        }
    }

    [Test]
    public void FrequentActions_DoNotRepeatThePreviousVariation()
    {
        var library = AssetDatabase.LoadAssetAtPath<GameplaySounds_SO>("Assets/00_Project/03_scriptableObjects/GameplaySounds.asset");
        foreach (System.Func<GameSound> choose in new System.Func<GameSound>[] { library.GetRandomCollectSFX,
            library.GetRandomThrowSFX, library.GetRandomShelfPlacementSFX })
        {
            AudioClip last = choose().Clip;
            for (int i = 0; i < 30; i++)
            {
                var next = choose().Clip;
                Assert.AreNotSame(last, next);
                last = next;
            }
        }
        Assert.AreNotSame(library.WrongSFX.Clip, library.WrongSectionSFX.Clip);
        Assert.Less(library.WrongSFX.Volume, library.WrongSectionSFX.Volume);
    }

    [Test]
    public void PlayableSceneDependencies_UseOnlyTheSelectedPackageAudio()
    {
        foreach (var scene in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            var legacy = AssetDatabase.GetDependencies(scene.path, true)
                .Where(p => AssetDatabase.GetMainAssetTypeAtPath(p) == typeof(AudioClip) && !p.StartsWith(PlayfulAudioBuilder.Root)).ToArray();
            Assert.IsEmpty(legacy, scene.path + ": " + string.Join(", ", legacy));
        }
    }

    [Test]
    public void ReplacementImports_PreserveRatesAndUseMonoEffectsStereoMusic()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { PlayfulAudioBuilder.Root.TrimEnd('/') });
        Assert.That(guids.Length, Is.EqualTo(23));
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            bool music = path.Contains("/Music/");
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            Assert.That(clip.channels, Is.EqualTo(music ? 2 : 1), path);
            Assert.That(clip.frequency, Is.EqualTo(44100), path);
            Assert.That(importer.forceToMono, Is.EqualTo(!music), path);
            Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.True, path);
            Assert.That(importer.defaultSampleSettings.sampleRateSetting, Is.EqualTo(AudioSampleRateSetting.PreserveSampleRate), path);
            Assert.That(importer.defaultSampleSettings.loadType,
                Is.EqualTo(music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad), path);
        }
    }
}
