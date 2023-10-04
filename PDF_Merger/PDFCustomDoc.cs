using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDF_Merger
{
    public class PdfCustomDoc
    {
        private int pageCount;
        private string path;
        private string name;
        private PdfDocument document;

        public PdfCustomDoc()
        {
            name = "";
            path = "";
            document = new PdfDocument();
        }
        public PdfCustomDoc(PdfDocument document, string name, string path, int pageCount)
        {
            this.name = name;
            this.path = path;
            this.pageCount = pageCount;
            this.document = document;
        }
        public int PageCount
        {
            get { return pageCount; }
        }
        public string Name
        {
            get { return name; }
        }
        public PdfDocument PdfDocument
        {
            get { return document; }
        }
        public string Path
        {
            get { return path; }
        }
    }
}