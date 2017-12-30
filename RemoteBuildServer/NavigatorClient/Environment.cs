///////////////////////////////////////////////////////////////////////////
// Environment.cs - defines environment properties for Client and Server //
// ver 1.0                                                               //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017       //
///////////////////////////////////////////////////////////////////////////
/*
 * //////////////////////////
 * changed
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 23 Oct 2017
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavigatorClient
{
    public struct Environment
    {
        public static string root { get; set; }
        public static long blockSize { get; } = 1024;
    public static string endPoint { get; set; }
    public static string address { get; set; }
    public static int port { get; set; }
    public static bool verbose { get; set; }
  }

  public struct ClientEnvironment
  {
    public static string root { get; set; } = "../../../Storage/MockClientStorage/";
    public static long blockSize  { get; }= 1024;
    public static string endPoint { get; set; } = "http://localhost:8060/IPluggableComm";
    public static string address { get; set; } = "http://localhost";
    public static int port { get; set; } = 8060;
    public static bool verbose { get; set; } = false;
  }

  public struct ServerEnvironment
  {
    public static string root { get; set; } = "../../../Storage/RepositoryStorage/";
    public static long blockSize  { get; } = 1024;
    public static string endPoint { get; set; } = "http://localhost:8070/IPluggableComm";
    public static string address { get; set; } = "http://localhost";
    public static int port { get; set; } = 8070;
    public static bool verbose { get; set; } = false;
  }
}
