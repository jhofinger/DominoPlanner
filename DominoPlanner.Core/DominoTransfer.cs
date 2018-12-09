﻿using Emgu.CV;
using Emgu.CV.Util;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DominoPlanner.Core
{
    [ProtoContract(SkipConstructor =true)]
    public class DominoTransfer : ICloneable
    {
        [ProtoMember(2, IsPacked = true)]
        public int[] dominoes { get; set; }
        [ProtoMember(1)]
        public IDominoShape[] shapes;
        [ProtoMember(3, AsReference = true)]
        ColorRepository colors;
        [ProtoMember(4, AsReference = true)]
        public IterationInformation iterationInfo {get; set;}
        public int length
        {
            get { return dominoes.Length; }
        }
        public int dominoLength
        {
            get
            {
                return shapes.Max(x => (x.position != null) ? x.position.x : 0) + 1;
            }
        }
        public int dominoHeight
        {
            get
            {
                return shapes.Max(y => (y.position != null) ? y.position.y : 0) + 1;
            }
        }
        public Tuple<IDominoShape, Color, string> this[int index]
        {
            get
            {
                return new Tuple<IDominoShape, Color, string>(shapes[index], colors[dominoes[index]].mediaColor, colors[dominoes[index]].name);
            }
        }
        public DominoTransfer(int[] dominoColors, IDominoShape[] shapes, ColorRepository colors)
        {
            if (dominoColors.Length != shapes.Length) throw new InvalidOperationException("Colors and shapes must have the same length");
            this.dominoes = dominoColors;
            this.shapes = shapes;
            this.colors = colors;
        }
        public Mat GenerateImage(int targetWidth = 0, bool borders = false)
        {
            return GenerateImage(Colors.White, targetWidth, borders);
        }
        public Mat GenerateImage(Color background, int targetWidth = 0, bool borders = false)
        {

            double scalingFactor = 1;
            int width = shapes.Max(s => s.GetContainer().x2);
            int heigth = shapes.Max(s => s.GetContainer().y2);
            if (targetWidth != 0)
            {
                // get dimensions of the structure
                
                int x_min = shapes.Min(s => s.GetContainer().x1);
                int y_min = shapes.Min(s => s.GetContainer().y1);
                scalingFactor = (double)targetWidth / width;
            }
            Image<Emgu.CV.Structure.Bgra, byte> bitmap
                = new Image<Emgu.CV.Structure.Bgra, byte>((int)(width * scalingFactor), (int)(heigth * scalingFactor),
                new Emgu.CV.Structure.Bgra() { Alpha = background.A, Blue = background.B, Green = background.G, Red = background.R });
            

            Parallel.For(0, dominoes.Length, (i) =>
           {
               Color c = colors[dominoes[i]].mediaColor;

               if (shapes[i] is RectangleDomino)
               {
                   DominoRectangle rect = shapes[i].GetContainer(scalingFactor);
                   CvInvoke.Rectangle(bitmap, new System.Drawing.Rectangle() { X = (int)rect.x, Y = (int)rect.y,
                       Width = (int)rect.width, Height = (int)rect.height }, new Emgu.CV.Structure.MCvScalar(c.B, c.G, c.R, c.A), -1, Emgu.CV.CvEnum.LineType.AntiAlias);
                   if (borders)
                   {
                       CvInvoke.Rectangle(bitmap, new System.Drawing.Rectangle()
                       {
                           X = (int)rect.x,
                           Y = (int)rect.y,
                           Width = (int)rect.width,
                           Height = (int)rect.height
                       }, new Emgu.CV.Structure.MCvScalar(0, 0, 0, 255), 1, Emgu.CV.CvEnum.LineType.AntiAlias);

                   }
               }
               else
               {
                   DominoPath shape = this[i].Item1.GetPath(scalingFactor);
                   bitmap.FillConvexPoly(shape.getSDPath(),
                       new Emgu.CV.Structure.Bgra(c.B, c.G, c.R, c.A), Emgu.CV.CvEnum.LineType.AntiAlias);
                   if (borders)
                   {
                       bitmap.DrawPolyline(shape.getSDPath(), true, 
                           new Emgu.CV.Structure.Bgra(0, 0, 0, 255), 1, Emgu.CV.CvEnum.LineType.AntiAlias);
                   }
               }
                    //bitmap.FillPolygon(shape.getWBXPath(), c);
                
               //else
               //{

                    //bitmap.FillPolygon(shape.GetPath(scalingFactor).getWBXPath(), c);
                    //for (int k = 0; k < shape.points.Length - 1; k++)
                    //{
                    //   bitmap.AaWidthLine(shape.points[k].X, shape.points[k].Y, shape.points[k + 1].Y, shape.points[k + 1].Y, Colors.Black, scalingFactor)
                    //}

                    //bitmap.DrawPolyline(shape.GetPath(scalingFactor).getWBXPath(), Colors.Black);
                    //bitmap.FillPolygon(shape.GetPath(scalingFactor).getOffsetRectangle((int)Math.Ceiling(scalingFactor)).getWBXPath(), Colors.Black); // outline
                    //bitmap.FillPolygon(shape.GetPath(scalingFactor).getOffsetRectangle(-(int)Math.Ceiling(scalingFactor)).getWBXPath(), c); // fill
                //}

           });
            return bitmap.Mat;
            /*WriteableBitmap bitmap = BitmapFactory.New((int)(width * scalingFactor), (int)(heigth * scalingFactor));
            using (bitmap.GetBitmapContext())
            {
                bitmap.Clear(Colors.White);
                for (int i = 0; i < dominoes.Length; i++)
                {
                    DominoPath shape = this[i].Item1.GetPath(scalingFactor);
                    Color c = this[i].Item2;
                    if (!borders)
                    {
                        bitmap.FillPolygon(shape.getWBXPath(), c);
                    }
                    else
                    {
                        
                        //bitmap.FillPolygon(shape.GetPath(scalingFactor).getWBXPath(), c);
                        //for (int k = 0; k < shape.points.Length - 1; k++)
                        //{
                         //   bitmap.AaWidthLine(shape.points[k].X, shape.points[k].Y, shape.points[k + 1].Y, shape.points[k + 1].Y, Colors.Black, scalingFactor)
                        //}

                        //bitmap.DrawPolyline(shape.GetPath(scalingFactor).getWBXPath(), Colors.Black);
                        //bitmap.FillPolygon(shape.GetPath(scalingFactor).getOffsetRectangle((int)Math.Ceiling(scalingFactor)).getWBXPath(), Colors.Black); // outline
                        //bitmap.FillPolygon(shape.GetPath(scalingFactor).getOffsetRectangle(-(int)Math.Ceiling(scalingFactor)).getWBXPath(), c); // fill
                    }
                }
            }
            return bitmap;
            */
        }

        public object Clone()
        {
            return Serializer.DeepClone<DominoTransfer>(this);
        }
    }

    
}
