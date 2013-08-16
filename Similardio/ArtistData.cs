using System;

namespace Similardio
{
	public struct ArtistData : IEquatable<ArtistData>
	{
		public string Name { get; set; }
		public double Match { get; set; }
		public string MbID { get; set; }
		public string PictureUrl { get; set; }
		public string[] SimilarArtists { get; set; }

		public bool Equals (ArtistData other)
		{
			return MbID == other.MbID;
		}

		public override bool Equals (object obj)
		{
			return obj is ArtistData && Equals ((ArtistData)obj);
		}

		public override int GetHashCode ()
		{
			return MbID.GetHashCode ();
		}
	}
}

