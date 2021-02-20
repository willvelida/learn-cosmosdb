using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DaprCosmosStateStore.State
{
    public class ToDoItemState
    {
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public ToDoItem ToDoItem { get; set; }
    }
}
