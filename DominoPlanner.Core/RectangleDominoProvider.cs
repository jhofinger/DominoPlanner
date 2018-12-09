﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.Windows;
using Emgu.CV;
using Emgu.CV.Util;
using System.ComponentModel;
using Emgu.CV.Structure;
using ProtoBuf;
//using Emgu.CV.Structure;

namespace DominoPlanner.Core
{
    /// <summary>
    /// Oberklasse für alle Strukturen, deren Farben aus beliebig angeordneten Rechtecken oder Pfaden berechnet werden.
    /// </summary>
    [ProtoContract]
    [ProtoInclude(100, typeof(StructureParameters))]
    [ProtoInclude(101, typeof(SpiralParameters))]
    [ProtoInclude(102, typeof(CircleParameters))]
    public abstract class RectangleDominoProvider : IDominoProvider
    {
        #region public properties
        private AverageMode _average;
        /// <summary>
        /// Gibt an, ob nur ein Punkt des Dominos (linke obere Ecke) oder ein Durchschnittswert aller Pixel unter dem Pfad verwendet werden soll, um die Farbe auszuwählen.
        /// </summary>
        [ProtoMember(1)]
        public AverageMode average
        {
            get
            {
                return _average;
            }

            set
            {
                _average = value;
                lastValid = false;
            }
        }
        private bool _allowStretch;
        /// <summary>
        /// Gibt an, ob beim Berechnen die Struktur an das Bild angepasst werden darf.
        /// </summary>
        [ProtoMember(2)]
        public bool allowStretch
        {
            get
            {
                return _allowStretch;
            }

            set
            {
                _allowStretch = value;
                lastValid = false;
            }
        }
        #endregion
        protected GenStructHelper shapes;
        #region constructors
        /// <summary>
        /// Erzeugt einen RectangleDominoProvider (Basiskonstruktor) mit den angegebenen Eigenschaften.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="colors"></param>
        /// <param name="comp"></param>
        /// <param name="useOnlyMyColors"></param>
        /// <param name="filter"></param>
        /// <param name="averageMode"></param>
        /// <param name="allowStretch"></param>
        protected RectangleDominoProvider(string imagePath, string colors, IColorComparison comp, 
            AverageMode averageMode, bool allowStretch, IterationInformation iterationInformation)
            : base(imagePath, comp, colors, iterationInformation)
        {
            this.allowStretch = allowStretch;
            average = averageMode;
        }
        protected RectangleDominoProvider(int imageWidth, int imageHeight, Color background, string colors, IColorComparison comp,
            AverageMode averageMode, bool allowStretch, IterationInformation iterationInformation)
            : base(imageWidth, imageHeight, background, comp, colors, iterationInformation)
        {
            this.allowStretch = allowStretch;
            average = averageMode;
        }
        protected RectangleDominoProvider() : base() { }
        #endregion
        #region public methods
        #endregion
        #region private helper methods
        /// <summary>
        /// Weist jedem Shape die ideale Farbe zu, basierend auf den festgelegten Eigenschaften.
        /// </summary>
        /// <returns>ein Int-Array mit den Farbindizes</returns>
        internal override void CalculateColors()
        {
            var colors = this.colors.RepresentionForCalculation;
            if (!shapesValid) throw new InvalidOperationException("Current shapes are invalid!");
            IterationInformation.weights = Enumerable.Repeat(1.0, colors.Length).ToArray();
            for (int iter = 0; iter < IterationInformation.maxNumberOfIterations; iter++)
            {
                ResetDitherColors(shapes.dominoes);
                IterationInformation.numberofiterations = iter;
                Console.WriteLine($"Iteration {iter}");
                Parallel.For(0, shapes.dominoes.Length, new ParallelOptions() { MaxDegreeOfParallelism = -1 }, (i) =>
                {
                    shapes.dominoes[i].CalculateColor(colors, colorMode, TransparencySetting, IterationInformation.weights);
                });
                // Farben zählen
                IterationInformation.EvaluateSolution(colors.ToArray(), shapes.dominoes);
                if (IterationInformation.colorRestrictionsFulfilled != false) break;
            }
            last = new DominoTransfer(shapes.dominoes, this.colors);
        }
        internal override void ReadUsedColors()
        {
            using (Image<Bgra, Byte> img = image_filtered.ToImage<Bgra, Byte>())
            {
                double scalingX = (source.Width - 1) / shapes.width;
                double scalingY = (source.Height - 1) / shapes.height;
                if (!allowStretch)
                {
                    if (scalingX > scalingY) scalingX = scalingY;
                    else scalingY = scalingX;
                }
                // tatsächlich genutzte Farben auslesen
                Parallel.For(0, shapes.dominoes.Length, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, (i) =>
                {
                    if (average == AverageMode.Corner)
                    {
                        DominoRectangle container = shapes.dominoes[i].GetContainer(scalingX, scalingY);
                        shapes.dominoes[i].originalColor = 
                        new Bgra(img.Data[container.y1, container.x1, 0], img.Data[container.y1, container.x1, 1],
                            img.Data[container.y1, container.x1, 2], img.Data[container.y1, container.x1, 3]);
                    }
                    else if (average == AverageMode.Average)
                    {
                        DominoRectangle container = shapes.dominoes[i].GetContainer(scalingX, scalingY);
                        double R = 0, G = 0, B = 0, A = 0;
                        int counter = 0;

                        // for loop: each container
                        for (int x_iterator = container.x1; x_iterator <= container.x2; x_iterator++)
                        {
                            for (int y_iterator = container.y1; y_iterator <= container.y2; y_iterator++)
                            {
                                if (shapes.dominoes[i].IsInside(new Point(x_iterator, y_iterator), scalingX, scalingY))
                                {
                                    R += img.Data[container.y1, container.x1, 2];
                                    G += img.Data[container.y1, container.x1, 1];
                                    B += img.Data[container.y1, container.x1, 0];
                                    A += img.Data[container.y1, container.x1, 3];
                                    counter++;
                                }
                            }
                        }
                        if (counter != 0)
                        {
                            shapes.dominoes[i].originalColor = new Bgra((byte)(B / counter), (byte)(G / counter), (byte)(R / counter), (byte)(A / counter));
                        }
                        else // rectangle too small
                        {
                            shapes.dominoes[i].originalColor = new Bgra(img.Data[container.y1, container.x1, 0], img.Data[container.y1, container.x1, 1],
                            img.Data[container.y1, container.x1, 2], img.Data[container.y1, container.x1, 3]);
                        }
                    }
                });
            }
        }
        #endregion
    }

}
