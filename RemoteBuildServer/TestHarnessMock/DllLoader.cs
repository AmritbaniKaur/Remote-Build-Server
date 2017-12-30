//////////////////////////////////////////////////////////////////////////////
// DllLoader.cs     - Demonstrate Robust loading and dynamic invocation of  //
//                  Dynamic Link Libraries found in specified location      //
//                  - tests now return bool for pass or fail                //
//                  ver 4.0	                                                //
//--------------------------------------------------------------------------//
//  Source:         Prof. Jim Fawcett, CST 4-187, jfawcett@twcny.rr.com     //
//	Author:			Amritbani Sondhi,										//
//					Graduate Student, Syracuse University					//
//					asondhi@syr.edu											//
//	Application:	CSE 681 Project #2, Fall 2017							//
//	Platform:		HP Envy x360, Core i7, Windows 10 Home					//
//  Environment:    C#, Visual Studio 2017 RC                               //
//////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * The Dll Loader helps the TestHarnessMock to Load the dll files and execute them
 * It also helps the TestHarness to Log the results in the TestHarnessStorage
 * 
 * Hardcoded the TestersDirectory for simplifying as it is a Server
 * 
 * Public Methods:
 * ==============
 *      Class DllLoaderExec -
 *      - DllLoaderExec()               : checks if Directories are present or not
 *      - createLogFile()               : creates Test Log File
 *      - LoadFromComponentLibFolder()  : library binding error event handler
 *      - loadAndExerciseTesters()      : load assemblies from testersLocation and run their tests
 *      - runSimulatedTest()            : run tester t from assembly asm 
 *      
 * Build Process:
 * ==============
 *	- Required Files:
 *          DllLoader.cs
 * 	- Build commands:
 *		    devenv RemoteBuildServer.sln
 *		    
 * Maintenance History:
 * ===================
 *      ver 4.0 : Dec 06, 2017
 *      ver 3.0 : Oct 05, 2017
 * 
 */


using System;
using System.Reflection;
using System.IO;

namespace DllLoaderDemo
{
    public class DllLoaderExec
    {
        public static string testersLocation { get; set; } = "../../../Storage/TestHarnessStorage";
        public static string logLocation { get; set; } = "../../../Storage/TestHarnessStorage/TestLogs/";
        public static string logFile { get; set; } = "TestLog_";
        private static string actStatus { get; set; } = "";
        // checks if Directories are present or not
        public DllLoaderExec()
        {
            if (!Directory.Exists(testersLocation))
                Directory.CreateDirectory(testersLocation);
            if (!Directory.Exists(logLocation))
                Directory.CreateDirectory(logLocation);
        }

        // creates Test Log File
        public StreamWriter createLogFile(string absDllName)
        {
            // Create a LogFile Path
            logLocation = System.IO.Path.GetFullPath(logLocation);

            string logFileName = Path.GetFileName(absDllName);

            logFileName = logFileName.Replace("BuildRequest_", "TestLog_");
            logFileName = logFileName.Replace(".dll", "");
            logFileName = logFileName.Replace(":", "-");
            logFileName = logFileName.Replace("/", "-");

            logFileName = logFileName + ".log";

            logFile = System.IO.Path.Combine(logLocation, logFileName);

            // Connect logFile with your StreamWriter
            StreamWriter _LogBuilder = new StreamWriter(logFile, append: true);
            return _LogBuilder;
        }

        /*----< library binding error event handler >------------------*/
        /*
         *  This function is an event handler for binding errors when
         *  loading libraries.  These occur when a loaded library has
         *  dependent libraries that are not located in the directory
         *  where the Executable is running.
         */
        static Assembly LoadFromComponentLibFolder(object sender, ResolveEventArgs args)
        {
            Console.WriteLine("\n  called binding error event handler");

            string folderPath = testersLocation;
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }

        //----< load assemblies from testersLocation and run their tests >-----
        public string loadAndExerciseTesters(string absDllName)
        {
            string file = absDllName;
            // Start Logging here
            StreamWriter _LogBuilder = createLogFile(absDllName);

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromComponentLibFolder);

            try
            {
                DllLoaderExec loader = new DllLoaderExec();

                Assembly asm = Assembly.LoadFile(file);
                string fileName = Path.GetFileName(file);

                Console.WriteLine("\n -----------------------------------------------------------------------------------------");
                Console.WriteLine(" Loaded {0}", fileName);

                _LogBuilder.WriteLine("\n -----------------------------------------------------------------------------------------");
                _LogBuilder.WriteLine(" Loaded {0}", fileName);

                // exercise each tester found in assembly
                Type[] types = asm.GetTypes();
                foreach (Type t in types)
                {
                    // if type supports ITest interface then run test
                    if (t.GetInterface("DemoApp.ITest", true) != null)
                        if (!loader.runSimulatedTest(t, asm, _LogBuilder))
                        {
                            Console.WriteLine(" Test {0} failed to run", t.ToString());
                            _LogBuilder.WriteLine(" Test {0} failed to run", t.ToString());
                        }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            // Flush and Close the StringWriter
            _LogBuilder.Flush();
            _LogBuilder.Close();

            return "Simulated Testing completed";
        }

        //----< run tester t from assembly asm >-------------------------------
        bool runSimulatedTest(Type t, Assembly asm, StreamWriter _LogBuilder)
        {
            try
            {
                Console.WriteLine(" Attempting to create instance of {0}", t.ToString());
                _LogBuilder.WriteLine(" Attempting to create instance of {0}", t.ToString());

                object obj = asm.CreateInstance(t.ToString());

                // run test
                bool status = false;
                MethodInfo method = t.GetMethod("test");
                if (method != null)
                    status = (bool)method.Invoke(obj, new object[0]);

                Func<bool, string> act = (bool pass) =>
                {
                    if (pass)
                        return "passed";
                    return "failed";
                };
                Console.WriteLine(" Test {0}", act(status));
                _LogBuilder.WriteLine(" Test {0}", act(status));
                actStatus = act(status);
            }
            catch (Exception ex)
            {
                Console.WriteLine(" Test failed with message \"{0}\"", ex.Message);
                _LogBuilder.WriteLine(" Test failed with message \"{0}\"", ex.Message);
                return false;
            }
            return true;
        }
    }
}
