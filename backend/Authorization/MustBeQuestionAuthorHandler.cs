using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using QandA.Data;

namespace QandA.Authorization
{
    public class MustBeQuestionAuthorHandler : AuthorizationHandler<MustBeQuestionAuthorRequirement>
    {
        private readonly IDataRepository _dataRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MustBeQuestionAuthorHandler(
            IDataRepository dataRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _dataRepository = dataRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        protected async override Task
            HandleRequirementAsync(
                AuthorizationHandlerContext context,
                MustBeQuestionAuthorRequirement requirement)
        {
            // TODO - check that the user is authenticated
            if(!context.User.Identity.IsAuthenticated)
            {
                context.Fail();
                return;
            }
            // TODO - get the question id from the request
            var questionId = _httpContextAccessor.HttpContext.Request
                .RouteValues["questionId"];
            int questionIdAsInt = Convert.ToInt32(questionId);
            // TODO - get the user id from the name identifier claim
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            // TODO - get the question from the data repository
            var question = _dataRepository.GetQuestion(questionIdAsInt);
            // TODO - if the question can't be found go to the next piece
            if (question == null)
            {
                // let if through so the controller can return a 404
                context.Succeed(requirement);
                return;
            }
            // TODO - return failure if the user id in the question from thedata repository is different to the user id in the request
            if (question.UserId != userId)
            {
                context.Fail();
                return;
            }
            // TODO - return success if we manage to get here
            context.Succeed(requirement);
        }
    }
}
