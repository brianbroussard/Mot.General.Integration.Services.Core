using System;
using System.ComponentModel.DataAnnotations;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MotCommonLib;
using MotHL7Lib;
using MotParserLib;

namespace ParserTests
{
    [TestClass]
    public class ParserTests
    {
        private const string TaggedStore =
            @"<Record><Table>Store</Table><Action>Add</Action><RxSys_StoreID>169252</RxSys_StoreID><StoreName>Southcare Pharmacy</StoreName><Address1>6499 38th Avenue N</Address1><Address2>Suite A1 St. Petersburg</Address2><City>St. Petersburg</City><State>FL</State><Zip>33710</Zip><Phone></Phone><Fax></Fax><DEANum></DEANum></Record>";
        private const string TaggedLocation =
            @"<Record><Table>Location</Table><Action>Add</Action><RxSys_LocID>CLR</RxSys_LocID><RxSys_StoreID>169252</RxSys_StoreID><LocationName>BENTON HOUSE OF CLERMONT</LocationName><Address1>16401 GOOD HEARTH BLVD</Address1><Address2></Address2><City>CLERMONT</City><State>FL</State><Zip>34711</Zip><Phone>3522419994</Phone><Comments></Comments><CycleDays></CycleDays><CycleType></CycleType></Record>";
        private const string TaggedPrescriber =
            @"<Record><Table>Prescriber</Table><Action>Add</Action><RxSys_DocID>1275584369</RxSys_DocID><LastName>MEDIRATTA</LastName><FirstName>NIBHA</FirstName><MiddleInitial></MiddleInitial><Address1>1970 HOSPITAL VIEW WAY</Address1><Address2>UNIT 1</Address2><City>CLERMONT</City><State>FL</State><Zip>347111926</Zip><Phone></Phone><Comments></Comments><DEA_ID>BM6089379</DEA_ID><TPID></TPID><Specialty></Specialty><Fax></Fax><PagerInfo></PagerInfo></Record>";
        private const string TaggedPatient =
            @"<Record><Table>Patient</Table><Action>Change</Action><RxSys_PatID>10637</RxSys_PatID><LastName>BAER</LastName><FirstName>FLORENCE</FirstName><MiddleInitial></MiddleInitial><Address1></Address1><Address2></Address2><City></City><State></State><Zip></Zip><Phone1></Phone1><Phone2></Phone2><WorkPhone></WorkPhone><RxSys_LocID>CLR</RxSys_LocID><Room></Room><Comments></Comments><CycleDate></CycleDate><CycleDays></CycleDays><CycleType></CycleType><Status>1</Status><RxSys_LastDoc></RxSys_LastDoc><RxSys_PrimaryDoc>1275584369</RxSys_PrimaryDoc><RxSys_AltDoc></RxSys_AltDoc><SSN>042243864</SSN><Allergies></Allergies><Diet></Diet><DxNotes> - </DxNotes><TreatmentNotes></TreatmentNotes><DOB>1928-08-12</DOB><Height></Height><Weight></Weight><ResponsibleName></ResponsibleName><InsName></InsName><InsPNo></InsPNo><AltInsName></AltInsName><AltInsPNo></AltInsPNo><MCareNum></MCareNum><MCaidNum></MCaidNum><AdmitDate></AdmitDate><ChartOnly></ChartOnly><Gender>F</Gender></Record>";
        private const string TaggedDrug =
            @"<Record><Table>Drug</Table><Action>Add</Action><RxSys_DrugID>00904198280</RxSys_DrugID><LblCode></LblCode><ProdCode></ProdCode><TradeName>ACETAMINOPHEN 325MG TABLET</TradeName><Strength>325</Strength><Unit>MG</Unit><RxOTC></RxOTC><DoseForm>TABS</DoseForm><Route></Route><DrugSchedule></DrugSchedule><VisualDescription></VisualDescription><DrugName>ACETAMINOPHEN 325MG TABLET</DrugName><ShortName></ShortName><NDCNum>00904198280</NDCNum><SizeFactor></SizeFactor><Template></Template><DefaultIsolate></DefaultIsolate><ConsultMsg></ConsultMsg><GenericFor></GenericFor></Record>";
        private const string TaggedTQ =
            @"<Record><Table>TimesQtys</Table><Action>Add</Action><RxSys_LocID>CLR</RxSys_LocID><DoseScheduleName>PRNP</DoseScheduleName><DoseTimesQtys>080001.00120001.00160001.00</DoseTimesQtys></Record>";
        private const string TaggedScrip =
            @"<Record><Table>Rx</Table><Action>Add</Action><RxSys_RxNum>517299</RxSys_RxNum><RxSys_PatID>10637</RxSys_PatID><RxSys_DocID>1275584369</RxSys_DocID><RxSys_DrugID>00904198280</RxSys_DrugID><Sig>TAKE 1-2 TABLETS BY MOUTH THREE TIMES DAILY AS NEEDED FOR PAIN (DAUGHTER PROVIDES)</Sig><RxStartDate>2017-11-22</RxStartDate><RxStopDate></RxStopDate><DiscontinueDate></DiscontinueDate><DoseScheduleName>PRNP</DoseScheduleName><Comments>Patient Notes:\n (1) Dose Schedule: PRNP\n</Comments><Refills>0</Refills><RxSys_NewRxNum></RxSys_NewRxNum><Isolate></Isolate><RxType>0</RxType><MDOMStart></MDOMStart><MDOMEnd></MDOMEnd><QtyPerDose>0</QtyPerDose><QtyDispensed>3</QtyDispensed><Status>1</Status><DoW></DoW><SpecialDoses></SpecialDoses><DoseTimesQtys></DoseTimesQtys><ChartOnly></ChartOnly><AnchorDate></AnchorDate></Record>";

        public class ParserTester : MotParser
        {
            public ParserTester() : base()
            {
            }

            public ParserTester(MotSocket socket, string data, bool truncate) : base(socket, data, truncate)
            {
            }

            public new XmlDocument ParseAndReturnTagged(string strData)
            {
                return base.ParseAndReturnTagged(strData);
            }

            public XmlDocument ParseXml(string dataIn)
            {
                return base.ParseXml(dataIn, false);
            }

            public string ParseJson(string dataIn)
            {
                return base.ParseJson(dataIn, false);
            }

            public new void ParseHL7(string dataIn)
            {
                try
                {
                    base.ParseHL7(dataIn);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        [TestMethod]
        public void XMLConversion()
        {
            try
            {
                var p = new ParserTester();

                var xmlTestStore = p.ParseAndReturnTagged(TaggedStore);
                var xmlTestLocation = p.ParseAndReturnTagged(TaggedLocation);
                var xmlTestPrescriber = p.ParseAndReturnTagged(TaggedPrescriber);
                var xmlTestPatient = p.ParseAndReturnTagged(TaggedPatient);
                var xmlTestDrug = p.ParseAndReturnTagged(TaggedDrug);
                var xmlTestTq = p.ParseAndReturnTagged(TaggedTQ);
                var xmlTestScrip = p.ParseAndReturnTagged(TaggedScrip);



                using (var socket = new MotSocket("localhost", 24042))
                {
                    p = new ParserTester(socket, xmlTestStore.InnerXml, false);
                    p = new ParserTester(socket, xmlTestLocation.InnerXml, false);
                    p = new ParserTester(socket, xmlTestPrescriber.InnerXml, false);
                    p = new ParserTester(socket, xmlTestPatient.InnerXml, false);
                    p = new ParserTester(socket, xmlTestDrug.InnerXml, false);
                    p = new ParserTester(socket, xmlTestTq.InnerXml, false);
                    p = new ParserTester(socket, xmlTestScrip.InnerXml, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Assert.Fail(ex.Message);
            }
        }
    }
}
