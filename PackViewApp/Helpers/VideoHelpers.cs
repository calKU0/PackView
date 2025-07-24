using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PackViewApp.Helpers
{
    public static class VideoHelpers
    {
        private static readonly List<Process> ffmpegProcesses = new();

        public static async Task<string> MergeFiles(List<string> tempFiles)
        {
            // Create temporary text file with list of files for ffmpeg concat
            string concatListPath = Path.Combine(Path.GetTempPath(), $"concat_list_{Guid.NewGuid():N}.txt");
            await File.WriteAllLinesAsync(concatListPath, tempFiles.Select(f => $"file '{f.Replace("\\", "/")}'"));

            string outputFilePath = Path.Combine(Path.GetTempPath(), $"merged_{Guid.NewGuid():N}.mp4");

            // FFmpeg merge command
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-f concat -safe 0 -i \"{concatListPath}\" -c copy \"{outputFilePath}\" -y",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var process = Process.Start(startInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start FFmpeg process.");

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"FFmpeg failed: {error}");
            }

            // Clean up temporary segment files
            foreach (var file in tempFiles)
                File.Delete(file);

            File.Delete(concatListPath);

            return outputFilePath;
        }

        public static void CopyFile(string source, string product)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Zapisz nagranie jako",
                Filter = "Pliki MP4 (*.mp4)|*.mp4",
                FileName = $"pakowanie_{product}_{DateTime.Now:yyyyMMdd_HHmm}.mp4"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.Move(source, dialog.FileName, overwrite: true);
                    MessageBox.Show("Plik został zapisany.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas zapisu pliku: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}