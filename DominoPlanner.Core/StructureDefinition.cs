﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace DominoPlanner.Core
{
    class ClusterStructureDefinition
    {
        public String name;
        public CellDefinition[,] cells;
        public bool hasProtocolDefinition;
        public ClusterStructureDefinition(XElement definition)
        {
            hasProtocolDefinition = definition.Attribute("HasProtocolDefinition").Value == "true";
            name = definition.Attribute("Name").Value;
            cells = new CellDefinition[3, 3];
            foreach (XElement part in definition.Elements("PartDefinition"))
            {
                int col = GetIndex(part.Attribute("HorizontalPosition").Value);
                int row = GetIndex(part.Attribute("VerticalPosition").Value);
                cells[col, row] = new CellDefinition(part);
            }
        }
        private double PreviewScaleFactor(int TargetDimension)
        {
            double largest = 0;
            // get largest dimension
            foreach (CellDefinition c in cells)
            {
                if (c.width > largest) largest = c.width;
                if (c.height > largest) largest = c.height;
            }
            return TargetDimension / largest;
        }
        private int GetIndex(string Position)
        {
            if (Position == "Center") return 1;
            else if (Position == "Left" || Position == "Right")
            {
                return (Position == "Left") ? 0 : 2;
            }
            else
            {
                return (Position == "Top") ? 0 : 2;
            }
        }
        public GenStructHelper GenerateStructure(int sWidth, int sHeight)
        {
            List<IDominoShape> DominoList = new List<IDominoShape>();
            GenStructHelper g = new GenStructHelper() // Initialize GenStructHelper with final size.
            {
                width = cells[0, 0].width + cells[1, 1].width * sWidth + cells[2, 2].width,
                height = cells[0, 0].height + cells[1, 1].height * sHeight + cells[2, 2].height
            };
            for (int x = -1; x < sWidth + 1; x++)
            {
                for (int y = -1; y < sHeight + 1; y++)
                {
                    DominoList.AddRange(
                        (cells[(x == -1) ? 0 : ((x == sWidth) ? 2 : 1), (y == -1) ? 0 : ((y == sHeight) ? 2 : 1)]
                        .TransformDefinition(
                            (x == -1) ? 0 : (cells[1, 1].width * x + cells[0, 0].width),
                            (y == -1) ? 0 : (cells[1, 1].height * y + cells[0, 0].height),
                            x, y, sWidth, sHeight))
                        .dominoes);
                }
            }
            g.dominoes = DominoList.ToArray();
            g.HasProtocolDefinition = hasProtocolDefinition;
            return g;
        }
        public WriteableBitmap DrawPreview(int col, int row, int targetDimension)
        {
            CellDefinition cell = cells[col, row];
            double scalingFactor = PreviewScaleFactor(targetDimension); // scale everything to the same size
            WriteableBitmap b = BitmapFactory.New((int)(cell.width * scalingFactor + 2), (int)(cell.height * scalingFactor + 2));
            for (int colc = (col == 2) ? 1 : 0; colc <= ((col == 0) ? 1 : 2); colc++)
            {
                for (int rowc = (row == 2) ? 1 : 0; rowc <= ((row == 0) ? 1 : 2); rowc++) // only use the cells next to the specified (so top left uses 4 top center, center left and center center).
                {
                    CellDefinition current = cells[colc, rowc];
                    int xOffsetMultiplier = colc - col; // for moving the cells
                    int yOffsetMultiplier = rowc - row;
                    for (int i = 0; i < current.dominoes.Length; i++)
                    {
                        IDominoShape transformed = current.dominoes[i].TransformDomino(
                            xOffsetMultiplier * ((xOffsetMultiplier > 0) ? cell.width : current.width),
                            yOffsetMultiplier * ((yOffsetMultiplier > 0) ? cell.height : current.height), 0, 0, 0, 0); // move the dominoes
                        DominoRectangle container = transformed.GetContainer(); // get containing rectangle
                        if (container.x >= cells[col, row].width || container.x + container.width <= 0 || container.y >= cells[col, row].height || container.y + container.height <= 0) continue; // check if rectangle is out of drawing area
                        b.FillPolygon(transformed.GetPath(scalingFactor).getOffsetRectangle((int)Math.Ceiling(scalingFactor)).getWBXPath(), System.Windows.Media.Colors.Black); // outline
                        b.FillPolygon(transformed.GetPath(scalingFactor).getOffsetRectangle(-(int)Math.Ceiling(scalingFactor)).getWBXPath(), System.Windows.Media.Colors.LightGray); // inner line
                    }
                }
            }
            return b;
        }
    }
    public struct GenStructHelper
    {
        public double width;
        public double height;
        public IDominoShape[] dominoes;
        public bool HasProtocolDefinition;
    }
    internal class CellDefinition
    {
        public double width;
        public double height;
        public IDominoShape[] dominoes;
        public CellDefinition() { }
        public CellDefinition(XElement part)
        {
            width = float.Parse(part.Attribute("Width").Value, CultureInfo.InvariantCulture);
            height = float.Parse(part.Attribute("Height").Value, CultureInfo.InvariantCulture);
            dominoes = part.Elements().Select(x => IDominoShape.LoadDefinition(x)).ToArray();
        }
        public CellDefinition TransformDefinition(double moveX, double moveY, int i, int j, int width, int height)
        {
            return new CellDefinition()
            {
                width = this.width,
                height = this.height,
                dominoes = this.dominoes.Select(d => d.TransformDomino(moveX, moveY, i, j, width, height)).ToArray()
            };
        }
    }
}
