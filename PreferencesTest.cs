using System;
using System.IO;
class PreferencesTest {
    static void Check(bool ok) { if(!ok) throw new Exception("Preference test failed"); }
    static void Main(string[] args) {
        string path = Path.Combine(Path.GetFullPath(args[0]), "preferences-test-" + Guid.NewGuid().ToString("N") + ".xml");
        try {
            Check(Preferences.Load(path).Quality == 2);
            for(int i=0;i<6;i++) {
                new Preferences { Quality=i, Stable=i%2==0, Name="AirLink-测试" }.Save(path);
                var actual=Preferences.Load(path);
                Check(actual.Quality==i && actual.Stable==(i%2==0) && actual.Name=="AirLink-测试");
            }
            File.WriteAllText(path,"<AirLink quality='999' stable='bad' name='-bad'/>");
            var fallback=Preferences.Load(path); Check(fallback.Quality==2 && fallback.Stable && Preferences.ValidName(fallback.Name));
            File.WriteAllText(path,"<broken"); Check(Preferences.Load(path).Quality==2);
            Console.WriteLine("PASS: defaults, six modes, overwrite, Unicode name, invalid values, damaged XML");
        } finally { if(File.Exists(path)) File.Delete(path); }
    }
}
