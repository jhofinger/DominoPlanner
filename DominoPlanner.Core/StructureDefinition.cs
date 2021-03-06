﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using System.Xml.Linq;
using SkiaSharp;

namespace DominoPlanner.Core
{
    partial class StructureParameters
    {
        public String name;
        internal CellDefinition[,] cells;
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
        public DominoTransfer GenerateStructure(int sWidth, int sHeight)
        {
            //GenStructHelper g = new GenStructHelper() // Initialize GenStructHelper with final size.
            //{
            //    width = cells[0, 0].width + cells[1, 1].width * sWidth + cells[2, 2].width,
            //    height = cells[0, 0].height + cells[1, 1].height * sHeight + cells[2, 2].height
            //};
            return new DominoTransfer(getNewShapes(sWidth, sHeight), colors);
            //g.HasProtocolDefinition = hasProtocolDefinition;
            //int a = g.dominoes.Max(s => s.GetContainer().x2);
            //return g;
        }
        public SKSurface DrawPreview(int col, int row, int targetDimension)
        {
            CellDefinition cell = cells[col, row];
            double scalingFactor = PreviewScaleFactor(targetDimension); // scale everything to the same size
            SKImageInfo info = new SKImageInfo((int)(cell.width * scalingFactor + 2), (int)(cell.height * scalingFactor + 2));
            var bitmap = SKSurface.Create(info);
            SKCanvas canvas = bitmap.Canvas;
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

                        canvas.DrawPath(transformed.GetPath(scalingFactor).GetSKPath(), new SKPaint() { Color = SKColors.Black, IsStroke=true, StrokeWidth = 1 }); // outline
                        canvas.DrawPath(transformed.GetPath(scalingFactor).GetSKPath(), new SKPaint() { Color = new SKColor(0, 0, 255, 128) }); // inner line
                    }
                }
            }
            return bitmap;
        }
        public static SKSurface[,] getPreviews(int targetDimension, XElement structure)
        {
            SKSurface[,] array = new SKSurface[3, 3];
            StructureParameters sp = new StructureParameters();
            sp.StructureDefinitionXML = structure;
            for (int col = 0; col < 3; col++)
            {
                for (int row = 0; row < 3; row++)
                {
                    array[col, row] = sp.DrawPreview(col, row, targetDimension);
                }
            }
            return array;
        }
    }
    public static class PathExtensions
    {
        public static SKPath GetSKPath(this DominoPath path)
        {
            var p = new SKPath();
            if (path.points.Length > 0)
            {
                p.MoveTo((float)path.points[0].X, (float)path.points[0].Y);
                foreach (var point in path.points.Skip(1))
                {
                    p.LineTo((float)point.X, (float)point.Y);
                }
                p.Close();
            }
            return p;
        }
    }
    [ProtoContract]
    public struct GenStructHelper
    {
        [ProtoMember(1)]
        public double width;
        [ProtoMember(2)]
        public double height;
        [ProtoMember(3)]
        public IDominoShape[] dominoes;
        [ProtoMember(4)]
        public bool HasProtocolDefinition;
    }
    public class CellDefinition
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
        public int Count => dominoes.Length;
    }
}
