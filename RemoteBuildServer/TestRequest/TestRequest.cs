//////////////////////////////////////////////////////////////////////////////
// TestRequest.cs - build and parse test requests                           //
// ver 1.1                                                                  //
//--------------------------------------------------------------------------//
//  Source:         Prof. Jim Fawcett, CST 4-187, jfawcett@twcny.rr.com     //
//	Author:			Amritbani Sondhi,										//
//					Graduate Student, Syracuse University					//
//					asondhi@syr.edu											//
//	Application:	CSE 681 Project #3, Fall 2017							//
//	Platform:		HP Envy x360, Core i7, Windows 10 Home					//
//  Environment:    C#, Visual Studio 2017 RC                               //
//////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Creates and parses TestRequest XML messages using XDocument
 *
 * Public Methods:
 * ==============
 *      Class TestRequest -
 *      - makeRequest() : build XML document that represents a test request
 *      - loadXml()     : load TestRequest from XML file
 *      - saveXml()     : save TestRequest to XML file
 *      - parse()       : parse document for property value 
 *      - parseList()   : parse document for property list
 *      
 * Build Process:
 * ==============
 *	- Required Files:
 *          TestRequest.cs
 * 	- Build commands:
 *		    devenv RemoteBuildServer.sln
 *		    
 * Maintenance History:
 * ===================
 *      ver 1.0 : 07 Sep 2017
 *          - first release
 *      ver 1.1 : 05 Oct, 2017
 *          - added: toRequest, fromRequest, typeOfRequest
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HelpSession
{
    ///////////////////////////////////////////////////////////////////
    // TestRequest class
    public class TestRequest
    {
        public string toRequest { get; set; } = "";
        public string fromRequest { get; set; } = "";

        public string typeOfRequest { get; set; } = ""; // Build Request or a Test Request
        public string dateTime { get; set; } = "";
        public string testDriver { get; set; } = "";
        public List<string> testedFiles { get; set; } = new List<string>();
        public XDocument doc { get; set; } = new XDocument();

        /*----< build XML document that represents a test request >----*/
        public void makeRequest()
        {
            XElement testRequestElem = new XElement("testRequest");
            doc.Add(testRequestElem);

            XElement toRequestElem = new XElement("toRequest");
            toRequestElem.Add(toRequest);
            testRequestElem.Add(toRequestElem);

            XElement fromRequestElem = new XElement("fromRequest");
            fromRequestElem.Add(fromRequest);
            testRequestElem.Add(fromRequestElem);

            XElement typeOfRequestElem = new XElement("typeOfRequest");
            typeOfRequestElem.Add(typeOfRequest);
            testRequestElem.Add(typeOfRequestElem);

            XElement dateTimeElem = new XElement("dateTime");
            dateTimeElem.Add(DateTime.Now.ToString());
            testRequestElem.Add(dateTimeElem);

            XElement testElem = new XElement("test");
            testRequestElem.Add(testElem);

            XElement driverElem = new XElement("testDriver");
            driverElem.Add(testDriver);
            testElem.Add(driverElem);

            foreach (string file in testedFiles)
            {
                XElement testedElem = new XElement("tested");
                testedElem.Add(file);
                testElem.Add(testedElem);
            }
        }

        /*----< load TestRequest from XML file >-----------------------*/
        public bool loadXml(string path)
        {
            try
            {
                doc = XDocument.Load(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        /*----< save TestRequest to XML file >-------------------------*/
        public bool saveXml(string path)
        {
            try
            {
                doc.Save(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        /*----< parse document for property value >--------------------*/
        public string parse(string propertyName)
        {

            string parseStr = doc.Descendants(propertyName).First().Value;
            if (parseStr.Length > 0)
            {
                switch (propertyName)
                {
                    case "toRequest":
                        toRequest = parseStr;
                        break;
                    case "fromRequest":
                        fromRequest = parseStr;
                        break;
                    case "typeOfRequest":
                        typeOfRequest = parseStr;
                        break;
                    case "dateTime":
                        dateTime = parseStr;
                        break;
                    case "testDriver":
                        testDriver = parseStr;
                        break;
                    default:
                        break;
                }
                return parseStr;
            }
            return "";
        }

        /*----< parse document for property list >---------------------
         * - now, there is only one property list for tested files */
        public List<string> parseList(string propertyName)
        {
            List<string> values = new List<string>();

            IEnumerable<XElement> parseElems = doc.Descendants(propertyName);

            if (parseElems.Count() > 0)
            {
                switch (propertyName)
                {
                    case "tested":
                        foreach (XElement elem in parseElems)
                        {
                            values.Add(elem.Value);
                        }
                        testedFiles = values;
                        break;
                    default:
                        break;
                }
            }
            return values;
        }
    }

//#if (TEST_TESTREQUEST)

    ///////////////////////////////////////////////////////////////////
    // test_TestRequest class
    class Test_TestRequest
    {
        static void Main(string[] args)
        {
            Console.WriteLine(" =========================================================================================");
            Console.WriteLine(" Testing TestRequest Package");
            Console.WriteLine(" =========================================================================================");

            Console.WriteLine("\n");
            Console.ReadKey();
        }
    }
    //#endif

}

