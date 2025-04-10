using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using WaylandSharp;

var wlDisplay = WlDisplay.Connect();
var wlRegistry = wlDisplay.GetRegistry();

WlCompositor? wlCompositor = null;
XdgWmBase? xdgWmBase = null;
WlShm? wlShm = null;
ZxdgDecorationManagerV1? zxdgDecorationManager = null;

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
    if(ea.Interface == WlInterface.ZxdgDecorationManagerV1.Name) {
        zxdgDecorationManager = wlRegistry.Bind<ZxdgDecorationManagerV1>(ea.Name, ea.Interface, ea.Version);
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
if(zxdgDecorationManager == null) {
    Console.WriteLine("Warning: No decoration manager found, using client side decorations / no decorations");
}

// handle pings so the compositor does not kill us
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
    Console.WriteLine($"Configure: {d.GetType()}");
    xdgSurface.AckConfigure(d.Serial);
};

// we are a toplevel surface
var xdgToplevel = xdgSurface.GetToplevel();
xdgToplevel.SetTitle("Hello Wayland");
xdgToplevel.SetAppId("de.neosolve.hellowayland");
xdgToplevel.Configure += (_, d) =>
{
    Console.WriteLine($"Toplevel Configure:{d.Width}x{d.Height}");
};

// handle window close requests
bool doExit = false;
xdgToplevel.Close += (_, d) =>
{
    Console.WriteLine($"Toplevel Close: {d.GetType()}");
    doExit = true;
};

// request Server Side decorations (does not work with mutter/GNOME as it does not support server side decorations)
if(zxdgDecorationManager != null) {
    var topLevelDecoration = zxdgDecorationManager.GetToplevelDecoration(xdgToplevel);
    topLevelDecoration.SetMode(ZxdgToplevelDecorationV1Mode.ServerSide);
}

// we want a 1024x768 window
var width = 1024;
var height = 768;

// allocate a shared memory buffer
var bufferSize = width * height * 3;
MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, bufferSize);

// create the shared memory pool
var pool = wlShm.CreatePool(mmf.SafeMemoryMappedFileHandle.DangerousGetHandle().ToInt32(), bufferSize);
if(pool == null)
    throw new NullReferenceException("Failed to create pool");

// request the buffer
var wlBuffer = pool.CreateBuffer(0, width, height, width * 3, WlShmFormat.Rgb888);

// get Span from buffer pointer
Span<byte> buffer;
unsafe {
    var va = mmf.CreateViewAccessor();
    var pointer = (byte*)va.SafeMemoryMappedViewHandle.DangerousGetHandle().ToPointer();
    buffer = new Span<byte>(pointer, bufferSize);
}

// comit changes to surface and then dispatch the configure callback
mySurface.Commit();
wlDisplay.Dispatch();

while(!doExit) {    
    // write random bytes to buffer so we see something in the windows instead of black
    Random.Shared.NextBytes(buffer);
    
    mySurface.Attach(wlBuffer, 0, 0);
    mySurface.Damage(0,0, width,height);
    mySurface.Commit();

    wlDisplay.Roundtrip();

    Thread.Sleep(TimeSpan.FromMilliseconds(20));
}