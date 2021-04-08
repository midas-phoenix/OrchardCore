using System;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util.Automaton;
using Newtonsoft.Json.Linq;

namespace OrchardCore.Search.Elastic.QueryProviders
{
    /// <summary>
    /// We may not need this at all as Elastic has full blown DSL with NEST OOTB
    /// </summary>
    public class FuzzyQueryProvider : IElasticQueryProvider
    {
        public Query CreateQuery(IElasticQueryService builder, ElasticQueryContext context, string type, JObject query)
        {
            if (type != "fuzzy")
            {
                return null;
            }

            var first = query.Properties().First();

            switch (first.Value.Type)
            {
                case JTokenType.String:
                    return new FuzzyQuery(new Term(first.Name, first.Value.ToString()));
                case JTokenType.Object:
                    var obj = (JObject)first.Value;

                    if (!obj.TryGetValue("value", out var value))
                    {
                        throw new ArgumentException("Missing value in fuzzy query");
                    }

                    obj.TryGetValue("fuzziness", out var fuzziness);
                    obj.TryGetValue("prefix_length", out var prefixLength);
                    obj.TryGetValue("max_expansions", out var maxExpansions);

                    var fuzzyQuery = new FuzzyQuery(
                        new Term(first.Name, value.Value<string>()),
                        fuzziness?.Value<int>() ?? LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE,
                        prefixLength?.Value<int>() ?? 0,
                        maxExpansions?.Value<int>() ?? 50,
                        true);

                    if (obj.TryGetValue("boost", out var boost))
                    {
                        fuzzyQuery.Boost = boost.Value<float>();
                    }

                    return fuzzyQuery;
                default: throw new ArgumentException("Invalid fuzzy query");
            }
        }
    }
}
