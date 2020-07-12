using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using QandA.Data.Models;

namespace QandA.Data
{
    public class QuestionCache : IQuestionCache
    {
        // TODO - create a memory cache
        private MemoryCache _cache { get; set; }
        public QuestionCache()
        {
            _cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 100
            });
        }
        // TODO - method to get a cached question
        private string GetCacheKey(int questionId) =>
            $"Question-{questionId}";
        public QuestionGetSingleResponse Get(int questionId)
        {
            QuestionGetSingleResponse question;
            _cache.TryGetValue(
                GetCacheKey(questionId),
                out question);
            return question;
        }
        // TODO - method to add a cached question
        public void Set(QuestionGetSingleResponse question)
        {
            var cacheEntryOptions =
                new MemoryCacheEntryOptions().SetSize(1);
            _cache.Set(GetCacheKey(question.QuestionId),
                question,
                cacheEntryOptions);
        }
        // TODO - method to remove a cached question
        public void Remove(int questionId)
        {
            _cache.Remove(GetCacheKey(questionId));
        }
    }
}
