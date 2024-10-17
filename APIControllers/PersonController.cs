using Microsoft.AspNetCore.Mvc;
using OpenAPIArtonit.DBControllers;
using OpenAPIArtonit.Legasy_Service;
using OpenAPIArtonit.Model;

namespace OpenAPIArtonit.APIControllers
{
    [ApiController]
    [Route("[controller]")]
    public class PersonController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get(int id)
        {
            var result = PersonDBController.GetById(id);
            Console.WriteLine(result.State);
            if (result.State == State.Successes)
            {
                return Ok(new { result.Value });
            }
            else if (result.State == State.NotFound)
            {
                return NotFound(result.ErrorMessage);
            }
            else
            {
                return BadRequest(result.ErrorMessage);
            }
        }
        [HttpGet(nameof(GetList))]
        public IActionResult GetList()
        {
            var result = PersonDBController.GetAll();
            Console.WriteLine(result.State);
            var people = new Dictionary<State, IActionResult>()
            {
                { State.Successes, Ok(new { result.Value })},
                { State.Error, BadRequest() },
            };
            return people[result.State];
        }
        [HttpPost]
        public IActionResult Add([FromBody] PersonPostSee body)
        {
            var result = PersonDBController.Add(new PersonPost(body));
            var people = new Dictionary<State, IActionResult>()
            {
                { State.Successes, Ok(new { result.Value })},
                { State.Error, BadRequest() },
            };
            return people[result.State];
        }
        [HttpPatch]
        public IActionResult Update([FromBody] PersonPach body)
        {
            var result = PersonDBController.Update(body);
            var people = new Dictionary<State, IActionResult>()
            {
                { State.Successes, Ok(new { result.Value })},
                { State.Error, BadRequest() },
            };
            return people[result.State];
        }
        [HttpDelete]
        public IActionResult Delite(int id)
        {
            var result = PersonDBController.Delete(id);
            var people = new Dictionary<State, IActionResult>()
            {
                { State.Successes, Ok(new { id })},
                { State.Error, BadRequest() },
            };
            return people[result.State];
        }
    }
}
