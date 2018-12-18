﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DominoPlanner.Core;

namespace DominoPlanner.CoreTests
{
    class PostFilterTests
    {
        public static void PostFilterTest(string path)
        {
            //PostFilterFieldTest(path);
            PostFilterStructureTest(path);
        }
        public static void PostFilterFieldTest(string path)
        {
            FieldParameters fp = new FieldParameters(path, "colors.DColor", 8, 8, 24, 8, 10000, Emgu.CV.CvEnum.Inter.Lanczos4,
                ColorDetectionMode.Cie94Comparison, new Dithering(), new NoColorRestriction());

            fp.Generate().GenerateImage().Save("tests/FilterTests/vorFilter.png");
            InsertRowFieldTest(fp);
            DeleteRowFieldTest(fp);
            CopyPasteFieldTest(fp);
            InsertColumnFieldTest(fp);
            DeleteColumnFieldTest(fp);
        }
        public static void PostFilterStructureTest(string path)
        {
            StreamReader sr = new StreamReader(new FileStream("Structures.xml", FileMode.Open));
            XElement xml = XElement.Parse(sr.ReadToEnd());
            StructureParameters s = new StructureParameters(path, xml.Elements().ElementAt(6), 3000,
                "colors.DColor", ColorDetectionMode.Cie1976Comparison, new Dithering(),
                AverageMode.Corner, new NoColorRestriction(), true);

            s.Generate().GenerateImage().Save("tests/StructureFilterTests/vorFilter.png");
            InsertRowStructureTest(s);
            DeleteRowStructureTest(s);
            CopyPasteFieldTest(s);
            //InsertColumnFieldTest(s);
            //DeleteColumnFieldTest(s);
        }
        public static void InsertRowStructureTest(StructureParameters s)
        {
            AddRows addRows = new AddRows(s, 40, 2, 5, true);
            addRows.Apply();
            AddRows addRows2 = new AddRows(s, 40, 2, 5, false);
            addRows2.Apply();
            s.last.GenerateImage().Save("tests/StructureFilterTests/nachRowEinfügen.png");
            SaveFieldPlan(s, "tests/StructureFilterTests/FeldplanNachRowEinfuegen.html");
            addRows2.Undo();
            addRows.Undo();
            s.last.GenerateImage().Save("tests/StructureFilterTests/nachRowEinfügenUndo.png");
        }
        public static void DeleteRowStructureTest(StructureParameters s)
        {
            DeleteRow deleteRow = new DeleteRow(s, new int[] { 40, 200, 500});
            deleteRow.Apply();
            s.last.GenerateImage().Save("tests/StructureFilterTests/nachRowLöschen.png");
            SaveFieldPlan(s, "tests/StructureFilterTests/FeldplanNachRowLöschen.html");
            deleteRow.Undo();
            s.last.GenerateImage().Save("tests/StructureFilterTests/nachRowLoeschenUndo.png");

        }
        public static void CopyPasteFieldTest(StructureParameters s)
        {
            int[] source_area = new int[100];
            int start = 33;
            int end = 459+126*10;
            for (int j = 0; j < 100; j++)
            {
                source_area[j] = start + j;
            }
            PasteFilter paste = new PasteFilter(s, start, source_area, end);
            paste.Apply();
            s.last.GenerateImage().Save("tests/StructureFilterTests/nachPaste.png");
            paste.Undo();
            s.last.GenerateImage().Save("tests/StructureFilterTests/nachPasteUndo.png");
        }
        public static void InsertRowFieldTest(FieldParameters fp )
        {
            AddRows addRows = new AddRows(fp, fp.height * fp.length - 1, 5, 5, true);
            addRows.Apply();
            AddRows addRows2 = new AddRows(fp, 0, 5, 5, false);
            addRows2.Apply();
            fp.last.GenerateImage().Save("tests/FilterTests/nachRowEinfügen.png");
            SaveFieldPlan(fp, "tests/FilterTests/FeldplanNachRowEinfuegen.html");
            addRows2.Undo();
            addRows.Undo();
            fp.last.GenerateImage().Save("tests/FilterTests/nachRowEinfügenUndo.png");
        }
        public static void DeleteRowFieldTest(FieldParameters fp)
        {
            DeleteRow deleteRow = new DeleteRow(fp, (new int[] { 0, fp.current_height -1 }).Select(x => x * fp.current_width).ToArray());
            deleteRow.Apply();
            fp.last.GenerateImage().Save("tests/FilterTests/nachRowLöschen.png");
            SaveFieldPlan(fp, "tests/FilterTests/FeldplanNachRowLöschen.html");
            deleteRow.Undo();
            fp.last.GenerateImage().Save("tests/FilterTests/nachRowLoeschenUndo.png");

        }
        public static void InsertColumnFieldTest(FieldParameters fp)
        {
            AddColumns addCol = new AddColumns(fp, 1, 5, 5, true);
            addCol.Apply();
            AddColumns addCol2 = new AddColumns(fp, 0, 5, 5, false);
            addCol2.Apply();
            fp.last.GenerateImage().Save("tests/FilterTests/nachColumnEinfügen.png");
            SaveFieldPlan(fp, "tests/FilterTests/FeldplanNachColumnEinfuegen.html");
            addCol2.Undo();
            addCol.Undo();
            fp.last.GenerateImage().Save("tests/FilterTests/nachColumnEinfügenUndo.png");
        }
        public static void DeleteColumnFieldTest(FieldParameters fp)
        {
            DeleteColumn deleteCol = new DeleteColumn(fp, (new int[] { 0, 1, 2, 3, 5, 7, 15 }));
            deleteCol.Apply();
            fp.last.GenerateImage().Save("tests/FilterTests/nachColLöschen.png");
            SaveFieldPlan(fp, "tests/FilterTests/FeldplanNachColLöschen.html");
            deleteCol.Undo();
            fp.last.GenerateImage().Save("tests/FilterTests/nachColLoeschenUndo.png");

        }
        public static void CopyPasteFieldTest(FieldParameters fp)
        {
            int[] source_area = new int[400];
            int startx = 20;
            int starty = 20;
            int target_x = 30;
            int target_y = 30;
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    source_area[j * 20 + i] = fp.current_width * (j + starty) + (startx + i); 
                }
            }
            PasteFilter paste = new PasteFilter(fp, fp.current_width * starty + startx, source_area, fp.current_width * target_y + target_x);
            paste.Apply();
            fp.last.GenerateImage().Save("tests/FilterTests/nachPaste.png");
            paste.Undo();
            fp.last.GenerateImage().Save("tests/FilterTests/nachPasteUndo.png");
        }
        public static void SaveFieldPlan(IDominoProvider fp, string path)
        {
            FileStream fs = new FileStream(path, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs); 
            sw.Write(fp.GetHTMLProcotol(new ObjectProtocolParameters()
            {
                backColorMode = ColorMode.Normal,
                foreColorMode = ColorMode.Intelligent,
                orientation = Core.Orientation.Horizontal,
                reverse = false,
                summaryMode = SummaryMode.Large,
                textFormat = "<font face=\"Verdana\">",
                templateLength = 20,
                textRegex = "%count% %color%",
                title = "Field"
            }));
        }
    }
}
