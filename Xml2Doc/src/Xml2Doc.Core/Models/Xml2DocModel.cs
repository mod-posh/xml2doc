using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Xml2Doc.Core.Models
{
    public sealed class Xml2Doc
    {
        public Dictionary<string, XMember> Members { get; } = new(StringComparer.Ordinal);

        public static Xml2Doc Load(string xmlPath)
        {
            var doc = XDocument.Load(xmlPath, LoadOptions.PreserveWhitespace);
            var model = new Xml2Doc();
            foreach (var m in doc.Descendants("member"))
            {
                var name = (string?)m.Attribute("name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                model.Members[name!] = new XMember(name!, m);
            }
            return model;
        }
    }

    public sealed record XMember(string Name, XElement Element)
    {
        public string Kind => Name.Split(':')[0]; // T, M, P, F, E, N
        public string Id => Name.Split(':')[1];
    }
}
