using System;
using System.Collections.Generic;
using System.CommandLine;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OmniRAG.Core.Configuration;
using OmniRAG.Core.Constants;
using OmniRAG.Core.Exceptions;
using OmniRAG.Core.Interfaces;
using OmniRAG.Core.Models;
using OmniRAG.Core.Services;
using OmniRAG.Core.Validation;
using OmniRAG.Infrastructure.Chunking;
using OmniRAG.Infrastructure.DocumentLoaders;
using OmniRAG.Infrastructure.Embeddings;
using OmniRAG.Infrastructure.LanguageModels;
using OmniRAG.Infrastructure.Monitoring;
using OmniRAG.Infrastructure.Repositories;
using OmniRAG.Infrastructure.VectorStores;

using Spectre.Console;

namespace OmniRAG.Console;

/// <summary>
/// Main program entry point.
/// Clean Architecture: Composition root for dependency injection.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        (int defaultChunkSize, int defaultOverlap, int defaultTopK, int minTopK, int maxTopK) = LoadEarlyConfiguration();

        RootCommand rootCommand = CreateRootCommand(defaultChunkSize, defaultOverlap, defaultTopK, minTopK, maxTopK);

        (Option<EmbeddingStrategy> embeddingOption,
         Option<ChunkingStrategy> strategyOption,
         Option<int> chunkSizeOption,
         Option<int> overlapOption,
         Option<RetrievalStrategy> retrievalOption,
         Option<int> topKOption,
         Option<float> thresholdOption,
         Option<bool> listStrategiesOption) = CreateCommandLineOptions(defaultChunkSize, defaultOverlap, defaultTopK, minTopK, maxTopK);

        AddOptionsToCommand(rootCommand, embeddingOption, strategyOption, chunkSizeOption, 
            overlapOption, retrievalOption, topKOption, thresholdOption, listStrategiesOption);

        SetupCommandHandler(rootCommand, embeddingOption, strategyOption, chunkSizeOption, 
            overlapOption, retrievalOption, topKOption, thresholdOption, listStrategiesOption);

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Loads early configuration to read command-line defaults.
    /// Reduces Main method complexity (Rule 8).
    /// </summary>
    private static (int defaultChunkSize, int defaultOverlap, int defaultTopK, int minTopK, int maxTopK) LoadEarlyConfiguration()
    {
        IConfiguration earlyConfig = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        int defaultChunkSize = earlyConfig.GetValue<int>("OmniRAG:CommandLineDefaults:ChunkSize", DefaultValues.CommandLineDefaultChunkSize);
        int defaultOverlap = earlyConfig.GetValue<int>("OmniRAG:CommandLineDefaults:Overlap", DefaultValues.CommandLineDefaultOverlap);
        int defaultTopK = earlyConfig.GetValue<int>("OmniRAG:CommandLineDefaults:TopK", DefaultValues.CommandLineDefaultTopK);
        int minTopK = earlyConfig.GetValue<int>("OmniRAG:CommandLineDefaults:MinTopK", DefaultValues.CommandLineMinTopK);
        int maxTopK = earlyConfig.GetValue<int>("OmniRAG:CommandLineDefaults:MaxTopK", DefaultValues.CommandLineMaxTopK);

        return (defaultChunkSize, defaultOverlap, defaultTopK, minTopK, maxTopK);
    }

    /// <summary>
    /// Creates the root command for the CLI.
    /// Reduces Main method complexity (Rule 8).
    /// </summary>
    private static RootCommand CreateRootCommand(
        int defaultChunkSize, 
        int defaultOverlap, 
        int defaultTopK, 
        int minTopK, 
        int maxTopK)
    {
        return new RootCommand("OmniRAG - RAG-powered Technical Manual Assistant");
    }

    /// <summary>
    /// Creates all command-line options.
    /// Reduces Main method complexity (Rule 8).
    /// </summary>
    private static (
        Option<EmbeddingStrategy> embeddingOption,
        Option<ChunkingStrategy> strategyOption,
        Option<int> chunkSizeOption,
        Option<int> overlapOption,
        Option<RetrievalStrategy> retrievalOption,
        Option<int> topKOption,
        Option<float> thresholdOption,
        Option<bool> listStrategiesOption) CreateCommandLineOptions(
        int defaultChunkSize, 
        int defaultOverlap, 
        int defaultTopK, 
        int minTopK, 
        int maxTopK)
    {
        Option<EmbeddingStrategy> embeddingOption = CreateEmbeddingOption();
        Option<ChunkingStrategy> strategyOption = CreateChunkingOption();
        Option<int> chunkSizeOption = CreateChunkSizeOption(defaultChunkSize);
        Option<int> overlapOption = CreateOverlapOption(defaultOverlap);
        Option<RetrievalStrategy> retrievalOption = CreateRetrievalOption();
        Option<int> topKOption = CreateTopKOption(defaultTopK, minTopK, maxTopK);
        Option<float> thresholdOption = CreateThresholdOption();
        Option<bool> listStrategiesOption = CreateListStrategiesOption();

        return (embeddingOption, strategyOption, chunkSizeOption, overlapOption,
                retrievalOption, topKOption, thresholdOption, listStrategiesOption);
    }

    private static Option<EmbeddingStrategy> CreateEmbeddingOption()
    {
        Option<EmbeddingStrategy> option = new Option<EmbeddingStrategy>(
            name: "--embedding-strategy",
            description: "The embedding model to use for vector generation",
            getDefaultValue: () => EmbeddingStrategy.MiniLM);
        option.AddAlias("-e");
        return option;
    }

    private static Option<ChunkingStrategy> CreateChunkingOption()
    {
        Option<ChunkingStrategy> option = new Option<ChunkingStrategy>(
            name: "--chunking-strategy",
            description: "The chunking strategy to use for document processing",
            getDefaultValue: () => ChunkingStrategy.Semantic);
        option.AddAlias("-s");
        return option;
    }

    private static Option<int> CreateChunkSizeOption(int defaultChunkSize)
    {
        Option<int> option = new Option<int>(
            name: "--chunk-size",
            description: $"Target chunk size in tokens (default: {defaultChunkSize})",
            getDefaultValue: () => defaultChunkSize);
        option.AddAlias("-c");
        return option;
    }

    private static Option<int> CreateOverlapOption(int defaultOverlap)
    {
        Option<int> option = new Option<int>(
            name: "--overlap",
            description: $"Overlap size in tokens for applicable strategies " +
                $"(default: {defaultOverlap})",
            getDefaultValue: () => defaultOverlap);
        option.AddAlias("-o");
        return option;
    }

    private static Option<RetrievalStrategy> CreateRetrievalOption()
    {
        Option<RetrievalStrategy> option = new Option<RetrievalStrategy>(
            name: "--retrieval-strategy",
            description: "The retrieval strategy to use for vector search",
            getDefaultValue: () => RetrievalStrategy.TopK);
        option.AddAlias("-r");
        return option;
    }

    private static Option<int> CreateTopKOption(int defaultTopK, int minTopK, int maxTopK)
    {
        Option<int> option = new Option<int>(
            name: "--top-k",
            description: $"Maximum number of results to retrieve " +
                $"({minTopK}-{maxTopK}, default: {defaultTopK})",
            getDefaultValue: () => defaultTopK);
        option.AddAlias("-k");
        option.AddValidator(result =>
        {
            int value = result.GetValueForOption(option);
            if (value < minTopK || value > maxTopK)
            {
                result.ErrorMessage = ValidationMessages.TopKMustBeBetween1And50;
            }
        });
        return option;
    }

    private static Option<float> CreateThresholdOption()
    {
        Option<float> option = new Option<float>(
            name: "--min-similarity",
            description: "Minimum similarity threshold " +
                "(0.0-1.0, default: strategy-dependent)",
            getDefaultValue: () => DefaultValues.CommandLineUnsetThreshold);
        option.AddAlias("-t");
        option.AddValidator(result =>
        {
            float value = result.GetValueForOption(option);
            if (value != DefaultValues.CommandLineUnsetThreshold && 
                (value < DefaultValues.CommandLineMinSimilarity || 
                 value > DefaultValues.CommandLineMaxSimilarity))
            {
                result.ErrorMessage = ValidationMessages.SimilarityThresholdMustBeBetween0And1;
            }
        });
        return option;
    }

    private static Option<bool> CreateListStrategiesOption()
    {
        Option<bool> option = new Option<bool>(
            name: "--list-strategies",
            description: "List available strategies " +
                "(chunking, embedding, retrieval) and exit",
            getDefaultValue: () => false);
        option.AddAlias("-l");
        return option;
    }

    /// <summary>
    /// Adds all options to the root command.
    /// Reduces Main method complexity (Rule 8).
    /// </summary>
    private static void AddOptionsToCommand(
        RootCommand rootCommand,
        Option<EmbeddingStrategy> embeddingOption,
        Option<ChunkingStrategy> strategyOption,
        Option<int> chunkSizeOption,
        Option<int> overlapOption,
        Option<RetrievalStrategy> retrievalOption,
        Option<int> topKOption,
        Option<float> thresholdOption,
        Option<bool> listStrategiesOption)
    {
        rootCommand.AddOption(embeddingOption);
        rootCommand.AddOption(strategyOption);
        rootCommand.AddOption(chunkSizeOption);
        rootCommand.AddOption(overlapOption);
        rootCommand.AddOption(retrievalOption);
        rootCommand.AddOption(topKOption);
        rootCommand.AddOption(thresholdOption);
        rootCommand.AddOption(listStrategiesOption);
    }

    /// <summary>
    /// Sets up the command handler for the root command.
    /// Reduces Main method complexity (Rule 8).
    /// </summary>
    private static void SetupCommandHandler(
        RootCommand rootCommand,
        Option<EmbeddingStrategy> embeddingOption,
        Option<ChunkingStrategy> strategyOption,
        Option<int> chunkSizeOption,
        Option<int> overlapOption,
        Option<RetrievalStrategy> retrievalOption,
        Option<int> topKOption,
        Option<float> thresholdOption,
        Option<bool> listStrategiesOption)
    {
        rootCommand.SetHandler(
            async (embedding, strategy, chunkSize, overlap, retrieval, topK, threshold, listStrategies) =>
            {
                await HandleCommandAsync(
                    embedding, 
                    strategy, 
                    chunkSize, 
                    overlap, 
                    retrieval, 
                    topK, 
                    threshold, 
                    listStrategies);
            }, 
            embeddingOption, 
            strategyOption, 
            chunkSizeOption, 
            overlapOption, 
            retrievalOption, 
            topKOption, 
            thresholdOption, 
            listStrategiesOption);
    }

    /// <summary>
    /// Handles command execution logic.
    /// Reduces Main method complexity (Rule 8).
    /// </summary>
    private static async Task HandleCommandAsync(
        EmbeddingStrategy embedding,
        ChunkingStrategy strategy,
        int chunkSize,
        int overlap,
        RetrievalStrategy retrieval,
        int topK,
        float threshold,
        bool listStrategies)
    {
        if (listStrategies)
        {
            DisplayStrategies();
            return;
        }

        if (!ValidateCommandParameters(chunkSize, overlap))
        {
            return;
        }

        DisplayBanner();
        DisplayConfiguration(embedding, strategy, chunkSize, overlap, retrieval, topK, threshold);

        IConfiguration configuration = BuildConfiguration();

        if (!TryValidateConfiguration(configuration))
        {
            return;
        }

        IHost host = BuildHostWithDependencies(configuration, embedding, strategy, chunkSize, overlap, retrieval, topK, threshold);
        LogApplicationStartup(host, embedding, strategy, retrieval, topK);
        await RunApplicationAsync(host);
    }

    private static bool ValidateCommandParameters(int chunkSize, int overlap)
    {
        if (chunkSize <= 0)
        {
            AnsiConsole.MarkupLine($"[red]{ValidationMessages.ChunkSizeMustBePositive}[/]");
            return false;
        }

        if (overlap < 0)
        {
            AnsiConsole.MarkupLine($"[red]{ValidationMessages.OverlapCannotBeNegative}[/]");
            return false;
        }

        if (overlap >= chunkSize)
        {
            AnsiConsole.MarkupLine($"[red]{ValidationMessages.OverlapMustBeLessThanChunkSize}[/]");
            return false;
        }

        return true;
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    private static bool TryValidateConfiguration(IConfiguration configuration)
    {
        try
        {
            ValidateConfiguration(configuration);
            return true;
        }
        catch (ConfigurationValidationException ex)
        {
            DisplayConfigurationErrors(ex);
            return false;
        }
    }

    private static void DisplayConfigurationErrors(ConfigurationValidationException ex)
    {
        AnsiConsole.MarkupLine("[red bold]❌ Configuration Validation Failed[/]");
        AnsiConsole.WriteLine();
        
        foreach (string error in ex.ValidationErrors)
        {
            if (error.StartsWith("  →"))
            {
                AnsiConsole.MarkupLine($"[yellow]{error}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]• {error}[/]");
            }
        }
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]💡 Hint: Run the validation script:[/]");
        AnsiConsole.MarkupLine("[cyan]  .\\validate_setup.ps1[/]");
        AnsiConsole.WriteLine();
    }

    private static IHost BuildHostWithDependencies(
        IConfiguration configuration,
        EmbeddingStrategy embedding,
        ChunkingStrategy strategy,
        int chunkSize,
        int overlap,
        RetrievalStrategy retrieval,
        int topK,
        float threshold)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddOptions<OmniRAGOptions>()
                    .Bind(configuration.GetSection(OmniRAGOptions.Section))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                
                services.AddSingleton<IValidateOptions<OmniRAGOptions>, OmniRAGOptionsValidator>();
                
                ConfigureServices(services, configuration, embedding, strategy, chunkSize, overlap, retrieval, topK, threshold);
            })
            .Build();
    }

    private static void LogApplicationStartup(
        IHost host, 
        EmbeddingStrategy embedding, 
        ChunkingStrategy strategy, 
        RetrievalStrategy retrieval, 
        int topK)
    {
        ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("OmniRAG application started");
        logger.LogInformation(
            "Configuration: Embedding={Embedding}, Chunking={Chunking}, Retrieval={Retrieval}, TopK={TopK}",
            embedding,
            strategy,
            retrieval,
            topK);
    }

    private static async Task RunApplicationAsync(IHost host)
    {
        using IServiceScope scope = host.Services.CreateScope();
        OmniRAGApp app = scope.ServiceProvider.GetRequiredService<OmniRAGApp>();
        await app.RunAsync();
    }

    /// <summary>
    /// Validates configuration at startup using strongly-typed options and custom validators.
    /// Implements fail-fast principle: discover errors at startup, not during first request.
    /// </summary>
    private static void ValidateConfiguration(IConfiguration configuration)
    {
        // Bind configuration to strongly-typed options
        OmniRAGOptions options = new OmniRAGOptions();
        configuration.GetSection(OmniRAGOptions.Section).Bind(options);

        // Validate using Data Annotations
        ValidationContext validationContext = new ValidationContext(options);
        List<System.ComponentModel.DataAnnotations.ValidationResult> validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        
        if (!Validator.TryValidateObject(options, validationContext, validationResults, validateAllProperties: true))
        {
            List<string> errors = validationResults
                .Select(vr => vr.ErrorMessage ?? "Unknown validation error")
                .ToList();
            
            throw new ConfigurationValidationException(errors);
        }

        // Custom validation using IValidateOptions
        OmniRAGOptionsValidator customValidator = new OmniRAGOptionsValidator();
        ValidateOptionsResult customResult = customValidator.Validate(null, options);
        
        if (customResult.Failed)
        {
            throw new ConfigurationValidationException(customResult.Failures ?? new[] { "Custom validation failed" });
        }
    }

    private static void ConfigureServices(
        IServiceCollection services, 
        IConfiguration configuration,
        EmbeddingStrategy embeddingStrategy,
        ChunkingStrategy chunkingStrategy,
        int chunkSize,
        int overlap,
        RetrievalStrategy retrievalStrategy,
        int topK,
        float minSimilarity)
    {
        services.AddSingleton(configuration);

        ConfigurationPaths paths = ExtractConfigurationPaths(configuration);
        RegisterInfrastructureServices(services, embeddingStrategy, chunkingStrategy, chunkSize, overlap, paths);
        
        RetrievalOptions retrievalOptions = RetrievalOptions.Create(
            retrievalStrategy,
            topK,
            minSimilarity >= 0 ? minSimilarity : null);

        ConfigureLanguageModel(services, configuration);
        RegisterRagEngine(services, retrievalOptions);
        ConfigureDocumentMonitoring(services, configuration);
        RegisterApplication(services);
    }

    /// <summary>
    /// Extracts and validates required configuration paths.
    /// Reduces method complexity (Rule 16).
    /// </summary>
    private static ConfigurationPaths ExtractConfigurationPaths(IConfiguration configuration)
    {
        return new ConfigurationPaths
        {
            PythonDll = configuration["OmniRAG:Python:DllPath"] 
                ?? throw new InvalidOperationException("Python DLL path not configured"),
            PythonHome = configuration["OmniRAG:Python:HomePath"] 
                ?? throw new InvalidOperationException("Python home path not configured"),
            ChromaDir = configuration["OmniRAG:ChromaPersistDirectory"] 
                ?? throw new InvalidOperationException("Chroma persist directory not configured"),
            PdfDirectory = configuration["OmniRAG:PdfDirectory"]
                ?? throw new InvalidOperationException("PDF directory not configured")
        };
    }

    /// <summary>
    /// Registers all infrastructure services.
    /// Reduces method complexity (Rule 16).
    /// </summary>
    private static void RegisterInfrastructureServices(
        IServiceCollection services,
        EmbeddingStrategy embeddingStrategy,
        ChunkingStrategy chunkingStrategy,
        int chunkSize,
        int overlap,
        ConfigurationPaths paths)
    {
        services.AddSingleton<IDocumentRepository>(sp =>
            new FileSystemDocumentRepository(
                paths.PdfDirectory,
                sp.GetService<ILogger<FileSystemDocumentRepository>>()));

        services.AddSingleton<IEmbeddingService>(sp => 
            EmbeddingServiceFactory.Create(
                embeddingStrategy, 
                paths.PythonDll, 
                paths.PythonHome, 
                sp.GetService<ILoggerFactory>()));
        
        services.AddSingleton<IVectorStore>(sp => 
            new ChromaVectorStore(
                paths.ChromaDir, 
                sp.GetService<ILogger<ChromaVectorStore>>()));
        
        services.AddSingleton<ITextChunker>(sp => 
            TextChunkerFactory.Create(
                chunkingStrategy, 
                chunkSize, 
                overlap, 
                sp.GetService<ILoggerFactory>()));

        services.AddSingleton<IDocumentLoader>(sp => 
            new PdfDocumentLoader(
                sp.GetRequiredService<IEmbeddingService>(),
                sp.GetRequiredService<ITextChunker>(),
                sp.GetService<ILogger<PdfDocumentLoader>>()));
    }

    /// <summary>
    /// Registers the RAG engine with all dependencies.
    /// Reduces method complexity (Rule 16).
    /// </summary>
    private static void RegisterRagEngine(IServiceCollection services, RetrievalOptions retrievalOptions)
    {
        services.AddSingleton<IRagEngine>(sp =>
        {
            ILogger<RagEngine>? logger = sp.GetService<ILogger<RagEngine>>();
            return new RagEngine(
                retrievalOptions,
                sp.GetRequiredService<IDocumentLoader>(),
                sp.GetRequiredService<IEmbeddingService>(),
                sp.GetRequiredService<IVectorStore>(),
                sp.GetService<ILanguageModel>(),
                logger);
        });
    }

    /// <summary>
    /// Registers the main application service.
    /// Reduces method complexity (Rule 16).
    /// </summary>
    private static void RegisterApplication(IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            IDocumentMonitor? monitor = sp.GetService<IDocumentMonitor>();
            IDocumentRepository? repository = sp.GetService<IDocumentRepository>();
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            return new OmniRAGApp(
                configuration,
                sp.GetRequiredService<IRagEngine>(),
                monitor,
                repository,
                sp.GetService<ILogger<OmniRAGApp>>());
        });
    }

    /// <summary>
    /// Configures the language model service with graceful degradation.
    /// Reduces deep nesting in ConfigureServices method (Rule 17).
    /// </summary>
    private static void ConfigureLanguageModel(IServiceCollection services, IConfiguration configuration)
    {
        bool phi4Enabled = configuration.GetValue<bool>("OmniRAG:Phi4:Enabled", false);
        if (!phi4Enabled)
        {
            AnsiConsole.MarkupLine($"[dim]{DisplayMessages.Phi4DisabledInConfiguration}[/]");
            services.AddSingleton<ILanguageModel>(sp => null!);
            return;
        }

        string? modelPath = configuration["OmniRAG:Phi4:ModelPath"];
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            AnsiConsole.MarkupLine($"[yellow]{DisplayMessages.Phi4ModelPathNotConfigured}[/]");
            AnsiConsole.MarkupLine($"[yellow]{DisplayMessages.RunningInRetrievalOnlyMode}[/]");
            services.AddSingleton<ILanguageModel>(sp => null!);
            return;
        }

        int maxTokens = configuration.GetValue<int>("OmniRAG:Phi4:MaxTokens", DefaultValues.DefaultMaxTokens);
        float temperature = configuration.GetValue<float>("OmniRAG:Phi4:Temperature", DefaultValues.DefaultTemperature);

        TryRegisterPhi4LanguageModel(services, modelPath, maxTokens, temperature);
    }

    /// <summary>
    /// Attempts to register Phi-4 language model with error handling.
    /// Reduces deep nesting (Rule 17).
    /// </summary>
    private static void TryRegisterPhi4LanguageModel(
        IServiceCollection services, 
        string modelPath, 
        int maxTokens, 
        float temperature)
    {
        try
        {
            services.AddSingleton<ILanguageModel>(sp => 
                new Phi4LanguageModel(
                    modelPath, 
                    maxTokens, 
                    temperature, 
                    sp.GetService<ILogger<Phi4LanguageModel>>()));
            
            AnsiConsole.MarkupLine($"[green]{DisplayMessages.Phi4LanguageModelEnabled}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]{string.Format(DisplayMessages.Phi4InitializationFailed, ex.Message)}[/]");
            AnsiConsole.MarkupLine($"[yellow]{DisplayMessages.RunningInRetrievalOnlyMode}[/]");
            services.AddSingleton<ILanguageModel>(sp => null!);
        }
    }

    /// <summary>
    /// Configures document monitoring service.
    /// Reduces deep nesting in ConfigureServices method (Rule 17).
    /// </summary>
    private static void ConfigureDocumentMonitoring(IServiceCollection services, IConfiguration configuration)
    {
        bool enableAutoIndexing = configuration.GetValue<bool>("OmniRAG:EnableAutoIndexing", true);
        if (!enableAutoIndexing)
        {
            AnsiConsole.MarkupLine($"[dim]{DisplayMessages.DocumentMonitoringDisabled}[/]");
            return;
        }

        string pdfDirectoryPath = configuration["OmniRAG:PdfDirectory"] 
            ?? throw new InvalidOperationException("PDF directory not configured");
        
        string fullPdfPath = Path.IsPathRooted(pdfDirectoryPath) 
            ? pdfDirectoryPath 
            : Path.GetFullPath(pdfDirectoryPath);

        services.AddSingleton<IDocumentMonitor>(sp =>
        {
            ILogger<PdfDirectoryMonitor>? logger = sp.GetService<ILogger<PdfDirectoryMonitor>>();
            return new PdfDirectoryMonitor(fullPdfPath, logger);
        });

        AnsiConsole.MarkupLine($"[green]{DisplayMessages.DocumentMonitoringEnabled}[/]");
    }

    private static void DisplayBanner()
    {
        AnsiConsole.Write(
            new FigletText("OmniRAG")
                .Centered()
                .Color(Color.Blue));

        AnsiConsole.MarkupLine("[bold cyan]Elevate Your Document Intelligence[/]");
        AnsiConsole.MarkupLine("[dim]RAG-powered with Phi-4 + Semantic Kernel + ChromaDB[/]");
        AnsiConsole.WriteLine();
    }

    private static void DisplayConfiguration(
        EmbeddingStrategy embeddingStrategy, 
        ChunkingStrategy chunkingStrategy, 
        int chunkSize, 
        int overlap,
        RetrievalStrategy retrievalStrategy,
        int topK,
        float minSimilarity)
    {
        Table table = CreateConfigurationTable();
        AddEmbeddingConfiguration(table, embeddingStrategy);
        AddChunkingConfiguration(table, chunkingStrategy, chunkSize, overlap);
        AddRetrievalConfiguration(table, retrievalStrategy, topK, minSimilarity);
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static Table CreateConfigurationTable()
    {
        return new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[yellow]Configuration[/]")
            .AddColumn("[cyan]Value[/]");
    }

    private static void AddEmbeddingConfiguration(Table table, EmbeddingStrategy embeddingStrategy)
    {
        table.AddRow("[bold]Embedding Model[/]", "");
        table.AddRow(
            "  Strategy", 
            $"[green]{embeddingStrategy}[/] - {embeddingStrategy.GetDescription()}");
        table.AddRow("  Model Name", $"[dim]{embeddingStrategy.GetModelName()}[/]");
        table.AddRow("  Dimensions", $"[green]{embeddingStrategy.GetDimensions()}[/]");
        table.AddRow("  Performance", embeddingStrategy.GetPerformance());
        table.AddRow("  Model Size", embeddingStrategy.GetModelSize());
    }

    private static void AddChunkingConfiguration(
        Table table, 
        ChunkingStrategy chunkingStrategy, 
        int chunkSize, 
        int overlap)
    {
        table.AddRow("[bold]Chunking Strategy[/]", "");
        table.AddRow(
            "  Strategy", 
            $"[green]{chunkingStrategy}[/] - {chunkingStrategy.GetDescription()}");
        table.AddRow("  Target Chunk Size", $"[green]{chunkSize}[/] tokens");
        table.AddRow("  Overlap Size", $"[green]{overlap}[/] tokens");
        table.AddRow("  Recommended For", chunkingStrategy.GetRecommendedUseCase());
    }

    private static void AddRetrievalConfiguration(
        Table table, 
        RetrievalStrategy retrievalStrategy, 
        int topK, 
        float minSimilarity)
    {
        table.AddRow("[bold]Retrieval Strategy[/]", "");
        table.AddRow(
            "  Strategy", 
            $"[green]{retrievalStrategy}[/] - {retrievalStrategy.GetDescription()}");
        table.AddRow("  Top-K Results", $"[green]{topK}[/]");
        float actualThreshold = minSimilarity >= 0 
            ? minSimilarity 
            : retrievalStrategy.GetDefaultThreshold();
        table.AddRow("  Min Similarity", $"[green]{actualThreshold:P0}[/]");
        table.AddRow("  Performance", retrievalStrategy.GetPerformance());
        table.AddRow("  Result Count", retrievalStrategy.GetResultCount());
    }

    private static void DisplayStrategies()
    {
        DisplayStrategiesBanner();
        DisplayEmbeddingStrategies();
        DisplayChunkingStrategies();
        DisplayRetrievalStrategies();
        DisplayUsageExamples();
    }

    private static void DisplayStrategiesBanner()
    {
        AnsiConsole.Write(
            new FigletText("Available Strategies")
                .Centered()
                .Color(Color.Blue));
        AnsiConsole.WriteLine();
    }

    private static void DisplayEmbeddingStrategies()
    {
        AnsiConsole.MarkupLine("[bold yellow]EMBEDDING STRATEGIES[/]");
        AnsiConsole.WriteLine();

        Table embeddingTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[yellow]Strategy[/]")
            .AddColumn("[cyan]Model[/]")
            .AddColumn("[green]Dimensions[/]")
            .AddColumn("[blue]Performance[/]")
            .AddColumn("[magenta]Size[/]")
            .AddColumn("[white]Best For[/]");

        foreach (EmbeddingStrategy strategy in Enum.GetValues<EmbeddingStrategy>())
        {
            embeddingTable.AddRow(
                $"[bold]{strategy}[/]",
                $"[dim]{strategy.GetModelName()}[/]",
                strategy.GetDimensions().ToString(),
                strategy.GetPerformance(),
                strategy.GetModelSize(),
                strategy.GetRecommendedUseCase());
        }

        AnsiConsole.Write(embeddingTable);
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }

    private static void DisplayChunkingStrategies()
    {
        AnsiConsole.MarkupLine("[bold yellow]CHUNKING STRATEGIES[/]");
        AnsiConsole.WriteLine();

        Table chunkingTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[yellow]Strategy[/]")
            .AddColumn("[cyan]Description[/]")
            .AddColumn("[green]Best For[/]");

        foreach (ChunkingStrategy strategy in Enum.GetValues<ChunkingStrategy>())
        {
            chunkingTable.AddRow(
                $"[bold]{strategy}[/]",
                strategy.GetDescription(),
                strategy.GetRecommendedUseCase());
        }

        AnsiConsole.Write(chunkingTable);
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }

    private static void DisplayRetrievalStrategies()
    {
        AnsiConsole.MarkupLine("[bold yellow]RETRIEVAL STRATEGIES[/]");
        AnsiConsole.WriteLine();

        Table retrievalTable = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[yellow]Strategy[/]")
            .AddColumn("[cyan]Description[/]")
            .AddColumn("[green]Best For[/]")
            .AddColumn("[blue]Performance[/]")
            .AddColumn("[magenta]Result Count[/]");

        foreach (RetrievalStrategy strategy in Enum.GetValues<RetrievalStrategy>())
        {
            retrievalTable.AddRow(
                $"[bold]{strategy}[/]",
                strategy.GetDescription(),
                strategy.GetRecommendedUseCase(),
                strategy.GetPerformance(),
                strategy.GetResultCount());
        }

        AnsiConsole.Write(retrievalTable);
        AnsiConsole.WriteLine();
    }

    private static void DisplayUsageExamples()
    {
        AnsiConsole.MarkupLine("[dim]Usage examples:[/]");
        AnsiConsole.MarkupLine("  [green]OmniRAG.Console.exe --list-strategies[/]");
        AnsiConsole.MarkupLine("  [green]OmniRAG.Console.exe -e MiniLM -s Semantic -r TopK -k 5[/]");
        AnsiConsole.MarkupLine(
            "  [green]OmniRAG.Console.exe -e BGELarge -s Fixed " +
            "-r ThresholdBased -t 0.8[/]");
        AnsiConsole.MarkupLine(
            "  [green]OmniRAG.Console.exe -e Multilingual -s Sentence " +
            "-r MaxMarginalRelevance -k 10[/]");
        AnsiConsole.MarkupLine("  [green]OmniRAG.Console.exe -r Hybrid -k 7 -t 0.6[/]");
        AnsiConsole.WriteLine();
    }
}
