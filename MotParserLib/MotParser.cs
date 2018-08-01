// 
// MIT license
//
// Copyright (c) 2016 by Peter H. Jenney and Medicine-On-Time, LLC.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 




using System;
using System.Linq;
using System.Xml;
using System.Text.RegularExpressions;
using NLog;
using Newtonsoft.Json;
using MotCommonLib;
using MotHL7Lib;

namespace MotParserLib
{
    public class ParserEventMessageArgs : EventArgs
    {
        public string ParserIdentifier { get; set; }
        public InputType InputType { get; set; } = InputType.Unknown;

        public DateTime Timestamp { get; set; }
        public string Data { get; set; }

        // HL7 Specific
        public HL7SendingApplication HL7SendingApp { get; set; } = HL7SendingApplication.AutoDiscover;
    }

    public enum HL7SendingApplication
    {
        AutoDiscover = 0,
        FrameworkLTC,
        Epic,
        QS1,
        RX30,
        RNA,
        Pharmaserve,
        Enterprise,
        Unknown
    };

    public enum InputDataFormat
    {
        AutoDetect = 0,
        Delimited,
        Tagged,
        XML,
        JSON,
        Parada,
        Dispill,
        HL7,
        MTS,
        psEDI,
        Unknown
    }
    public enum InputType
    {
        File,
        Socket,
        WebService,
        Unknown
    }

    /// <inheritdoc />
    /// <summary>
    /// <c>ParserBase</c>
    /// Base class for all file parsers
    /// </summary>
    public class ParserBase : IDisposable
    {
        protected string data;
        public string ResponseMessage { get; set; }

        /// <summary>
        /// DebugMode - Flag for extra debug output
        /// </summary>
        public bool DebugMode { get; set; }

        /// <summary>
        /// AutoTruncate - flag to force string lengths down to motLegacy lengths
        /// </summary>
        public bool AutoTruncate { get; set; }

        /// <summary>
        /// <c>GatewaySocket</c>
        /// Target socket for output
        /// </summary>
        public MotSocket GatewaySocket { get; set; }

        /// <summary>
        /// <c>SenderSocket</c>
        ///  Source socket for input
        /// </summary>
        public MotSocket SenderSocket { get; set; }
        /// <summary>
        /// eventLogger - NLOG logger
        /// </summary>
        public Logger EventLogger { get; set; }

        /// <summary>
        /// <c>InputFormat</c>
        /// </summary>
        public InputDataFormat InputFormat { get; set; }

        /// <summary>
        /// <c>SendEof</c>
        /// Forces motLegacy gateway to close
        /// </summary>
        public bool SendEof { get; set; }

        /// <summary>
        /// <c>RunAsService</c>
        /// Flag to tell if the process is running as a service
        /// </summary>
        public bool RunAsService { get; set; }

        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //Socket?.Dispose();
                //Socket = null;
            }
        }

        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// <c>ParserBase</c>
        /// Constructor
        /// </summary>
        public ParserBase(string inputStream)
        {
            EventLogger = LogManager.GetLogger("motInboundLib.Parser");

            if (string.IsNullOrEmpty(inputStream))
            {
                EventLogger.Error($"inputStream is NULL or Empty");
                throw new ArgumentNullException($"inputStream is NULL or Empty");
            }

            data = inputStream;
        }

        protected virtual void WriteListToGateway()
        {

        }

        private void WriteBlockToGateway(string inboundData)
        {
            if (string.IsNullOrEmpty(inboundData))
            {
                throw new ArgumentNullException($"inboundData data Null or Empty");
            }
            if (GatewaySocket == null)
            {
                throw new ArgumentNullException($"Invalid Socket Reference");
            }

            try
            {
                GatewaySocket.Write(inboundData);

                if (SendEof)
                {
                    GatewaySocket.Write("<EOF/>");
                }

                if (DebugMode)
                {
                    EventLogger.Debug(inboundData);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed to write to gateway: {ex.Message}");
                throw new Exception($"Failed to write to gateway: {ex.Message}");
            }
        }

        /// <summary>
        /// <c>parseTagged</c>
        /// </summary>
        /// <param name="dataIn"></param>
        protected void ParseTagged(string dataIn)
        {
            try
            {
                if (string.IsNullOrEmpty(dataIn) || !dataIn.ToLower().Contains("<record>"))
                {
                    throw new ArgumentException("Malformed DataIn argument");
                }

                WriteBlockToGateway(dataIn);
            }
            catch (Exception e)
            {
                var errorStr = $"Tagged Parser Error: {e.Message}";
                EventLogger.Error(errorStr);
                throw new Exception(errorStr);
            }
        }
    }

    /// <summary>
    /// <c>motParser</c>
    /// General data format transformation engine
    /// </summary>
    public class MotParser : ParserBase, IDisposable
    {
        /// <summary>
        /// <c>parseAndReturnTagged</c>
        /// Converts tagged data into true Xml
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns>XmlDocumnt containing tagged data</returns>
        protected XmlDocument ParseAndReturnTagged(string inputStream)
        {
            var test = new XmlDocument();
            test.Load(inputStream);
            return test;
        }

        /// <summary>
        /// <c>parseJSON</c>
        /// </summary>
        /// Converts JSON to XML and then passes to the XML parser to deal with
        /// <param name="dataIn"></param>
        /// <param name="xmlOut"></param>
        /// <returns></returns>
        protected string ParseJson(string dataIn, bool xmlOut = true)
        {
            var retVal = string.Empty;

            try
            {
                if (xmlOut)
                {
                    var xmlDoc = JsonConvert.DeserializeXmlNode(dataIn, "Record");
                    if (xmlDoc != null)
                    {
                        if (GatewaySocket != null)
                        {
                            ParseTagged(xmlDoc.InnerXml);
                        }
                    }
                }
                else
                {
                    retVal = JsonConvert.DeserializeObject<string>(dataIn);
                }
            }
            catch (JsonReaderException e)
            {
                throw new Exception($"JSON Reader error {e.Message}");
            }
            catch (JsonSerializationException e)
            {
                throw new Exception($"JSON Serialization error: {e.Message}");
            }

            return retVal;
        }

        /// <summary>
        /// <c>parseXML</c>
        /// Takes a true Xml and converts it to the motLegacy tagged format
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="writeToGateway"></param>
        /// <returns></returns>
        protected XmlDocument ParseXml(string inputStream, bool writeToGateway = true)
        {
            if (GatewaySocket == null)
            {
                writeToGateway = false;
            }

            var xmlDoc = new XmlDocument();

            try
            {
                // Check if it's actual XML or not. If so, strip headers up to <Record>
                if (inputStream.Contains("<?xml") == false)
                {
                    throw new ArgumentException("Malformed XML");
                }

                xmlDoc.LoadXml(inputStream);

                //
                // Clear out all the comments
                //
                var nodeList = xmlDoc.SelectNodes("//comment()");
                if (nodeList != null)
                {
                    foreach (XmlNode node in nodeList)
                    {
                        node.ParentNode?.RemoveChild(node);
                    }
                }

                //
                // Validate all required fields have content
                // 
                nodeList = xmlDoc.SelectNodes("//*[@required]");
                if (nodeList != null)
                {
                    foreach (XmlNode node in nodeList)
                    {
                        if (node.Attributes != null &&
                            (node.Attributes[0].Value.ToLower() == "true" &&
                             node.NodeType == XmlNodeType.Element))
                        {
                            if (node.InnerText.Length == 0)
                            {
                                throw new ArgumentException(
                                    $@"XML Missing Require Element Content {node.Name}in {xmlDoc.Name}");
                            }

                            node.Attributes.RemoveNamedItem("required");
                        }
                    }
                }

                //
                // Validate field lengths and remove attributes
                //
                nodeList = xmlDoc.SelectNodes("//*[@size]");
                if (nodeList != null)
                {
                    foreach (XmlNode node in nodeList)
                    {
                        if (node.NodeType == XmlNodeType.Element)
                        {
                            if (node.Attributes != null &&
                                node.InnerText.Length > Convert.ToUInt32(node.Attributes[0].Value))
                            {
                                throw new ArgumentException(@"Element Size Overflow at {0}", node.Name);
                            }

                            node.Attributes?.RemoveNamedItem("size");
                        }
                    }
                }

                //
                // Validate for numeric overflow
                //
                nodeList = xmlDoc.SelectNodes("//*[@maxvalue]");
                if (nodeList != null)
                {
                    foreach (XmlNode node in nodeList)
                    {
                        if (node.NodeType == XmlNodeType.Element)
                        {
                            if (node.Attributes != null && Convert.ToDouble(node.InnerText) >
                                Convert.ToDouble(node.Attributes[0].Value))
                            {
                                throw new ArgumentException(@"Element MaxValue Overflow at {0}", node.Name);
                            }

                            node.Attributes?.RemoveNamedItem("maxvalue");
                        }
                    }
                }
            }
            catch (XmlException e)
            {
                throw new Exception($"XML Parse Failure {e.Message}");
            }
            catch (FormatException e)
            {
                throw new Exception($"XML Parse Error {e.Message}");
            }

            //
            // Finally, clear out the namespace attributes.
            //
            const string xmlnsPattern = "\\s+xmlns\\s*(:\\w)?\\s*=\\s*\\\"(?<url>[^\\\"]*)\\\"";
            var matchCol = Regex.Matches(xmlDoc.InnerXml, xmlnsPattern);

            foreach (Match m in matchCol)
            {
                xmlDoc.InnerXml = xmlDoc.InnerXml.Replace(m.ToString(), "");
            }

            // Finally, get the <?xml line and be done with it.
            xmlDoc.InnerXml = xmlDoc.InnerXml.Substring(xmlDoc.InnerXml.IndexOf(">", StringComparison.Ordinal) + 1);

            if (writeToGateway)
            {
                ParseTagged(xmlDoc.InnerXml);
            }

            return xmlDoc;
        }

        /// <summary>
        /// psEDI file parser 
        /// </summary>
        /// <param name="inputStream"></param>
        protected void ParseMTS(string inputStream)
        {
            using (var mtsParser = new MtsParser(inputStream))
            {
                mtsParser.GatewaySocket = GatewaySocket;
                mtsParser.AutoTruncate = AutoTruncate;
                mtsParser.DebugMode = DebugMode;
                mtsParser.EventLogger = EventLogger;
                mtsParser.Go();
            }
        }

        /// <summary>
        /// psEDI file parser 
        /// </summary>
        /// <param name="inputStream"></param>
        protected void ParseEdi(string inputStream)
        {
            using (var psEdiParser = new PsEdiParser(inputStream))
            {
                psEdiParser.GatewaySocket = GatewaySocket;
                psEdiParser.AutoTruncate = AutoTruncate;
                psEdiParser.DebugMode = DebugMode;
                psEdiParser.EventLogger = EventLogger;
                psEdiParser.Go();
            }
        }

        /// <summary>
        /// <c>ParseParada</c>
        /// Transforms a parada job table into a proper MOT record.  Used by BestRx
        /// </summary>
        /// <param name="inputStream"></param>
        protected void ParseParada(string inputStream)
        {
            using (var paradaParser = new ParadaParser(inputStream))
            {
                paradaParser.GatewaySocket = GatewaySocket;
                paradaParser.AutoTruncate = AutoTruncate;
                paradaParser.DebugMode = DebugMode;
                paradaParser.EventLogger = EventLogger;
                paradaParser.Go();
            }
        }

        /// <summary>
        /// <c>ParseDelimited</c>
        /// Process MOT Legacy Delimited input stream
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="v1Format"></param>
        protected void ParseDelimited(string inputStream, bool v1Format = false)
        {
            using (var delimitedParser = new LegacyDelimitedParser(inputStream))
            {
                delimitedParser.GatewaySocket = GatewaySocket;
                delimitedParser.AutoTruncate = AutoTruncate;
                delimitedParser.DebugMode = DebugMode;
                delimitedParser.EventLogger = EventLogger;
                delimitedParser.Go(v1Format);
            }
        }

        /// <summary>
        /// <c>ParseHL7</c>
        /// Process HL7 2.5+ input stream
        /// </summary>
        /// <param name="inputStream"></param>
        protected void ParseHL7(string inputStream)
        {
            using (var hl7Parser = new MotHl7MessageProcessor(inputStream))
            {
                hl7Parser.Socket = GatewaySocket;
                hl7Parser.AutoTruncate = AutoTruncate;
                hl7Parser.DebugMode = DebugMode;
                hl7Parser.EventLogger = EventLogger;
                hl7Parser.Go();

                ResponseMessage = hl7Parser.ResponseMessage;
            }
        }

        /// <summary>
        /// <c>ParseDispill</c>
        /// Process Dispill input stream
        /// </summary>
        /// <param name="inputStream"></param>
        protected void ParseDispill(string inputStream)
        {
            using (var dispillParser = new DispillParser(inputStream))
            {
                dispillParser.GatewaySocket = GatewaySocket;
                dispillParser.AutoTruncate = AutoTruncate;
                dispillParser.DebugMode = DebugMode;
                dispillParser.EventLogger = EventLogger;
                dispillParser.Go();
            }
        }

        /// <summary>
        /// <c>ParseByGuess</c>
        /// Try to determine the content type and send it to the right parser
        /// </summary>
        /// <param name="inputStream"></param>
        private void ParseByGuess(string inputStream)
        {
            try
            {
                //
                // Figure out what the input type is and set up the right parser
                //
                if (inputStream.Contains("MSH") || inputStream.Contains(@"|^~\&"))
                {
                    // HL7 is a slam dunk
                    ParseHL7(inputStream);
                    return;
                }

                if (inputStream.Contains("<?") && inputStream.ToLower().Contains("xml"))
                {
                    // Pretty sure its a live XML file
                    ParseXml(inputStream);
                    return;
                }

                if (inputStream.Contains("{") && inputStream.Contains(":"))
                {
                    // Pretty sure its a live JSON file
                    ParseJson(inputStream);
                    return;
                }

                if (inputStream.ToLower().Contains("<record>") && inputStream.ToLower().Contains("<table>"))
                {
                    // Pretty sure its a MOT tagged file
                    ParseTagged(inputStream);
                    return;
                }

                if (inputStream.Substring(0, 1) == "P")
                {
                    ParseDispill(inputStream);
                    return;
                }

                if (inputStream.Contains("~\r"))
                {
                    ParseParada((inputStream));
                    return;
                }

                if (inputStream.Contains('\xEE') && inputStream.Contains('\xE2'))   // MOT Delimited                   
                {
                    ParseDelimited(inputStream);
                    return;
                }

                if (Regex.Match(inputStream, "\\d{10}S").Success)    // QS/1 Delimited
                {
                    ParseDelimited(inputStream);
                    return;
                }

                EventLogger.Error("Unidentified file type");
                throw new Exception("Unidentified file type");
            }
            catch (Exception ex)
            {
                EventLogger.Error("Parse failure: {0}", ex.Message);
                throw new Exception("Parse failure: " + ex.Message);
            }
        }
        /// <inheritdoc />
        public MotParser(string inputStream, InputDataFormat inputDataFormat, bool autoTruncate = false, bool sendEof = false, bool debugMode = false) : base(inputStream)
        {
            try
            {
                GatewaySocket = new MotSocket("localhost", 24042);
                RunParser(inputDataFormat, inputStream);
            }
            catch
            {
                EventLogger.Error("motParser failed on input type {0} and data: {1}", inputDataFormat.ToString(), inputStream);
                throw;
            }
        }
        /// <inheritdoc />
        public MotParser(MotSocket outSocket, string inputStream, bool autoTruncate) : base(inputStream)
        {
            GatewaySocket = outSocket ?? throw new ArgumentNullException($"NULL Socket passed to motParser");

            if (GatewaySocket.Disposed)
            {
                throw new ArgumentNullException($"Disposed Socket passed to motParser");
            }

            AutoTruncate = autoTruncate;

            try
            {
                ParseByGuess(inputStream);
            }
            catch
            {
                EventLogger.Error("motParser failed on input type {0} and data: {1}", "Best Guess", inputStream);
                throw;
            }
        }
        /// <inheritdoc />
        public MotParser(MotSocket outSocket, string inputStream, InputDataFormat inputDataFormat, bool autoTruncate = false, bool sendEof = false, bool debugMode = false) : base(inputStream)
        {
            GatewaySocket = outSocket ?? throw new ArgumentNullException($"NULL Socket passed to motParser");

            if (GatewaySocket.Disposed)
            {
                throw new ArgumentNullException($"Disposed Socket passed to motParser");
            }

            SendEof = sendEof;
            DebugMode = debugMode;
            AutoTruncate = autoTruncate;

            try
            {
                RunParser(inputDataFormat, inputStream);
            }
            catch
            {
                EventLogger.Error($"motParser failed on input type {inputDataFormat} and data: {inputStream}");
                throw;
            }
        }
        /// <inheritdoc />
        ~MotParser()
        {
            Dispose(true);
        }
        private void RunParser(InputDataFormat inputDataFormat, string inputStream)
        {
            try
            {
                switch (inputDataFormat)
                {
                    case InputDataFormat.AutoDetect:
                        ParseByGuess(inputStream);

                        if (DebugMode)
                        {
                            EventLogger.Debug($"Completed Best Guess processing: {inputStream}");
                        }
                        break;

                    case InputDataFormat.XML:
                        ParseXml(inputStream);

                        if (DebugMode)
                        {
                            EventLogger.Debug($"Completed XML processing: {inputStream}");
                        }
                        break;

                    case InputDataFormat.JSON:
                        ParseJson(inputStream);

                        if (DebugMode)
                        {
                            EventLogger.Debug($"Completed JSON processing: {inputStream}");
                        }
                        break;

                    case InputDataFormat.Delimited:
                        ParseDelimited(inputStream);

                        if (DebugMode)
                        {
                            EventLogger.Debug($"Completed Delimited File processing: {inputStream}");
                        }
                        break;

                    case InputDataFormat.Tagged:
                        ParseTagged(inputStream);

                        if (DebugMode)
                        {
                            EventLogger.Debug($"Completed Tagged File processing: {inputStream}");
                        }
                        break;

                    case InputDataFormat.Parada:
                        ParseParada(inputStream);

                        if (DebugMode)
                        {
                            EventLogger.Debug($"Completed Parda file processng: {inputStream}");
                        }
                        break;

                    case InputDataFormat.Dispill:
                        ParseDispill(inputStream);

                        if (DebugMode)
                        {
                            EventLogger.Debug($"Completed Dispill File Processing: {inputStream}");
                        }
                        break;

                    case InputDataFormat.HL7:
                        ParseHL7(inputStream);

                        if (DebugMode)
                        {
                            EventLogger.Debug($"Completed HL7 File Processing: {inputStream}");
                        }
                        break;

                    case InputDataFormat.MTS:
                    case InputDataFormat.psEDI:
                        ParseEdi(inputStream);
                        if (DebugMode)
                        {
                            EventLogger.Debug($"Completed Oasis File Processing: {inputStream}");
                        }
                        break;

                    case InputDataFormat.Unknown:

                        if (DebugMode)
                        {
                            EventLogger.Debug($"Unknown File Type: {inputStream}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"motParser failed on input type {inputDataFormat.ToString()} and data: {inputStream}. Error {ex.Message}");
                throw;
            }
        }
    }
}
