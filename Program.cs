using System.IO.MemoryMappedFiles;
using WaylandSharp;

var wlDisplay = WlDisplay.Connect();
var wlRegistry = wlDisplay.GetRegistry();

WlCompositor? wlCompositor = null;
WlSeat? wlSeat = null;
XdgWmBase? xdgWmBase = null;
WlShm? wlShm = null;


ZwpTextInputManagerV3? textInputManager = null;
ZwpTextInputV3? textInput = null;


/*
wlDisplay.Error += (_, ea) =>
{
    Console.WriteLine($"WlDisplay Error: COde: {ea.Code} Message: {ea.Message}");
};
*/

wlRegistry.Global += (_, ea) =>
{
    if(ea.Interface == WlInterface.WlCompositor.Name) {
        wlCompositor = wlRegistry.Bind<WlCompositor>(ea.Name, ea.Interface, ea.Version);
        Console.WriteLine($"Found: {ea.Name} {ea.Interface} {ea.Version}");
    }
    if(ea.Interface == WlInterface.XdgWmBase.Name) {
        xdgWmBase = wlRegistry.Bind<XdgWmBase>(ea.Name, ea.Interface, ea.Version);
        Console.WriteLine($"Found: {ea.Name} {ea.Interface} {ea.Version}");
    }
    if(ea.Interface == WlInterface.WlShm.Name) {
        wlShm = wlRegistry.Bind<WlShm>(ea.Name, ea.Interface, ea.Version);
        Console.WriteLine($"Found: {ea.Name} {ea.Interface} {ea.Version}");
    }

    if(ea.Interface == WlInterface.WlSeat.Name) {
        wlSeat = wlRegistry.Bind<WlSeat>(ea.Name, ea.Interface, ea.Version);
        Console.WriteLine($"Found: {ea.Name} {ea.Interface} {ea.Version}");
    }
    if(ea.Interface == WlInterface.ZwpTextInputManagerV3.Name) {
        textInputManager = wlRegistry.Bind<ZwpTextInputManagerV3>(ea.Name, ea.Interface, ea.Version);
        // tim.GetTextInput();
        Console.WriteLine($"Found: {ea.Name} {ea.Interface} {ea.Version}");
    }
};
wlDisplay.Roundtrip();

if(wlCompositor == null)
    throw new NotSupportedException($"Wayland registry did not advertise {WlInterface.WlCompositor.Name} found");
if(xdgWmBase == null)
    throw new NotSupportedException($"Wayland registry did not advertise {WlInterface.XdgWmBase.Name} found");
if(wlShm == null)
    throw new NotSupportedException($"Wayland registry did not advertise {WlInterface.WlShm.Name} found");

if(wlSeat == null)
    throw new NotSupportedException($"Wayland registry did not advertise {WlInterface.WlSeat.Name} found");
if(textInputManager == null)
    throw new NotSupportedException($"Wayland registry did not advertise {WlInterface.ZwpTextInputManagerV3.Name} found");


xdgWmBase.Ping += (_, d) =>
{
    Console.WriteLine($"Ping: {d.Serial}");
    xdgWmBase.Pong(d.Serial);
};


var mySurface = wlCompositor.CreateSurface();
Console.WriteLine("Created surface!");

var xdgSurface = xdgWmBase.GetXdgSurface(mySurface);
xdgSurface.Configure += (_, d) =>
{
    Console.WriteLine($"Configure: {d.Serial}");
    xdgSurface.AckConfigure(d.Serial);
};
Console.WriteLine("Created xdg surface!");

var xdgToplevel = xdgSurface.GetToplevel();
xdgToplevel.SetTitle("Hello World");
xdgToplevel.SetAppId("com.example.helloworld");
// xdgToplevel.SetMinSize(100, 100);
// xdgToplevel.SetParent(null);


mySurface.Commit();
Console.WriteLine("Committed surface!");

MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, 1024*1024);
// MemoryMappedViewStream stream = mmf.CreateViewStream(0, 200, MemoryMappedFileAccess.ReadWrite);

var pool = wlShm.CreatePool(mmf.SafeMemoryMappedFileHandle.DangerousGetHandle().ToInt32(), 1024*1024);
if(pool == null)
    throw new NullReferenceException("Failed to create pool");

var wlBuffer = pool.CreateBuffer(0, 16, 16, 48, WlShmFormat.Argb8888);


mySurface.Attach(wlBuffer, 0, 0);

wlDisplay.Dispatch();

while(true) {
    mySurface.Damage(0, 0, 16, 16);
    mySurface.Frame();    
    Thread.Sleep(100);
    Console.WriteLine("Dispatching...");
    wlDisplay.Roundtrip();
}


textInput = textInputManager.GetTextInput(wlSeat)
    ?? throw new NullReferenceException("Failed to get text input");

textInput.CommitString += (_, d) => { Console.WriteLine($"CommitString: {d.Text}"); };
textInput.DeleteSurroundingText += (_, d) => { Console.WriteLine($"DeleteSurroundingText: {d.BeforeLength} {d.AfterLength}"); };
textInput.Done += (_, d) => { Console.WriteLine($"Done: {d.Serial}"); };
textInput.Enter += (_, d) => { Console.WriteLine($"Enter: {d.Surface}"); };
textInput.Leave += (_, d) => { Console.WriteLine($"Leave: {d.Surface}"); };
textInput.PreeditString += (_, d) => { Console.WriteLine($"PreeditString: {d.Text} {d.CursorBegin} {d.CursorEnd}"); };
textInput.Enable();

// var tim = WlInterface.FromInterfaceName("zwp_text_input_manager_v3");

Console.ReadLine();
Console.WriteLine("HERE");
// zwp_text_input_manager_v3