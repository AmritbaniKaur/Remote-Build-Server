using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoApp
{
    public class TestDriver3 : ITest
    {
        bool testMultiply()
        {
            bool status=false;
            //try
            //{
                Demo1App3 multiplyObj = new Demo1App3();
                int a=4, b=5, result=0;
                result = multiplyObj.multiplyInputs(a,b);
                Console.WriteLine(" multiplyInputs() in Demo1App3 returns: {0}\tMultiplication of: {1} and {2}", result, a, b);
                status = true;
            //}
            //catch (Exception ex)
            //{
                //Console.Write("\n\n  An error occured while trying to build Demo1App3 \n Details: {0}\n\n", ex.Message);
                //Console.WriteLine("==========================================================================================\n");
                //status = false;
            //}
            return status;
        }

        bool testDivide()
        {
            bool status = false;
            //try
            //{
                Demo2App3 divideObj = new Demo2App3();
                int a = 10, b = 0, result = 0;
                result = divideObj.divideInputs(a, b);
                Console.WriteLine(" divideInputs() in Demo2App3 returns: {0}\tDivision of: {1} from {2}", result, b, a);
                status = true;
            //}
            //catch (Exception ex)
            //{
            //    Console.Write("\n\n  An error occured while trying to build Demo2App3 \n Details: {0}\n\n", ex.Message);
            //    Console.WriteLine("==========================================================================================\n");
            //    status = false;
            //}
            return status;
        }

        public bool test()
        {
            Console.WriteLine("\n ==========================================================================");
            Console.WriteLine(" DemoApp3 for TestDriver3 is being executed! \n");

            bool result1 = testMultiply();
            bool result2 = testDivide();

            Console.WriteLine(" Demonstrating: the Test3 libraries  will have an exception");
            Console.WriteLine(" Built through .cs files for 'Load/Execute Fail' case, TestLibrary3.dll was generated but cannot be executed");
            Console.WriteLine(" ==========================================================================\n");

            return result1 && result2;
        }
    }
}
