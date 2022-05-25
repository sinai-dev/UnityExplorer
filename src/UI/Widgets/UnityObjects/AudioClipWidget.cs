using System.Collections;
using System.Text;
using UnityExplorer.Config;
using UnityExplorer.Inspectors;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.ObjectPool;

namespace UnityExplorer.UI.Widgets
{
    public class AudioClipWidget : UnityObjectWidget
    {
        static GameObject AudioPlayerObject;
        static AudioSource Source;
        static AudioClipWidget CurrentlyPlaying;
        static Coroutine CurrentlyPlayingCoroutine;
        static readonly string zeroLengthString = GetLengthString(0f);

        public AudioClip audioClip;
        string fullLengthText;

        ButtonRef toggleButton;
        bool audioPlayerWanted;

        GameObject audioPlayerRoot;
        ButtonRef playStopButton;
        Text progressLabel;
        GameObject saveObjectRow;
        InputFieldRef savePathInput;
        GameObject cantSaveRow;

        public override void OnBorrowed(object target, Type targetType, ReflectionInspector inspector)
        {
            base.OnBorrowed(target, targetType, inspector);

            this.audioPlayerRoot.transform.SetParent(inspector.UIRoot.transform);
            this.audioPlayerRoot.transform.SetSiblingIndex(inspector.UIRoot.transform.childCount - 2);

            audioClip = target.TryCast<AudioClip>();
            this.fullLengthText = GetLengthString(audioClip.length);

            if (audioClip.loadType == AudioClipLoadType.DecompressOnLoad)
            {
                cantSaveRow.SetActive(false);
                saveObjectRow.SetActive(true);
                SetDefaultSavePath();
            }
            else
            {
                cantSaveRow.SetActive(true);
                saveObjectRow.SetActive(false);
            }

            ResetProgressLabel();
        }

        public override void OnReturnToPool()
        {
            audioClip = null;

            if (audioPlayerWanted)
                ToggleAudioWidget();

            if (CurrentlyPlaying == this)
                StopClip();

            this.audioPlayerRoot.transform.SetParent(Pool<AudioClipWidget>.Instance.InactiveHolder.transform);

            base.OnReturnToPool();
        }

        private void ToggleAudioWidget()
        {
            if (audioPlayerWanted)
            {
                audioPlayerWanted = false;

                toggleButton.ButtonText.text = "Show Player";
                audioPlayerRoot.SetActive(false);
            }
            else
            {
                audioPlayerWanted = true;

                toggleButton.ButtonText.text = "Hide Player";
                audioPlayerRoot.SetActive(true);
            }
        }

        void SetDefaultSavePath()
        {
            string name = audioClip.name;
            if (string.IsNullOrEmpty(name))
                name = "untitled";
            savePathInput.Text = Path.Combine(ConfigManager.Default_Output_Path.Value, $"{name}.wav");
        }

        static string GetLengthString(float seconds)
        {
            TimeSpan ts = TimeSpan.FromSeconds(seconds);

            StringBuilder sb = new();

            if (ts.Hours > 0)
                sb.Append($"{ts.Hours}:");

            sb.Append($"{ts.Minutes:00}:");
            sb.Append($"{ts.Seconds:00}:");
            sb.Append($"{ts.Milliseconds:000}");

            return sb.ToString();
        }

        private void ResetProgressLabel()
        {
            this.progressLabel.text = $"{zeroLengthString} / {fullLengthText}";
        }

        private void OnPlayStopClicked()
        {
            SetupAudioPlayer();

            if (CurrentlyPlaying == this)
            {
                // we are playing a clip. stop it.
                StopClip();
            }
            else
            {
                // If something else is playing a clip, stop that.
                if (CurrentlyPlaying != null)
                    CurrentlyPlaying.StopClip();

                // we want to start playing a clip.
                CurrentlyPlayingCoroutine = RuntimeHelper.StartCoroutine(PlayClipCoroutine());
            }
        }

        static void SetupAudioPlayer()
        {
            if (AudioPlayerObject)
                return;

            AudioPlayerObject = new GameObject("UnityExplorer.AudioPlayer");
            UnityEngine.Object.DontDestroyOnLoad(AudioPlayerObject);
            AudioPlayerObject.hideFlags = HideFlags.HideAndDontSave;
            AudioPlayerObject.transform.position = new(int.MinValue, int.MinValue); // move it as far away as possible
#if CPP
            Source = AudioPlayerObject.AddComponent(UnhollowerRuntimeLib.Il2CppType.Of<AudioSource>()).TryCast<AudioSource>();
#else
            Source = AudioPlayerObject.AddComponent<AudioSource>();
#endif
            AudioPlayerObject.AddComponent<AudioListener>();
        }

        private IEnumerator PlayClipCoroutine()
        {
            playStopButton.ButtonText.text = "Stop Clip";
            CurrentlyPlaying = this;
            Source.clip = this.audioClip;
            Source.Play();

            while (Source.isPlaying)
            {
                progressLabel.text = $"{GetLengthString(Source.time)} / {fullLengthText}";
                yield return null;
            }

            CurrentlyPlayingCoroutine = null;
            StopClip();
        }

        private void StopClip()
        {
            if (CurrentlyPlayingCoroutine != null)
                RuntimeHelper.StopCoroutine(CurrentlyPlayingCoroutine);

            Source.Stop();
            CurrentlyPlaying = null;
            CurrentlyPlayingCoroutine = null;
            playStopButton.ButtonText.text = "Play Clip";

            ResetProgressLabel();
        }

        public void OnSaveClipClicked()
        {
            if (!audioClip)
            {
                ExplorerCore.LogWarning("AudioClip is null, maybe it was destroyed?");
                return;
            }

            if (string.IsNullOrEmpty(savePathInput.Text))
            {
                ExplorerCore.LogWarning("Save path cannot be empty!");
                return;
            }

            string path = savePathInput.Text;
            if (!path.EndsWith(".wav", StringComparison.InvariantCultureIgnoreCase))
                path += ".wav";

            path = IOUtility.EnsureValidFilePath(path);

            if (File.Exists(path))
                File.Delete(path);

            SavWav.Save(audioClip, path);
        }

        public override GameObject CreateContent(GameObject uiRoot)
        {
            GameObject ret = base.CreateContent(uiRoot);

            // Toggle Button

            toggleButton = UIFactory.CreateButton(UIRoot, "AudioWidgetToggleButton", "Show Player", new Color(0.2f, 0.3f, 0.2f));
            toggleButton.Transform.SetSiblingIndex(0);
            UIFactory.SetLayoutElement(toggleButton.Component.gameObject, minHeight: 25, minWidth: 170);
            toggleButton.OnClick += ToggleAudioWidget;

            // Actual widget

            audioPlayerRoot = UIFactory.CreateVerticalGroup(uiRoot, "AudioWidget", false, false, true, true, spacing: 5);
            UIFactory.SetLayoutElement(audioPlayerRoot, flexibleWidth: 9999, flexibleHeight: 50);
            audioPlayerRoot.SetActive(false);

            // Player 

            GameObject playerRow = UIFactory.CreateHorizontalGroup(audioPlayerRoot, "PlayerWidget", false, false, true, true,
                spacing: 5, padding: new() { x = 3f, w = 3f, y = 3f, z = 3f });

            playStopButton = UIFactory.CreateButton(playerRow, "PlayerButton", "Play", normalColor: new(0.2f, 0.4f, 0.2f));
            playStopButton.OnClick += OnPlayStopClicked;
            UIFactory.SetLayoutElement(playStopButton.GameObject, minWidth: 60, minHeight: 25);

            progressLabel = UIFactory.CreateLabel(playerRow, "ProgressLabel", "0 / 0");
            UIFactory.SetLayoutElement(progressLabel.gameObject, flexibleWidth: 9999, minHeight: 25);

            ResetProgressLabel();

            // Save helper

            saveObjectRow = UIFactory.CreateHorizontalGroup(audioPlayerRoot, "SaveRow", false, false, true, true, 2, new Vector4(2, 2, 2, 2),
                new Color(0.1f, 0.1f, 0.1f));

            ButtonRef saveBtn = UIFactory.CreateButton(saveObjectRow, "SaveButton", "Save .WAV", new Color(0.2f, 0.25f, 0.2f));
            UIFactory.SetLayoutElement(saveBtn.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            saveBtn.OnClick += OnSaveClipClicked;

            savePathInput = UIFactory.CreateInputField(saveObjectRow, "SaveInput", "...");
            UIFactory.SetLayoutElement(savePathInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 9999);

            // cant save label
            cantSaveRow = UIFactory.CreateHorizontalGroup(audioPlayerRoot, "CantSaveRow", true, true, true, true);
            UIFactory.SetLayoutElement(cantSaveRow, minHeight: 25, flexibleWidth: 9999);
            UIFactory.CreateLabel(
                cantSaveRow,
                "CantSaveLabel",
                "Cannot save this AudioClip as the data is compressed or streamed. Try a tool such as AssetRipper to unpack it.",
                color: Color.grey);

            return ret;
        }
    }

    #region SavWav

    //	Copyright (c) 2012 Calvin Rien
    //        http://the.darktable.com
    //
    //	This software is provided 'as-is', without any express or implied warranty. In
    //	no event will the authors be held liable for any damages arising from the use
    //	of this software.
    //
    //	Permission is granted to anyone to use this software for any purpose,
    //	including commercial applications, and to alter it and redistribute it freely,
    //	subject to the following restrictions:
    //
    //	1. The origin of this software must not be misrepresented; you must not claim
    //	that you wrote the original software. If you use this software in a product,
    //	an acknowledgment in the product documentation would be appreciated but is not
    //	required.
    //
    //	2. Altered source versions must be plainly marked as such, and must not be
    //	misrepresented as being the original software.
    //
    //	3. This notice may not be removed or altered from any source distribution.
    //
    //  =============================================================================
    //
    //  derived from Gregorio Zanon's script
    //  http://forum.unity3d.com/threads/119295-Writing-AudioListener.GetOutputData-to-wav-problem?p=806734&viewfull=1#post806734

    public static class SavWav
    {
        public const int HEADER_SIZE = 44;
        public const float RESCALE_FACTOR = 32767; // to convert float to Int16

        public static void Save(AudioClip clip, string filepath)
        {
            using FileStream fileStream = CreateEmpty(filepath);

            ConvertAndWrite(fileStream, clip);
            WriteHeader(fileStream, clip);
        }

        static FileStream CreateEmpty(string filepath)
        {
            FileStream fileStream = new(filepath, FileMode.Create);
            byte emptyByte = default;

            for (int i = 0; i < HEADER_SIZE; i++) //preparing the header
                fileStream.WriteByte(emptyByte);

            return fileStream;
        }

        static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
        {
#if CPP
            UnhollowerBaseLib.Il2CppStructArray<float> samples = new float[clip.samples * clip.channels];
            AudioClip.GetData(clip, samples, clip.samples, 0);
#else
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
#endif

            int len = samples.Length;

            // converting in 2 float[] steps to Int16[], then Int16[] to Byte[]
            short[] intData = new short[len];

            // bytesData array is twice the size of dataSource array because a float converted in Int16 is 2 bytes.
            byte[] bytesData = new byte[len * 2];

            for (int i = 0; i < len; i++)
            {
                intData[i] = (short)(samples[i] * RESCALE_FACTOR);
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            fileStream.Write(bytesData, 0, bytesData.Length);
        }

        static void WriteHeader(FileStream stream, AudioClip clip)
        {
            int hz = clip.frequency;
            int channels = clip.channels;
            int samples = clip.samples;

            stream.Seek(0, SeekOrigin.Begin);

            byte[] riff = Encoding.UTF8.GetBytes("RIFF");
            stream.Write(riff, 0, 4);

            byte[] chunkSize = BitConverter.GetBytes(stream.Length - 8);
            stream.Write(chunkSize, 0, 4);

            byte[] wave = Encoding.ASCII.GetBytes("WAVE");
            stream.Write(wave, 0, 4);

            byte[] fmt = Encoding.ASCII.GetBytes("fmt ");
            stream.Write(fmt, 0, 4);

            byte[] subChunk1 = BitConverter.GetBytes(16);
            stream.Write(subChunk1, 0, 4);

            byte[] audioFormat = BitConverter.GetBytes(1);
            stream.Write(audioFormat, 0, 2);

            byte[] numChannels = BitConverter.GetBytes(channels);
            stream.Write(numChannels, 0, 2);

            byte[] sampleRate = BitConverter.GetBytes(hz);
            stream.Write(sampleRate, 0, 4);

            byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
            stream.Write(byteRate, 0, 4);

            ushort blockAlign = (ushort)(channels * 2);
            stream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

            ushort bps = 16;
            byte[] bitsPerSample = BitConverter.GetBytes(bps);
            stream.Write(bitsPerSample, 0, 2);

            byte[] datastring = Encoding.UTF8.GetBytes("data");
            stream.Write(datastring, 0, 4);

            byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
            stream.Write(subChunk2, 0, 4);

            stream.Seek(0, SeekOrigin.Begin);
        }

        #endregion
    }
}