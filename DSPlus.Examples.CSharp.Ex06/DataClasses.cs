// THIS FILE IS A PART OF EMZI0767'S BOT EXAMPLES
//
// --------
// 
// Copyright 2017 Emzi0767
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//  http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// --------
//
// This is a WPF example. It shows how to use WPF without deadlocks.

using DSharpPlus;
using DSharpPlus.Entities;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace DSPlus.Examples
{
    public struct BotGuild
    {
        public DiscordGuild Guild { get; }
        public ulong Id => this.Guild.Id;
        public string Name => this.Guild.Name;
        public string IconUrl => $"{this.Guild.IconUrl}?size=32";

        public BotGuild(DiscordGuild gld)
        {
            this.Guild = gld;
        }

        public override string ToString()
        {
            return this.Guild.Name;
        }
    }


    public struct BotChannel
    {
        public DiscordChannel Channel { get; }
        public ulong Id => this.Channel.Id;
        public string Name => this.Channel.Name;

        public BotChannel(DiscordChannel chn)
        {
            this.Channel = chn;
        }

        public override string ToString()
        {
            return $"#{this.Channel.Name}";
        }
    }

    public struct BotVoiceChannel
    {
        public DiscordChannel Channel { get; }
        public ulong Id => this.Channel.Id;
        public string Name => this.Channel.Name;

        public BotVoiceChannel(DiscordChannel chn)
        {
            this.Channel = chn;
        }

        public override string ToString()
        {
            return this.Channel.Name;
        }
    }

    public struct BotMessage
    {
        public DiscordMessage Message { get; }
        public ulong Id => this.Message.Id;
        public string AuthorName => this.Message.Author.Username;
        public string AuthorAvatarUrl => this.Message.Author.GetAvatarUrl(ImageFormat.Png, 32);
        public string Content => this.Message.Content;

        public BotMessage(DiscordMessage msg)
        {
            this.Message = msg;
        }

        public override string ToString()
        {
            return this.Message.Content;
        }
    }

    public struct BotUser
    {
        public DiscordUser User { get; }
        public ulong Id => this.User.Id;
        public string Username => this.User.Username;
        public bool IsBot => this.User.IsBot;
        public string Mention => this.User.Mention;
        public string AvatarUrl => $"{this.User.AvatarUrl}?size=32";

        public BotUser(DiscordUser user)
        {
            this.User = user;
        }

        public override string ToString()
        {
            return this.User.Username;
        }
    }

    public struct AudioSource
    {

        public static byte[] MergeAudioSample(byte[] sampleA, double volumeA, byte[] sampleB, double volumeB)
        {
            if (sampleA.Length == sampleB.Length)
            {
                byte[] output = new byte[sampleA.Length];
                for (int i = 0; i < sampleA.Length; i = i + 2)
                {
                    byte[] bytePair = BitConverter.GetBytes((short)((BitConverter.ToInt16(sampleA, i)*volumeA) + (BitConverter.ToInt16(sampleB, i)*volumeB) / (short)2));
                    output[i] = bytePair[0];
                    output[i + 1] = bytePair[1];
                }
                return output;
            }
            return null;
        }

        public static byte[] ApplyVolume(byte[] sampleA, double volumeA)
        {
            byte[] output = new byte[sampleA.Length];
            for (int i = 0; i < sampleA.Length; i = i + 2)
            {
                byte[] bytePair = BitConverter.GetBytes((short)((BitConverter.ToInt16(sampleA, i) * volumeA)));
                output[i] = bytePair[0];
                output[i + 1] = bytePair[1];
            }
            return output;
            
        }

        public bool IsLoaded;

        public string Filepath;
        public string Name
        {
            get
            {
                return Path.GetFileNameWithoutExtension(Filepath);
            }
        }
        public double AudioLength
        {
            get
            {
                if (this.AudioStream != null)
                    return (double)this.AudioStream.Length;
                return 0; 
            }
        }
        public double AudioPosition
        {
            get
            {
                if (this.AudioStream != null)
                    return this.AudioStream.Position;
                return 0;
            }
            set
            {
                long val = Convert.ToInt64(value);
                if (val % 2 == 0)
                    this.AudioStream.Position = val;
                else
                    this.AudioStream.Position = val + 1;
            }
        }

        public MemoryStream AudioStream;

        public void ImportAudio()
        {
            AudioStream.Seek(0, SeekOrigin.Begin);
            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $@"-i ""{this.Filepath}"" -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false

            };
            var ffmpeg = Process.Start(psi);
            var ffout = ffmpeg.StandardOutput.BaseStream;

            //ffmpeg.WaitForExit();

            var buff = new byte[3840];
            var br = 0;
            while ((br = ffout.Read(buff, 0, buff.Length)) > 0)
            {
                this.AudioStream.Write(buff, 0, br);
                if (br < buff.Length) // not a full sample, mute the rest
                    for (var i = br; i < buff.Length; i++)
                        buff[i] = 0;
                //AudioStream.Write(buff, 0, buff.Length);

            }
            AudioStream.Seek(0, SeekOrigin.Begin);
            IsLoaded = true;
        }

        public void ClearAudio()
        {
            this.IsLoaded = false;
            this.AudioStream = new MemoryStream();
        }

        public AudioSource(string filepathIn)
        {
            
            this.AudioStream = new MemoryStream();
            this.Filepath = filepathIn;
            this.IsLoaded = false;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
