using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Logging.BuildDb;
using Microsoft.Build.Logging.StructuredLogger;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LogProcessor
{
    [Command("createdb", Description = "Replay a binary log and create a Build Database from it.")]
    public class CreateDbCommand
    {
        [Option("-o|--output <OUTPUTFILE>", Description = "The output file to write, defaults to the same file as the input but with the extension '.builddb'.")]
        public string OutputFile { get; set; }

        [Required]
        [FileExists]
        [Argument(0, "<FILE>", "The file to load.")]
        public string File { get; set; }

        public async Task<int> OnExecuteAsync(IConsole console)
        {
            if(string.IsNullOrEmpty(OutputFile))
            {
                OutputFile = Path.ChangeExtension(File, ".builddb");
            }

            var eventSource = new BinLogReader();

            var error = false;
            eventSource.OnBlobRead += (kind, bytes) =>
            {
                console.Error.WriteLine($"WARNING: Blob of kind {kind} not yet supported.");
            };
            eventSource.OnException += ex =>
            {
                error = true;
                console.Error.WriteLine("ERROR: An exception occurred while reading from the binary log.");
                console.Error.WriteLine(ex.ToString());
            };

            var dbLogger = new BuildDbLogger
            {
                Parameters = OutputFile
            };
            dbLogger.Initialize(eventSource);

            console.WriteLine("Replaying binary log...");

            var sw = Stopwatch.StartNew();
            await eventSource.ReplayAsync(File);
            var elapsed = sw.Elapsed;

            dbLogger.Shutdown();

            console.WriteLine($"Binary log replayed in {elapsed.TotalSeconds:0.00} seconds.");
            console.WriteLine($"Build database saved to {OutputFile}.");

            return error ? 1 : 0;
        }
    }
}
