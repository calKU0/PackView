using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PackViewApp.Helpers
{
    public static class XmlParser
    {
        public static List<string> ExtractPlaybackUris(string xmlResponse)
        {
            XNamespace ns = "http://www.hikvision.com/ver20/XMLSchema";
            var document = XDocument.Parse(xmlResponse);

            var uris = document.Descendants(ns + "searchMatchItem")
                .Select(item => item
                    .Element(ns + "mediaSegmentDescriptor")?
                    .Element(ns + "playbackURI")?.Value)
                .Where(uri => !string.IsNullOrWhiteSpace(uri))
                .Select(uri => uri)
                .ToList();

            return uris;
        }
    }
}