using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Runtime.InteropServices;
using Mot.Common.Interface.Lib;
using Mot.Listener.Interface.Lib;
using System.Net.Sockets;

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
            var _path = GetPlatformOs.Current == PlatformOs.Windows ? $@"\motNext\io" : "~/Project/Tests/ListenerTests/io";

            try
            {
                using (var hl7Listener = new Hl7SocketListener(24045, callback))
                {
                    hl7Listener.Go();
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }

            try
            {
                using (var fsListener = new FilesystemListener(_path, callback))
                {
                    fsListener.Go();
                }

                using (var fsListener = new FilesystemListener(_path, callback, true))
                {
                    fsListener.Go();
                }
            }
            catch (Exception ex)
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
                using (var hl7Listener = new Hl7SocketListener(24045, null))
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

        public string Parse(string val)
        {
            return "Ok";
        }

        [TestMethod]
        public void ConfirmReturnValues()
        {
            try
            {
                var locId = Guid.NewGuid().ToString();

                using (var testSession = new TcpClient("proxyplayground.medicineontime.com", 24042))
                {
                    var loc = new MotFacilityRecord("Add")
                    {
                        LocationID = locId,
                        LocationName = "The Banxzai Institute",
                        Address1 = "1 Banzai Blvd",
                        City = "Fort Lee",
                    };

                    using (var stream = testSession.GetStream())
                    {
                        loc.Write(stream);

                        loc = new MotFacilityRecord("Change")
                        {
                            LocationID = locId,
                            LocationName = "The Banxzai Institute",
                            Address1 = "1010101 Banzai Blvd",
                            City = "Fort Lee",
                        };

                        loc.Write(stream);

                        loc = new MotFacilityRecord("Delete")
                        {
                            LocationID = locId
                        };

                        loc.Write(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
    }
}
