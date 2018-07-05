using System;
using MotParserLib;

namespace TransformerWebApi.Models
{
    public class WebApiContext
    {
        MotTransformerInterface transformerInterface;

        public WebApiContext()
        {
            transformerInterface = new MotTransformerInterface();
        }
    }
}
