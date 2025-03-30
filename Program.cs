using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
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
Console.WriteLine("Created xdg surface!");

var xdgToplevel = xdgSurface.GetToplevel();
xdgToplevel.SetTitle("Hello World");
xdgToplevel.SetAppId("com.example.helloworld");

xdgSurface.SetWindowGeometry(0, 0, 1024, 1024);

xdgToplevel.Configure += (_, d) =>
{
    Console.WriteLine($"Toplevel Configure: {d.GetType()} {d.Width} {d.Height} StatesSize: {d.States.Size}");
};

xdgSurface.Configure += (_, d) =>
{
    Console.WriteLine($"Configure: {d.GetType()}");
    // xdgSurface.SetWindowGeometry(d.X, d.Y, d.Width, d.Height);
    xdgSurface.AckConfigure(d.Serial);
    // xdgToplevel.Resize(wlSeat, d.Serial, XdgToplevelResizeEdge.Top);
};


mySurface.Commit();
Console.WriteLine("Committed surface!");

MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, 1024*1024);

var pool = wlShm.CreatePool(mmf.SafeMemoryMappedFileHandle.DangerousGetHandle().ToInt32(), 1024*1024);
if(pool == null)
    throw new NullReferenceException("Failed to create pool");

var wlBuffer = pool.CreateBuffer(0, 128, 128, 128*3, WlShmFormat.Rgb888);

wlDisplay.Dispatch();

wlBuffer.Release += (_, d) =>
{
    Console.WriteLine($"Buffer released: {d}");
};

unsafe {
    var va = mmf.CreateViewAccessor();
    var pointer = (byte*)va.SafeMemoryMappedViewHandle.DangerousGetHandle().ToPointer();
    var span = new Span<byte>(pointer, 128*128*3);
    while(true) {    
        span.Fill((byte)Random.Shared.Next());
        /*
        for(int i = 0; i < span.Length; i++) {
            span[i] = (byte)Random.Shared.Next();;
            // span[i] = (byte)Random.Shared.Next();
        }
        */

        mySurface.Attach(wlBuffer, 0, 0);
        mySurface.Damage(0,0, 128,128);
        mySurface.Commit();

        wlDisplay.Roundtrip();
        Thread.Sleep(TimeSpan.FromMilliseconds(20));
        // Console.WriteLine("Dispatching...");
    }
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