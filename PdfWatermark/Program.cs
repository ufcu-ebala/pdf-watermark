using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.IO.Source;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Kernel.Pdf.Xobject;
using iText.Layout;
using iText.Layout.Element;
using Path = System.IO.Path;

namespace PdfWatermark
{
    class Program
    {
        private static LogLevel _level = LogLevel.Info;
        private static bool _continue = true;
        private static bool _error;

        /// <summary>
        /// main entry point for watermarking PDFs
        /// </summary>
        /// <param name="args">args[0] - Log Level: Defaults to LogLevel.Info</param>
        static void Main(string[] args)
        {
            //using var list = File.CreateText(@"E:\unique_ids.csv");
            //string Generate()
            //{
            //    var random = new Random();
            //    return random.Next(1, 100) < 90
            //        ? Guid.NewGuid().ToString("N")
            //        : string.Empty;
            //}
            //foreach (var file in Directory.GetFiles(@"E:\Loan Securitization\"))
            //    list.WriteLine($"{new FileInfo(file).Name.Replace(".TIF", "")},{Generate()},{Generate()},{Generate()}");
            //return;
            //var source = @"E:\19060_300.TIF";
            //var destination = @"E:\results.pdf";
            //if (File.Exists(destination))
            //{
            //    Log("Deleting Destination File");
            //    File.Delete(destination);
            //}
            //Log($"[TEST RUN] Converting: {source} to {destination}");
            //TiffToPdf(source, destination);
            //Log("Press Any Key to continue...");
            //Console.ReadLine();
            //return;

            if (args?.Length > 0)
                _level = Enum.Parse<LogLevel>(args[0]);
            if (args?.Length > 1)
                _continue = bool.Parse(args[1]);

            // Setup Directory Content
            string directory, pdfs, original, archive, failures, csv;
            directory = pdfs = original = archive = failures = csv = string.Empty;
            var content = Array.Empty<string>();
            try
            {
                if (args?.Length > 2)
                    directory = args[2];
                if (args?.Length > 3)
                    pdfs = args[3];
                if (args?.Length > 4)
                    csv = args[4];

                Console.Write("Working Directory: ");
                if (string.IsNullOrEmpty(directory))
                    directory = Console.ReadLine() ?? Directory.GetCurrentDirectory();
                else
                    Console.Write($"{directory}\n");
                Console.Write("PDFs Locations: ");
                if (string.IsNullOrEmpty(pdfs))
                    pdfs = Console.ReadLine() ?? throw new ArgumentException("PDF Directory cannot be null!");
                else
                    Console.Write($"{pdfs}\n");
                original = Path.Join(directory, "Originals");
                archive = Path.Join(directory, "Archive");
                failures = Path.Join(directory, "Failures");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                if (!Directory.Exists(original))
                    Directory.CreateDirectory(original);
                if (!Directory.Exists(archive))
                    Directory.CreateDirectory(archive);
                if (!Directory.Exists(failures))
                    Directory.CreateDirectory(failures);
                Console.Write("CSV File (full filepath): ");
                if (string.IsNullOrEmpty(csv))
                    csv = Console.ReadLine() ?? throw new ArgumentException("CSV File cannot be null");
                else
                    Console.Write($"{csv}\n");
                content = File.ReadAllLines(csv);
            }
            catch (Exception e)
            {
                Log("Unable to setup Directory", LogLevel.Error);
                Log(e.ToString(), LogLevel.Warning);
                Console.WriteLine("Press Any Key To Continue...");
                Console.ReadLine();
            }

            // check original files, error'ing on files missing
            Log("Checking Original Files...");
            using (var progress = new ProgressBar())
            {
                for (var i = 0; i < content.Length; i++)
                {
                    if (File.Exists(Path.Join(pdfs, $"{content[i].Split(',')[0]}.tif"))) continue;
                    Log($"File Missing! {content[i].Split(',')[0]}.tif");
                    var temp = content.ToList();
                    temp.RemoveAt(i);
                    i--;
                    content = temp.ToArray();
                    _error = true;
                    progress.Report((double) i / content.Length);
                }

                progress.Report(1);
            }

            if (_error && !Prompt()) return;

            // now actually move the files to the original location
            Log("Moving files to eOriginal Location");
            using (var progress = new ProgressBar())
            {
                for (var i = 0; i < content.Length; i++)
                {
                    var row = content[i].Split(',');
                    if (!File.Exists(Path.Join(pdfs, $"{row[0]}.tif")))
                    {
                        Log($"Skipping File: {row[0]}.tif", LogLevel.Warning);
                        progress.Report((double) i / content.Length);
                        continue;
                    }
                    // go ahead and copy the file to the original location for storage
                    System.Diagnostics.Debug.WriteLine($"Copying File: {row[0]}.tif");
                    try
                    {
                        TiffToPdf(Path.Join(pdfs, $"{row[0]}.tif"), Path.Join(original, $"{row[0]}.pdf"));
                    }
                    catch (Exception e)
                    {
                        Log($"Error attempting to copy {row[0]}.tif\n{e}", LogLevel.Error);
                        if (!Prompt()) return;
                        _error = true;
                    }

                    progress.Report((double) i / content.Length);
                }

                progress.Report(1);
            }

            if (_error && !Prompt()) return;

            // second call: write 'failures': PDFs that do not have enough information to index with 'Warning' level
            Log("Copying Failure PDFs due to missing IDX Content");
            using (var progress = new ProgressBar())
            {
                for (var i = 0; i < content.Length; i++)
                {
                    var row = content[i].Split(',');
                    try
                    {
                        if (row.Length != 4 || row.Any(string.IsNullOrEmpty))
                        {
                            File.Copy(Path.Join(original, $"{row[0]}.pdf"), Path.Join(failures, $"{row[0]}.pdf"));
                            var temp = content.ToList();
                            temp.RemoveAt(i);
                            i--;
                            content = temp.ToArray();
                        }
                    }
                    catch (Exception e)
                    {
                        Log($"Error attempting to copy {row[0]}.pdf\n{e}", LogLevel.Error);
                        if (!Prompt()) return;
                        _error = true;
                    }
                    System.Diagnostics.Debug.WriteLineIf(row.Length != 4, $"File {row[0]}.pdf missing content for indexing: {row.Length}");
                    progress.Report((double) i / content.Length);
                }

                progress.Report(1);
            }

            if (_error && !Prompt()) return;

            // third call: write watermark PDFs and generate associated index file
            Log("Generating Archive and associated IDX Files");
            using (var progress = new ProgressBar())
            {
                for (var i = 0; i < content.Length; i++)
                {
                    var row = content[i].Split(',');
                    try
                    {
                        // now we make the watermarked archive file
                        WatermarkPdf(Path.Join(original, $"{row[0]}.pdf"), Path.Join(archive, $"{row[0]}.pdf"));
                    }
                    catch (Exception e)
                    {
                        Log($"Error attempting to watermark PDF: {row[0]}.pdf\n{e}", LogLevel.Error);
                        if (!Prompt()) return;
                    }
                    try
                    {
                        // now we create the idx file
                        System.Diagnostics.Debug.WriteLine($"Creating Indexing File: {row[0]}.csv");
                        using (var writer = File.CreateText(Path.Join(archive, $"{row[0]}.csv")))
                            writer.WriteLine($"{row[1]},{row[2]},{row[3]}");
                        System.Diagnostics.Debug.WriteLine("IDX File Completed");
                    }
                    catch (Exception e)
                    {
                        Log($"Error attempting to generate IDX: {row[0]}.csv\n{e}", LogLevel.Error);
                        if (!Prompt()) return;
                    }

                    progress.Report((double) i / content.Length);
                }

                progress.Report(1);
            }

            Console.WriteLine("Finished Execution, press any key to continue...");
            Console.ReadLine();
        }

        private static void TiffToPdf(string original, string destination)
        {
            var writer = new PdfWriter(destination);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);
            var bytes = File.ReadAllBytes(original);
            var pages = TiffImageData.GetNumberOfPages(bytes);
            for (var i = 1; i <= pages; i++)
                document.Add(new Image(ImageDataFactory.CreateTiff(bytes, false, i, false)));
            document.Close();
        }

        private static bool Prompt()
        {
            if (!_continue)
            {
                Console.Write("\nContinue w/Errors? (Y/n): ");
                var result = Console.ReadLine();
                if (string.IsNullOrEmpty(result)) result = "y";
                if (!result.Equals("y", StringComparison.InvariantCultureIgnoreCase)) return false;
            }

            System.Diagnostics.Debug.WriteLine("Continuing Execution");
            Log("Continuing Execution");
            _error = false;  // reset error flag
            return true;
        }

        private static void WatermarkPdf(string sourceFile, string destinationPath)
        {
            const float watermarkTrimmingRectangleWidth = 300;
            const float watermarkTrimmingRectangleHeight = 300;

            const float formWidth = 300;
            const float formHeight = 300;
            const float formXOffset = 0;
            const float formYOffset = 0;

            const float xTranslation = 50;
            const float yTranslation = 25;

            const double rotationInRads = Math.PI / 3;

            try { FontCache.ClearSavedFonts(); }
            catch (Exception) { }   // ignored

            var font = PdfFontFactory.CreateFont(StandardFonts.COURIER);
            const float fontSize = 119;

            using var reader = new PdfReader(new MemoryStream(File.ReadAllBytes(sourceFile)));
            // using var file = File.CreateText(destinationPath);
            using var pdfDoc = new PdfDocument(reader, new PdfWriter(destinationPath));
            // using var pdfDoc = new PdfDocument(new PdfReader(sourceFile), new PdfWriter(destinationPath));
            var numberOfPages = pdfDoc.GetNumberOfPages();
            PdfPage page = null;

            for (var i = 1; i <= numberOfPages; i++)
            {
                page = pdfDoc.GetPage(i);
                var ps = page.GetPageSize();

                //Center the annotation
                var bottomLeftX = ps.GetWidth() / 2 - watermarkTrimmingRectangleWidth / 2;
                var bottomLeftY = ps.GetHeight() / 2 - watermarkTrimmingRectangleHeight / 2;
                var watermarkTrimmingRectangle = new Rectangle(bottomLeftX, bottomLeftY, watermarkTrimmingRectangleWidth, watermarkTrimmingRectangleHeight);

                var watermark = new PdfWatermarkAnnotation(watermarkTrimmingRectangle);

                //Apply linear algebra rotation math
                //Create identity matrix
                var transform = new AffineTransform();//No-args constructor creates the identity transform
                                                                  //Apply translation
                transform.Translate(xTranslation, yTranslation);
                //Apply rotation
                transform.Rotate(rotationInRads);

                var fixedPrint = new PdfFixedPrint();
                watermark.SetFixedPrint(fixedPrint);
                //Create appearance
                var formRectangle = new Rectangle(formXOffset, formYOffset, formWidth, formHeight);

                //Observation: font XObject will be resized to fit inside the watermark rectangle
                var form = new PdfFormXObject(formRectangle);
                var gs1 = new PdfExtGState().SetFillOpacity(0.6f);
                var canvas = new PdfCanvas(form, pdfDoc);

                var transformValues = new float[6];
                transform.GetMatrix(transformValues);
                canvas.SaveState()
                    .BeginText().SetColor(ColorConstants.GRAY, true).SetExtGState(gs1)
                    .SetTextMatrix(transformValues[0], transformValues[1], transformValues[2], transformValues[3], transformValues[4], transformValues[5])
                    .SetFontAndSize(font, fontSize)
                    .ShowText("COPY")
                    .EndText()
                    .RestoreState();

                canvas.Release();

                watermark.SetAppearance(PdfName.N, new PdfAnnotationAppearance(form.GetPdfObject()));
                watermark.SetFlags(PdfAnnotation.PRINT);

                page.AddAnnotation(watermark);

            }
            page?.Flush();
            pdfDoc.Close();
        }

        private static void Log(string message, LogLevel level = LogLevel.Info)
        {
            switch (level)
            {
                case LogLevel.Warning:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"[{DateTime.Now:s}] [WRN] {message}");
                    Console.ResetColor();
                    break;
                case LogLevel.Error:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now:s}] [ERR] {message}");
                    Console.ResetColor();
                    break;
                case LogLevel.Verbose when level >= _level:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"[{DateTime.Now:s}] [VRB] {message}");
                    Console.ResetColor();
                    break;
                case LogLevel.Info:
                default:
                    Console.WriteLine($"[{DateTime.Now:s}] [{level}] {message}");
                    break;
            }
        }

        private enum LogLevel
        {
            Verbose = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
        }
    }
}
