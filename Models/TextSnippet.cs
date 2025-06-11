using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace RAGTest.Models
{
    public sealed class TextSnippet
    {
        [VectorStoreKey]
        public required string Key { get; set; }

        [TextSearchResultValue]
        [VectorStoreData]
        public string? Text { get; set; }

        [TextSearchResultName]
        [VectorStoreData]
        public string? ReferenceDescription { get; set; }

        [TextSearchResultLink]
        [VectorStoreData]
        public string? ReferenceLink { get; set; }

        [VectorStoreVector(1536)]
        public string? TextEmbedding => this.Text;
    }
}
