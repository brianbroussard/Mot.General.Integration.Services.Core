using System;
using MotParserLib;

namespace TransformerJsonApi.Models
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
