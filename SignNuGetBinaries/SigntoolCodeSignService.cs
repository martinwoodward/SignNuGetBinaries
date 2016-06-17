using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SignNuGetBinaries
{
    class SigntoolCodeSignService : ICodeSignService
    {
        private readonly string _completionPath;
        private readonly string _timeStampURL;
        private readonly string _description;
        private readonly string _descriptionURL;

        // TODO: Calculate the location of Signtool from registry + platform? (HKLM\SOFTWARE\Microsoft\Windows Kits\Installed Roots)
        private readonly string _signtoolPath = @"C:\Program Files (x86)\Windows Kits\8.1\bin\x64\signtool.exe";
                

        public SigntoolCodeSignService(string completionPath, string timestampURL, string description, string descriptionURL)
        {
            _completionPath = completionPath;
            _timeStampURL = timestampURL;
            _description = description;
            _descriptionURL = descriptionURL;

            Directory.CreateDirectory(completionPath);
        }

        public Task<string> Submit(string name, string[] files)
        {
            Console.WriteLine("Signing job {0} with {1} files", name, files.Length);

            var outPath = Path.Combine(_completionPath, Guid.NewGuid().ToString());
            Directory.CreateDirectory(outPath);

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var outFile = Path.Combine(outPath, fileName);

                // Copy File
                File.Copy(file, outFile);

                // Sign it with sha1
                Process signtool = new Process();
                signtool.StartInfo.WorkingDirectory = outPath;
                signtool.StartInfo.FileName = _signtoolPath;
                signtool.StartInfo.UseShellExecute = false;
                signtool.StartInfo.RedirectStandardError = false;
                signtool.StartInfo.RedirectStandardOutput = false;
                signtool.StartInfo.Arguments = String.Format(@"sign /t {0} /sm /d ""{1}"" /du {2} /a ""{3}""", _timeStampURL, _description, _descriptionURL, outFile);
                Console.WriteLine(@"""{0}"" {1}", signtool.StartInfo.FileName, signtool.StartInfo.Arguments);
                signtool.Start();
                signtool.WaitForExit();
                if (signtool.ExitCode != 0)
                {
                    Console.Error.WriteLine("Error: Signtool returned {0}", signtool.ExitCode);
                }
                signtool.Close();

                // Append a sha256 signature
                signtool = new Process();
                signtool.StartInfo.WorkingDirectory = outPath;
                signtool.StartInfo.FileName = _signtoolPath;
                signtool.StartInfo.UseShellExecute = false;
                signtool.StartInfo.RedirectStandardError = false;
                signtool.StartInfo.RedirectStandardOutput = false;
                signtool.StartInfo.Arguments = String.Format(@"sign /tr {0} /sm /as /fd sha256 /td sha256 /d ""{1}"" /du {2} /a ""{3}""", _timeStampURL, _description, _descriptionURL, outFile);
                Console.WriteLine(@"""{0}"" {1}", signtool.StartInfo.FileName, signtool.StartInfo.Arguments);
                signtool.Start();
                signtool.WaitForExit();
                if (signtool.ExitCode != 0)
                {
                    Console.Error.WriteLine("Error: Signtool returned {0}", signtool.ExitCode);
                }
                signtool.Close();
            }

            return Task.FromResult(outPath);
        }
    }
}
