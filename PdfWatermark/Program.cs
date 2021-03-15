using System;
using System.IO;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Kernel.Pdf.Xobject;
using Path = System.IO.Path;

namespace PdfWatermark
{
    class Program
    {
        private static LogLevel _level = LogLevel.Info;

        /// <summary>
        /// main entry point for watermarking PDFs
        /// </summary>
        /// <param name="args">args[0] - Log Level: Defaults to LogLevel.Info</param>
        static void Main(string[] args)
        {
            try
            {
                if (args?.Length > 0)
                    _level = Enum.Parse<LogLevel>(args[0]);
                var error = false;
                Console.Write("Working Directory: ");
                var directory = Console.ReadLine() ?? Directory.GetCurrentDirectory();
                Console.Write("PDFs Locations: ");
                var pdfs = Console.ReadLine() ?? throw new ArgumentException("PDF Directory cannot be null!");
                var original = Path.Join(directory, "Originals");
                var archive = Path.Join(directory, "Archive");
                var failures = Path.Join(directory, "Failures");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                if (!Directory.Exists(original))
                    Directory.CreateDirectory(original);
                if (!Directory.Exists(archive))
                    Directory.CreateDirectory(archive);
                if (!Directory.Exists(failures))
                    Directory.CreateDirectory(failures);
                Console.Write("CSV File (full filepath): ");
                var csv = Console.ReadLine() ?? throw new ArgumentException("CSV File cannot be null");
                var content = File.ReadAllLines(csv);
                using (var progress = new ProgressBar())
                {
                    for (var i = 0; i < content.Length; i++)
                    {
                        var row = content[i].Split(',');
                        // First check if the file exists
                        if (!File.Exists(Path.Join(pdfs, $"{row[0]}.pdf")))
                        {
                            System.Diagnostics.Debug.WriteLine($"File Not Found: {row[0]}.pdf");
                            continue;
                        }

                        // go ahead and copy the file to the original location for storage
                        System.Diagnostics.Debug.WriteLine($"Copying File: {row[0]}.pdf");
                        File.Copy(Path.Join(pdfs, $"{row[0]}.pdf"), Path.Join(original, $"{row[0]}.pdf"));
                        // now check if we have all data points
                        if (row.Length != 4)
                        {
                            System.Diagnostics.Debug.WriteLine($"File {row[0]}.pdf missing content: {row.Length}");
                            // we move the file to the failures directory
                            File.Copy(Path.Join(pdfs, $"{row[0]}.pdf"), Path.Join(failures, $"{row[0]}.pdf"));
                            error = true;
                            continue;
                        }
                        // now we make the watermarked archive file
                        WatermarkPdf(Path.Join(pdfs, $"{row[0]}.pdf"), Path.Join(archive, $"{row[0]}.pdf"));
                        // now we create the idx file
                        System.Diagnostics.Debug.WriteLine($"Creating Indexing File: {row[0]}.csv");
                        using (var writer = File.CreateText(Path.Join(archive, $"{row[0]}.csv")))
                            writer.WriteLine($"{row[1]},{row[2]},{row[3]}");
                        System.Diagnostics.Debug.WriteLine("IDX File Completed");
                        progress.Report((double)i / content.Length);
                    }

                    progress.Report(1);
                }

                Log($"Finished Parsing Directory: {pdfs}");
                if (error) Log("Failures were gracefully handled", LogLevel.Error);
                Console.WriteLine("Press Any Key to Continue...");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Log(e.Message, LogLevel.Error);
                Log(e.StackTrace, LogLevel.Error);
                Console.WriteLine("System Encountered a Fatal Error, Press any key to continue...");
                Console.ReadLine();
            }
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
                    Console.WriteLine($"[{DateTime.Now:s}] [${level}] {message}");
                    break;
            }
        }

        private enum LogLevel
        {
            Verbose = 0,
            Info = 1,
            Error = 2,
        }
    }
}
