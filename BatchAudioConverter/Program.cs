using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TagLib;

namespace BatchAudioConverter
{
    class Program
    {
        static private string PathIn = "";
        static private string PathOut = "";
        static private bool DeleteOrigin = false;
        static private int BitrateOutput = 192;
        static private readonly List<int> Bitrates = new List<int>() { 96, 128, 192, 256, 320 };
        public static readonly List<string> AcceptedExtentions = new List<string>() { ".aiff", ".mp3", ".wma", ".aac", ".flac", ".ogg", ".m4a" };

        public static Int32 TimeStamp() { return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; }

        static void Main(string[] args)
        {
            Console.WriteLine("============================================================");
            Console.WriteLine("================== Batch Audio Converter ===================");
            Console.WriteLine("============================================================");
            string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
            string exePath = BaseDir + "ffmpeg.exe";
            if (!System.IO.File.Exists(exePath)) { Console.WriteLine("ERROR: Ffmpeg executable not in app dir"); return; }

            if (args.Length == 0)
            {
                string path = "";
                while (path == "")
                {
                    Console.WriteLine("Folder Input:");
                    path = Console.ReadLine().Trim().TrimEnd(Path.DirectorySeparatorChar);
                    if (!Directory.Exists(path)) { path = ""; Console.WriteLine(" == ERROR INVALID FOLDER"); }
                }
                PathIn = path;
                Console.WriteLine(" ===");
                Console.WriteLine("Folder Output: (default = <Folder Input>" + Path.DirectorySeparatorChar + "TMP )");
                PathOut = Console.ReadLine().Trim().TrimEnd(Path.DirectorySeparatorChar);

                Console.WriteLine(" ===");
                string consent = "";
                while (consent == "")
                {
                    Console.WriteLine("Delete Original: (default = No)");
                    consent = Console.ReadLine().Trim().ToLower();
                    if (consent != "yes" && consent != "no" && consent != "") { consent = ""; Console.WriteLine(" == ERROR INVALID VALUE"); }
                    else { if (consent == "yes") { DeleteOrigin = true; } else { DeleteOrigin = false; }; break; }
                }

                Console.WriteLine(" ===");
                int quality = 0;
                string tmp = "";
                while (quality == 0)
                {
                    Console.WriteLine("Output Bitrate: (default = " + BitrateOutput + " in " + JsonConvert.SerializeObject(Bitrates) + ")");
                    tmp = Console.ReadLine().Trim();
                    if (tmp == "") { break; }
                    else
                    {
                        quality = Convert.ToInt32(tmp);
                        if (!Bitrates.Contains(quality)) { quality = 0; Console.WriteLine(" == ERROR INVALID VALUE"); }
                        else { BitrateOutput = quality; }
                    }
                }
            }
            else
            {
                foreach (string arg in args)
                {
                    if (arg.StartsWith("--in="))
                    {
                        PathIn = arg.Replace("--in=", "").Replace("\"", "").TrimEnd(Path.DirectorySeparatorChar);
                    }
                    if (arg.StartsWith("--out=")) { PathOut = arg.Replace("--out=", "").Replace("\"", "").TrimEnd(Path.DirectorySeparatorChar); }
                    if (arg == "--replace") { DeleteOrigin = true; }
                    if (arg.StartsWith("--bitrate="))
                    {
                        string tmp = arg.Replace("--bitrate=", "").Trim();
                        try { int Bitrate = Convert.ToInt32(tmp); if (Bitrates.Contains(Bitrate)) { BitrateOutput = Bitrate; } } catch { }
                    }
                }
            }

            if (PathIn == "") { Console.WriteLine("ERROR: No Input Path assigned"); return; }
            if (PathOut == "") { PathOut = PathIn + Path.DirectorySeparatorChar + "TMP"; }
            if (!Directory.Exists(PathOut)) { Directory.CreateDirectory(PathOut); }

            string[] files = Directory.GetFiles(PathIn, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (AcceptedExtentions.Contains(ext))
                {
                    TagLib.File Tags = TagLib.File.Create(file, ReadStyle.Average);
                    Tag OriginTags = new TagLib.Id3v2.Tag();
                    Tags.Tag.CopyTo(OriginTags, true);
                    Tags.Dispose();
                    Tags = null;

                    Task.Delay(200);
                    Console.WriteLine("-----------------------------------------");
                    Console.WriteLine(" => Conversion file: " + file);
                    string EndFile = "";
                    if (DeleteOrigin == true) { EndFile = file.Replace(ext, ".mp3"); }
                    else { EndFile = PathOut + Path.DirectorySeparatorChar + file.Replace(PathIn + Path.DirectorySeparatorChar, "").Replace(ext, ".mp3"); }
                    string EndDir = new FileInfo(EndFile).DirectoryName;
                    if (!Directory.Exists(EndDir)) { Directory.CreateDirectory(EndDir); }

                    Console.WriteLine(" => To file: " + EndFile);

                    Console.WriteLine(" => Processing ...");
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.CreateNoWindow = true;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                    startInfo.UseShellExecute = false;
                    startInfo.FileName = exePath;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.Arguments = "-i \"" + file + "\" -acodec mp3 -b:a " + BitrateOutput + "k \"" + EndFile + "\"";
                    int start = TimeStamp();
                    bool aborted = false;

                    Debug.WriteLine(startInfo.FileName + " " + startInfo.Arguments);
                    try
                    {
                        if (System.IO.File.Exists(EndFile)) { System.IO.File.Delete(EndFile); }
                        using (Process exeProcess = Process.Start(startInfo))
                        {
                            while (!exeProcess.HasExited)
                            {
                                Console.WriteLine(exeProcess.StandardError.ReadToEnd());
                                if (TimeStamp() - start > 30) { Console.WriteLine(" => ERROR : File Convertion Timeout"); aborted = true; break; }
                            }
                        }

                        if (aborted == false)
                        {
                            Console.WriteLine(" => Copy Tags");
                            try
                            {
                                Tags = TagLib.File.Create(EndFile, ReadStyle.Average);
                                OriginTags.CopyTo(Tags.Tag, true);
                                Tags.Save();
                                Tags.Dispose();
                            }
                            catch (Exception err)
                            {
                                Console.WriteLine(" => ERROR : " + JsonConvert.SerializeObject(err));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(" => ERROR : " + JsonConvert.SerializeObject(e));
                    }

                    OriginTags.Clear();
                    OriginTags = null;
                    if (DeleteOrigin == true) { System.IO.File.Delete(file); }
                }
            }



            Console.WriteLine(" ===>> THE END <<=== ");
        }
    }
}
