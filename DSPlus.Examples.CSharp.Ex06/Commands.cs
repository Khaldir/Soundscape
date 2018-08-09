using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.VoiceNext;

namespace DSPlus.Examples
{
    public class Commands
    {
        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            // Get voicenext Client
            var vnext = ctx.Client.GetVoiceNextClient();

            // Check if the bot is already connected
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc != null)
                throw new InvalidOperationException("Already connected in this guild.");

            // Check if the user is in a voice channel
            var chn = ctx.Member?.VoiceState?.Channel;
            if (chn == null)
                throw new InvalidOperationException("You need to be in a voice channel.");

            // Connect to Voice Channel
            vnc = await vnext.ConnectAsync(chn);
            await ctx.RespondAsync("👌");
        }

        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            // Get voicenext Client
            var vnext = ctx.Client.GetVoiceNextClient();

            // Check if the bot is connected
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
                throw new InvalidOperationException("Not connected in this guild.");

            // Disconnect
            vnc.Disconnect();
            await ctx.RespondAsync("👌");
        }

        [Command("play")]
        public async Task Play(CommandContext ctx, [RemainingText] string file)
        {
            // Get voicenext Client
            var vnext = ctx.Client.GetVoiceNextClient();

            // Check connectio,
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
                throw new InvalidOperationException("Not connected in this guild.");

            // Check file exists
            if (!File.Exists(file))
                throw new FileNotFoundException("File was not found.");

            await ctx.RespondAsync("👌");

            // Send speaking indicator
            await vnc.SendSpeakingAsync(true);

            //Create ffmpeg instance
            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $@"-i ""{file}"" -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            var ffmpeg = Process.Start(psi);
            var ffout = ffmpeg.StandardOutput.BaseStream;

            //give file to ffmpeg
            var buff = new byte[3840];
            var br = 0;
            while ((br = ffout.Read(buff, 0, buff.Length)) > 0)
            {
                if (br < buff.Length) // not a full sample, mute the rest
                    for (var i = br; i < buff.Length; i++)
                        buff[i] = 0;

                await vnc.SendAsync(buff, 20);
            }

            //finish speaking
            await vnc.SendSpeakingAsync(false);
        }

    }
}
