// Made by Dadpul

namespace Dadpul.Jarvis.Console
{
    using Dadpul.Jarvis.Core.Application;
    using Dadpul.Jarvis.Core.Application.Propmpts;
    using Dadpul.Jarvis.Core.Chat;
    using Dadpul.Jarvis.Core.Conversation;
    using Dadpul.Jarvis.Core.Ollama;
    using Dadpul.Jarvis.Discord;
    using Dadpul.Jarvis.Embeddings;
    using Dadpul.Jarvis.Interfaces.Frontend;
    using Dadpul.Jarvis.Interfaces.Tools;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Net.Http.Json;
    using System.Reflection;
    using Console = System.Console;

    internal static class Program
    {
        #region Methods

        private static ChatConversation CreateConversation()
        {
            ChatConversation conversation = new ChatConversation();

            conversation.AddSystemMessage(JarvisSystemPrompt.Content);

            return conversation;
        }

        private static async Task Main(string[] args)
        {
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            //Console stuff
            SetupConsole(cancellationTokenSource);
           

            //Config
            IConfigurationRoot configuration = LoadConfig();


            //DI service registrations
            var services = RegisterServices(configuration);
            using ServiceProvider serviceProvider = services.BuildServiceProvider();


            //MEF
            
            using var catalog = CreateMefCatalog(); 
            using CompositionContainer compositionContainer = CreateMefContainer(catalog);
            CompositionBatch compositionBatch = new CompositionBatch();
            compositionBatch.AddExportedValue<IConfiguration>(configuration);

            //Ollama stuff
            OllamaOptions ollamaOptions = serviceProvider
   .GetRequiredService<IOptions<OllamaOptions>>()
   .Value;

            using HttpClient ollamaHttpClient = new HttpClient
            {
                BaseAddress = ollamaOptions.BaseAddress
                  ?? throw new InvalidOperationException(
                     "Ollama:BaseAddress was not configured.")
            };
            await StartOllama(ollamaOptions,ollamaHttpClient, compositionBatch, cancellationTokenSource);
            
            
            compositionContainer.Compose(compositionBatch);


            //Tools
            RegisterTools(compositionContainer);
            

            //Orchestration
            IConversationOrchestrator? orchestrator = compositionContainer.GetExportedValue<IConversationOrchestrator>();

            IConversationProvider conversationProvider = new InMemoryConversationProvider(CreateConversation);


            List<IFrontend> frontends = new List<IFrontend>
            {
                new ConsoleFrontend()
            };

            //Discord
            var discord = CreateDiscordFrontend(configuration,serviceProvider);
            if(discord != null)
            {
                frontends.Add(discord);
            }
            

            //App
            JarvisApplication application = new JarvisApplication(orchestrator, conversationProvider, frontends);
            await application.RunAsync(cancellationTokenSource.Token);
        }

        private static DiscordFrontend CreateDiscordFrontend(IConfigurationRoot configuration, ServiceProvider serviceProvider)
        {
            string? discordToken = configuration["Discord:Token"];

            if (string.IsNullOrWhiteSpace(discordToken))
            {
                throw new InvalidOperationException("Discord:Token is missing from User Secrets.");
            }

            DiscordOptions discordOptions = serviceProvider
               .GetRequiredService<IOptions<DiscordOptions>>()
               .Value;
            if (discordOptions.Enabled)
            {
                var discord = new DiscordFrontend(discordOptions.Token);
                //await discord.RunAsync(null, CancellationToken.None);
                return discord;  
            }
            return null;
        }

        private static void RegisterTools(CompositionContainer compositionContainer)
        {
            IToolRegistry? registry = compositionContainer.GetExportedValue<IToolRegistry>();

            foreach (ITool tool in compositionContainer.GetExportedValues<ITool>())
            {
                Console.WriteLine($"registering {tool.Name} {tool.Version}");

                registry.Register(tool);
            }
        }

        private static AggregateCatalog CreateMefCatalog()
        {
            string assemblyDirectory =
               Path.GetDirectoryName(
                  Assembly.GetExecutingAssembly().Location)
               ?? throw new InvalidOperationException(
                  "Unable to resolve application directory.");

            return new AggregateCatalog(
               new AssemblyCatalog(
                  Assembly.GetExecutingAssembly()),

               new DirectoryCatalog(
                  assemblyDirectory,
                  "Dadpul.Jarvis.Tools.*.dll"),

               new DirectoryCatalog(
                  assemblyDirectory,
                  "Dadpul.Jarvis.Core.dll"));
        }
        private static CompositionContainer CreateMefContainer(AggregateCatalog catalog)
        {
            
             CompositionContainer compositionContainer = new CompositionContainer(catalog, isThreadSafe:true);
            return compositionContainer;
        }

        private static async Task  StartOllama(OllamaOptions ollamaOptions, HttpClient httpClient, CompositionBatch compositionBatch, CancellationTokenSource cancellationTokenSource)
        {
                       

            IChatModel chatModel = new OllamaChatModel(httpClient, ollamaOptions);
            IChatModelSelector chatModelSelector =   new ChatModelSelector(chatModel);
            IEmbeddingGenerator embeddingGenerator = new OllamaEmbeddingGenerator(httpClient, ollamaOptions);

            compositionBatch.AddExportedValue(chatModelSelector);
            compositionBatch.AddExportedValue(embeddingGenerator);
            compositionBatch.AddExportedValue("embeddingModel", ollamaOptions.EmbeddingModel);

            if (ollamaOptions.Preload.Enabled)
            {
                await PreloadModelAsync(
                   httpClient,
                   ollamaOptions.Model,
                   ollamaOptions.Preload.KeepAlive,
                   cancellationTokenSource.Token);
            }

        }

        private static void SetupConsole(CancellationTokenSource cancellationTokenSource)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellationTokenSource.Cancel();
            };
        }

        private static ServiceCollection RegisterServices(IConfigurationRoot configuration)
        {
            ServiceCollection services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(configuration);

            services.AddOptions<OllamaOptions>()
                .Bind(configuration.GetRequiredSection(OllamaOptions.SectionName))
                .Validate(options => options.BaseAddress is not null,
                "Ollama:BaseAddress is required.")
                .Validate(
                options => !string.IsNullOrWhiteSpace(options.Model),
                "Ollama:Model is required.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.EmbeddingModel),
                "Ollama:EmbeddingModel is required.");

            services.AddOptions<DiscordOptions>()
                .Bind(configuration.GetRequiredSection(DiscordOptions.SectionName))
                .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.Token),
                "Discord:Token is required when Discord is enabled.");

            return services;
        }

        private static IConfigurationRoot LoadConfig()
        {
           return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
                .AddEnvironmentVariables().Build();
        }

        private static async Task PreloadModelAsync(
   HttpClient httpClient,
   string model,
   int keepAlive,
   CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new ArgumentException("The Ollama model name cannot be empty.", nameof(model));
            }

            using HttpResponseMessage response = await httpClient.PostAsJsonAsync("api/generate", new { model, keep_alive = keepAlive }, cancellationToken);

            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                   $"Ollama preload failed for model '{model}'. " + $"HTTP {(int)response.StatusCode} {response.StatusCode}. "
                                                                  + $"Response: {responseBody}", inner: null, response.StatusCode);
            }
        }

        #endregion
    }
}