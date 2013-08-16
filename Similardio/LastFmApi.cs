using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace Similardio
{
	public class LastFmApi
	{
		const string SimilarArtistApi = "https://ws.audioscrobbler.com/2.0/?method=artist.getsimilar&artist={1}&api_key={0}&limit={2}";

		HttpClient client;

		public LastFmApi ()
		{
			client = new HttpClient ();
		}

		public async Task<IList<ArtistData>> GetSimilarArtistsAsync (string artist, int limit = 5)
		{
			var url = GetApiUrl (artist, limit);
			var content = await client.GetStringAsync (url).ConfigureAwait (false);
			var doc = XDocument.Parse (content);
			var artists = doc.Root.Element ("similarartists").Elements ("artist");

			return artists.Select (a => new ArtistData {
				Name = a.Element ("name").Value,
				Match = double.Parse (a.Element ("match").Value, CultureInfo.InvariantCulture),
				MbID = a.Element ("mbid").Value,
				PictureUrl = a.Elements ("image").First (i => ((string)i.Attribute ("size")) == "mega").Value,
			}).ToArray ();
		}

		static string GetApiUrl (string artist, int limit)
		{
			return string.Format (SimilarArtistApi, Keys.LastFmApiKey, artist, limit.ToString ());
		}
	}
}

