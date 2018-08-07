using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Runtime.InteropServices;
using MotCommonLib;
using MotListenerLib;


namespace ListenerTests
{
    [TestClass]
    public class ListenerTests
    {      

        public string callback(string data)
        {
            Console.WriteLine(data);
            return "Good to go";
        }

        [TestMethod]
        public void Construct()
        {
            var _path = GetPlatformOs.Go() == PlatformOs.Windows ? $@"\motNext\io" : "~/Project/Tests/ListenerTests/io";

            try
            {
                using (var hl7Listener = new Hl7SocketListener(24045, callback))
                {
                    hl7Listener.Go();
                }
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);    
            }

            try
            {
                using(var fsListener = new FilesystemListener(_path, callback))
                {
                    fsListener.Go();
                }

                using (var fsListener = new FilesystemListener(_path, callback, true))
                {
                    fsListener.Go();
                }
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }

            // Force failures
            try
            {
                using (var hl7Listener = new Hl7SocketListener(0, callback))
                {
                    hl7Listener.Go();
                }

                Assert.Fail("Did not trap illegal socket address");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Illegal aocket address {ex.Message}");   
            }

            try
            {
                using (var hl7Listener = new Hl7SocketListener(24054, null))
                {
                    hl7Listener.Go();
                }

                Assert.Fail("Did not trap null callback");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Null callback {ex.Message}");
            }


            try
            {
                using (var fsListener = new FilesystemListener(null, callback))
                {
                    fsListener.Go();
                }

                Assert.Fail("Allowed null path parameter");
            }
            catch
            {
                Console.WriteLine("Trapped null path parameter");
            }

            try
            {
                using (var fsListener = new FilesystemListener(_path, null))
                {
                    fsListener.Go();
                }

                Assert.Fail("Allowed null callback parameter");
            }
            catch
            {
                Console.WriteLine("Trapped null callback parameter");
            }
        }
    }
}
