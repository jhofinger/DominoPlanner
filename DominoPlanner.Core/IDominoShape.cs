﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DominoPlanner.Core
{
    /// <summary>
    /// Interface für alle Formen von Steinen (Pfad-Stein, Rechteck-Stein). 
    /// Stellt Methoden wie IsInside, GetContainer oder GetPath bereit.
    /// In dieser Klasse wird nur die Form angegeben, die Verknüpfung mit der Farbe erfolgt in 
    /// </summary>
    public abstract class IDominoShape : IEquatable<IDominoShape>
    {
        /// <summary>
        /// Gibt an, ob der Stein eine Protokolldefinition enthält
        /// </summary>
        public bool hasProtocolDefinition { get; set; }
        /// <summary>
        /// Gibt an, ob der Stein eine verschiebbare Protokolldefinition enthält (z.B. bei Feldern oder Walls)
        /// </summary>
        public bool hasTransformableProtocolDefinition
        {
            get
            {
                return (position != null && position.xParams != null && position.yParams != null);
            }
        }
        /// <summary>
        /// Die ProtocolDefinition des Steins.
        /// </summary>
        public ProtocolDefinition position;
        /// <summary>
        /// Gibt die Grenze eines Steins als Punktliste zurück.  
        /// </summary>
        /// <param name="scaling_x">Multiplikator in x-Richtung</param>
        /// <param name="scaling_y">Multiplikator in y-Richtung</param>
        /// <returns></returns>
        public abstract DominoPath GetPath(double scaling_x, double scaling_y);
        /// <summary>
        /// Gibt die Grenze eines Steins als Punktliste zurück, mit seitenverhältniserhaltender Skalierung.
        /// </summary>
        /// <param name="scaling">Skalierungsfaktor</param>
        /// <returns></returns>
        public DominoPath GetPath(double scaling = 1) { return GetPath(scaling, scaling); }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scaling_x">Multiplikator in x-Richtung</param>
        /// <param name="scaling_y">Multiplikator in y-Richtung</param>
        /// <returns></returns>
        public abstract DominoRectangle GetContainer(double scaling_x, double scaling_y);
        /// <summary>
        /// Gibt den Container des Steins als Rechteck zurück, mit seitenverhältniserhaltender Skalierung.
        /// Das Rechteck berührt den Stein, sodass das Rechteck alle Punkte des Steins enthält.
        /// </summary>
        /// <param name="scaling">Skalierungsfaktor</param>
        /// <returns></returns>
        public DominoRectangle GetContainer(double scaling = 1) { return GetContainer(scaling, scaling); }
        /// <summary>
        /// Überprüft, ob ein Punkt innerhalb des Steins liegt.
        /// </summary>
        /// <param name="point">Punkt, der geprüft werden soll</param>
        /// <param name="scaling_x">Multiplikator in x-Richtung</param>
        /// <param name="scaling_y">Multiplikator in y-Richtung</param>
        /// <returns></returns>
        public abstract bool IsInside(System.Windows.Point point, double scaling_x, double scaling_y);
        /// <summary>
        /// Überprüft, ob ein Punkt innerhalb des Steins liegt, mit seitenverhältniserhaltender Skalierung.
        /// </summary>
        /// <param name="point">Punkt, der geprüft werden soll</param>
        /// <param name="scaling">Skalierungsfaktor</param>
        /// <returns></returns>
        public bool IsInside(System.Windows.Point point, double scaling = 1) { return IsInside(point, scaling, scaling); }
        /// <summary>
        /// Überprüft, ob zwei Dominosteine gleich sind. 
        /// Berücksichtigt keine Unterschiede in der Protokolldefinition
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public abstract bool Equals(IDominoShape other);
        /// <summary>
        /// Verschiebt einen Stein um angegebene Koordinaten und transformiert die Protokolldefinition in die neue Position.
        /// Dabei wird die endgültige Position im Feldplan angegeben.
        /// </summary>
        /// <param name="moveX">Wert, um den alle x-Koordinaten des Steins verschoben werden</param>
        /// <param name="moveY">Wert, um den alle y-Koordinaten des Steins verschoben werden</param>
        /// <param name="i">Index des Blocks (horizontal), z.B. in einer Wall</param>
        /// <param name="j">Index des Blocks (vertikal), z.B. in einer Wall</param>
        /// <param name="width">Anzahl der horizontalen Blöcke</param>
        /// <param name="height">Anzahl der vertikalen Blöcke</param>
        /// <returns></returns>
        public abstract IDominoShape TransformDomino(double moveX, double moveY, int i, int j, int width, int height);
        /// <summary>
        /// Erstellt ein neues DominoShape und initialisiert die Protokolldefinition
        /// </summary>
        public IDominoShape()
        {
            position = new ProtocolDefinition();
        }
        /// <summary>
        /// Verschiebt das Protokoll. Dabei wird die endgültige Position im Feldplan errechnet.
        /// </summary>
        /// <param name="i">Index des Blocks (horizontal), z.B. in einer Wall</param>
        /// <param name="j">Index des Blocks (vertikal), z.B. in einer Wall</param>
        /// <param name="width">Anzahl der horizontalen Blöcke</param>
        /// <param name="height">Anzahl der vertikalen Blöcke</param>
        /// <returns></returns>
        internal ProtocolDefinition TransformProtocol(int i, int j, int width, int height)
        {
            if (hasTransformableProtocolDefinition)
            {
                return position.FinalizeProtocol(i, j, width, height);
            }
            else
            {
                // if not clusterStructure: x and y of position might be set, but the protocol parameters are not!
                return position;
            }
        }
        /// <summary>
        /// Lädt ein DominoShape aus einer XML-Strukturdefinition
        /// </summary>
        /// <param name="domino">XML-Tag, das die Informationen enthält</param>
        /// <returns></returns>
        public static IDominoShape LoadDefinition(XElement domino)
        {
            IDominoShape dominoDefinition =
                (domino.Name == "RectangleDomino") ? (IDominoShape)(new RectangleDomino(domino)) 
                    : ((domino.Name == "PathDomino") ? new PathDomino(domino) : null);

            dominoDefinition.position = (domino.Elements("ProtocolDefinition").Count() > 0) ? new ProtocolDefinition(domino.Element("ProtocolDefinition")) : null;


            return dominoDefinition;
        }
    }
}
