using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebHotel.Services;

namespace WebHotel.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Customer")]
    [Produces("application/json")]
    public class ChatApiController : ControllerBase
    {
        private readonly IChatBotService _chatBot;
        public ChatApiController(IChatBotService chatBot) => _chatBot = chatBot;

        /// <summary>Send a message to the hotel assistant chat bot.</summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Send([FromBody] ChatRequest request)
        {
            var response = _chatBot.GetResponse(request.Message ?? "");
            return Ok(new { reply = response });
        }
    }

    public record ChatRequest(string? Message);
}
