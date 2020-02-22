using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ElevatorBot
{
    public class ElevatorBotCommands : BaseCommandModule
    {
        [Command("ping")] // let's define this method as a command
        [Description("Example ping command")] // this will be displayed to tell users what this command does when they invoke help
        [Aliases("pong")] // alternative names for the command
        public async Task Ping(CommandContext ctx) // this command takes no arguments
        {
            //trigger a typing indicator to let users know 
            await ctx.TriggerTypingAsync();

            var emoji = DiscordEmoji.FromName(ctx.Client, ":ping_pong:");

            // respond with ping
            await ctx.RespondAsync($"{emoji} Pong! Ping: {ctx.Client.Ping}ms");
        }
    }

    public class ElevatorBotVoice : BaseCommandModule
    {
        [Command("connect")]
        [Description("Play elevator music")]
        [Aliases("join", "start", "play")]
        public async Task Start(CommandContext ctx, string Choice = "null", DiscordChannel channel = null)
        {
            string[] musicType = { "kevin", "ben", "portal" };

            Choice = Choice.ToLower();
            if (!musicType.Any(x => Choice.Contains(x)))
            {
                await ctx.RespondAsync($"Please pick an elevator music type, choices are: `Kevin` (MacLeod), `Ben`(Sounds), and `Portal` Radio");
                return;
            }

            var vnext = ctx.Client.GetVoiceNext();
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnext == null)
            {
                await ctx.RespondAsync("Voice is not enabled for this bot.");
                return;
            }

            if (vnc != null)
            {
                await ctx.RespondAsync("Already connected to a channel.");
                return;
            }

            var vstat = ctx.Member?.VoiceState;
            if (vstat?.Channel == null && channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            if (channel == null)
                channel = vstat.Channel;

            vnc = await vnext.ConnectAsync(channel);
            // await ctx.RespondAsync($"Connected to `{channel.Name}");
            int rand = new Random().Next(1, 2);
            if (rand == 1)
            {
                await ctx.RespondAsync($"`{channel.Name}` Going up!");
            } else
            {
                await ctx.RespondAsync($"`{channel.Name}` Going down!");
            } 

            // play
            // await ctx.Message.RespondAsync($"Playing Elevator Music");
            await vnc.SendSpeakingAsync(true);

            //loop elevator
            do
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"elevator-{Choice}.mp3\" -ac 2 -f s16le -ar 48000 pipe:1",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                var ffmpeg = Process.Start(psi);
                var ffout = ffmpeg.StandardOutput.BaseStream;
                VoiceTransmitStream vStream = vnc.GetTransmitStream(20);

                var buff = new byte[3840];
                var br = 0;
                while ((br = ffout.Read(buff, 0, buff.Length)) > 0)
                {
                    if (br < buff.Length) // not a full sample, mute the rest
                        for (var i = br; i < buff.Length; i++)
                            buff[i] = 0;

                    await vStream.WriteAsync(buff);
                }

                await vnc.SendSpeakingAsync(false); // we're not speaking anymore

                while (vnc.IsPlaying)
                {
                    await vnc.WaitForPlaybackFinishAsync();
                }
            } while (true);
        }

        [Command("stop")]
        [Description("Stop playing music")]
        [Aliases("disconnect", "quit", "end")]
        public async Task End(CommandContext ctx)
        {
            var voiceNext = ctx.Client.GetVoiceNext();
            if (voiceNext == null)
            {
                // not enabled
                await ctx.RespondAsync("VNext is not enabled or configured.");
                return;
            }

            // check whether we are connected
            var voiceConnection = voiceNext.GetConnection(ctx.Guild);
            if (voiceConnection == null)
            {
                // not connected
                await ctx.RespondAsync("Not connected in this server.");
                return;
            }

            string channel = voiceConnection.Channel.Name;
            // disconnect
            voiceConnection.Disconnect();
            await ctx.RespondAsync($"`{channel}` has reached their floor");
        }
    }
}
