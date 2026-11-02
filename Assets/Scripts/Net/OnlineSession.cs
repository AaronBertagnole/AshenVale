using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace AshenVale
{
    // Online games through Unity Gaming Services: sign in anonymously, then create a session (the host gets a
    // short join code) or join one by code. The session uses Relay, so nobody needs to open ports, and it starts
    // Netcode for GameObjects on the NetworkManager that NetSession created.
    //
    // Needs the project linked to Unity Cloud (Edit > Project Settings > Services) with Relay turned on in the
    // Unity Dashboard. Playing on one PC or a local network (NetSession.HostDirect / JoinDirect) needs none of this.
    public sealed class OnlineSession
    {
        ISession session;

        public string Code { get { return session != null ? session.Code : ""; } }
        public bool Active { get { return session != null; } }

        static async Task SignInAsync()
        {
            // A build made before the project was linked to Unity Cloud carries no project ID: say so plainly instead
            // of failing deep inside the services with a long stack of "not linked" errors.
            if (string.IsNullOrEmpty(Application.cloudProjectId))
                throw new InvalidOperationException(Application.isEditor
                    ? "This project is not linked to Unity Cloud yet: Edit > Project Settings > Services, then link a project."
                    : "This build was made before the game was linked to Unity Cloud. Ask the host for a build made after linking.");
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var options = new InitializationOptions();
#if UNITY_EDITOR
                // Multiplayer Play Mode runs several game windows on one PC; each needs its own anonymous player.
                options.SetProfile("editor_" + UnityEngine.Random.Range(0, 1000000));
#endif
                await UnityServices.InitializeAsync(options);
            }
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        public async Task HostAsync(int maxPlayers)
        {
            await SignInAsync();
            var options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork();
            session = await MultiplayerService.Instance.CreateSessionAsync(options);
        }

        public async Task JoinAsync(string code)
        {
            await SignInAsync();
            session = await MultiplayerService.Instance.JoinSessionByCodeAsync((code ?? "").Trim().ToUpperInvariant());
        }

        public async void Leave()
        {
            var s = session;
            session = null;
            if (s == null) return;
            try { await s.LeaveAsync(); }
            catch (Exception e) { Debug.LogWarning("Ashen Vale: leaving the online session failed: " + e.Message); }
        }

        // A readable reason for the player, plus the likely fix when the project isn't set up for online play.
        public static string Explain(Exception e)
        {
            // The services wrap the real reason in an AggregateException; the first inner one is the useful part.
            var aggregate = e as AggregateException;
            if (aggregate != null && aggregate.InnerExceptions.Count > 0) e = aggregate.InnerExceptions[0];
            string message = e.Message ?? e.GetType().Name;
            if (message.Length > 300) message = message.Substring(0, 300) + "...";
            if (message.IndexOf("project", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("initializ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("cloud", StringComparison.OrdinalIgnoreCase) >= 0)
                message += " (Is the project linked to Unity Cloud with Relay turned on? See the README.)";
            return message;
        }
    }
}
