using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Akis
{
    internal enum PlaybackResult { Confirmed, Rejected, TimedOut, SessionChanged }

    internal sealed class MediaCommands
    {
        private readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);
        internal async Task Run(Func<Task> command)
        {
            await gate.WaitAsync();
            try { await command(); }
            finally { gate.Release(); }
        }

        // A successful request can precede the player's actual state update.
        // Keep the next toggle queued until that update arrives (with a bound).
        internal static async Task<PlaybackResult> Toggle(Func<bool> current,Func<bool> playing,
            Func<Task<bool>> send,Action refresh,int timeoutMs=1500)
        {
            if(!current())return PlaybackResult.SessionChanged;
            bool before=playing();
            if(!await send())return PlaybackResult.Rejected;
            var wait=Stopwatch.StartNew();
            do
            {
                if(!current())return PlaybackResult.SessionChanged;
                refresh();
                if(playing()!=before)return PlaybackResult.Confirmed;
                await Task.Delay(50);
            }while(wait.ElapsedMilliseconds<timeoutMs);
            refresh();
            return PlaybackResult.TimedOut;
        }
    }
}
