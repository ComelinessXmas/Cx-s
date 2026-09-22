using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

sealed class Preferences {
    public int Quality = 2;
    public bool Stable = true;
    public string Name = "AirLink-" + Environment.MachineName;
    public static bool ValidName(string value) { return value != null && Regex.IsMatch(value, @"^[\p{L}\p{N}_.][\p{L}\p{N}_.-]{0,47}$"); }
    public static Preferences Load(string path) {
        var result = new Preferences();
        try {
            var doc = new XmlDocument { XmlResolver = null };
            using(var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null })) doc.Load(reader);
            var root = doc.DocumentElement;
            if(root == null || root.Name != "AirLink") return result;
            int quality; bool stable;
            if(int.TryParse(root.GetAttribute("quality"), out quality) && quality >= 0 && quality < 6) result.Quality = quality;
            if(bool.TryParse(root.GetAttribute("stable"), out stable)) result.Stable = stable;
            if(ValidName(root.GetAttribute("name"))) result.Name = root.GetAttribute("name");
        } catch(IOException) {} catch(UnauthorizedAccessException) {} catch(XmlException) {}
        return result;
    }
    public void Save(string path) {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            using(var writer = XmlWriter.Create(temporary, new XmlWriterSettings { Indent = true })) {
                writer.WriteStartElement("AirLink");
                writer.WriteAttributeString("quality", Quality.ToString());
                writer.WriteAttributeString("stable", Stable.ToString());
                writer.WriteAttributeString("name", Name);
                writer.WriteEndElement();
            }
            if(File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
        } finally { if(File.Exists(temporary)) File.Delete(temporary); }
    }
}
