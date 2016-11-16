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
		POLY,        // QcPoly      polygon (lomená čára)
		ARCH,        // QcArc       kruhový oblouk
		CIRC,        // QcCirc      kružnice
		LINE,        // QcSpline    lokální kubický splajn
		BSPL,        // QcBSpline   B - spline křivka
		BEZ ,        // QcBeziere   Bezierova křivka
		ONSF,        // QcOnSurf    linie, ležící na povrchu plochy
		PIPE,        // QcPipe      povrchová linie PIPE plochy (speciální případ QcOnSurf)
		SECT,        // QcInter     křivka průniku dvou ploch (interSECTion)
		ARCN,        // ---         oblouk zadaný pomocí dvou bodů a průvěsu (pouze Nexis)
    }
}
