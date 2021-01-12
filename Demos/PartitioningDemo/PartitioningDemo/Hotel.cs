using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace PartitioningDemo
{
    public class Hotel
    {
        [JsonProperty("id")]
        public string HotelId { get; set; }
        public string HotelName { get; set; }
        public int StarRating { get; set; }
        public string CityName { get; set; }
    }
}
