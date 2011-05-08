﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FMOD;
using System.Xml.Linq;
using MegaMan;
using MegaManR.Audio;
using MegaManR.Extensions;

namespace Mega_Man
{
    public class SoundSystem : IDisposable
    {
        private FMOD.System soundSystem;

        private Dictionary<string, Music> loadedMusic = new Dictionary<string, Music>();
        private Dictionary<string, ISoundEffect> loadedSounds = new Dictionary<string, ISoundEffect>();
        private List<int> playCount = new List<int>();
        private List<Channel> channels = new List<Channel>();
        private System.Windows.Forms.Timer updateTimer;

        private BackgroundMusic bgm;
        private SoundEffect sfx;
        public static byte CurrentSfxPriority { get; set; }

        public SoundSystem()
        {
            FMOD.Factory.System_Create(ref soundSystem);
            uint version = 0;
            soundSystem.getVersion(ref version);
            soundSystem.init(32, FMOD.INITFLAGS.NORMAL, (IntPtr)null);

            AudioManager.Instance.Initialize();
            AudioManager.Instance.Stereo = true;

            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 10;
            updateTimer.Tick += new EventHandler(updateTimer_Tick);

            AudioManager.Instance.SFXPlaybackStopped += new Action(Instance_SFXPlaybackStopped);
            CurrentSfxPriority = 255;
        }

        void Instance_SFXPlaybackStopped()
        {
            CurrentSfxPriority = 255;
        }

        public void Start()
        {
            updateTimer.Start();
            if (AudioManager.Instance.Paused) AudioManager.Instance.ResumeBGMPlayback();
        }

        public void Stop()
        {
            updateTimer.Stop();
            AudioManager.Instance.PauseBGMPlayback();
        }

        public void LoadEffectsFromXml(XElement node)
        {
            foreach (XElement soundNode in node.Elements("Sound"))
            {
                EffectFromXml(soundNode);
            }
        }

        public string EffectFromXml(XElement soundNode)
        {
            string name = soundNode.RequireAttribute("name").Value;
            if (loadedSounds.ContainsKey(name)) return name;

            XAttribute pathattr = soundNode.Attribute("path");
            ISoundEffect sound;
            if (pathattr != null)
            {
                string path = System.IO.Path.Combine(Game.CurrentGame.BasePath, pathattr.Value);

                bool loop;
                soundNode.TryBool("loop", out loop);

                float vol;
                if (!soundNode.TryFloat("volume", out vol)) vol = 1;

                sound = new WavEffect(this.soundSystem, path, loop, vol);
            }
            else
            {
                XAttribute trackAttr = soundNode.Attribute("track");
                if (trackAttr == null)
                {
                    // we trust that the sound they're talking about will be loaded eventually.
                    return name;
                }

                int track;
                if (!trackAttr.Value.TryParse(out track) || track <= 0) throw new GameXmlException(trackAttr, "Sound track attribute must be an integer greater than zero.");

                int priority;
                if (!soundNode.TryInteger("priority", out priority)) priority = 100;
                sound = new NsfEffect(this.sfx, track, (byte)priority);
            }
            loadedSounds[name] = sound;
            return name;
        }

        void updateTimer_Tick(object sender, EventArgs e)
        {
            if (soundSystem != null) soundSystem.update();
        }

        public void Unload()
        {
            foreach (Channel channel in channels) channel.stop();
            foreach (ISoundEffect sound in loadedSounds.Values) sound.Dispose();
            foreach (Music music in loadedMusic.Values) music.Dispose();
            loadedSounds.Clear();
            channels.Clear();
            loadedMusic.Clear();
            AudioManager.Instance.StopBGMPlayback();
            if (bgm != null) bgm.Release();
            if (sfx != null) sfx.Release();
        }

        public void Dispose()
        {
            Unload();
            soundSystem.release();
        }

        public Music LoadMusic(string intro, string loop, float volume)
        {
            string key = intro + loop;

            if (!string.IsNullOrEmpty(key) && loadedMusic.ContainsKey(intro + loop)) return loadedMusic[intro + loop];

            Music music = new Music(soundSystem, intro, loop, volume);
            loadedMusic[intro + loop] = music;
            return music;
        }

        public void LoadMusicNSF(string path)
        {
            bgm = new BackgroundMusic(AudioContainer.LoadContainer(path));
            AudioManager.Instance.LoadBackgroundMusic(bgm);
        }

        public void LoadSfxNSF(string path)
        {
            sfx = new SoundEffect(AudioContainer.LoadContainer(path), 1);
            AudioManager.Instance.LoadSoundEffect(sfx);
        }

        public void PlayMusicNSF(uint track)
        {
            bgm.CurrentTrack = track-1;
            AudioManager.Instance.PlayBackgroundMusic(bgm);
        }

        public void PlaySfx(string name)
        {
            if (loadedSounds.ContainsKey(name))
            {
                loadedSounds[name].Play();
            }
            else throw new GameEntityException("Tried to play sound effect called " + name + ", but none was defined!");
        }

        public void StopMusicNSF()
        {
            AudioManager.Instance.StopBGMPlayback();
        }

        public void StopSfxNSF(string name)
        {
            if (loadedSounds.ContainsKey(name))
            {
                loadedSounds[name].Stop();
            }
        }
    }
}
