using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Mot.Parser.InterfaceLib;
using TransformerJsonApi.Models;

namespace TransformerJsonApi.Controllers
{
    public class JsonDoc
    {
        public string Type { get; set; }
        public string Body { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class TransformController : ControllerBase
    {
        private readonly MotTransformerInterface _context;

        public TransformController()
        {          
            _context = new MotTransformerInterface();
        }

        // GET api/transform/config
        [HttpGet("config")]
        public ActionResult<IEnumerable<string>> GetConfig()
        {
            return _context.GetConfigList();
        }

        // GET api/transform
        [HttpGet]
        public ActionResult<IEnumerable<string>> GetMessages()
        {
            

/*
            _context.Responses.Add("FOO: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aenean sem nisi, gravida et pretium sed, dapibus in metus. Morbi condimentum nisi lacus, sed consequat tellus.");
            _context.Responses.Add("Donec sagittis mi et justo ornare, in egestas arcu luctus. Nulla sed nunc vel sapien dapibus consectetur. , condimentum id mauris eget, viverra fermentum ex. Vivamus posuere, mi eu commodo dapibus, lectus est semper nisi, in semper dui nulla nec urna. Curabitur a libero leo. In pellentesque volutpat sem, in porttitor libero mollis quis. Suspendisse libero mauris, dapibus vel fringilla vitae, posuere et est. Fusce a blandit enim. Duis quis dapibus enim. Suspendisse rutrum dolor eget congue faucibus. Donec nulla erat, semper quis massa eget, varius placerat lorem. Mauris facilisis enim eu accumsan scelerisque. Quisque eu malesuada felis. Proin non leo at quam maximus tincidunt quis in libero.");
            _context.Responses.Add("Etiam ultricies vel nunc ac cursus. Pellentesque dolor nibh");
            _context.Responses.Add("The End");


            foreach (var str in _context.Responses)
            {
                response.Add($"{str}\n");
            }
*/

            var response = new List<string>();
            response.Add("Medicine-On-Time Gateway Interface Transformer 1.0");
            return response;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        [HttpPost]
        public ActionResult<string> Post([FromBody] JsonDoc data)
        {
            switch(data.Type.ToLower())
            {
                case "hl7":  
                    return _context.Parse(data.Body, InputDataFormat.HL7);

                case "xml":
                    return _context.Parse(data.Body, InputDataFormat.XML);

                case "delimited":
                    return _context.Parse(data.Body, InputDataFormat.Delimited);

                case "mts":
                    return _context.Parse(data.Body, InputDataFormat.MTS);

                case "psedi":
                    return _context.Parse(data.Body, InputDataFormat.psEDI);

                case "parada":
                    return _context.Parse(data.Body, InputDataFormat.Parada);

                case "json":
                    return _context.Parse(data.Body, InputDataFormat.JSON);

                case "tagged":
                case "mot":
                    return _context.Parse(data.Body, InputDataFormat.Tagged);
                
                default:
                    break;
            }

            return _context.Parse(data.Body);
        }

        [HttpPost("raw")]
        public ActionResult<string> PostRaw(string data)
        {
            return _context.Parse(data);
        }

        // POST api/textblock
        [HttpPost("hl7")]
        public ActionResult<string> PostHL7([FromBody] JsonDoc data)
        {
            return _context.Parse(data.Body, InputDataFormat.HL7);
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
