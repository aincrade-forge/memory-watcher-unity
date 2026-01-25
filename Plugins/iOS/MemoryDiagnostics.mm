// File: Plugins/iOS/MemoryDiagnostics.mm
// iOS native plugin for Unity - Memory Diagnostics
// Minimum iOS: 10.0

#import <Foundation/Foundation.h>
#include <mach/mach.h>

static long long ReadFootprintBytes()
{
    // Prefer TASK_VM_INFO.phys_footprint; fallback to resident_size
    task_vm_info_data_t vmInfo = {};
    mach_msg_type_number_t count = TASK_VM_INFO_COUNT;
    kern_return_t kr = task_info(mach_task_self_, TASK_VM_INFO, (task_info_t)&vmInfo, &count);

    long long footprint = 0;
    if (kr == KERN_SUCCESS) {
        if (vmInfo.phys_footprint > 0) {
            footprint = (long long)vmInfo.phys_footprint;
        } else if (vmInfo.resident_size > 0) {
            footprint = (long long)vmInfo.resident_size;
        }
    }

    if (footprint <= 0) {
        // Final fallback using mach_task_basic_info
        mach_task_basic_info_data_t basicInfo = {};
        mach_msg_type_number_t basicCount = MACH_TASK_BASIC_INFO_COUNT;
        if (task_info(mach_task_self_, MACH_TASK_BASIC_INFO, (task_info_t)&basicInfo, &basicCount) == KERN_SUCCESS) {
            footprint = (long long)basicInfo.resident_size;
        }
    }

    if (footprint < 0) {
        footprint = 0;
    }
    return footprint;
}

extern "C" {

// Returns current memory footprint in bytes
long long MD_GetMemoryFootprintBytes(void)
{
    @autoreleasepool {
        return ReadFootprintBytes();
    }
}

}
