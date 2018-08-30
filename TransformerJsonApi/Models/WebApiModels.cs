using System;
using Mot.Parser.InterfaceLib;

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
