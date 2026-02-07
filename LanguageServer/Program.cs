using LanguageServer;
using LanguageServer.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;

var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options =>
    options
        .WithInput(Console.OpenStandardInput())
        .WithOutput(Console.OpenStandardOutput())
        .ConfigureLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Warning);
        })
        .WithServices(services =>
        {
            services.AddSingleton<DocumentStore>();
            services.AddSingleton<DocumentAnalyser>();
            services.AddSingleton<TokenLocator>();
        })
        .WithHandler<TextDocumentSyncHandler>()
        .WithHandler<HoverHandler>()
        .WithHandler<CompletionHandler>()
).ConfigureAwait(false);

await server.WaitForExit.ConfigureAwait(false);
