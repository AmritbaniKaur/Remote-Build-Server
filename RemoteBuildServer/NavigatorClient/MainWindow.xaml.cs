//////////////////////////////////////////////////////////////////////////////////////
// NavigatorClient.xaml.cs - Demonstrates Directory Navigation in WPF App           //
//                           ver 2.0                                                //
//----------------------------------------------------------------------------------//
//  Source:         Prof. Jim Fawcett, CST 4-187, jfawcett@twcny.rr.com             //
//	Author:			Amritbani Sondhi,										        //
//					Graduate Student, Syracuse University					        //
//					asondhi@syr.edu											        //
//	Application:	CSE 681 Project #3, Fall 2017							        //
//	Platform:		HP Envy x360, Core i7, Windows 10 Home					        //
//  Environment:    C#, Visual Studio 2017 RC                                       //
//////////////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * This package, creates a GUI to be used by the Clients to generate Build Requests and Send it to the repository
 * 
 * Public Methods:
 * ==============
 *      Class MainWindow -
 *      - getTopFiles()     - show files and dirs in root path
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
 *      ver 2.0 : Dec 06, 2017
 *      ver 1.0 : Nov 01, 2017
 *          - first release
 *      
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using HelpSession;
using System.Runtime.InteropServices;
using System.Diagnostics;
using MessagePassingComm;
using System.Threading;

namespace NavigatorClient
{
    // creates main GUI window and provides UI and functionality to it
    public partial class MainWindow : Window
    {
        // Endpoints
        private static string clientEndpoint { get; set; } = "http://localhost:8060/IPluggableComm";
        private static string repositoryEndpoint { get; set; } = "http://localhost:8070/IPluggableComm";
        private static string motherBuilderEndpoint { get; set; } = "http://localhost:8080/IPluggableComm";
        
        // Storages
        private string buildRequestPath { get; set; } = "..\\..\\..\\Storage\\MockClientStorage\\BuildRequests\\";
        private string repoBuildRequestStorage { get; set; } = "..\\..\\..\\Storage\\RepositoryStorage\\BuildRequests\\";

        private IFileMgr fileMgr { get; set; } = null;  // note: Navigator just uses interface declarations
        Comm comm { get; set; } = null;
        Dictionary<string, Action<CommMessage>> messageDispatcher = new Dictionary<string, Action<CommMessage>>();
        Thread rcvThread = null;
        private static Stack<string> pathStack { get; set; } = new Stack<string>();

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(buildAll_Click);

            // Shows the demonstration of the project
            demoProject();

            Console.WriteLine("\n =============================================================================================================================================");
            Console.WriteLine(" Client Functionality: ");
            Console.WriteLine(" ============================================================================================================================================= \n");

            fileMgr = FileMgrFactory.create(FileMgrType.Local);

            if (this.filesListBox.Items.Count > 0)
                this.filesListBox.SelectedIndex = 0;

            comm = new Comm(ClientEnvironment.address, ClientEnvironment.port);
            initializeMessageDispatcher();
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();

            Console.Title = "Client with EndPoint: " + ClientEnvironment.endPoint;
        }


        //----< make Environment equivalent to ClientEnvironment >-------
        void initializeEnvironment()
        {
            Environment.root = ClientEnvironment.root;
            Environment.address = ClientEnvironment.address;
            Environment.port = ClientEnvironment.port;
            Environment.endPoint = ClientEnvironment.endPoint;
        }

        //----< define how to process each message command >-------------
        private void initializeMessageDispatcher()
        {
            // load remoteFiles listbox with files from root
            messageDispatcher["getTopFiles"] = (CommMessage msg) =>
            {
                string relativeFile = "";
                filesListBox.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    relativeFile = System.IO.Path.GetFileName(file);
                    filesListBox.Items.Add(relativeFile);
                }
            };
            // load remoteDirs listbox with dirs from root
            messageDispatcher["getTopDirs"] = (CommMessage msg) =>
            {
                string relativeFile = "";
                directoryListBox.Items.Clear();
                foreach (string dir in msg.arguments)
                {
                    relativeFile = System.IO.Path.GetFileName(dir);
                    directoryListBox.Items.Add(relativeFile);
                }
            };
            // load remoteFiles listbox with files from folder
            messageDispatcher["moveIntoFolderFiles"] = (CommMessage msg) =>
            {
                string relativeFile = "";
                filesListBox.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    relativeFile = System.IO.Path.GetFileName(file);

                    filesListBox.Items.Add(relativeFile);
                }
            };
            // load remoteDirs listbox with dirs from folder
            messageDispatcher["moveIntoFolderDirs"] = (CommMessage msg) =>
            {
                handleMoveIntoFolderDirs(msg);
            };
            // load remoteFiles listbox with files from folder
            messageDispatcher["testStatusMessage"] = (CommMessage msg) =>
            {
                Console.WriteLine("Client received a Test Log {0} from the Test Harness!", msg.requestName);
                msg.show();
            };
        }

        private void handleMoveIntoFolderDirs(CommMessage msg)
        {
            string relativeFile = "";
            directoryListBox.Items.Clear();
            foreach (string dir in msg.arguments)
            {
                relativeFile = System.IO.Path.GetFileName(dir);

                directoryListBox.Items.Add(relativeFile);
            }
        }
        
        //----< define processing for GUI's receive thread >-------------
        private void rcvThreadProc()
        {
            Console.Write("\n Starting Client's Receive Thread");
            while (true)
            {
                CommMessage msg = comm.getMessage();
                msg.show();
                if (msg.command == null)
                    continue;

                // pass the Dispatcher's action value to the main thread for execution
                Dispatcher.Invoke(messageDispatcher[msg.command], new object[] { msg });
            }
        }

        
        //----< shut down comm when the main window closes >-------------
        private void Window_Closed(object sender, EventArgs e)
        {
            comm.close();

            // The step below should not be nessary, but I've apparently caused a closing event to 
            // hang by manually renaming packages instead of getting Visual Studio to rename them.
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        //----< show files and dirs in root path >-----------------------
        private void getTopFiles(ListBox fromListBoxName, ListBox toListBoxName)
        {
            List<string> files = fileMgr.getFiles().ToList<string>();
            toListBoxName.Items.Clear();

            string fileName = null;
            foreach (string file in files)
            {
                fileName = System.IO.Path.GetFileName(file);

                if (file.Contains("ITest"))
                {
                    // don't show it, or else it can be added twice in the list
                }
                else
                {
                    toListBoxName.Items.Add(fileName);
                }
            }
            List<string> dirs = fileMgr.getDirs().ToList<string>();

            if (fromListBoxName != null)
            { fromListBoxName.Items.Clear(); }

            foreach (string dir in dirs)
            {
                fromListBoxName.Items.Add(dir);
            }
        }

        //----< show selected file in code popup window >----------------
        private void filesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string fileName = filesListBox.SelectedValue as string;
            try
            {
                string path = System.IO.Path.Combine(ClientEnvironment.root, fileName);
                string contents = File.ReadAllText(path);
                CodePopUp popup = new CodePopUp();
                popup.codeView.Text = contents;
                popup.Show();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        //----< move to parent directory and show files and subdirs >----
        private void prevDirectory_Click(object sender, RoutedEventArgs e)
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Amrit";
            msg1.command = "getTopFiles";
            msg1.show();
            comm.postMessage(msg1);

            CommMessage msg2 = msg1.clone();
            msg2.command = "getTopDirs";
            msg2.show();
            comm.postMessage(msg2);
        }

        //----< display source in popup window (Same as FilesListBox) >-------
        private void selectedFileListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string fileName = selectedFileListBox.SelectedValue as string;
            try
            {
                string path = System.IO.Path.Combine(ClientEnvironment.root, fileName);
                string contents = File.ReadAllText(path);
                CodePopUp popup = new CodePopUp();
                popup.codeView.Text = contents;
                popup.Show();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        //----< display Build request file contents (Same as FilesListBox)>--
        private void buildRequestListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string fileName = buildRequestListBox.SelectedValue as string;
            try
            {
                string path = System.IO.Path.Combine(ClientEnvironment.root, fileName);
                string contents = File.ReadAllText(path);
                CodePopUp popup = new CodePopUp();
                popup.codeView.Text = contents;
                popup.Show();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        // Creates a Build Request according to the files selected
        private void createBR_Click(object sender, RoutedEventArgs e)
        {
            string file = createBuildRequests();

            // display in buildRequestListBox
            fileMgr.currentPath = buildRequestPath;
            fileMgr.pathStack.Push(fileMgr.currentPath);

            getTopFiles(null, buildRequestListBox);

            string relativeFileName = System.IO.Path.GetFileName(file);

            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Amrit";
            msg1.command = "bRFileReceived";
            msg1.arguments.Add(relativeFileName);
            msg1.show();
            comm.postMessage(msg1);

            Thread.Sleep(100);
            comm.postFile(relativeFileName, buildRequestPath, repoBuildRequestStorage, repositoryEndpoint);
        }

        // Removes the selected file from the selected File List
        private void removeFile_Click(object sender, RoutedEventArgs e)
        {
            string itemValue = selectedFileListBox.SelectedValue.ToString();
            int itemIndex = selectedFileListBox.SelectedIndex;

            selectedFileListBox.Items.RemoveAt(itemIndex);
        }

        // Populates selectedFileListBox 
        private void addFile_Click(object sender, RoutedEventArgs e)
        {
            string itemValue = "";

            itemValue = filesListBox.SelectedValue.ToString();
            selectedFileListBox.Items.Add(itemValue);
        }

        // creates Build requests to be sent to the MockRepo
        private string createBuildRequests()
        {
            // checks if the Directory exists at Client, if not then create
            if (!System.IO.Directory.Exists(buildRequestPath))
                System.IO.Directory.CreateDirectory(buildRequestPath);
            // Create a File Path
            buildRequestPath = System.IO.Path.GetFullPath(buildRequestPath);
            string dtTime = DateTime.Now.ToString();
            dtTime = dtTime.Replace(":", "-");
            dtTime = dtTime.Replace("/", "-");
            dtTime = dtTime.Replace(" ", "_");
            string fileName1 = "BuildRequest_";
            fileName1 = fileName1 + dtTime + ".xml";
            string fileSpec1 = System.IO.Path.Combine(buildRequestPath, fileName1);
            // List of code files
            List<string> codeFiles = new List<string>();
            string fileName = null;
            codeFiles.Add("ITest.cs");
            foreach (string file in selectedFileListBox.Items)
            {
                fileName = System.IO.Path.GetFileName(file);
                codeFiles.Add(fileName);
            }
            string testDriverName = "";
            string fileToCheck = "";
            // Find the Test Driver among the list
            foreach (string a in codeFiles)
            {
                fileToCheck = a;
                if(fileToCheck.Contains("TestDriver"))
                {
                    testDriverName = fileToCheck;
                    testDriverName = testDriverName.Substring(0, testDriverName.Length - 3);
                    break;
                }
            }
            // initialize your parameters for all your Build Requests
            TestRequest tr1 = initializeRequestParameters("ProjectBuilder", "Client", "BuildRequest", /*reqName,*/ testDriverName, codeFiles);
            tr1.makeRequest();
            // Saves an xml file on the ClientStorage
            Console.WriteLine("\n Saving Test Request to \"{0}\" \n", fileSpec1);
            tr1.saveXml(fileSpec1);
            selectedFileListBox.Items.Clear();
            return fileSpec1;
        }

        // initializes all the Request Parameters for the Build Request
        private TestRequest initializeRequestParameters(string to, string from, string type, string testDriverName, List<string> testFiles)
        {
            TestRequest tr = new TestRequest();

            // The Client passes the parameters and it gets initialized for the TestRequest class
            tr.toRequest = to;
            tr.fromRequest = from;
            tr.typeOfRequest = type;
            tr.testDriver = testDriverName;

            foreach (string codeFile in testFiles)
                tr.testedFiles.Add(codeFile);

            return tr;
        }

        //----< move to root of remote directories >---------------------
        /*
         * - sends a message to server to get files from root
         * - recv thread will create an Action<CommMessage> for the UI thread
         *   to invoke to load the remoteFiles listbox
         */
        private void rootDirectory_Click(object sender, RoutedEventArgs e)
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.command = "moveIntoFolderFiles";

            msg1.arguments.Add("");
            msg1.show();
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "moveIntoFolderDirs";
            comm.postMessage(msg2);
            msg2.show();

        }

        //----< move into remote subdir and display files and subdirs >--
        /*
         * - sends messages to server to get files and dirs from folder
         * - recv thread will create Action<CommMessage>s for the UI thread
         *   to invoke to load the remoteFiles and remoteDirs listboxs
         */
        private void showFiles_Click(object sender, RoutedEventArgs e)
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.command = "moveIntoFolderFiles";
            msg1.arguments.Add(directoryListBox.SelectedValue as string);
            msg1.show();
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "moveIntoFolderDirs";
            comm.postMessage(msg2);
            msg2.show();

            pathStack.Push(directoryListBox.SelectedValue as string);
        }

        private void buildSelected_Click(object sender, RoutedEventArgs e)
        {
            string file = buildRequestListBox.SelectedValue.ToString();
            string absfile = System.IO.Path.Combine(buildRequestPath, file);

            CommMessage msg = new CommMessage(CommMessage.MessageType.request);
            msg.from = ClientEnvironment.endPoint;
            msg.to = ServerEnvironment.endPoint;
            msg.command = "buildRequest";

            // Change the reqName without the .xml
            string reqName = file;
            string sTo = ".xml";
            int pTo = reqName.IndexOf(sTo);
            reqName = reqName.Substring(0, pTo);
            msg.requestName = reqName;
            string[] contents = System.IO.File.ReadAllLines(absfile);

            foreach (string line in contents)
            {
                msg.arguments.Add(line);
            }
            msg.show();
            comm.postMessage(msg);
        }

        private void buildAll_Click(object sender, RoutedEventArgs e)
        {
            CommMessage msg = new CommMessage(CommMessage.MessageType.request);
            msg.from = ClientEnvironment.endPoint;
            msg.to = ServerEnvironment.endPoint;
            msg.command = "buildAllRequest";
            msg.arguments.Add("");
            msg.show();
            comm.postMessage(msg);
        }

        private void QuitMsg_Click(object sender, RoutedEventArgs e)
        {
            CommMessage msg = new CommMessage(CommMessage.MessageType.request);
            msg.from = ClientEnvironment.endPoint;
            msg.to = motherBuilderEndpoint;
            msg.command = "quitMessage";
            msg.arguments.Add("");
            msg.show();
            comm.postMessage(msg);
        }

        private void ShowBR_Click(object sender, RoutedEventArgs e)
        {
            filesListBox.Items.Clear();

            // display in buildRequestListBox
            fileMgr.currentPath = buildRequestPath;
            fileMgr.pathStack.Push(fileMgr.currentPath);

            getTopFiles(null, buildRequestListBox);


        }


        private void demoProject()
        {
            Console.WriteLine(" =============================================================================================================================================");
            Console.WriteLine("\n The Project Demonstrates: \n Req 1 : prepared using C#, the .Net Framework, and Visual Studio 2017");
            Console.WriteLine("\n Req 2 : includes a Message-Passing Communication Service built with WCF. Built on Comm Prototype 3.");
            Console.WriteLine("\n Req 3 : supports accessing build requests by Pool Processes from the mother Builder process, \n sending and receiving build requests, and sending and receiving files.");
            Console.WriteLine("\n Req 4 : provides a Repository server that supports client browsing to find files to build, \n builds an XML build request string and sends that and the cited files to the Build Server.");
            Console.WriteLine("\n Req 5 : provides a Process Pool component that creates a specified number of processes on command.");
            Console.WriteLine("\n Req 6 : uses message-passing communication to access messages from the mother Builder process.");
            Console.WriteLine("\n Req 7 : attempts to build each library, cited in a retrieved build request, logging warnings and errors.");
            Console.WriteLine("\n Req 8 : If the build succeeds, sends a test request and libraries to the Test Harness for execution, \n and sends the build log to the repository.");
            Console.WriteLine("\n Req 9 : The Test Harness attempts to load each test library it receives and execute it. \n It submits the results of testing to the Repository.");
            Console.WriteLine("\n Req 10: includes a Graphical User Interface, built using WPF.");
            Console.WriteLine("\n Req 11: The GUI client is a separate process, implemented with WPF and using message-passing communication. \n It provides mechanisms to get file lists from the Repository, and select files for packaging into a test library1, \n e.g., a test element specifying driver and tested files, added to a build request structure. \n It provides the capability of repeating that process to add other test libraries to the build request structure.");
            Console.WriteLine("\n Req 12:	The client sends build request structures to the repository for storage and transmission to the Build Server.");
			Console.WriteLine("\n Req 13: The client is able to request the repository to send a build request in its storage to the Build Server for build processing.");

			Console.WriteLine("\n =============================================================================================================================================");
            Console.WriteLine("\n Folder Details:");
			Console.WriteLine(" --------------");
			Console.WriteLine(" \RemoteBuildServer\Storage\");
            Console.WriteLine(" \n    - BuilderStorage:");
            Console.WriteLine("         - Separate folders will be created according to Child Builder Endpoints \n");
			Console.WriteLine("			- Creates Build Request specific folders too");
			Console.WriteLine("			- Logs\ : saves the log files generated during the compilation process");
			Console.WriteLine("     - TestHarnessStorage:");
            Console.WriteLine("         - stores the dll library files received from the Build Server for execution");
			Console.WriteLine("			- Logs\ : saves the log files generated during the execution process\n");
            Console.WriteLine("     - MockClientStorage :  \n");
            Console.WriteLine("         - \\CodeFiles:");
            Console.WriteLine("             Contains Sample Directory Structure present at Client side to create Build Requests from");
            Console.WriteLine("             A copy of this folder is in the main RemoteBuildServer folder \n");
            Console.WriteLine("         - \\BuildRequests:");
            Console.WriteLine("             created by the GUI will be saved here \n");
			Console.WriteLine("      	- \Logs\ : ");
			Console.WriteLine("      		saves the Logs received from the Build Server and the Test Harness \n");
            Console.WriteLine("     - RepositoryStorage : \n");
            Console.WriteLine("         - \\BuildRequests:");
            Console.WriteLine("             Build Requests sent by the client will be stored here");
            Console.WriteLine("         - \\RepositoryStorage: \n");
            Console.WriteLine("             < .cs files > : Currently these are not uploaded by the Client.A copy of this folder is in the main RemoteBuildServer folder");
           	Console.WriteLine("      	- \Logs\ : ");
			Console.WriteLine("      		saves the Logs received from the Build Server and the Test Harness \n");
			Console.WriteLine(" =============================================================================================================================================");

        }


    }
}
