using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniRAG.Core.Configuration;
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
using System.CommandLine;
using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Console;

/// <summary>
/// Configuration paths container for service registration.
/// Reduces parameter count in helper methods (Rule 16).
/// </summary>
internal class ConfigurationPaths
{
    public required string PythonDll { get; init; }
    public required string PythonHome { get; init; }
    public required string ChromaDir { get; init; }
    public required string PdfDirectory { get; init; }
}

/// <summary>
/// Main program entry point.
/// Clean Architecture: Composition root for dependency injection.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Create root command with options
        RootCommand rootCommand = new RootCommand("OmniRAG - RAG-powered Technical Manual Assistant");

        // Embedding strategy option
        Option<EmbeddingStrategy> embeddingOption = new Option<EmbeddingStrategy>(
            name: "--embedding-strategy",
            description: "The embedding model to use for vector generation",
            getDefaultValue: () => EmbeddingStrategy.MiniLM);
        embeddingOption.AddAlias("-e");

        // Chunking strategy option
        Option<ChunkingStrategy> strategyOption = new Option<ChunkingStrategy>(
            name: "--chunking-strategy",
            description: "The chunking strategy to use for document processing",
            getDefaultValue: () => ChunkingStrategy.Semantic);
        strategyOption.AddAlias("-s");

        // Chunk size option
        Option<int> chunkSizeOption = new Option<int>(
            name: "--chunk-size",
            description: "Target chunk size in tokens (default: 512)",
            getDefaultValue: () => 512);
        chunkSizeOption.AddAlias("-c");

        // Overlap size option
        Option<int> overlapOption = new Option<int>(
            name: "--overlap",
            description: "Overlap size in tokens for applicable strategies (default: 100)",
            getDefaultValue: () => 100);
        overlapOption.AddAlias("-o");

        // Retrieval strategy option
        Option<RetrievalStrategy> retrievalOption = new Option<RetrievalStrategy>(
            name: "--retrieval-strategy",
            description: "The retrieval strategy to use for vector search",
            getDefaultValue: () => RetrievalStrategy.TopK);
        retrievalOption.AddAlias("-r");

        // Top-K option
        Option<int> topKOption = new Option<int>(
            name: "--top-k",
            description: "Maximum number of results to retrieve (1-50, default: 5)",
            getDefaultValue: () => 5);
        topKOption.AddAlias("-k");
        topKOption.AddValidator(result =>
        {
            int value = result.GetValueForOption(topKOption);
            if (value < 1 || value > 50)
            {
                result.ErrorMessage = "Top-K must be between 1 and 50";
            }
        });

        // Similarity threshold option
        Option<float> thresholdOption = new Option<float>(
            name: "--min-similarity",
            description: "Minimum similarity threshold (0.0-1.0, default: strategy-dependent)",
            getDefaultValue: () => -1.0f); // -1 means use strategy default
        thresholdOption.AddAlias("-t");
        thresholdOption.AddValidator(result =>
        {
            float value = result.GetValueForOption(thresholdOption);
            if (value != -1.0f && (value < 0.0f || value > 1.0f))
            {
                result.ErrorMessage = "Similarity threshold must be between 0.0 and 1.0";
            }
        });

        // List strategies option
        Option<bool> listStrategiesOption = new Option<bool>(
            name: "--list-strategies",
            description: "List available strategies (chunking, embedding, retrieval) and exit",
            getDefaultValue: () => false);
        listStrategiesOption.AddAlias("-l");

        rootCommand.AddOption(embeddingOption);
        rootCommand.AddOption(strategyOption);
        rootCommand.AddOption(chunkSizeOption);
        rootCommand.AddOption(overlapOption);
        rootCommand.AddOption(retrievalOption);
        rootCommand.AddOption(topKOption);
        rootCommand.AddOption(thresholdOption);
        rootCommand.AddOption(listStrategiesOption);

        rootCommand.SetHandler(async (embedding, strategy, chunkSize, overlap, retrieval, topK, threshold, listStrategies) =>
        {
            // Handle --list-strategies
            if (listStrategies)
            {
                DisplayStrategies();
                return;
            }

            // Validate parameters
            if (chunkSize <= 0)
            {
                AnsiConsole.MarkupLine("[red]Error: Chunk size must be positive[/]");
                return;
            }

            if (overlap < 0)
            {
                AnsiConsole.MarkupLine("[red]Error: Overlap cannot be negative[/]");
                return;
            }

            if (overlap >= chunkSize)
            {
                AnsiConsole.MarkupLine("[red]Error: Overlap must be less than chunk size[/]");
                return;
            }

            // Display banner
            DisplayBanner();

            // Display selected configuration
            DisplayConfiguration(embedding, strategy, chunkSize, overlap, retrieval, topK, threshold);

            // Build configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Validate configuration at startup (fail-fast principle)
            try
            {
                ValidateConfiguration(configuration);
            }
            catch (ConfigurationValidationException ex)
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
                
                return;
            }

            // Build host with dependency injection
            IHost host = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .ConfigureServices((context, services) =>
                {
                    // Register options with validation
                    services.AddOptions<OmniRAGOptions>()
                        .Bind(configuration.GetSection(OmniRAGOptions.Section))
                        .ValidateDataAnnotations()
                        .ValidateOnStart();
                    
                    services.AddSingleton<IValidateOptions<OmniRAGOptions>, OmniRAGOptionsValidator>();
                    
                    ConfigureServices(services, configuration, embedding, strategy, chunkSize, overlap, retrieval, topK, threshold);
                })
                .Build();

            // Log application startup
            ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("OmniRAG application started");
            logger.LogInformation(
                "Configuration: Embedding={Embedding}, Chunking={Chunking}, Retrieval={Retrieval}, TopK={TopK}",
                embedding,
                strategy,
                retrieval,
                topK);

            // Run the application
            using IServiceScope scope = host.Services.CreateScope();
            OmniRAGApp app = scope.ServiceProvider.GetRequiredService<OmniRAGApp>();
            await app.RunAsync();

        }, embeddingOption, strategyOption, chunkSizeOption, overlapOption, retrievalOption, topKOption, thresholdOption, listStrategiesOption);

        return await rootCommand.InvokeAsync(args);
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
                sp.GetRequiredService<IDocumentLoader>(),
                sp.GetRequiredService<IEmbeddingService>(),
                sp.GetRequiredService<IVectorStore>(),
                sp.GetService<ILanguageModel>(),
                retrievalOptions,
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
                sp.GetRequiredService<IRagEngine>(),
                configuration,
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
            AnsiConsole.MarkupLine("[dim]ℹ Phi-4 disabled in configuration (retrieval-only mode)[/]");
            services.AddSingleton<ILanguageModel>(sp => null!);
            return;
        }

        string? modelPath = configuration["OmniRAG:Phi4:ModelPath"];
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            AnsiConsole.MarkupLine("[yellow]⚠[/] Phi-4 model path not configured");
            AnsiConsole.MarkupLine("[yellow]  Running in retrieval-only mode[/]");
            services.AddSingleton<ILanguageModel>(sp => null!);
            return;
        }

        int maxTokens = configuration.GetValue<int>("OmniRAG:Phi4:MaxTokens", 2048);
        float temperature = configuration.GetValue<float>("OmniRAG:Phi4:Temperature", 0.7f);

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
            
            AnsiConsole.MarkupLine("[green]✓[/] Phi-4 language model enabled");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] Phi-4 initialization failed: {ex.Message}");
            AnsiConsole.MarkupLine("[yellow]  Running in retrieval-only mode[/]");
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
            AnsiConsole.MarkupLine("[dim]ℹ Document monitoring disabled[/]");
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

        AnsiConsole.MarkupLine("[green]✓[/] Document monitoring enabled");
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
        Table table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[yellow]Configuration[/]")
            .AddColumn("[cyan]Value[/]");

        // Embedding configuration
        table.AddRow("[bold]Embedding Model[/]", "");
        table.AddRow("  Strategy", $"[green]{embeddingStrategy}[/] - {embeddingStrategy.GetDescription()}");
        table.AddRow("  Model Name", $"[dim]{embeddingStrategy.GetModelName()}[/]");
        table.AddRow("  Dimensions", $"[green]{embeddingStrategy.GetDimensions()}[/]");
        table.AddRow("  Performance", embeddingStrategy.GetPerformance());
        table.AddRow("  Model Size", embeddingStrategy.GetModelSize());
        
        // Chunking configuration
        table.AddRow("[bold]Chunking Strategy[/]", "");
        table.AddRow("  Strategy", $"[green]{chunkingStrategy}[/] - {chunkingStrategy.GetDescription()}");
        table.AddRow("  Target Chunk Size", $"[green]{chunkSize}[/] tokens");
        table.AddRow("  Overlap Size", $"[green]{overlap}[/] tokens");
        table.AddRow("  Recommended For", chunkingStrategy.GetRecommendedUseCase());

        // Retrieval configuration
        table.AddRow("[bold]Retrieval Strategy[/]", "");
        table.AddRow("  Strategy", $"[green]{retrievalStrategy}[/] - {retrievalStrategy.GetDescription()}");
        table.AddRow("  Top-K Results", $"[green]{topK}[/]");
        float actualThreshold = minSimilarity >= 0 ? minSimilarity : retrievalStrategy.GetDefaultThreshold();
        table.AddRow("  Min Similarity", $"[green]{actualThreshold:P0}[/]");
        table.AddRow("  Performance", retrievalStrategy.GetPerformance());
        table.AddRow("  Result Count", retrievalStrategy.GetResultCount());

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void DisplayStrategies()
    {
        AnsiConsole.Write(
            new FigletText("Available Strategies")
                .Centered()
                .Color(Color.Blue));

        AnsiConsole.WriteLine();
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

        AnsiConsole.MarkupLine("[dim]Usage examples:[/]");
        AnsiConsole.MarkupLine("  [green]OmniRAG.Console.exe --list-strategies[/]");
        AnsiConsole.MarkupLine("  [green]OmniRAG.Console.exe -e MiniLM -s Semantic -r TopK -k 5[/]");
        AnsiConsole.MarkupLine("  [green]OmniRAG.Console.exe -e BGELarge -s Fixed -r ThresholdBased -t 0.8[/]");
        AnsiConsole.MarkupLine("  [green]OmniRAG.Console.exe -e Multilingual -s Sentence -r MaxMarginalRelevance -k 10[/]");
        AnsiConsole.MarkupLine("  [green]OmniRAG.Console.exe -r Hybrid -k 7 -t 0.6[/]");
        AnsiConsole.WriteLine();
    }
}
