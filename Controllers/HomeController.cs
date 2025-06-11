using System.Diagnostics;
using Azure.AI.OpenAI;
using System.Runtime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using RAGTest.Models;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using System.Collections;
using Microsoft.Extensions.VectorData;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Security.Cryptography.Xml;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Memory;
using System.Text;

namespace RAGTest.Controllers
{
    public class HomeController : Controller
    {
#pragma warning disable SKEXP0001

        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            string collectionName = "RAGtest";

            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();

            kernelBuilder.Services.AddSingleton<IEmbeddingGenerator>(
            sp => new AzureOpenAIClient(new Uri(_configuration["AIServices:AzureOpenAIEmbeddings:Endpoint"]), new System.ClientModel.ApiKeyCredential(_configuration["AIServices:AzureOpenAIEmbeddings:ApiKey"]))
                    .GetEmbeddingClient(_configuration["AIServices:AzureOpenAIEmbeddings:DeploymentName"])
            .AsIEmbeddingGenerator());

            kernelBuilder.Services.AddSqlServerCollection<string, TextSnippet>(collectionName, _configuration.GetConnectionString("VectorDatabase"),
                       new SqlServerCollectionOptions { Schema = "Embeddings" });

            kernelBuilder.AddVectorStoreTextSearch<TextSnippet>();
            var kernel = kernelBuilder.Build();
            var vectorStoreCollection = kernel.GetRequiredService<VectorStoreCollection<string, TextSnippet>>();

            // Create the collection if it doesn't exist
            await vectorStoreCollection.EnsureCollectionExistsAsync(CancellationToken.None);

            // Create the TextSnippet
            var textSnippet = new TextSnippet
            {
                Key = Guid.NewGuid().ToString(),
                Text = "My favourite colour is red!",
                ReferenceDescription = "Description",
                ReferenceLink = "http://sandbox",
            };

            // Upsert the record to the vector store
            await vectorStoreCollection.UpsertAsync(textSnippet, CancellationToken.None);

            // Lets search it!
            var textSearch = new VectorStoreTextSearch<TextSnippet>(vectorStoreCollection);
            KernelSearchResults<TextSearchResult> textResults = await textSearch.GetTextSearchResultsAsync("What is your favourite colour?", new() { Top = 50, Skip = 0 });

            StringBuilder memoryBuilder = new();

            try
            {
                await foreach (TextSearchResult result in textResults.Results)
                {
                    memoryBuilder.AppendLine(result.Value);
                }
            }
            catch (Exception ex)
            {
                throw;
            }



            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
