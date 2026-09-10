using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StratoDomainDDNSChanger;
using StratoDomainDDNSChanger.Core;
class Tests
{
    static int checks;
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
    static Settings Sample() => new Settings {
        UpdateUrl = "https://provider.example/update?mx=nochg", Username = "example.com", Password = "test-secret-unique",
        IPv4LookupUrl = "https://lookup.example/v4", IPv6LookupUrl = "https://lookup.example/v6", AutoStart = false,
        Hosts = new List<HostEntry> {
            new HostEntry { Hostname="example.com", Mode=AddressMode.IPv4AndIPv6 },
            new HostEntry { Hostname="game.example.com", Mode=AddressMode.IPv4Only },
            new HostEntry { Hostname="ipv6.example.com", Mode=AddressMode.IPv6Only }
        }
    };
    sealed class Fake : HttpMessageHandler
    {
        public string V4="192.0.2.1", V6="2001:db8::1";
        public string FailHost, FailureCode="dnserr";
        public bool FailLookup6;
        public readonly List<Tuple<string,string>> Updates = new List<Tuple<string,string>>();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if(request.RequestUri.Host=="lookup.example") {
                Assert(request.Headers.Authorization==null,"Credentials leaked to lookup");
                return Task.FromResult(new HttpResponseMessage(request.RequestUri.AbsolutePath=="/v6" && FailLookup6 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
                  { Content=new StringContent(request.RequestUri.AbsolutePath=="/v4" ? V4 : V6) });
            }
            var query=HttpUtility.ParseQueryString(request.RequestUri.Query);
            Assert(query["mx"]=="nochg","Existing query lost");
            Assert(request.Headers.Authorization?.Scheme=="Basic","Missing update auth");
            Updates.Add(Tuple.Create(query["hostname"],query["myip"]));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content=new StringContent(query["hostname"]==FailHost ? FailureCode : "good "+query["myip"]) });
        }
    }
    static async Task NetworkTests()
    {
        var fake=new Fake();
        using(var service=new UpdateService(Sample(),fake)) {
            await service.CheckAsync(CancellationToken.None);
            Assert(fake.Updates.Count==3,"Initial update must publish each host");
            Assert(fake.Updates[0].Item2=="192.0.2.1,2001:db8::1","Dual mode");
            Assert(fake.Updates[1].Item2=="192.0.2.1","Game must not publish IPv6");
            Assert(fake.Updates[2].Item2=="2001:db8::1","IPv6 mode");
            await service.CheckAsync(CancellationToken.None);
            Assert(fake.Updates.Count==3,"Unchanged addresses should not republish");
            fake.V6="2001:db8::2";
            await service.CheckAsync(CancellationToken.None);
            Assert(fake.Updates.Count==5 && fake.Updates.Skip(3).All(u=>u.Item1!="game.example.com"),"IPv6 change should skip IPv4-only game");
            fake.V4="192.0.2.2"; fake.FailLookup6=true;
            await service.CheckAsync(CancellationToken.None);
            Assert(fake.Updates.Count==6 && fake.Updates.Last().Item1=="game.example.com","Failed IPv6 lookup must not block game update");
        }
        fake=new Fake{FailHost="game.example.com"};
        using(var service=new UpdateService(Sample(),fake)) {
            await service.CheckAsync(CancellationToken.None);
            fake.FailHost=null;
            await service.CheckAsync(CancellationToken.None);
            Assert(fake.Updates.Count==4 && fake.Updates.Last().Item1=="game.example.com","Retry failed host even with unchanged IP");
        }
        fake=new Fake{FailHost="game.example.com",FailureCode="badauth"};
        using(var service=new UpdateService(Sample(),fake)) {
            await service.CheckAsync(CancellationToken.None); await service.CheckAsync(CancellationToken.None);
            Assert(fake.Updates.Count==3,"Do not repeat invalid credentials automatically");
            var canceled=new CancellationToken(true);
            bool sawCancel=false;
            try {await service.CheckAsync(canceled);} catch(OperationCanceledException){sawCancel=true;}
            Assert(sawCancel,"Canceled check must stop");
        }
        fake=new Fake{FailHost="example.com",FailureCode="911"};
        using(var service=new UpdateService(Sample(),fake)) {
            await service.CheckAsync(CancellationToken.None); await service.CheckAsync(CancellationToken.None);
            Assert(fake.Updates.Count==1,"Provider outage requires backoff");
        }
    }
    static void ConfigTests(string dir)
    {
        var configPath=Path.Combine(dir,"config-test.json");
        var store=new ConfigStore(configPath);
        var config=Sample();
        store.Save(config);
        var raw=File.ReadAllText(configPath);
        Assert(!raw.Contains(config.Password) && !raw.Contains("\"Password\":"),"Saved plaintext password");
        var roundtrip=store.Load();
        Assert(roundtrip.Password==config.Password && roundtrip.Hosts[1].Mode==AddressMode.IPv4Only,"Settings round trip");
        store.Save(roundtrip);
        Assert(!File.Exists(configPath+".tmp"),"Temporary settings file left behind");
        foreach(var value in new[]{"a.example:19132","https://example.com","a.example,b.example","192.0.2.1","-bad.example"}) {
            bool rejected=false;try{Protocol.NormalizeHostname(value);}catch(ArgumentException){rejected=true;}
            Assert(rejected,"Invalid hostname accepted");
        }
        Assert(Protocol.NormalizeHostname(" Game.Example.COM. ")=="game.example.com","Hostname normalization");
        Assert(!Protocol.Success(Protocol.ResponseCode("goodness")) && !Protocol.Success(Protocol.ResponseCode("good 1\nbadauth")),"Malformed success accepted");
        var duplicate=Sample();duplicate.Hosts.Add(new HostEntry{Hostname="EXAMPLE.COM"});
        bool duplicateRejected=false;try{duplicate.Validate();}catch(ArgumentException){duplicateRejected=true;}
        Assert(duplicateRejected,"Duplicate host accepted");
    }
    static void RenderUi(string dir)
    {
        var store=new ConfigStore(Path.Combine(dir,"config-test.json"));
        var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
        var window=new MainWindow(store, Sample());
        window.Width=1000;window.Height=800;
        var tabs=(TabControl)window.FindName("MainTabs");
        var root=(FrameworkElement)window.Content;
        window.Content=null;
        root.Resources=window.Resources;
        for(int index=0;index<2;index++) {
            tabs.SelectedIndex=index;
            root.Measure(new Size(1000,800));root.Arrange(new Rect(0,0,1000,800));root.UpdateLayout();
            var bitmap=new RenderTargetBitmap(1000,800,96,96,PixelFormats.Pbgra32);
            bitmap.Render(root);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using(var stream=File.Create(Path.Combine(dir,index==0?"status.png":"settings.png")))encoder.Save(stream);
        }
        Assert(((DataGrid)window.FindName("HostsGrid")).Items.Count==3,"UI hostname rows");
        Assert(((PasswordBox)window.FindName("PasswordInput")).Password=="test-secret-unique","UI password loading");
        app.Shutdown();
    }
    [STAThread] static int Main(string[] args)
    {
        try {
            if(args.Length>0 && args[0]=="--live-check") {
                var settings=new ConfigStore().Load();
                using(var service=new UpdateService(settings)) {
                    var accepted=new HashSet<string>();
                    service.HostStatus+=(host,status)=> {
                        Console.WriteLine(host+": "+status);
                        if(status.StartsWith("Accepted (")) accepted.Add(host);
                    };
                    service.CheckAsync(CancellationToken.None).GetAwaiter().GetResult();
                    if(accepted.Count!=settings.Hosts.Count) throw new Exception("Not all configured hostnames were accepted by the provider.");
                    service.CheckAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                return 0;
            }
            if(args.Length==4 && args[0]=="--migrate") {
                var legacy=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(args[1]));
                var settings=new Settings {
                    Username=(string)legacy["UserName"],Password=(string)legacy["Password"],
                    UpdateUrl=(string)legacy["UpdateUrl"],AutoStart=true,
                    Hosts=new List<HostEntry> {
                        new HostEntry{Hostname=args[2],Mode=AddressMode.IPv4AndIPv6},
                        new HostEntry{Hostname=args[3],Mode=AddressMode.IPv4Only}
                    }
                };
                var store=new ConfigStore();
                if(File.Exists(store.FilePath))throw new Exception("Destination config already exists; inspect before replacing.");
                store.Save(settings);
                var verified=store.Load();
                if(verified.Password!=settings.Password || verified.Hosts.Count!=2)
                    throw new Exception("Migrated settings could not be read back.");
                Console.WriteLine("Migrated settings to "+store.FilePath+" with Windows-protected password.");
                return 0;
            }
            var dir=Path.GetFullPath(args.Length>0?args[0]:"artifacts");
            Directory.CreateDirectory(dir);
            bool skipProtection = args.Contains("--skip-protection");
            if (!skipProtection) ConfigTests(dir);
            NetworkTests().GetAwaiter().GetResult();RenderUi(dir);
            if (skipProtection) Console.WriteLine("Windows DPAPI/config persistence tests SKIPPED; require a loaded Windows user profile.");
            Console.WriteLine("PASS: "+checks+" assertions in the selected tests.");
            return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex.GetType().Name+": "+ex.Message);return 1;}
    }
}
