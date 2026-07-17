// Made by Dadpul

namespace Dadpul.Jarvis.Discord;

using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Text;

using Dadpul.Jarvis.Interfaces.Frontend;

using global::Discord;
using global::Discord.WebSocket;

[Export(typeof(IFrontend))]
public sealed class DiscordFrontend : IFrontend
{
   #region Constants and Fields

   private readonly DiscordSocketClient client;

   private readonly ConcurrentDictionary<string, ResponseState> responses = new();

   private readonly string token;

   #endregion

   #region Constructors and Destructors

   public DiscordFrontend(string token)
   {
      if (string.IsNullOrWhiteSpace(token))
      {
         throw new ArgumentException("The Discord bot token cannot be empty.", nameof(token));
      }

      this.token = token;

      var configuration = new DiscordSocketConfig
      {
         GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
      };

      client = new DiscordSocketClient(configuration);
   }

   #endregion

   #region IFrontend Members

   public string Name => "Discord";

   public Task BeginResponseAsync(string conversationId, CancellationToken cancellationToken)
   {
      responses.AddOrUpdate(conversationId, _ => new ResponseState(), (_, existing) =>
      {
         existing.Content.Clear();
         return existing;
      });

      return Task.CompletedTask;
   }

   public async Task CompleteResponseAsync(string conversationId, ChatMetrics? metrics, CancellationToken cancellationToken)
   {
      if (!responses.TryRemove(conversationId, out var response))
      {
         return;
      }

      var content = response.Content.ToString();

      if (string.IsNullOrWhiteSpace(content))
      {
         content = "I could not generate a response.";
      }

      var channel = client.GetChannel(response.ChannelId) as IMessageChannel;

      if (channel is null)
      {
         return;
      }

      await channel.SendMessageAsync(content);
   }

   public async Task RunAsync(Func<FrontendMessage, CancellationToken, Task> messageHandler, CancellationToken cancellationToken)
   {
      client.Log += OnLogAsync;

      client.MessageReceived += message => OnMessageReceivedAsync(message, messageHandler, cancellationToken);

        Console.WriteLine("Logging in Discord bot");
      await client.LoginAsync(TokenType.Bot, token);
      await client.StartAsync();

        Console.WriteLine("Successfully logged in discord bot");
      try
      {
         await Task.Delay(Timeout.Infinite, cancellationToken);
      }
      finally
      {
         await client.StopAsync();
         await client.LogoutAsync();
      }
   }

   public Task WriteResponseChunkAsync(string conversationId, string content, CancellationToken cancellationToken)
   {
      if (responses.TryGetValue(conversationId, out var response))
      {
         response.Content.Append(content);
      }

      return Task.CompletedTask;
   }

   #endregion

   #region Methods

   private Task OnLogAsync(LogMessage message)
   {
      Console.WriteLine($"Discord: {message}");

      return Task.CompletedTask;
   }

   private async Task OnMessageReceivedAsync(SocketMessage message, Func<FrontendMessage, CancellationToken, Task> messageHandler,
      CancellationToken cancellationToken)
   {
      if (message.Author.IsBot)
      {
         return;
      }

      if (string.IsNullOrWhiteSpace(message.Content))
      {
         return;
      }
        //await message.Channel.SendMessageAsync("hello there");
        //return;
        var conversationId = message.Channel.Id.ToString();

      responses.AddOrUpdate(conversationId, _ => new ResponseState { ChannelId = message.Channel.Id }, (_, existing) =>
      {
         existing.ChannelId = message.Channel.Id;
         return existing;
      });

      var frontendMessage = new FrontendMessage(conversationId, message.Content);

      await messageHandler(frontendMessage, cancellationToken);
   }

   #endregion

   private sealed class ResponseState
   {
      #region Public Properties

      public ulong ChannelId { get; set; }

      public StringBuilder Content { get; } = new();

      #endregion
   }
}