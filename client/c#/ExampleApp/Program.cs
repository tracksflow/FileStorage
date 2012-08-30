using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FSLib;

namespace ExampleApp
{
    class Program
    {
        const string BinaryFileName = "test.png";
        const string TextFileName = "test.txt";

        static void Main()
        {
            CommonTest();
            BigFileTest(100);
            ParallelLoadTest(100);
        }

        static void BigFileTest(long sizeMb)
        {
            var testFile = "test.dat";
            var time = DateTime.Now;
            Console.WriteLine("Creating test file...");
            Random rnd = new Random();
            string id = rnd.Next(int.MaxValue).ToString();
            using (var sw = new FileStream(testFile, FileMode.Create))
            {
                var buf = new byte[1048576];
                for (long i = 0; i < sizeMb; i++)
                {
                    rnd.NextBytes(buf);
                    sw.Write(buf, 0, buf.Length);
                }
            }
            Console.WriteLine(string.Format("Done in {0} seconds", (DateTime.Now - time).TotalSeconds));

            time = DateTime.Now;
            Console.WriteLine("Sending file to storage...");
            var client = new FSClient(512 * 1024, 10, Properties.Settings.Default.StorageUrl, 1);
            using (var file = new FileStream(testFile, FileMode.Open))
            {
                var sendResult = client.SendFile(file, testFile, id, "test");
                if (sendResult.Status != ActionStatus.Ok)
                {
                    Console.WriteLine("Test result: failed while sending file to storage!");
                    return;
                }
            }
            Console.WriteLine(string.Format("Done in {0} seconds", (DateTime.Now - time).TotalSeconds));

            string localChecksum;
            string remoteChecksum;
            time = DateTime.Now;
            Console.WriteLine("Calculating local MD5...");
            using (var file = new FileStream(testFile, FileMode.Open))
            {
                localChecksum = FSClient.FileMD5Local(file);
            }
            Console.WriteLine(string.Format("Done in {0} seconds", (DateTime.Now - time).TotalSeconds));

            time = DateTime.Now;
            Console.WriteLine("Calculating remote MD5...");
            var checksumResult = client.FileMD5(testFile, id, "test");
            if (checksumResult.Status != ActionStatus.Ok)
            {
                 Console.WriteLine("Test result: failed while calculating file checksum!");
                 return;
            }
            remoteChecksum = checksumResult.Value;
            Console.WriteLine(string.Format("Done in {0} seconds", (DateTime.Now - time).TotalSeconds));

            client.DelFile(testFile, id, "test");
            Console.WriteLine(string.Format("Test result: {0}", localChecksum == remoteChecksum));

        }

        static void ParallelLoadTest(int threadsNum)
        {
            Random rnd = new Random();
            bool success = true;
            var mutex = new object();
            var time = DateTime.Now;
            string localChecksum = FSClient.FileMD5Local(new FileStream(Path.Combine("files", BinaryFileName), FileMode.Open, FileAccess.Read, FileShare.Read));
            var result = Parallel.For(0, threadsNum, (i, state) =>
                                            {
                                                var client = new FSClient(512 * 1024, 10,
                                                                          Properties.Settings.Default.
                                                                              StorageUrl, threadsNum);
                                                string id = rnd.Next().ToString();
                                                using (
                                                    var file =
                                                        new FileStream(Path.Combine("files", BinaryFileName),
                                                                       FileMode.Open, FileAccess.Read, FileShare.Read))
                                                {
                                                    var response = client.SendFile(file, BinaryFileName, id,
                                                                                   "test");
                                                    var remoteChecksum = client.FileMD5(BinaryFileName, id, "test").Value;
                                                    if (response.Status != ActionStatus.Ok || localChecksum != remoteChecksum)
                                                        lock (mutex)
                                                        {
                                                            success = false;
                                                        }
                                                    
                                                    client.DelFile(BinaryFileName, id, "test");
                                                }
                                            });
            while (!result.IsCompleted)
            {
                Thread.Sleep(100);
            }
            Console.WriteLine(string.Format("Success: {0}; time: {1}", success, (DateTime.Now - time).TotalSeconds));
        }

        static void CommonTest()
        {
            var client = new FSClient(512 * 1024, 1, Properties.Settings.Default.StorageUrl, 1);

            SimpleActionResult response;
            Console.WriteLine("Sending file to a server...");
            using(var file = new FileStream(Path.Combine("files", BinaryFileName), FileMode.Open))
            {
                response = client.SendFile(file, BinaryFileName, "1", "test");
            }
            Console.WriteLine(response.Status);

            Console.WriteLine("Checking file existance on a server...");
            response = client.FileExists(BinaryFileName, "1", "test");
            Console.WriteLine(response.Status);

            Console.WriteLine("Checking file MD5 sum on a server...");
            response = client.FileMD5(BinaryFileName, "1", "test");
            using(var file = new FileStream(Path.Combine("files", BinaryFileName), FileMode.Open))
            {
                Console.WriteLine(((StringActionResult)response).Value == FSClient.FileMD5Local(file));
            }

            Console.WriteLine("Checking file length on a server...");
            response = client.FileLength(BinaryFileName, "1", "test");
            using (var file = new FileStream(Path.Combine("files", BinaryFileName), FileMode.Open))
            {
                Console.WriteLine(((LongActionResult)response).Value == file.Length);
            }

            Console.WriteLine("Checking get file from a server...");
            using (var file = new FileStream(Path.Combine("files", TextFileName), FileMode.Open))
            {
                client.SendFile(file, TextFileName, "1", "test");
            }
            response = client.GetFile(TextFileName, "1", "test");
            using (var sr = new StreamReader(((StreamActionResult)response).Value))
            {
                Console.WriteLine(string.Format("File data is: {0}", sr.ReadToEnd()));
            }

            Console.WriteLine("Deleting file from a server...");
            response = client.DelFile(BinaryFileName, "1", "test");
            Console.WriteLine(response.Status);
        }
    }
}
