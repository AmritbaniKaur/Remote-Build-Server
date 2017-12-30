//////////////////////////////////////////////////////////////////////////////
// RepoMock.cs -    Demonstrate a Mock Repo Operations                      //
//                  ver 4.0                                                 //
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
 *      Simulates basic Repository operations. The Repository is supposed to
 *      get BuildRequests from Client. Then it should send the code files and
 *      build requests to the Build Server for Compilation.
 *      
 *      Here, we are generating a no. of Build Requests and sending it to the MotherBuilder
 *      for compilation. When the assigned ChildBuilder will need files from the repo to process
 *      a build request, the Child builder will send a message asking for the specific files.
 *      The Repo, should then send the specific files to the Child Builder.
 * 
 * Public Methods:
 * ==============
 *      Class RepoMock -
 *      - RepoMock()        : initializes RepoMock Storage and Comm objects 
 *      - requestHandler()  : overridden from ChannelComm Abstract class, shows how comm messages will
 *                            be handled
 *      
 * Build Process:
 * ==============
 *	- Required Files:
 *          RepoMock.cs
 * 	- Build commands:
 *		    devenv RemoteBuildServer.sln
 *		    
 * Maintenance History:
 * ===================
 *      ver 4.0 : Dec 06, 2017
 *      ver 3.0 : Nov 01, 2017
 *      ver 2.0 : Oct 05, 2017
 *      ver 1.0 : Sep 07, 2017
 *          - first release
 *      
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MessagePassingComm;
using System.Threading;
using NavigatorClient;

namespace Federation
{
    ///////////////////////////////////////////////////////////////////
    // RepoMock class
    // - begins to simulate basic Repo operations
    // - and Demonstrates how the communication will happen with the Repo

    public class RepoMock : ChannelComm
    {
        // Endpoints
        private static string repositoryEndpoint { get; set; } = "http://localhost:8070/IPluggableComm";
        private static string motherBuilderEndpoint { get; set; } = "http://localhost:8080/IPluggableComm";

        // Storages
        private string repoStorage { get; set; } = "..\\..\\..\\Storage\\RepositoryStorage\\";
        private string childBuilderStorage { get; set; } = null;
        private static string currentPath { get; set; } = "\\";
        private static Stack<string> pathStack { get; set; } = new Stack<string>();
        private List<string> fileList { get; set; } = new List<string>();

        // Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher = new Dictionary<string, Func<CommMessage, CommMessage>>();

        /*----< initialize RepoMock Storage and Comm objects >---------------------------*/
        public RepoMock(string baseAddress, int port)
        {
            
            // checks if Directories are present or not
            if (!Directory.Exists(repoStorage))
                Directory.CreateDirectory(repoStorage);

            // Comm functions
            repositoryEndpoint = baseAddress + ":" + port.ToString() + "/IPluggableComm";
            base.initializeComm(baseAddress, port);

            Console.Title = "Repository with EndPoint: " + repositoryEndpoint;
        }

        // overriding from abstract base class 'ChannelComm' present in IMPCommService
        // demonstrates how the Repo will handled the messages received by it
        public override void requestHandler(CommMessage commMsg)
        {
            switch (commMsg.type)
            {
                case CommMessage.MessageType.connect:
                    {
                        Console.WriteLine("\n Connected to Repository!");
                        break;
                    }
                case CommMessage.MessageType.request:
                    {
                        handleRequestMessage(commMsg);
                        break;
                    }
                case CommMessage.MessageType.reply:
                    {
                        Console.WriteLine("\n Repository received a {0} message from {1}! ", commMsg.type, commMsg.from);
                        break;
                    }
                default:
                    {
                        Console.WriteLine("\n Inside default in Switch-Case of Repository");
                        Console.WriteLine("\n You are missing some message case");
                        break;
                    }
            }
        }

        private void handleRequestMessage(CommMessage commMsg)
        {
            if (commMsg.command == "fileRequest")
            {
                handleFileRequestMessage(commMsg);
            }
            else if (commMsg.command == "getTopFiles")
            {
                handleGetTopFiles(commMsg);
            }
            else if (commMsg.command == "getTopDirs")
            {
                handleGetTopDirs(commMsg);
            }
            else if (commMsg.command == "moveIntoFolderFiles")
            {
                handleMoveIntoFolderFiles(commMsg);
            }
            else if (commMsg.command == "moveIntoFolderDirs")
            {
                handleMoveIntoFolderDirsRequest(commMsg);
            }
            else if (commMsg.command == "bRFileReceived")
            {
                Console.WriteLine("\n Repository received a {0} message from EndPoint {1}! ", commMsg.command, commMsg.from);
                commMsg.show();
            }
            else if (commMsg.command == "buildRequest")
            {
                Console.WriteLine("\n Repository received a {0} from EndPoint {1}! ", commMsg.command, commMsg.from);
                commMsg.show();

                handleBuildRequest(commMsg);
            }
            else if (commMsg.command == "buildAllRequest")
            {
                handleBuildAllRequest(commMsg);
            }
            else
            {
                Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                Console.WriteLine("\n Repository received a {0} from {1}! ", commMsg.command, commMsg.from);
                Console.WriteLine("\n Inside else case for Request Message");
                Console.WriteLine("CommMessage: ");
                commMsg.show();
                Console.WriteLine("\n You are missing some message case!");
                Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            }

        }

        private void handleFileRequestMessage(CommMessage commMsg)
        {
            Console.WriteLine("\n Repository received a {0} from EndPoint {1}! ", commMsg.command, commMsg.from);
            commMsg.show();

            string childBuilder = commMsg.from;

            // The message will go as a reply to the ChildBuilder
            commMsg.type = CommMessage.MessageType.reply;
            commMsg.to = commMsg.from;
            commMsg.from = repositoryEndpoint;

            // populate the list of files
            fileList.Clear();
            foreach (string file in commMsg.arguments)
            {
                fileList.Add(file);
            }

            string toEndPoint = commMsg.to;
            // retrieve port no. of ChildBuilder
            int index = toEndPoint.IndexOf("t:") + 2;
            string port = toEndPoint.Substring(index, 4);
            int portNo = Convert.ToInt32(port);

            // set up Storage Directory for the Child
            childBuilderStorage = "..\\..\\..\\Storage\\BuilderStorage\\Child_" + portNo + "\\";

            if (!Directory.Exists(childBuilderStorage))
                Directory.CreateDirectory(childBuilderStorage);

            childBuilderStorage = Path.Combine(childBuilderStorage, commMsg.requestName);

            if (!Directory.Exists(childBuilderStorage))
                Directory.CreateDirectory(childBuilderStorage);

            //Send File to Specific ChildBuilder
            triggerRepo(commMsg.to, commMsg.requestName);

            //Send Reply to Specific ChildBuilder
            Console.WriteLine("\n Repository Sends a Reply to ChildBuilder {0}", childBuilder);
            commChannel.postMessage(commMsg);

        }

        private void handleGetTopFiles(CommMessage commMsg)
        {
            Console.WriteLine("\n Repository received a {0} from EndPoint {1}! ", commMsg.command, commMsg.from);
            commMsg.show();

            //////////////////////////////////////////////////////////
            // getFiles()
            List<string> files = new List<string>();

            // send the files in a message
            CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
            reply.to = commMsg.from;
            reply.from = commMsg.to;
            reply.command = "getTopFiles";

            string path = "";

            if (pathStack.Count >= 2)
            {
                pathStack.Pop();
                path = pathStack.Peek();
            }
            else
            {
                path = repoStorage;
            }

            string absPath = Path.GetFullPath(path);
            files = Directory.GetFiles(path).ToList<string>();
            for (int i = 0; i < files.Count(); ++i)
            {
                files[i] = Path.Combine(absPath, Path.GetFileName(files[i]));
            }

            reply.arguments = files;

            reply.show();
            commChannel.postMessage(reply);

        }

        private void handleGetTopDirs(CommMessage commMsg)
        {
            Console.WriteLine("\n Repository received a {0} from EndPoint {1}! ", commMsg.command, commMsg.from);
            commMsg.show();

            // send the dirs in a message
            CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
            reply.to = commMsg.from;
            reply.from = commMsg.to;
            reply.command = "getTopDirs";

            ////////////////////////////////////////////////////
            // getDirs()
            List<string> dirs = new List<string>();

            if (repoStorage == null)
            {
                repoStorage = "..\\..\\..\\Storage\\RepositoryStorage\\";
            }

            string path = repoStorage;

            if (pathStack.Count >= 2)
            {
                path = pathStack.Peek();
            }

            string absPath = Path.GetFullPath(path);
            dirs = Directory.GetDirectories(path).ToList<string>();
            for (int i = 0; i < dirs.Count(); ++i)
            {
                string dirName = new DirectoryInfo(dirs[i]).Name;
                dirs[i] = Path.Combine(absPath, dirName);
            }

            reply.arguments = dirs;

            reply.show();
            commChannel.postMessage(reply);

        }

        private void handleMoveIntoFolderFiles(CommMessage commMsg)
        {
            Console.WriteLine("\n Repository received a {0} from EndPoint {1}! ", commMsg.command, commMsg.from);
            commMsg.show();

            if (commMsg.arguments.Count() == 1)
                currentPath = commMsg.arguments[0];

            CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
            reply.to = commMsg.from;
            reply.from = commMsg.to;
            reply.command = "moveIntoFolderFiles";

            ///////////////////////////////////////////////////////
            // <getFiles()>
            List<string> files = new List<string>();
            // For the sub folder structure
            string path = "";

            if (pathStack.Count != 0)
                path = pathStack.Peek();
            else
                path = repoStorage;

            if (currentPath != null)
            {
                path = Path.Combine(path, currentPath);
                pathStack.Push(path);
            }
            else
            {
                currentPath = repoStorage;
                pathStack.Push(path);
            }

            string absPath = Path.GetFullPath(path);
            files = Directory.GetFiles(path).ToList<string>();
            for (int i = 0; i < files.Count(); ++i)
            {
                files[i] = Path.Combine(absPath, Path.GetFileName(files[i]));
            }
            reply.arguments = files;
            reply.show();
            commChannel.postMessage(reply);
        }

        private void handleMoveIntoFolderDirsRequest(CommMessage commMsg)
        {
            Console.WriteLine("\n Repository received a {0} from EndPoint {1}! ", commMsg.command, commMsg.from);
            commMsg.show();

            if (commMsg.arguments.Count() == 1)
            {
                currentPath = commMsg.arguments[0];
            }

            CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
            reply.to = commMsg.from;
            reply.from = commMsg.to;
            reply.command = "moveIntoFolderDirs";

            ////////////////////////////////////////////////////////////////
            // getDirs()
            List<string> dirs = new List<string>();

            if (repoStorage == null)
                repoStorage = "..\\..\\..\\Storage\\RepositoryStorage\\";

            string path = "";

            if (pathStack.Count != 0)
            {
                path = pathStack.Peek();
            }
            else
            {
                path = repoStorage;
            }

            string absPath = Path.GetFullPath(path);
            dirs = Directory.GetDirectories(path).ToList<string>();
            for (int i = 0; i < dirs.Count(); ++i)
            {
                string dirName = new DirectoryInfo(dirs[i]).Name;
                dirs[i] = Path.Combine(absPath, dirName);
            }

            reply.arguments = dirs;

            reply.show();
            commChannel.postMessage(reply);

        }

        private void handleBuildAllRequest(CommMessage commMsg)
        {
            Console.WriteLine("\n Repository received a {0} from EndPoint {1}! ", commMsg.command, commMsg.from);
            commMsg.show();

            string buildRequestStorage = Path.Combine(repoStorage, "BuildRequests\\");
            // Get All Build Requests from the Repo
            List<string> files = new List<string>();
            string absPath = Path.GetFullPath(buildRequestStorage);
            files = Directory.GetFiles(absPath).ToList<string>();
            for (int i = 0; i < files.Count(); ++i)
            {
                files[i] = Path.Combine(absPath, Path.GetFileName(files[i]));
            }

            // Create new Message for sending a normal buildRequest to the Repo itself
            string reqName = "";
            string sTo = "";
            int pTo = -1;

            // Send Build Requests for all the files to the motherBuilder
            foreach (string fi in files)
            {
                CommMessage buildAllMsg = new CommMessage(CommMessage.MessageType.request);
                buildAllMsg.to = repositoryEndpoint;
                buildAllMsg.from = repositoryEndpoint;
                buildAllMsg.command = "buildRequest";

                // Change the reqName without the .xml
                reqName = Path.GetFileName(fi);
                sTo = ".xml";
                pTo = reqName.IndexOf(sTo);
                reqName = reqName.Substring(0, pTo);
                buildAllMsg.requestName = reqName;

                // Arguments will contain the file contents
                buildAllMsg.arguments.Clear();
                string[] contents = System.IO.File.ReadAllLines(fi);
                foreach (string line in contents)
                {
                    buildAllMsg.arguments.Add(line);
                }
                handleBuildRequest(buildAllMsg);
                Thread.Sleep(1000);
            }

        }

        // Parses a string to get the list of files
        private List<string> parse(string mainRequest)
        {
            ///////////////////////////////////////////////////////////////////
            // Parsing the arguments
            List<string> fileNames = new List<string>();

            // to keep the string after "</tested>"
            string restOfTheRequest = "";
            string copy1OfMainRequest = mainRequest;

            string toBeSearched = "<test>";
            int indexToBeSearched = copy1OfMainRequest.IndexOf(toBeSearched);
            string copy2OfMainRequest = "";

            restOfTheRequest = copy1OfMainRequest.Substring(indexToBeSearched + toBeSearched.Length);

            string name = "";
            while (indexToBeSearched != -1)
            {
                string sFrom = "<tested>";
                string sTo = "</tested>";
                copy2OfMainRequest = restOfTheRequest;

                int pFrom = copy2OfMainRequest.IndexOf(sFrom) + sFrom.Length;
                int pTo = copy2OfMainRequest.LastIndexOf(sTo);
                restOfTheRequest = copy2OfMainRequest.Substring(pFrom/*, pTo - pFrom*/);

                // to get .cs file names
                sTo = "</tested>";
                pTo = restOfTheRequest.IndexOf(sTo);
                name = restOfTheRequest.Substring(0, pTo);
                fileNames.Add(name);

                indexToBeSearched = restOfTheRequest.IndexOf(sFrom);

            }
            return fileNames;
        }

        // handles build Request messages
        private void handleBuildRequest(CommMessage commMsg)
        {
            if (commMsg.arguments != null)
            {
                // Saving the .xml in a string
                StringBuilder mainReq = new StringBuilder();
                foreach (string sb in commMsg.arguments)
                {
                    mainReq.Append(sb);
                }
                string mainRequest = mainReq.ToString();

                // Parse the request and get the names of the file you want to build
                List<string> fileNames = parse(mainRequest);

                //////////////////////////////////////////////////////////////
                // create a build request message for the MotherBuilder
                CommMessage buildRequest = commMsg.clone();
                buildRequest.to = motherBuilderEndpoint;
                buildRequest.from = repositoryEndpoint;

                buildRequest.arguments.Clear();

                foreach (string file in fileNames)
                {
                    buildRequest.arguments.Add(file);
                }

                Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++");
                Console.WriteLine("\n Printing the new message for mother!");
                commMsg.show();

                // Send the Build Request Message to the MotherBuilder
                commChannel.postMessage(buildRequest);
            }

        }


        // sends files to the ChildBuilder
        private void triggerRepo(string toEndPoint, string buildRequestName)
        {
            Console.WriteLine("\n =========================================================================================");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine(" Triggering Mock Repository for sending files to the Child Builder!");
            Console.ResetColor();
            Console.WriteLine(" =========================================================================================");

            Console.WriteLine("\n Sending the following files to {0} \n", childBuilderStorage);

            // specify the list of files which are being sent
            foreach (string file in fileList)
            {
                Console.WriteLine("\n\t {0}", file.ToString());

                // Send files to the ChildBuilder
                Thread.Sleep(100);
                commChannel.postFile(file, repoStorage, childBuilderStorage, toEndPoint);
            }

            // for the buildRequest.xml file
            string buildRequestFolder = Path.Combine(repoStorage, "BuildRequests");
            string fileName = buildRequestName + ".xml";

            Thread.Sleep(100);
            commChannel.postFile(fileName, buildRequestFolder, childBuilderStorage, toEndPoint);

            Console.WriteLine("\n =========================================================================================");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine(" Mock Repo functionality Completed!");
            Console.WriteLine(" The Mock Repository sent the code files to the Child Build Server");
            Console.ResetColor();
            Console.WriteLine(" =========================================================================================");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("\n =============================================================================================================================================");
            Console.WriteLine(" Repository Functionality: ");
            Console.WriteLine(" ============================================================================================================================================= \n");

            RepoMock repo = new RepoMock("http://localhost", 8070);

            Thread.Sleep(100000000);
            Thread.Sleep(100000000);
            Thread.Sleep(100000000);
            Thread.Sleep(100000000);

            Console.ReadKey();
        }
    }
}
