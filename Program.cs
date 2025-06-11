using Microsoft.Extensions.VectorData;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Azure.AI.OpenAI;
using RAGTest.Models;
using Microsoft.Extensions.Configuration;
using System.Collections;


namespace RAGTest_Console
{
    internal class Program
    {
#pragma warning disable SKEXP0001

        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) 
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();

            kernelBuilder.Services.AddSingleton<IEmbeddingGenerator>(
            sp => new AzureOpenAIClient(new Uri(config["AzureOpenAIEmbeddingsEndpoint"]), new System.ClientModel.ApiKeyCredential(config["AzureOpenAIEmbeddingsApiKey"]))
                    .GetEmbeddingClient(config["AzureOpenAIEmbeddingsDeploymentName"])
            .AsIEmbeddingGenerator());

            kernelBuilder.Services.AddSqlServerCollection<string, TextSnippet>(config["CollectionName"], config["AzureSQLVectorDatabaseConnString"], new SqlServerCollectionOptions { Schema = "Embeddings" });

            kernelBuilder.AddVectorStoreTextSearch<TextSnippet>();
            var kernel = kernelBuilder.Build();
            var vectorStoreCollection = kernel.GetRequiredService<VectorStoreCollection<string, TextSnippet>>();

            // Create the collection if it doesn't exist
            await vectorStoreCollection.EnsureCollectionExistsAsync(CancellationToken.None);

            // Create the TextSnippet
            var textSnippet = new TextSnippet
            {
                Key = Guid.NewGuid().ToString(),
                Text = config["EmbeddingText"],
                ReferenceDescription = "Description",
                ReferenceLink = "http://sandbox",
            };

            // Upsert the record to the vector store
            await vectorStoreCollection.UpsertAsync(textSnippet, CancellationToken.None);

            // Lets search it!
            var textSearch = new VectorStoreTextSearch<TextSnippet>(vectorStoreCollection);
            KernelSearchResults<TextSearchResult> textResults = await textSearch.GetTextSearchResultsAsync(config["EmbeddingSearchQry"], new() { Top = 5, Skip = 0 });

            try
            {
                await foreach (TextSearchResult result in textResults.Results)
                {
                    Console.WriteLine(result.Value);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
