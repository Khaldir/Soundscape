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

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.EventArgs;

using System.IO;
using System.Diagnostics;
using Microsoft.WindowsAPICodePack.Dialogs;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.VoiceNext;

namespace DSPlus.Examples
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged // INotifyPropertyChanged is for easy UI updates
    {
        public Task AudioTask;
        public CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public CancellationToken ct;

        // this property controls the title of this window
        public string WindowTitle
        {
            get => this._window_title;
            set { this._window_title = value; this.OnPropertyChanged(nameof(this.WindowTitle)); }
        }
        private string _window_title;

        // this property will hold the text on the bot start/stop button
        public string ControlButtonText
        {
            get => this._ctl_btn_text;
            set { this._ctl_btn_text = value; this.OnPropertyChanged(nameof(this.ControlButtonText)); }
        }
        private string _ctl_btn_text;

        // this property will hold the next message the user intends to send
        public string NextMessage
        {
            get => this._next_message;
            set { this._next_message = value; this.OnPropertyChanged(nameof(this.NextMessage)); }
        }
        private string _next_message;

        // this property will enable or disable certain UI elements
        public bool EnableUI
        {
            get => this._enable_ui;
            set { this._enable_ui = value; this.OnPropertyChanged(nameof(this.EnableUI)); }
        }
        private bool _enable_ui;

        // this will hold the thread on which the bot will run
        private Task BotThread { get; set; }

        // this will hold the bot itself
        private Bot Bot { get; set; }

        // this will hold a token required to make the bot quit cleanly
        private CancellationTokenSource TokenSource { get; set; }

        // these are for UI state
        public BotGuild SelectedGuild
        {
            get => this._selected_guild;
            set
            {
                this._selected_guild = value;
                this._selected_channel = default(BotChannel);
                this._selected_voice = default(BotVoiceChannel);
                this._selected_message = default(BotMessage);
                this._selected_user = default(BotUser);
                this._selected_audio = default(AudioSource);
                this.Channels.Clear();
                this.VoiceChannels.Clear();
                this.Banter.Clear();
                this.Users.Clear();

                if (this._selected_guild.Guild != null)
                {
                    var chns = this._selected_guild.Guild.Channels
                        .Where(xc => xc.Type == ChannelType.Text)
                        .OrderBy(xc => xc.Position)
                        .Select(xc => new BotChannel(xc));
                    var vchns = this._selected_guild.Guild.Channels
                        .Where(xc => xc.Type == ChannelType.Voice)
                        .OrderBy(xc => xc.Position)
                        .Select(xc => new BotVoiceChannel(xc));
                    foreach (var xbc in chns)
                        this.Channels.Add(xbc);
                    foreach (var xbc in vchns)
                        this.VoiceChannels.Add(xbc);
                }

                this.OnPropertyChanged(nameof(this.SelectedGuild), nameof(this.SelectedChannel), nameof(this.SelectedMessage), nameof(this.SelectedVoice));
            }
        }
        private BotGuild _selected_guild;

        public BotChannel SelectedChannel
        {
            get => this._selected_channel;
            set
            {
                this._selected_channel = value;
                this._selected_message = default(BotMessage);
                this.Banter.Clear();
                this.OnPropertyChanged(nameof(this.SelectedChannel), nameof(this.SelectedMessage));
            }
        }
        private BotChannel _selected_channel;

        public BotVoiceChannel SelectedVoice
        {
            get => this._selected_voice;
            set
            {
                this._selected_voice = value;
                this._selected_user = default(BotUser);
                this.Users.Clear();
                    if(this._selected_voice.Channel != null)
                    {
                        var users = this._selected_guild.Guild.VoiceStates
                            .Where(xc => xc.Channel == _selected_voice.Channel)
                            .Select(xc => new BotUser(xc.User));
                        foreach (var xbc in users)
                            this.Users.Add(xbc);
                    }
                this.OnPropertyChanged(nameof(this.SelectedVoice), nameof(this.SelectedUser));
            }
        }
        private BotVoiceChannel _selected_voice;

        public BotUser SelectedUser
        {
            get => this._selected_user;
            set
            {
                this._selected_user = value;
                this.OnPropertyChanged(nameof(this.SelectedUser));
            }
        }
        private BotUser _selected_user;

        public BotMessage SelectedMessage
        {
            get => this._selected_message;
            set { this._selected_message = value; this.OnPropertyChanged(nameof(this.SelectedMessage)); }
        }
        private BotMessage _selected_message;

        public AudioSource SelectedAudio
        {
            get => this._selected_audio;
            set
            {
                
                //this.SelectedAudio.ClearAudio();
                //AudioIsLoaded = false;
                this._selected_audio = value;

                Debug.WriteLine(this._selected_audio.Name);


                this.OnPropertyChanged(nameof(this.SelectedAudio));
            }
        }
        private AudioSource _selected_audio;

        public AudioSource SelectedSoundEffect
        {
            get => this._selected_se;
            set
            {
                this._selected_se = value;

                Debug.WriteLine(this._selected_se.Name);

                this.OnPropertyChanged(nameof(this.SelectedSoundEffect));
            }
        }
        private AudioSource _selected_se;


        // these will hold the respective collections
        // they're observable, so any changes made to them will be automatically
        // reflected in the UI
        public ObservableCollection<BotGuild> Guilds { get; }
        public ObservableCollection<BotChannel> Channels { get; }
        public ObservableCollection<BotVoiceChannel> VoiceChannels { get; }
        public ObservableCollection<BotMessage> Banter { get; }
        public ObservableCollection<BotUser> Users { get; }
        public ObservableCollection<AudioSource> AudioSources { get; }
        public ObservableCollection<AudioSource> SoundEffects { get; }

        bool AudioRepeat = true;
        bool SoundEffectRepeat = true;

        bool AudioPlaylist = true;
        bool AudioWasPaused = false;
        bool IsAudioPlaying = false;
        bool IsSEPlaying = false;

        bool ChangedByCode = false;
        
        bool AudioIsLoaded = false;
        bool SEIsLoaded = false;

        bool FadeOutMusic = false;
        bool FadeOutSE = false;

        int LastTrackIndex = -1;

        public double MusicVolume
        {
            get { return this._music_volume; }
            set
            {
                this._music_volume = value;
                this.OnPropertyChanged(nameof(this.MusicVolume));
            }
        }
        private double _music_volume;

        public double SEVolume
        {
            get { return this._se_volume; }
            set
            {
                this._se_volume = value;
                this.OnPropertyChanged(nameof(this.SEVolume));
            }
        }
        private double _se_volume;

        public MainWindow()
        {
            this._window_title = "Soundscape";      // set the initial title
            this._ctl_btn_text = "Connect";        // set the initial button text
            this._next_message = "";                // set the initial message
            this._enable_ui = true;                 // enable the UI

            this.Guilds = new ObservableCollection<BotGuild>();                 // initialize the guild collection
            this.Channels = new ObservableCollection<BotChannel>();             // initialize the channel collection
            this.VoiceChannels = new ObservableCollection<BotVoiceChannel>();   // initialize the voice channel collection
            this.Banter = new ObservableCollection<BotMessage>();               // initialize the message collection
            this.Users = new ObservableCollection<BotUser>();                   // initialise the user collection
            this.AudioSources = new ObservableCollection<AudioSource>();        // initialise the audio collection
            this.SoundEffects = new ObservableCollection<AudioSource>();        // initialise the sound effect collection

            this.MusicVolume = 1.0;
            this.SEVolume = 1.0;

            ct = cancellationTokenSource.Token;

            GetAudioFiles("Music",true);
            GetAudioFiles("Sounds", false);

            

            InitializeComponent();
        }
        
        private void GetAudioFiles(string filepath, bool IsMusic)
        {
            
            if (IsMusic)
            {
                if(LastTrackIndex != -1)
                {
                    ChangedByCode = true;
                    LastTrackIndex = -1;
                }
                   
                this.AudioSources.Clear();
                
                string[] audiofiles = Directory.GetFiles(filepath);
                foreach (string file in audiofiles)
                {
                    AudioSource newSource = new AudioSource(file);
                    this.AudioSources.Add(newSource);
                }
            }
            else
            {
                this.SoundEffects.Clear();
                string[] audiofiles = Directory.GetFiles(filepath);
                foreach (string file in audiofiles)
                {
                    AudioSource newSource = new AudioSource(file);
                    this.SoundEffects.Add(newSource);
                }
            }
            
        }

        // this occurs when user presses the send message button
        private void Button_Click(object sender, RoutedEventArgs e)
            => this.SendMessage();

        // this occurs when user presses a button inside the message
        // text box, we use that to handle enter key press
        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            // check if the key pressed was enter
            if (e.Key == Key.Enter)
            {
                // if yes, mark the event as handled, and send
                // the message
                e.Handled = true;
                this.SendMessage();
            }
        }

        // this occurs when user presses the start/stop button
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // lock the controls until they can be used again
            this.EnableUI = false;

            // check if a bot thread is running
            if (this.BotThread == null)
            {
                //Show Guilds box
                this.GuildsBox.Visibility = Visibility.Visible;
                
                // start the bot

                // change the button's text to indicate it now 
                // stops the bot instead
                this.ControlButtonText = "Disconnect";

                // create the bot container
                this.Bot = new Bot(this.tokenBox.Text);

                // hook all the bot events
                this.Bot.Client.Ready += this.Bot_Ready;
                this.Bot.Client.GuildAvailable += this.Bot_GuildAvailable;
                this.Bot.Client.GuildCreated += this.Bot_GuildCreated;
                this.Bot.Client.GuildUnavailable += this.Bot_GuildUnavailable;
                this.Bot.Client.GuildDeleted += this.Bot_GuildDeleted;
                this.Bot.Client.MessageCreated += this.Bot_MessageCreated;
                this.Bot.Client.ClientErrored += this.Bot_ClientErrored;
                this.Bot.Client.VoiceStateUpdated += this.Bot_VoiceStateUpdated;

                // create a cancellation token, this will be used 
                // to cancel the infinite delay task
                this.TokenSource = new CancellationTokenSource();

                // finally, start the thread with the bot
                this.BotThread = Task.Run(this.BotThreadCallback);
            }
            else
            {
                //Hide Boxes
                this.GuildsBox.Visibility = Visibility.Collapsed;
                this.ChannelsBox.Visibility = Visibility.Collapsed;
                this.UserBox.Visibility = Visibility.Collapsed;

                // stop the bot

                // change the button's text to indicate it now 
                // starts the bot instead
                this.ControlButtonText = "Connect";

                // request cancelling the task preventing the 
                // bot from stopping
                // this will effectively stop the bot
                this.TokenSource.Cancel();
            }

            
        }

        // this is called by the send button and message textbox 
        // key press handler
        private void SendMessage()
        {
            // check if we have a channel selected, if not, do 
            // nothing
            if (this.SelectedChannel.Channel == null)
                return;

            // capture the next message and reset the text box
            var txt = this.NextMessage;
            this.NextMessage = "";

            // check if a message was typed in at all, if not,
            // do nothing
            if (string.IsNullOrWhiteSpace(txt))
                return;

            // start an asynchronous task which will send the 
            // message, and once it's done, set the message 
            // textbox's text to empty using the UI thread
            _ = Task.Run(() => this.BotSendMessageCallback(txt, this.SelectedChannel));
        }

        private void JoinVoice()
        {
            // check if we have a channel selected, if not, do 
            // nothing
            if (this.SelectedVoice.Channel == null)
                return;
            _ = Task.Run(() => this.BotJoinVoiceChannel(this.SelectedVoice));
        }

        // this method will be ran on the bot's thread
        // it will take care of the initialization logic, as 
        // well as actually handling the bot
        private async Task BotThreadCallback()
        {
            // this will start the bot
            await this.Bot.StartAsync().ConfigureAwait(false);

            // once the bot is started, we can enable the UI
            // elements again
            this.SetProperty(x => x.EnableUI, true);

            // here we wait indefinitely, or until the wait is
            // cancelled
            try
            {
                // the token will cancel the way once it's 
                // requested
                await Task.Delay(-1, this.TokenSource.Token).ConfigureAwait(false);
            }
            catch { /* ignore the exception; it's expected */ }

            // this will stop the bot
            await this.Bot.StopAsync().ConfigureAwait(false);

            // once the bot is stopped, we can enable the UI 
            // elements again
            this.SetProperty(x => x.EnableUI, true);
            this.SetProperty(x => x.WindowTitle, "Soundscape");

            // and reset the UI state
            this.SetProperty(x => x.SelectedVoice, default(BotVoiceChannel));
            this.SetProperty(x => x.SelectedUser, default(BotUser));
            this.InvokeAction(new Action(this.Guilds.Clear));

            // and finally, dispose of our bot stuff
            this.Bot = null;
            this.TokenSource = null;
            this.BotThread = null;
        }

        // this is used by the send message method, to 
        // asynchronously send the message
        private Task BotSendMessageCallback(string text, BotChannel chn)
            => chn.Channel.SendMessageAsync(text);

        private async Task BotJoinVoiceChannel(BotVoiceChannel chn)
        {
            //Get VoiceNext Client
            var vnext = this.Bot.Client.GetVoiceNextClient();
            //Check if bot is already connected
            var vnc = vnext.GetConnection(_selected_guild.Guild);
            if (vnc != null)
                await BotLeaveVoiceChannel();

            //Connect
            vnc = await vnext.ConnectAsync(chn.Channel);
        }

        private Task BotLeaveVoiceChannel()
        {
            //Get VoiceNext Client
            var vnext = this.Bot.Client.GetVoiceNextClient();
            //Check bot is actually connected
            var vnc = vnext.GetConnection(this.SelectedGuild.Guild);
            if (vnc == null)
                throw new InvalidOperationException("No connection to disconnect in this Guild");
            //Disconnect
            vnc.Disconnect();
            return Task.CompletedTask;
        }

        public async Task BotPlayAudio(VoiceNextConnection vnc)
        {
            bool keepLooping = (IsAudioPlaying || IsSEPlaying);

            double MusicVolumeModifier = 1.0;
            double SoundEffectVolumeModifier = 1.0;

            await vnc.SendSpeakingAsync(true);
            if (!AudioIsLoaded || !SEIsLoaded)
            {
                if(IsAudioPlaying && !AudioIsLoaded)
                {
                    this.SelectedAudio.ImportAudio();
                    AudioIsLoaded = true;
                }
                if(IsSEPlaying && !SEIsLoaded)
                {
                    this.SelectedSoundEffect.ImportAudio();
                    SEIsLoaded = true;
                }
                    
                cancellationTokenSource = new CancellationTokenSource();
                ct = cancellationTokenSource.Token;
            }
            do
            {
                if(FadeOutMusic)
                {
                    MusicVolumeModifier = MusicVolumeModifier - 0.005;
                    if (MusicVolumeModifier <= 0)
                    {
                        IsAudioPlaying = false;
                        SelectedAudio.ClearAudio();
                        MusicVolumeModifier = 1.0;
                        FadeOutMusic = false;
                    }
                }
                if (FadeOutSE)
                {
                    SoundEffectVolumeModifier = SoundEffectVolumeModifier - 0.005;
                    if (SoundEffectVolumeModifier <= 0)
                    {
                        IsSEPlaying = false;
                        SelectedSoundEffect.ClearAudio();
                        SoundEffectVolumeModifier = 1.0;
                        FadeOutSE = false;
                    }
                }
                if (ct.IsCancellationRequested)
                {
                    this.SelectedAudio.ClearAudio();
                    this.SelectedSoundEffect.ClearAudio();
                    AudioIsLoaded = false;
                    ct.ThrowIfCancellationRequested();
                }
                var audioBuff = new byte[3840];
                var seBuff = new byte[3840];
                var output = new byte[3840];
                
                // Get Audio Sample
                if (IsAudioPlaying && AudioIsLoaded)
                {
                    var br = 0;
                    
                    br = await this.SelectedAudio.AudioStream.ReadAsync(audioBuff, 0, audioBuff.Length);
                    if (br < audioBuff.Length) // not a full sample, mute the rest
                        for (var i = br; i < audioBuff.Length; i++)
                            audioBuff[i] = 0;
                    if(!IsSEPlaying)
                    {
                        output = AudioSource.ApplyVolume(audioBuff,MusicVolume*MusicVolumeModifier);
                    }
                }
                // Get Sound Effect Sample
                if(IsSEPlaying && SEIsLoaded)
                {
                    var se_br = 0;
                    
                    se_br = await this.SelectedSoundEffect.AudioStream.ReadAsync(seBuff, 0, seBuff.Length);
                    if (se_br < seBuff.Length) // not a full sample, mute the rest
                        for (var i = se_br; i < seBuff.Length; i++)
                            seBuff[i] = 0;
                    if (!IsAudioPlaying)
                    {
                        output = AudioSource.ApplyVolume(seBuff,SEVolume*SoundEffectVolumeModifier);
                    }
                }
                // Combine both samples
                if (IsAudioPlaying && IsSEPlaying)
                {
                    output = AudioSource.MergeAudioSample(audioBuff, MusicVolume*MusicVolumeModifier, seBuff, SEVolume*SoundEffectVolumeModifier);
                }

                if (ct.IsCancellationRequested)
                {
                    this.SelectedAudio.ClearAudio();
                    this.SelectedSoundEffect.ClearAudio();
                    AudioIsLoaded = false;
                    IsAudioPlaying = false;
                    IsSEPlaying = false;
                    ct.ThrowIfCancellationRequested();
                }


                await vnc.SendAsync(output, 20);
                this.OnPropertyChanged(nameof(this.SelectedAudio));
                this.OnPropertyChanged(nameof(this.SelectedSoundEffect));
                if(this.SelectedAudio.AudioStream != null && !ct.IsCancellationRequested)
                {
                    if (IsAudioPlaying && this.SelectedAudio.AudioStream.Position == this.SelectedAudio.AudioStream.Length)
                    {
                        int AudioIndex = AudioSources.IndexOf(SelectedAudio);
                        //Set current song to start again
                        if (AudioRepeat && !AudioPlaylist && !ct.IsCancellationRequested)
                            this.SelectedAudio.AudioStream.Seek(0, SeekOrigin.Begin);
                        //Queue next song
                        else if (AudioRepeat && AudioPlaylist && !ct.IsCancellationRequested)
                        {
                            this.SelectedAudio.ClearAudio();
                            AudioIndex++;
                            if (AudioIndex >= AudioSources.Count)
                                AudioIndex = 0;
                            ChangedByCode = true;
                            SelectedAudio = AudioSources.ElementAt(AudioIndex);
                            SelectedAudio.ImportAudio();
                            AudioIsLoaded = true;
                        }
                        else if (!AudioRepeat && AudioPlaylist && !ct.IsCancellationRequested)
                        {
                            this.SelectedAudio.ClearAudio();
                            AudioIndex++;
                            if (AudioIndex >= AudioSources.Count)
                                IsAudioPlaying = false;
                            else
                            {
                                ChangedByCode = true;
                                SelectedAudio = AudioSources.ElementAt(AudioIndex);
                                SelectedAudio.ImportAudio();
                                AudioIsLoaded = true;
                            }
                        }
                        else
                        {
                            this.SelectedAudio.ClearAudio();
                            AudioIsLoaded = false;
                            IsAudioPlaying = false;
                        }
                            
                    }
                }
                if(IsSEPlaying && this.SelectedSoundEffect.AudioStream != null && !ct.IsCancellationRequested)
                {
                    if (this.SelectedSoundEffect.AudioStream.Position == this.SelectedSoundEffect.AudioStream.Length)
                    {
                        int AudioIndex = AudioSources.IndexOf(SelectedSoundEffect);
                        //Set current song to start again
                        if (SoundEffectRepeat && !ct.IsCancellationRequested)
                        {
                            this.SelectedSoundEffect.AudioStream.Seek(0, SeekOrigin.Begin);
                        }
                        else
                        {
                            this.SelectedSoundEffect.ClearAudio();
                            IsSEPlaying = false;
                        }
                                                    
                    }
                }
               

                keepLooping = (IsAudioPlaying || IsSEPlaying || AudioWasPaused );
            } while (keepLooping);
            await vnc.SendSpeakingAsync(false);
            
        }

        // this handles the bot's ready event
        private Task Bot_Ready(ReadyEventArgs e)
        {
            // set the window title to indicate we are connected
            this.SetProperty(xf => xf.WindowTitle, "Soundscape (connected)");
            return Task.CompletedTask;
        }

        // called when any of the bot's guilds becomes available
        private Task Bot_GuildAvailable(GuildCreateEventArgs e)
        {
            // add the guild to the bot's guild collection
            this.InvokeAction(new Action<BotGuild>(this.AddGuild), new BotGuild(e.Guild));
            return Task.CompletedTask;
        }

        // called when any of the bot joins a guild
        private Task Bot_GuildCreated(GuildCreateEventArgs e)
        {
            // add the guild to the bot's guild collection
            this.InvokeAction(new Action<BotGuild>(this.AddGuild), new BotGuild(e.Guild));
            return Task.CompletedTask;
        }

        // called when any of the bot's guilds becomes unavailable
        private Task Bot_GuildUnavailable(GuildDeleteEventArgs e)
        {
            // remove the guild from the bot's guild collection
            this.InvokeAction(new Action<ulong>(this.RemoveGuild), e.Guild.Id);
            return Task.CompletedTask;
        }

        // called when any of the bot leaves a guild
        private Task Bot_GuildDeleted(GuildDeleteEventArgs e)
        {
            // remove the guild from the bot's guild collection
            this.InvokeAction(new Action<ulong>(this.RemoveGuild), e.Guild.Id);
            return Task.CompletedTask;
        }

        // called when the bot receives a message
        private Task Bot_MessageCreated(MessageCreateEventArgs e)
        {
            // if this message is not meant for the currently 
            // selected channel, ignore it
            if (this.SelectedChannel.Channel?.Id != e.Channel.Id)
                return Task.CompletedTask;

            // if it is, add it to the banter box
            this.InvokeAction(new Action<BotMessage>(this.AddMessage), new BotMessage(e.Message));
            return Task.CompletedTask;
        }

        // called when an unhandled exception occurs in any of the 
        // event handlers
        private Task Bot_ClientErrored(ClientErrorEventArgs e)
        {
            // show a message box by dispatching it to the UI thread
            this.InvokeAction(new Action(() => MessageBox.Show(this, $"Exception in {e.EventName}: {e.Exception.ToString()}", "Unhandled exception in the bot", MessageBoxButton.OK, MessageBoxImage.Warning)));
            return Task.CompletedTask;
        }

        private Task Bot_VoiceStateUpdated(Voice​State​Update​Event​Args e)
        {
            //Do not trigger on bot users (including self)
            if (e.User.IsBot)
                return Task.CompletedTask;
            //Add moved user to current channel if they moved here
            if (e.Channel.Equals(this.SelectedVoice.Channel))
            {
                this.InvokeAction(new Action<BotUser>(this.AddUser), new BotUser(e.User));
            }
            //If they moved elsewhere, check they weren't here previously
            else if (this.Users.Contains( new BotUser(e.User)))
            {
                //If they were, remove them
                this.InvokeAction(new Action<ulong>(this.RemoveUser), e.User.Id);
            }
            return Task.CompletedTask;
        }

        private void AddUser(BotUser usr)
            => this.Users.Add(usr);

        private void RemoveUser(ulong id)
        {
            var usr = this.Users.FirstOrDefault(xbg => xbg.Id == id);
            this.Users.Remove(usr);
        }

        // this is called when a new guild becomes available
        private void AddGuild(BotGuild gld)
            => this.Guilds.Add(gld);

        // this is called when a guild is no longer available
        private void RemoveGuild(ulong id)
        {
            var gld = this.Guilds.FirstOrDefault(xbg => xbg.Id == id);
            this.Guilds.Remove(gld);
        }

        // this is called to add a message to the banter box
        private void AddMessage(BotMessage msg)
        {
            this.Banter.Add(msg);
            this.SelectedMessage = msg;
            this.lbBanter.ScrollIntoView(msg);
        }

        // this is to call the PropertyChanged event
        private void OnPropertyChanged(params string[] props)
        {
            if (this.PropertyChanged != null)
                foreach (var prop in props)
                    this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        // this will notify the UI about changes
        public event PropertyChangedEventHandler PropertyChanged;

        private void VoiceChannelSelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            this.UserBox.Visibility = Visibility.Visible;
            this.JoinVoice();

        }

        private void GuildSelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            this.ChannelsBox.Visibility = Visibility.Visible;
        }

        private void AudioSelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ChangedByCode)
            {
                ChangedByCode = false;
                return;
            }
            else
            {
                if (LastTrackIndex != -1)
                    this.AudioSources.ElementAt(LastTrackIndex).ClearAudio();
            }
            LastTrackIndex = AudioSources.IndexOf(SelectedAudio);
            // check if we have a channel selected, if not, do 
            // nothing
            if (this.SelectedAudio.AudioStream == null)
                return;
            
            //Get VoiceNext Client
            var vnext = this.Bot.Client.GetVoiceNextClient();
            //Check bot is actually connected
            var vnc = vnext.GetConnection(this.SelectedGuild.Guild);
            if (vnc == null)
                throw new InvalidOperationException("No connection to disconnect in this Guild");

            if (!IsSEPlaying && !IsAudioPlaying)
            {
                IsAudioPlaying = true;
                AudioTask = Task.Run(() => this.BotPlayAudio(vnc));
                
            }
            else
            {
                AudioWasPaused = true;
                IsAudioPlaying = false;

                this.SelectedAudio.ImportAudio();
                this.SelectedAudio.AudioStream.Seek(0, SeekOrigin.Begin);
                AudioIsLoaded = true;
                IsAudioPlaying = true;
                AudioWasPaused = false;
            }
        }

        private void SESelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ChangedByCode)
            {
                ChangedByCode = false;
                return;
            }
            // check if we have a channel selected, if not, do 
            // nothing
            if (this.SelectedSoundEffect.AudioStream == null)
                return;
            
            //Get VoiceNext Client
            var vnext = this.Bot.Client.GetVoiceNextClient();
            //Check bot is actually connected
            var vnc = vnext.GetConnection(this.SelectedGuild.Guild);
            if (vnc == null)
                throw new InvalidOperationException("No connection to disconnect in this Guild");

            if (!IsAudioPlaying && !IsSEPlaying)
            {
                IsSEPlaying = true;
                AudioTask = Task.Run(() => this.BotPlayAudio(vnc));
            }
            else
            {
                this.SelectedSoundEffect.ImportAudio();
                this.SelectedSoundEffect.AudioStream.Seek(0, SeekOrigin.Begin);
                SEIsLoaded = true;
                IsSEPlaying = true;
            }
        }

        private void AudioFolderSelect(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            var result = dialog.ShowDialog();
            if(result == CommonFileDialogResult.Ok)
            {
                GetAudioFiles(dialog.FileName,true);
            }
        }

        private void SEFolderSelect(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                GetAudioFiles(dialog.FileName, false);
            }
        }

        private void PlaylistClicked(object sender, RoutedEventArgs e)
        {
            AudioPlaylist = (this.PlaylistCheck.IsChecked ?? false);
        }

        private void RepeatClicked(object sender, RoutedEventArgs e)
        {
            AudioRepeat = (this.RepeatCheck.IsChecked ?? false);
        }

        private void SERepeatClicked(object sender, RoutedEventArgs e)
        {
            SoundEffectRepeat = (this.SERepeatCheck.IsChecked ?? false);
        }

        private void Seekbar_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            if(!AudioWasPaused)
            {
                IsAudioPlaying = false;
                AudioWasPaused = true;
            }
            
        }

        private void Seekbar_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if((string)PauseButton.Content != "▶️")
            {
                IsAudioPlaying = true;
                AudioWasPaused = false;
            }
            
        }

        private void PausePlayClick(object sender, RoutedEventArgs e)
        {
            if(AudioWasPaused)
            {
                PauseButton.Content = "⏸️";
                IsAudioPlaying = true;
                AudioWasPaused = false;
            }
            else
            {
                PauseButton.Content = "▶️";
                AudioWasPaused = true;
                IsAudioPlaying = false;
            }
        }

        private void MusicProgressChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            string strSecs;
            string strMins;
            double Current = (SelectedAudio.AudioPosition / (48000 * 4));
            int mins = (int)Math.Floor(Current / 60.0);
            int secs = (int)(Current % 60);
            if (secs < 10)
                strSecs = "0" + secs.ToString(); 
            else
                strSecs = secs.ToString();
            if (mins< 10)
                strMins = "0" + mins.ToString();
            else
                strMins = mins.ToString();
            CurrentTime.Content = strMins + ":" + strSecs;
            Current = (SelectedAudio.AudioLength / (48000 * 4));
            mins = (int)Math.Floor(Current / 60.0);
            secs = (int)(Current % 60);
            if (secs < 10)
                strSecs = "0" + secs.ToString();
            else
                strSecs = secs.ToString();
            if (mins < 10)
                strMins = "0" + mins.ToString();
            else
                strMins = mins.ToString();
            TotalTime.Content = strMins + ":" + strSecs;
        }

        private void FadeMusicOut(object sender, RoutedEventArgs e)
        {
            FadeOutMusic = true;
        }

        private void FadeSEOut(object sender, RoutedEventArgs e)
        {
            FadeOutSE = true;
        }
    }
}
