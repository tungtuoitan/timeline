using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SuperApp.Application.Features.Notes.Queries.GetNotes;
using MediatR;

namespace SuperAppAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestEfController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TestEfController> _logger;

        public TestEfController(IMediator mediator, ILogger<TestEfController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Test EF Core implementation for GetNotes
        /// </summary>
        [HttpGet("notes")]
        public async Task<IActionResult> TestGetNotesWithEfCore(
            [FromQuery] bool getAll = false,
            [FromQuery] string? searchText = null)
        {
            try
            {
                _logger.LogInformation("Testing EF Core GetNotes with getAll={GetAll}, searchText={SearchText}", 
                    getAll, searchText);

                var query = new GetNotesQuery(getAll, searchText, null);
                var result = await _mediator.Send(query);

                return Ok(new 
                { 
                    message = "EF Core GetNotes working correctly",
                    count = result.Count,
                    data = result 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing EF Core GetNotes");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}