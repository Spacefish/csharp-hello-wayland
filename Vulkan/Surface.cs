using Evergine.Bindings.Vulkan;
using WaylandSharp;

public unsafe partial class Engine {

    public VkSurfaceKHR CreateWaylandSurface(WlDisplay wlDisplay, WlSurface wlSurface)
    {
        VkWaylandSurfaceCreateInfoKHR surfaceCreateInfo = new VkWaylandSurfaceCreateInfoKHR
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_WAYLAND_SURFACE_CREATE_INFO_KHR,
            display = wlDisplay.RawPointer,
            surface = wlSurface.RawPointer
        };

        VkSurfaceKHR surface;
        Helpers.CheckErrors(VulkanNative.vkCreateWaylandSurfaceKHR(Instance, &surfaceCreateInfo, null, &surface));
        return surface;
    }
}