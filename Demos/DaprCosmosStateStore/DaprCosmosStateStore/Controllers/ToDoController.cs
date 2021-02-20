using Dapr;
using Dapr.Client;
using DaprCosmosStateStore.State;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DaprCosmosStateStore.Controllers
{
    [ApiController]
    public class ToDoController : ControllerBase
    {
        public const string StoreName = "todostore";

        [HttpPost("todoitem")]
        public async Task<ActionResult<ToDoItem>> CreateItem(ToDoItem toDoItem, [FromServices] DaprClient daprClient)
        {
            Console.WriteLine("Enter ToDoItem");

            var state = await daprClient.GetStateEntryAsync<ToDoItemState>(StoreName, toDoItem.Name);
            state.Value ??= new ToDoItemState()
            {
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                ToDoItem = toDoItem
            };

            var data = new ToDoItem() 
            { 
                Id = Guid.NewGuid().ToString(),
                Name = toDoItem.Name,
                Description = toDoItem.Description,
                IsCompleted = toDoItem.IsCompleted 
            };

            await daprClient.SaveStateAsync(StoreName, data.Id, data);

            Console.WriteLine($"Submitted ToDoItem: {data.Id} | {data.Name}");

            return data;
        }

        [HttpGet("todoitem/{id}")]
        public ActionResult<ToDoItem> Get([FromState(StoreName)] StateEntry<ToDoItem> state)
        {
            Console.WriteLine("Enter ToDoItem Retrieval");

            if (state.Value == null)
            {
                return NotFound();
            }

            var result = new ToDoItem
            {
                Id = state.Value.Id,
                Name = state.Value.Name,
                Description = state.Value.Description,
                IsCompleted = state.Value.IsCompleted
            };

            Console.WriteLine($"Retrieved ToDoItem: {result.Id} | {result.Name}");

            return result;
        }
    }
}
