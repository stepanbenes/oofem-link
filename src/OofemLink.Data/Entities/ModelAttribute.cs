using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;

namespace OofemLink.Data.Entities
{
    public class ModelAttribute : IModelEntity
    {
		public int ModelId { get; set; }
		public virtual Model Model { get; set; }
		public int Id { get; set; }

		public int? LocalNumber { get; set; }

		public AttributeType Type { get; set; }

		[Required]
		public string Name { get; set; }

		public string Parameters { get; set; }

		public int? TimeFunctionId { get; set; }

		public virtual TimeFunction TimeFunction { get; set; }

		public virtual ICollection<AttributeComposition> ParentAttributes { get; set; } = new List<AttributeComposition>();
		public virtual ICollection<AttributeComposition> ChildAttributes { get; set; } = new List<AttributeComposition>();

		public virtual ICollection<VertexAttribute> VertexAttributes { get; set; } = new List<VertexAttribute>();
		public virtual ICollection<CurveAttribute> CurveAttributes { get; set; } = new List<CurveAttribute>();
		public virtual ICollection<SurfaceAttribute> SurfaceAttributes { get; set; } = new List<SurfaceAttribute>();
		public virtual ICollection<VolumeAttribute> VolumeAttributes { get; set; } = new List<VolumeAttribute>();

		#region NotMapped helper properties

		[NotMapped]
		public virtual ICollection<MacroAttribute> MacroAttributes { get; set; } = new List<MacroAttribute>();
		[NotMapped]
		public bool AppliesToAllMacros { get; set; }

		#endregion

		#region GetHashCode & Equals

		public override int GetHashCode() => Id;

		public override bool Equals(object obj)
		{
			var other = obj as ModelAttribute;
			if (other == null)
				return false;
			return this.ModelId == other.ModelId && this.Id == other.Id;
		}

		#endregion
	}
}
