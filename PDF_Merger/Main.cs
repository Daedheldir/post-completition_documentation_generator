using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using Tommy;


//TODO: Page counting is off 
namespace PDF_Merger
{
    internal class PDFMergerConfig
    {
        public string inputFilepath = "";
        public string outputFilepath = "";
        public string titleFilepath = "";
        public string tableOfContentsFilepath = "";
        public string declarationFilepath = "";
        public string fullInputFilepath = "";
        public bool multipleDeclarationPages = false;
        public bool continousNumeration = false;
    }
    internal class PDF_Merger
    {
        public static List<Tuple<string, PdfCustomDoc>> createDocumentList(string targetPath, List<Tuple<string, string, List<string>>> directoryFileList, string declarationPath = "0", string tableOfContentsPath = "0", string titlePagePath = "0", bool multipleDeclarationPages = true)
        {
            List<Tuple<string, PdfCustomDoc>> pagesDocumentList = new List<Tuple<string, PdfCustomDoc>>();   //list<directort<pageCount, document>>
            PdfDocument targetDoc = new PdfDocument();
            PdfDocument declarationDoc = new PdfDocument();
            PdfDocument tempDoc = new PdfDocument();

            ProcessTitlePage(titlePagePath, targetDoc);
            ProcessTableOfContentsPage(tableOfContentsPath, targetDoc);

            if (declarationPath != "0")
            {
                declarationDoc = PdfReader.Open(declarationPath, PdfDocumentOpenMode.Import);
            }

            int currentTotalPageCount = 0;

            foreach (Tuple<string, string, List<string>> tuple in directoryFileList)
            {
                //add pages for each file in list
                foreach (string file in tuple.Item3)
                {
                    int pageCount = 0;

                    //if file is a pdf
                    if (file.Contains(".pdf"))
                    {
                        AddPdfPage(targetPath, declarationPath, multipleDeclarationPages, targetDoc, declarationDoc, ref tempDoc, file, ref pageCount, ref currentTotalPageCount);
                    }

                    //if file isnt a pdf convert it
                    else if (file.Contains(".jpg") || file.Contains(".jpeg") || file.Contains(".png"))
                    {
                        tempDoc = AddImagePage(declarationPath, targetDoc, declarationDoc, file, ref pageCount);
                    }

                    //if page count is not even add a declaration page
                    if ((targetDoc.PageCount % 2) != 0)
                    {
                        pageCount++;
                        if (declarationPath != "0")
                        {
                            targetDoc.AddPage(declarationDoc.Pages[0]);
                        }
                    }
                    string[] name = file.Split('\\');
                    string file_path = "";
                    for (int i = 0; i < name.Length - 1; ++i)
                    {
                        file_path += '/' + name[i];
                    }
                    int actualPageCount = (((pageCount / 2) >= 1) ? (pageCount / 2) : (pageCount));

                    //add new pair of directory , file
                    pagesDocumentList.Add(new Tuple<string, PdfCustomDoc>(tuple.Item1, new PdfCustomDoc(tempDoc, name[name.Length - 1].Split('.')[0], file_path, actualPageCount)));
                }
            }
            try { 
            targetDoc.Save(targetPath + "\\merged.pdf");
                }catch(DirectoryNotFoundException e)
            {
                Console.WriteLine("Directory " + targetPath + "\\merged.pdf couldn't be found!\\" + e.Message + "\\\\ PRESS ANY KEY TO CONTINUE...");
                Console.ReadLine();
            }
            return pagesDocumentList;
        }

        private static PdfDocument AddImagePage(string declarationPath, PdfDocument targetDoc, PdfDocument declarationDoc, string file, ref int pageCount)
        {
            PdfDocument tempDoc = new PdfDocument();
            try
            {
                // Create an empty page or load existing
                PdfPage page = targetDoc.AddPage();
                tempDoc.AddPage();

                // Get an XGraphics object for drawing
                using (XGraphics gfx = XGraphics.FromPdfPage(page))
                {
                    double width = page.Width;
                    double height = page.Height;
                    XImage image = XImage.FromFile(file);
                    gfx.DrawImage(image, 0, 0, width, height);
                }

                pageCount += 1;

                //TODO: MUST BE DONE IN THE SAME PLACE WHERE THE FILES ARE OPENED

                //add pages from current doc to target doc

                //targetDoc.AddPage(page);
                if (declarationPath != "0")
                {
                    targetDoc.AddPage(declarationDoc.Pages[0]);
                    pageCount++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(tempDoc.FullPath);
                Console.WriteLine(e + "\n");
            }

            return tempDoc;
        }

        private static void AddPdfPage(string targetPath, string declarationPath, bool multipleDeclarationPages, PdfDocument targetDoc, PdfDocument declarationDoc, ref PdfDocument tempDoc, string file, ref int pageCount, ref int currentTotalPageCount)
        {
            try
            {
                using (PdfDocument currentDocument = PdfReader.Open(file, PdfDocumentOpenMode.Import))
                {
                    pageCount += currentDocument.PageCount;
                    tempDoc = currentDocument;

                    // Make a font and a brush to draw the page counter.
                    XFont font = new XFont("Arial", 8, XFontStyle.Bold);
                    XBrush brush = XBrushes.Black;

                    //add pages from current doc to target doc
                    for (int i = 0; i < currentDocument.PageCount; i++)
                    {
                        try
                        {
                            // Add the page counter.
                            PdfPage page = currentDocument.Pages[i];
                            bool pageRotated = false;
                            if(page.Width > page.Height)
                            {
                                page.Rotate = 90;
                                pageRotated = true;
                            }
                            targetDoc.AddPage(page);

                            // Make a layout rectangle.
                            XRect layoutRectangle = new XRect(0/*X*/, page.Height - 1.5 * font.Height/*Y*/, page.Width - font.Height/*Width*/, font.Height/*Height*/);


                            using (XGraphics gfx = XGraphics.FromPdfPage(targetDoc.Pages[targetDoc.Pages.Count - 1]))
                            {
                                if (pageRotated)
                                {
                                    layoutRectangle = new XRect(0/*X*/, 0/*Y*/, page.Width - font.Height/*Width*/, font.Height/*Height*/);
                                    gfx.RotateAtTransform(-90, layoutRectangle.BottomRight);
                                }
                                gfx.DrawString(
                                    (++currentTotalPageCount).ToString() + " strona dokumentacji.",
                                    font,
                                    brush,
                                    layoutRectangle,
                                    XStringFormats.CenterRight);
                                //gfx.DrawRectangle(brush, layoutRectangle);
                            }

                            //add declaration after first page
                            if ((i == 0 || multipleDeclarationPages) && declarationPath != "0")
                            {
                                targetDoc.AddPage(declarationDoc.Pages[0]);
                                pageCount++;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(currentDocument.FullPath);
                            Console.WriteLine(e + "\n");
                        }
                    }
                }
            }
            catch (NullReferenceException e)
            {
                StreamWriter writer = new StreamWriter(targetPath + "\\log.txt");
                writer.Write("Error when opening " + file + "\n" + e.Message);
                writer.Close();
                Console.WriteLine("Error when opening " + file + "\n" + e.Message);
            }
            catch (Exception e)
            {
                StreamWriter writer = new StreamWriter(targetPath + "\\log.txt");
                writer.Write("Error when opening " + file + "\n" + e.Message);
                writer.Close();
                Console.WriteLine("Error when opening " + file + "\n" + e.Message);
            }
        }

        private static void ProcessTableOfContentsPage(string tableOfContentsPath, PdfDocument targetDoc)
        {
            if (tableOfContentsPath == "0")
            {
                return;
            }
            using (PdfDocument tableOfContentsDoc = PdfReader.Open(tableOfContentsPath, PdfDocumentOpenMode.Import))
            {
                for (int i = 0; i < tableOfContentsDoc.PageCount; i++)
                {
                    targetDoc.AddPage(tableOfContentsDoc.Pages[i]);
                }
                //add empty page behind table of contents
                if (targetDoc.PageCount % 2 != 0)
                    targetDoc.AddPage();
            }
        }

        private static void ProcessTitlePage(string titlePagePath, PdfDocument targetDoc)
        {
            if (titlePagePath == "0")
            {
                return;
            }
            using (PdfDocument titlePageDoc = PdfReader.Open(titlePagePath, PdfDocumentOpenMode.Import))
            {
                for (int i = 0; i < titlePageDoc.PageCount; i++)
                {
                    targetDoc.AddPage(titlePageDoc.Pages[i]);
                }
                //add empty page behind title pages
                if (targetDoc.PageCount % 2 != 0)
                    targetDoc.AddPage();
            }
        }

        //<Directory_name,  < Subdir_name , Files >>
        private static List<Tuple<string, string, List<string>>> loadFilePaths(string path)
        {
            List<Tuple<string, string, List<string>>> directoryFileList = new List<Tuple<string, string, List<string>>>();
            List<string> directories = new List<string>();

            if (path.Contains(".txt"))
            {
                StreamReader reader = new StreamReader(path);
                try
                {
                    do
                    {
                        directories.Add(reader.ReadLine());
                    }
                    while (reader.Peek() != -1);
                }
                catch
                {
                    Console.WriteLine("File is empty!!!");
                    Thread.Sleep(10000);
                }
                finally
                {
                    reader.Close();
                }
            }
            else
            {
                directories.Add(path);
            }

            //add subdirs to table;
            foreach (string dir in directories)
            {
                List<string> subDirs = loadSubDirectories(dir, 0);
                foreach (string subDir in subDirs)
                {
                    directoryFileList.Add(new Tuple<string, string, List<string>>(dir, subDir, new List<string>()));
                }
            }

            foreach (Tuple<string, string, List<string>> triple in directoryFileList)
            {
                string[] paths = Directory.GetFiles(triple.Item2);

                foreach (string p in paths)
                {
                    triple.Item3.Add(p);
                }
            }
            return directoryFileList;
        }

        private static List<string> loadSubDirectories(string currentPath, int searchDepth)
        {
            List<string> output = new List<string>();
            string[] dirTable = Directory.GetDirectories(currentPath);
            if (dirTable.Length <= 0 || searchDepth > 5)
            {
                //if there are no subdirectories add to table
                output.Add(currentPath);
            }
            else
            {
                //if there are subdirs
                foreach (string dir in dirTable)
                {
                    //for each subdir get their subdirs
                    List<string> subDirs = loadSubDirectories(dir, searchDepth + 1);

                    //add subdirs to output
                    foreach (string subDir in subDirs)
                    {
                        output.Add(subDir);
                    }
                }
            }
            return output;
        }

        private static void Main(string[] args)
        {
            PDFMergerConfig config = new PDFMergerConfig();
            string selection = "";
            do
            {
                Console.WriteLine("Read from config [0] or manually input configuration[1]?");
                selection = Console.ReadLine();

            } while (selection != "0" && selection != "1");

            if (selection == "0")
            {
                string configPath = "";
                do {
                    Console.WriteLine("Input config path:");
                    configPath = Console.ReadLine();
                }
                while (!configPath.Contains(".toml"));
                config = readConfig(configPath);
            }
            else
            {
                config = readConfig();
            }

            if (config is null) return;

            //create list of documents
            List<Tuple<string, string, List<string>>> filePaths = loadFilePaths(config.fullInputFilepath);
            List<Tuple<string, PdfCustomDoc>> docList = createDocumentList(config.outputFilepath, filePaths, config.declarationFilepath, config.tableOfContentsFilepath, config.titleFilepath, config.multipleDeclarationPages);

            //create merged document
            savePageNumbers(docList, config.outputFilepath);
            savePageEnumeration(docList, config.outputFilepath, config.continousNumeration);
            saveDocumentNames(docList, config.outputFilepath);

            //mergedDoc.Save(outputFilepath + "\\mergedDoc.pdf");
            Console.WriteLine("Finished...");
            Thread.Sleep(10000);
        }
        private static PDFMergerConfig readConfig(string configFilepath) { 
            var config = new PDFMergerConfig();
            using(StreamReader reader = File.OpenText(configFilepath))
            {
                TomlTable table = TOML.Parse(reader);
                config.inputFilepath = table["input_filepath"].AsString;
                config.inputFilepath = config.inputFilepath.Replace("\\", "/");
                config.outputFilepath = table["output_filepath"].AsString;
                config.outputFilepath = config.outputFilepath.Replace("\\", "/");
                config.titleFilepath = table["title_filepath"].AsString;
                config.titleFilepath = config.titleFilepath.Replace("\\", "/");
                config.declarationFilepath = table["declaration_filepath"].AsString;
                config.declarationFilepath = config.declarationFilepath.Replace("\\", "/");
                config.tableOfContentsFilepath = table["table_of_contents_filepath"].AsString;
                config.tableOfContentsFilepath = config.tableOfContentsFilepath.Replace("\\", "/");

                config.multipleDeclarationPages = table["multiple_declaration_pages"].AsInteger == 1;
                config.continousNumeration = table["continous_numeration"].AsInteger == 1;
                config.fullInputFilepath = config.inputFilepath;
            }
            return config;
        }
        private static PDFMergerConfig readConfig()
        {
            var config = new PDFMergerConfig();

            do
            {
                Console.WriteLine("Input filepath: ");
                config.inputFilepath = Console.ReadLine().Replace("\\", "/");

                Console.WriteLine("Output filepath: ");
                config.outputFilepath = Console.ReadLine().Replace("\\", "/");


                if (config.outputFilepath.Contains('.') || config.inputFilepath.Contains('.'))
                {
                    Console.WriteLine("Filepath can't contain file in it!\n");
                }
            } while (config.outputFilepath.Contains('.') || config.inputFilepath.Contains('.'));
            Console.WriteLine("Title page filepath or \"0\" if there is none: ");
            config.titleFilepath = Console.ReadLine().Replace("\\", "/");

            Console.WriteLine("Table of Contents filepath or \"0\" if there is none: ");
            config.tableOfContentsFilepath = Console.ReadLine().Replace("\\", "/");

            Console.WriteLine("Declaration filepath or \"0\" if there is none: ");
            config.declarationFilepath = Console.ReadLine().Replace("\\", "/");

            if (config.declarationFilepath != "0")
            {
                Console.WriteLine("Declaration page after each document page [1] or anything if declaration page only after first page: ");
                config.multipleDeclarationPages = Console.ReadLine() == "1";
            }

            Console.WriteLine("Continue numeration between sections [1] or start from 1 for each new section [0]: ");
            config.continousNumeration = Console.ReadLine() == "1";

            config.fullInputFilepath = (config.inputFilepath == "0" ? Directory.GetCurrentDirectory() + "/input.txt" : config.inputFilepath);

            if (config.inputFilepath == "0")
            {
                try
                {
                    StreamReader testReader = new StreamReader(config.fullInputFilepath);
                    testReader.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Thread.Sleep(10000);
                    return null;
                }
            }
            return config;
        }
        private static void saveDocumentNames(List<Tuple<string, PdfCustomDoc>> docList, string outputPath)
        {
            StreamWriter writer = new StreamWriter(outputPath + "\\docNames.txt");

            string currentDir = "";

            foreach (Tuple<string, PdfCustomDoc> tuple in docList)
            {
                if (currentDir != tuple.Item1)
                {
                    currentDir = tuple.Item1;

                    writer.WriteLine(currentDir);
                }

                writer.WriteLine(tuple.Item2.Name);
            }

            writer.Close();
        }

        private static void savePageEnumeration(List<Tuple<string, PdfCustomDoc>> docList, string outputPath, bool continousNumeration)
        {
            StreamWriter writer = new StreamWriter(outputPath + "\\pageEnumeration.txt");

            string currentDir = "";
            int pageCount = 1;

            foreach (Tuple<string, PdfCustomDoc> tuple in docList)
            {
                if (currentDir != tuple.Item1)
                {
                    if (!continousNumeration)
                        pageCount = 1;

                    currentDir = tuple.Item1;

                    writer.WriteLine(currentDir);
                }
                int pages = tuple.Item2.PageCount;

                writer.Write(pageCount.ToString());

                if (pages > 1)
                {
                    writer.Write('-');
                    writer.Write((pageCount + pages - 1).ToString());
                }
                writer.Write('\n');

                pageCount += pages;
            }

            writer.Close();
        }

        private static void savePageNumbers(List<Tuple<string, PdfCustomDoc>> docList, string outputPath)
        {
            StreamWriter writer = new StreamWriter(outputPath + "\\pageCount.txt");

            string currentDir = "";

            foreach (Tuple<string, PdfCustomDoc> tuple in docList)
            {
                if (currentDir != tuple.Item1)
                {
                    currentDir = tuple.Item1;

                    writer.WriteLine(currentDir);
                }

                writer.WriteLine(tuple.Item2.PageCount);
            }
            writer.Close();
        }
    }
}