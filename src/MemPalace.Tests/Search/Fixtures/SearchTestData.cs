using MemPalace.Core.Backends;
using MemPalace.Search;

namespace MemPalace.Tests.Search.Fixtures;

/// <summary>
/// Test data fixtures for Search unit tests.
/// Provides realistic sample memories and expected rankings.
/// </summary>
public static class SearchTestData
{
    /// <summary>
    /// Sample technical memory: machine learning basics
    /// </summary>
    public static readonly string TechnicalMemory1 =
        "Machine learning algorithms enable computers to learn from data without explicit programming. " +
        "Common algorithms include supervised learning, unsupervised learning, and reinforcement learning. " +
        "Applications span image recognition, natural language processing, and recommendation systems.";

    /// <summary>
    /// Sample technical memory: deep neural networks
    /// </summary>
    public static readonly string TechnicalMemory2 =
        "Deep neural networks with multiple hidden layers can model complex nonlinear relationships. " +
        "Convolutional networks excel at image processing tasks. Recurrent networks handle sequential data. " +
        "Transformers have revolutionized natural language processing with attention mechanisms.";

    /// <summary>
    /// Sample technical memory: authentication best practices
    /// </summary>
    public static readonly string TechnicalMemory3 =
        "Authentication mechanisms must validate user identity securely. " +
        "Use bcrypt or Argon2 for password hashing. " +
        "Implement OAuth 2.0 for federated authentication. " +
        "Enable multi-factor authentication to reduce account compromise risk.";

    /// <summary>
    /// Sample conversational memory: team standup
    /// </summary>
    public static readonly string ConversationalMemory1 =
        "Today's standup: Alice completed the API migration. Bob is working on database optimization. " +
        "Carol reported a critical bug in the payment processor. " +
        "Team decided to prioritize bug fix over new features this sprint.";

    /// <summary>
    /// Sample conversational memory: retrospective notes
    /// </summary>
    public static readonly string ConversationalMemory2 =
        "Retrospective highlights: improved documentation, better communication channels established. " +
        "Action items: implement automated testing for deployment pipeline and schedule team knowledge transfer. " +
        "Team morale is high after successful product launch last week.";

    /// <summary>
    /// Sample structured memory: API documentation
    /// </summary>
    public static readonly string StructuredMemory1 =
        "POST /api/v1/auth/login accepts email and password. " +
        "Returns JWT token with 24-hour expiration. " +
        "Status codes: 200 OK, 401 Unauthorized, 429 Too Many Requests. " +
        "Rate limit: 5 requests per minute per IP address.";

    /// <summary>
    /// Sample structured memory: database schema
    /// </summary>
    public static readonly string StructuredMemory2 =
        "Users table contains id, email, password_hash, created_at, updated_at columns. " +
        "Email field is unique and indexed for fast lookups. " +
        "Password hash uses bcrypt with cost factor of 12. " +
        "Automatically track modification timestamps.";

    /// <summary>
    /// Sample memory about vector embeddings
    /// </summary>
    public static readonly string EmbeddingsMemory =
        "Vector embeddings convert text into numerical representations for semantic similarity. " +
        "Embeddings trained on large corpora capture semantic meaning. " +
        "Common models: BERT, GPT embeddings, and sentence transformers. " +
        "Embeddings enable similarity search and clustering.";

    /// <summary>
    /// Sample memory about hybrid search
    /// </summary>
    public static readonly string HybridSearchMemory =
        "Hybrid search combines vector similarity with keyword matching for better results. " +
        "Reciprocal Rank Fusion (RRF) merges scores from multiple signals. " +
        "BM25 algorithm provides corpus-aware keyword scoring. " +
        "Hybrid approaches balance semantic understanding with exact term matching.";

    /// <summary>
    /// All sample memories organized by type
    /// </summary>
    public static readonly IReadOnlyList<string> AllMemories = new[]
    {
        TechnicalMemory1,
        TechnicalMemory2,
        TechnicalMemory3,
        ConversationalMemory1,
        ConversationalMemory2,
        StructuredMemory1,
        StructuredMemory2,
        EmbeddingsMemory,
        HybridSearchMemory
    };

    /// <summary>
    /// Create mock SearchHits for testing expectations
    /// </summary>
    public static SearchHit CreateMockSearchHit(
        string id,
        string document,
        float score,
        string? wing = null,
        string? source = null)
    {
        var metadata = new Dictionary<string, object?>();
        if (wing != null)
            metadata["wing"] = wing;
        if (source != null)
            metadata["source"] = source;

        return new SearchHit(id, document, score, metadata.Count > 0 ? metadata : null);
    }

    /// <summary>
    /// BM25 parameter recommendations for tuning
    /// </summary>
    public static class BM25Parameters
    {
        /// <summary>
        /// K1: Controls term frequency saturation point (typical: 1.2-2.0)
        /// Lower values → earlier saturation, higher values → linear growth
        /// </summary>
        public const float K1_Default = 1.5f;
        public const float K1_Conservative = 1.2f;
        public const float K1_Aggressive = 2.0f;

        /// <summary>
        /// B: Controls field length normalization (typical: 0.0-1.0)
        /// 0.0 → no normalization, 1.0 → full normalization
        /// </summary>
        public const float B_Default = 0.75f;
        public const float B_NoNormalization = 0.0f;
        public const float B_FullNormalization = 1.0f;

        /// <summary>
        /// Default top-K for overselection in fusion (vs final limit)
        /// Allows reranking to have more candidates
        /// </summary>
        public const int OverSelectMultiplier = 2;

        /// <summary>
        /// Reciprocal Rank Fusion constant
        /// </summary>
        public const int RrfConstant = 60;
    }

    /// <summary>
    /// Expected rankings for common test queries
    /// </summary>
    public static class ExpectedRankings
    {
        /// <summary>
        /// Query: "machine learning"
        /// Expected: TechnicalMemory1 > TechnicalMemory2 > HybridSearchMemory
        /// </summary>
        public static readonly IReadOnlyList<int> MachineLearningQuery = new[] { 0, 1, 8 };

        /// <summary>
        /// Query: "authentication"
        /// Expected: TechnicalMemory3 > StructuredMemory1 > others
        /// </summary>
        public static readonly IReadOnlyList<int> AuthenticationQuery = new[] { 2, 5 };

        /// <summary>
        /// Query: "neural networks"
        /// Expected: TechnicalMemory2 > TechnicalMemory1 > EmbeddingsMemory
        /// </summary>
        public static readonly IReadOnlyList<int> NeuralNetworksQuery = new[] { 1, 0, 7 };

        /// <summary>
        /// Query: "team standup"
        /// Expected: ConversationalMemory1 > ConversationalMemory2 > others
        /// </summary>
        public static readonly IReadOnlyList<int> TeamStandupQuery = new[] { 3, 4 };

        /// <summary>
        /// Query: "API database"
        /// Expected: StructuredMemory1 > StructuredMemory2 > others
        /// </summary>
        public static readonly IReadOnlyList<int> ApiDatabaseQuery = new[] { 5, 6 };

        /// <summary>
        /// Query: "hybrid reciprocal"
        /// Expected: HybridSearchMemory > EmbeddingsMemory (mention reciprocal/RRF)
        /// </summary>
        public static readonly IReadOnlyList<int> HybridQuery = new[] { 8 };
    }

    /// <summary>
    /// Common special character and edge case test strings
    /// </summary>
    public static class EdgeCases
    {
        public const string EmptyString = "";
        public const string SingleSpace = " ";
        public const string MultipleSpaces = "   ";
        public const string TabAndNewlines = "\t\n\r";
        public const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:',.<>?";
        public const string UnicodeChars = "こんにちは世界 🌍 café naïve résumé";
        public const string MixedCaseAndSpaces = "  TeSt   QuErY  ";
        public static readonly string VeryLongString = string.Concat(Enumerable.Repeat("word ", 1000));
    }

    /// <summary>
    /// Creates mock backend query results for testing
    /// </summary>
    public static QueryResult CreateMockQueryResult(
        IReadOnlyList<string> ids,
        IReadOnlyList<string> documents,
        IReadOnlyList<IReadOnlyDictionary<string, object?>>? metadatas = null,
        IReadOnlyList<float>? distances = null)
    {
        if (ids.Count != documents.Count)
            throw new ArgumentException("IDs and documents must have same length");

        var metas = metadatas ?? Enumerable.Range(0, ids.Count)
            .Select(_ => new Dictionary<string, object?> { { "wing", "default" } } as IReadOnlyDictionary<string, object?>)
            .ToList();

        var dists = distances ?? Enumerable.Range(0, ids.Count)
            .Select(i => (float)i * 0.1f)
            .ToList();

        return new QueryResult(
            Ids: new[] { ids },
            Documents: new[] { documents },
            Metadatas: new[] { metas },
            Distances: new[] { dists });
    }

    /// <summary>
    /// Creates a collection of records for batch operations
    /// </summary>
    public static IReadOnlyList<Record> CreateMockRecords(
        IReadOnlyList<string> documents,
        string? wing = null,
        bool includeCreatedAt = true)
    {
        var records = new List<Record>();
        for (int i = 0; i < documents.Count; i++)
        {
            var metadata = new Dictionary<string, object?> { { "wing", wing ?? "default" } };
            if (includeCreatedAt)
                metadata["created_at"] = DateTime.UtcNow.AddHours(-i);

            records.Add(new Record(
                Id: $"doc-{i}",
                Document: documents[i],
                Metadata: metadata));
        }
        return records;
    }
}
