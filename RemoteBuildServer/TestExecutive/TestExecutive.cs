//////////////////////////////////////////////////////////////////////////////////////
// MotherBuilder.cs -    creates Process Pool, and handles requests using queues    //
//                       ver 2.0                                                    //
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
 *      Demonstrates Testing of Remote Build Server Federation
 *      
 * Build Process:
 * ==============
 *	- Required Files:
 *          RepoMock.cs, Motherbuilder.cs
 * 	- Build commands:
 *		    devenv RemoteBuildServer.sln
 *		    
 * Maintenance History:
 * ===================
 *      ver 2.0 : Dec 06, 2017
 *      ver 1.0 : Nov 01, 2017
 *          - first release
 */
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Federation;
using System.Threading;

namespace TestExecutive
{
    class TestExecutive
    {
        static void Main(string[] args)
        {
            Console.Title = "Test Executive";
            Console.WriteLine("====================================================================================");
            Console.WriteLine("\n Testing Federation");

            int numOfProc = Convert.ToInt32(args[0]);

            MotherBuilder builderObj = new MotherBuilder("http://localhost", 8080, numOfProc);
            Thread.Sleep(100000000);

            RepoMock repo = new RepoMock("http://localhost", 8070);

            Console.WriteLine("====================================================================================");
        }
    }
}
