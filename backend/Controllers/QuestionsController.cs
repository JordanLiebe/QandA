using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using QandA.Data;
using QandA.Data.Models;
using QandA.Hubs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;

namespace QandA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly IDataRepository _dataRepository;
        private readonly IHubContext<QuestionsHub> _questionHubContext;
        private readonly IQuestionCache _cache;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _auth0UserInfo;

        public QuestionsController(IDataRepository dataRepository, IHubContext<QuestionsHub> questionHubContext, IQuestionCache cache, IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            // TODO - set referense to _dataRepository
            _dataRepository = dataRepository;
            _questionHubContext = questionHubContext;
            _cache = cache;
            _clientFactory = clientFactory;
            _auth0UserInfo = $"{configuration["Auth0:Authority"]}userinfo";
        }

        private async Task<string> GetUserName()
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                _auth0UserInfo);
            request.Headers.Add("Authorization",
                Request.Headers["Authorization"].First());
            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            if(response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<User>(
                    jsonContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                return user.Name;
            }
            else
            {
                return "";
            }
        }

        [HttpGet]
        public IEnumerable<QuestionGetManyResponse> GetQuestions(string search, bool includeAnswers, int page = 1, int pageSize = 20)
        {
            if(string.IsNullOrEmpty(search))
            {
                if(includeAnswers)
                {
                    return _dataRepository.GetQuestionsWithAnswers();
                }
                else
                {
                    return _dataRepository.GetQuestions();
                }
            }
            else
            {
                return _dataRepository.GetQuestionsBySearchWithPaging(
                    search,
                    page,
                    pageSize);
            }
        }

        [HttpGet("unanswered")]
        public async Task<IEnumerable<QuestionGetManyResponse>> GetUnansweredQuestions()
        {
            return await _dataRepository.GetUnansweredQuestionsAsync();
        }

        [HttpGet("{questionId}")]
        public ActionResult<QuestionGetSingleResponse>
            GetQuestion(int questionId)
        {
            // TODO - call the data repository to get the question
            var question = _cache.Get(questionId);
            // TODO - return HTTP status code 404 if the question isn't found
            if(question == null)
            {
                question = _dataRepository.GetQuestion(questionId);
                if(question == null)
                {
                    return NotFound();
                }
                _cache.Set(question);
            }
            // TODO - return question in response with status code 200
            return question;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<QuestionGetSingleResponse>>
            PostQuestion(QuestionPostRequest questionPostRequest)
        {
            // TODO - call the data repository
            var savedQuestion =
                _dataRepository.PostQuestion(new QuestionPostFullRequest
                {
                    Title = questionPostRequest.Title,
                    Content = questionPostRequest.Content,
                    UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value,
                    UserName = await GetUserName(),
                    Created = DateTime.UtcNow
                });
            // TODO - return HTTP status code 201
            return CreatedAtAction(nameof(GetQuestion),
                new { questionId = savedQuestion.QuestionId },
                savedQuestion);
        }

        [Authorize(Policy = "MustBeQuestionAuthor")]
        [HttpPut("{questionId}")]
        public ActionResult<QuestionGetSingleResponse>
            PutQuestion(int questionId,
                QuestionPutRequest questionPutRequest)
        {
            // TODO - get the question from the data repository
            var question =
                _dataRepository.GetQuestion(questionId);
            // TODO - return HTTP status code 404 if the question isnt found
            if (question == null)
                return NotFound();
            // TODO - update the question model
            questionPutRequest.Title = 
                string.IsNullOrEmpty(questionPutRequest.Title) ?
                question.Title :
                questionPutRequest.Title;
            questionPutRequest.Content = 
                string.IsNullOrEmpty(questionPutRequest.Content) ?
                question.Content :
                questionPutRequest.Content;
            // TODO - call the data repository with the updated question
            var savedQuestion =
                _dataRepository.PutQuestion(questionId,
                    questionPutRequest);
            // TODO - remove from cache
            _cache.Remove(savedQuestion.QuestionId);
            // TODO - return the saved question
            return savedQuestion;
        }

        [Authorize(Policy = "MustBeQuestionAuthor")]
        [HttpDelete("{questionId}")]
        public ActionResult DeleteQuestion(int questionId)
        {
            var question = _dataRepository.GetQuestion(questionId);
            if(question == null)
            {
                return NotFound();
            }
            // TODO - delete question from database
            _dataRepository.DeleteQuestion(questionId);
            // TODO - remove question from cache
            _cache.Remove(questionId);
            // TODO - return a no content response
            return NoContent();
        }

        [Authorize]
        [HttpPost("answer")]
        public async Task<ActionResult<AnswerGetResponse>>
            PostAnswer(AnswerPostFullRequest answerPostRequest)
        {
            var questionExists = _dataRepository.QuestionExists(answerPostRequest.QuestionId.Value);

            if(!questionExists)
            {
                return NotFound();
            }

            var savedAnswer = 
                _dataRepository.PostAnswer(new AnswerPostFullRequest
                {
                    QuestionId = answerPostRequest.QuestionId.Value,
                    Content = answerPostRequest.Content,
                    UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value,
                    UserName = await GetUserName(),
                    Created = DateTime.UtcNow
                }
            );

            _cache.Remove(answerPostRequest.QuestionId.Value);

            await _questionHubContext.Clients.Group($"Question-{answerPostRequest.QuestionId.Value}")
                .SendAsync("ReceiveQuestion", _dataRepository.GetQuestion(answerPostRequest.QuestionId.Value));

            return savedAnswer;
        }
    }
}
