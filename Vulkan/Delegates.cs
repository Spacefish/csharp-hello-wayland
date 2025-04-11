using Evergine.Bindings.Vulkan;

public unsafe class Delegates {
    public delegate VkResult vkGetMemoryFdKHRDelegate(VkDevice device, VkMemoryGetFdInfoKHR* pGetFdInfo, int* pFd);
}