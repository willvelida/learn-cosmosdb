using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionalBatchDemo
{
    public class Contact
    {
        [JsonProperty("id")]
        public string ContactId { get; set; }
        public string ContactName { get; set; }
        //public Address ContactAddress { get; set; }
    }

    public class Address
    {
        [JsonProperty("id")]
        public string ContactId { get; set; }
        public string ContactName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int ZipCode { get; set; }
    }
}
