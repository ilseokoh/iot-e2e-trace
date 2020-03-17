using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiService.Services;
using BackendService.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChillerController : ControllerBase
    {
        private readonly IChillerDbService _chillerDbService; 

        public ChillerController(IChillerDbService dbService)
        {
            _chillerDbService = dbService;
        }

        // GET: api/Chiller
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ChillerMessage>>> Get()
        {
            var query = "SELECT TOP 10 * FROM c ORDER BY timestamp DESC";
            try
            {
                var results = await _chillerDbService.GetMessageQueryAsync(query);
                return Ok(results);
            }
            catch(Exception ex)
            {
                return NotFound(ex);
            }

            
        }

        // GET: api/Chiller/5
        [HttpGet("{id}", Name = "Get")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ChillerMessage>> Get(string id)
        {
            ChillerMessage item = await _chillerDbService.GetMessageAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return item;
        }

        // POST: api/Chiller
        [HttpPost()]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task Post([FromBody] ChillerMessage msg)
        {
            await _chillerDbService.AddMessageAsync(msg);
        }

        // PUT: api/Chiller/5
        [HttpPut("{id}")]
        public async Task Put(string id, [FromBody] ChillerMessage msg)
        {
            await _chillerDbService.UpdateMessageAsync(id, msg);
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public async Task Delete(string id)
        {
            await _chillerDbService.DeleteMessageAsync(id);
        }
    }
}
