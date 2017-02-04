using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Common.Enumerations
{
    public enum CurveType : byte
    {
		Undefined = 0,
     // ID              Qc-OBJEKT   POPIS LINIE
		STRL = 1,    // QcDir       úsečka (STRaight Line)
		POLY = 2,    // QcPoly      polygon (lomená čára)
		ARCH = 3,    // QcArc       kruhový oblouk
		CIRC = 4,    // QcCirc      kružnice
		LINE = 5,    // QcSpline    lokální kubický splajn
		BSPL = 6,    // QcBSpline   B - spline křivka
		BEZ  = 7,    // QcBeziere   Bezierova křivka
		ONSF = 8,    // QcOnSurf    linie, ležící na povrchu plochy
		PIPE = 9,    // QcPipe      povrchová linie PIPE plochy (speciální případ QcOnSurf)
		SECT = 10,   // QcInter     křivka průniku dvou ploch (interSECTion)
		ARCN = 11,   // ---         oblouk zadaný pomocí dvou bodů a průvěsu (pouze Nexis)
    }
}
