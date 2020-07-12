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

namespace QandA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly IDataRepository _dataRepository;
        private readonly IHubContext<QuestionsHub> _questionHubContext;
        private readonly IQuestionCache _cache;

        public QuestionsController(IDataRepository dataRepository, IHubContext<QuestionsHub> questionHubContext, IQuestionCache cache)
        {
            // TODO - set referense to _dataRepository
            _dataRepository = dataRepository;
            _questionHubContext = questionHubContext;
            _cache = cache;
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

        [HttpPost]
        public ActionResult<QuestionGetSingleResponse>
            PostQuestion(QuestionPostRequest questionPostRequest)
        {
            // TODO - call the data repository
            var savedQuestion =
                _dataRepository.PostQuestion(new QuestionPostFullRequest
                {
                    Title = questionPostRequest.Title,
                    Content = questionPostRequest.Content,
                    UserId = "1",
                    UserName = "bob.test@test.com",
                    Created = DateTime.UtcNow
                });
            // TODO - return HTTP status code 201
            return CreatedAtAction(nameof(GetQuestion),
                new { questionId = savedQuestion.QuestionId },
                savedQuestion);
        }

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

        [HttpPost("answer")]
        public ActionResult<AnswerGetResponse>
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
                    UserId = "1",
                    UserName = "bob.test@test.com",
                    Created = DateTime.UtcNow
                }
            );

            _cache.Remove(answerPostRequest.QuestionId.Value);

            _questionHubContext.Clients.Group($"Question-{answerPostRequest.QuestionId.Value}")
                .SendAsync("ReceiveQuestion", _dataRepository.GetQuestion(answerPostRequest.QuestionId.Value));

            return savedAnswer;
        }
    }
}
