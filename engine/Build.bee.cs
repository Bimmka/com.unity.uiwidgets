using System;
using System.Linq;
using Bee.Core;
using Bee.DotNet;
using Bee.ProjectGeneration.VisualStudio;
using Bee.VisualStudioSolution;
using static Bee.NativeProgramSupport.NativeProgramConfiguration;
using Bee.NativeProgramSupport;

class Build
{
    static void Main()
    {
        var libUIWidgets = SetupLibUIWidgets();

        var builder = new VisualStudioNativeProjectFileBuilder(libUIWidgets);
        builder = libUIWidgets.SetupConfigurations.Aggregate(
            builder,
            (current, c) => current.AddProjectConfiguration(c));

        var sln = new VisualStudioSolution();
        sln.Path = "libUIWidgets.gen.sln";
        sln.Projects.Add(builder.DeployTo("libUIWidgets.gen.vcxproj"));
        Backend.Current.AddAliasDependency("ProjectFiles", sln.Setup());
    }

    static NativeProgram SetupLibUIWidgets()
    {
        var np = new NativeProgram("libUIWidgets")
        {
            Sources =
            {
                "src/common/settings.cc",
                "src/common/settings.h",
                "src/common/task_runners.cc",
                "src/common/task_runners.h",

                "src/flow/layers/backdrop_filter_layer.cc",
                "src/flow/layers/backdrop_filter_layer.h",
                "src/flow/layers/clip_path_layer.cc",
                "src/flow/layers/clip_path_layer.h",
                "src/flow/layers/clip_rect_layer.cc",
                "src/flow/layers/clip_rect_layer.h",
                "src/flow/layers/clip_rrect_layer.cc",
                "src/flow/layers/clip_rrect_layer.h",
                "src/flow/layers/color_filter_layer.cc",
                "src/flow/layers/color_filter_layer.h",
                "src/flow/layers/container_layer.cc",
                "src/flow/layers/container_layer.h",
                "src/flow/layers/image_filter_layer.cc",
                "src/flow/layers/image_filter_layer.h",
                "src/flow/layers/layer.cc",
                "src/flow/layers/layer.h",
                "src/flow/layers/layer_tree.cc",
                "src/flow/layers/layer_tree.h",
                "src/flow/layers/opacity_layer.cc",
                "src/flow/layers/opacity_layer.h",
                "src/flow/layers/performance_overlay_layer.cc",
                "src/flow/layers/performance_overlay_layer.h",
                "src/flow/layers/physical_shape_layer.cc",
                "src/flow/layers/physical_shape_layer.h",
                "src/flow/layers/picture_layer.cc",
                "src/flow/layers/picture_layer.h",
                "src/flow/layers/platform_view_layer.cc",
                "src/flow/layers/platform_view_layer.h",
                "src/flow/layers/shader_mask_layer.cc",
                "src/flow/layers/shader_mask_layer.h",
                "src/flow/layers/texture_layer.cc",
                "src/flow/layers/texture_layer.h",
                "src/flow/layers/transform_layer.cc",
                "src/flow/layers/transform_layer.h",
                "src/flow/compositor_context.cc",
                "src/flow/compositor_context.h",
                "src/flow/embedded_views.cc",
                "src/flow/embedded_views.h",
                "src/flow/instrumentation.cc",
                "src/flow/instrumentation.h",
                "src/flow/matrix_decomposition.cc",
                "src/flow/matrix_decomposition.h",
                "src/flow/paint_utils.cc",
                "src/flow/paint_utils.h",
                "src/flow/raster_cache.cc",
                "src/flow/raster_cache.h",
                "src/flow/raster_cache_key.cc",
                "src/flow/raster_cache_key.h",
                "src/flow/rtree.cc",
                "src/flow/rtree.h",
                "src/flow/skia_gpu_object.cc",
                "src/flow/skia_gpu_object.h",
                "src/flow/texture.cc",
                "src/flow/texture.h",

                "src/lib/ui/painting/canvas.cc",
                "src/lib/ui/painting/canvas.h",
                "src/lib/ui/painting/image.cc",
                "src/lib/ui/painting/image.h",
                "src/lib/ui/painting/image_decoder.cc",
                "src/lib/ui/painting/image_decoder.h",
                "src/lib/ui/painting/image_encoding.cc",
                "src/lib/ui/painting/image_encoding.h",
                "src/lib/ui/painting/picture.cc",
                "src/lib/ui/painting/picture.h",
                "src/lib/ui/painting/picture_recorder.cc",
                "src/lib/ui/painting/picture_recorder.h",

                "src/lib/ui/io_manager.h",
                "src/lib/ui/snapshot_delegate.h",
                "src/lib/ui/ui_mono_state.cc",
                "src/lib/ui/ui_mono_state.h",
                
                "src/runtime/mono_api.cc",
                "src/runtime/mono_api.h",
                "src/runtime/mono_isolate.cc",
                "src/runtime/mono_isolate.h",
                "src/runtime/mono_isolate_scope.cc",
                "src/runtime/mono_isolate_scope.h",
                "src/runtime/mono_microtask_queue.cc",
                "src/runtime/mono_microtask_queue.h",
                "src/runtime/mono_state.cc",
                "src/runtime/mono_state.h",

                "src/shell/common/animator.cc",
                "src/shell/common/animator.h",
                "src/shell/common/persistent_cache.cc",
                "src/shell/common/persistent_cache.h",
                "src/shell/common/pipeline.cc",
                "src/shell/common/pipeline.h",
                "src/shell/common/rasterizer.cc",
                "src/shell/common/rasterizer.h",
                "src/shell/common/shell_io_manager.cc",
                "src/shell/common/shell_io_manager.h",
                "src/shell/common/surface.cc",
                "src/shell/common/surface.h",
                "src/shell/common/thread_host.cc",
                "src/shell/common/thread_host.h",
                "src/shell/common/vsync_waiter.cc",
                "src/shell/common/vsync_waiter.h",
                "src/shell/common/vsync_waiter_fallback.cc",
                "src/shell/common/vsync_waiter_fallback.h",

                "src/shell/version/version.cc",
                "src/shell/version/version.h",
                
                "src/engine.cc",
                "src/platform_base.h",
                "src/render_api.cc",
                "src/render_api.h",
                "src/render_api_d3d11.cc",
                //"src/render_api_vulkan.cc",
                //"src/render_api_opengles.cc",
            },
            OutputName = {c => $"libUIWidgets{(c.CodeGen == CodeGen.Debug ? "_d" : "")}"},
        };
        np.CompilerSettings().Add(c => c.WithCppLanguageVersion(CppLanguageVersion.Cpp17));

        np.IncludeDirectories.Add("src");
        np.IncludeDirectories.Add("third_party");

        np.Defines.Add("UIWIDGETS_ENGINE_VERSION=\\\"0.0\\\"", "SKIA_VERSION=\\\"0.0\\\"");
        
        np.Defines.Add(c => c.CodeGen == CodeGen.Debug,
            new[] {"_ITERATOR_DEBUG_LEVEL=2", "_HAS_ITERATOR_DEBUGGING=1", "_SECURE_SCL=1"});

        np.Defines.Add(c => c.CodeGen == CodeGen.Release,
            new[] {"UIWidgets_RELEASE=1"});

        np.LinkerSettings().Add(l => l.WithCustomFlags_workaround(new[] {"/DEBUG:FULL"}));

        SetupFml(np);
        SetupSkia(np);

        var toolchain = ToolChain.Store.Windows().VS2019().Sdk_17134().x64();

        var codegens = new[] {CodeGen.Debug};
        foreach (var codegen in codegens)
        {
            var config = new NativeProgramConfiguration(codegen, toolchain, lump: true);

            var builtNP = np.SetupSpecificConfiguration(config, toolchain.DynamicLibraryFormat)
                .DeployTo("build");

            builtNP.DeployTo("../Samples/UIWidgetsSamples_2019_4/Assets/Plugins/x86_64");
        }

        //
        // var npAndroid = new NativeProgram("libUIWidgets")
        // {
        //     Sources =
        //     {
        //         "src/engine.cc",
        //         "src/platform_base.h",
        //         "src/render_api.cc",
        //         "src/render_api.h",
        //         "src/render_api_vulkan.cc",
        //         "src/render_api_opengles.cc",
        //     },
        //     OutputName = {c => $"libUIWidgets{(c.CodeGen == CodeGen.Debug ? "_d" : "")}"},
        // };
        //
        // npAndroid.Defines.Add("SUPPORT_VULKAN");
        // npAndroid.CompilerSettings().Add(c => c.WithCppLanguageVersion(CppLanguageVersion.Cpp17));
        // npAndroid.IncludeDirectories.Add("third_party");
        //
        // SetupSkiaAndroid(npAndroid);
        //
        // var androidToolchain = ToolChain.Store.Android().r19().Arm64();
        //
        // foreach (var codegen in codegens)
        // {
        //     var config = new NativeProgramConfiguration(codegen, androidToolchain, lump: true);
        //
        //     // var builtNP = npAndroid.SetupSpecificConfiguration(config, androidToolchain.DynamicLibraryFormat)
        //     //     .DeployTo("build_android_arm64");
        //     //
        //     // builtNP.DeployTo("../Samples/UIWidgetsSamples_2019_4/Assets/Plugins/Android/arm64");
        //     // builtNP.DeployTo("../Samples/UIWidgetsSamples_2019_4/BuildAndroid/unityLibrary/src/main/jniLibs/arm64-v8a/");
        // }

        return np;
    }

    static void SetupFml(NativeProgram np)
    {
        var flutterRoot = Environment.GetEnvironmentVariable("FLUTTER_ROOT");
        if (string.IsNullOrEmpty(flutterRoot))
        {
            flutterRoot = Environment.GetEnvironmentVariable("USERPROFILE") + "/engine/src";
        }

        np.Defines.Add(new[]
        {
            // gn desc out\host_debug_unopt\ //flutter/fml:fml_lib defines
            "USE_OPENSSL=1",
            "__STD_C",
            "_CRT_RAND_S",
            "_CRT_SECURE_NO_DEPRECATE",
            "_HAS_EXCEPTIONS=0",
            "_SCL_SECURE_NO_DEPRECATE",
            "WIN32_LEAN_AND_MEAN",
            "NOMINMAX",
            "_ATL_NO_OPENGL",
            "_WINDOWS",
            "CERT_CHAIN_PARA_HAS_EXTRA_FIELDS",
            "NTDDI_VERSION=0x06030000",
            "PSAPI_VERSION=1",
            "WIN32",
            "_SECURE_ATL",
            "_USING_V110_SDK71_",
            "_UNICODE",
            "UNICODE",
            "_WIN32_WINNT=0x0603",
            "WINVER=0x0603",
            "_LIBCPP_ENABLE_THREAD_SAFETY_ANNOTATIONS",
            "_DEBUG",
            "FLUTTER_RUNTIME_MODE_DEBUG=1",
            "FLUTTER_RUNTIME_MODE_PROFILE=2",
            "FLUTTER_RUNTIME_MODE_RELEASE=3",
            "FLUTTER_RUNTIME_MODE_JIT_RELEASE=4",
            "FLUTTER_RUNTIME_MODE=1",
            "FLUTTER_JIT_RUNTIME=1",
        });

        np.IncludeDirectories.Add(flutterRoot);

        np.Libraries.Add(c =>
        {
            var basePath = flutterRoot + "/out/host_debug_unopt";
            return new PrecompiledLibrary[]
            {
                new StaticLibrary(basePath + "/obj/flutter/fml/fml_lib.lib"),
                new SystemLibrary("Rpcrt4.lib"),
            };
        });
    }


    static void SetupSkia(NativeProgram np)
    {
        var skiaRoot = Environment.GetEnvironmentVariable("SKIA_ROOT");
        if (string.IsNullOrEmpty(skiaRoot))
        {
            skiaRoot = Environment.GetEnvironmentVariable("USERPROFILE") + "/skia_repo/skia";
        }

        np.Defines.Add(new[]
        {
            // bin\gn desc out\Debug\ //:skia defines
            "SK_ENABLE_SPIRV_VALIDATION",
            "_CRT_SECURE_NO_WARNINGS",
            "_HAS_EXCEPTIONS=0",
            "WIN32_LEAN_AND_MEAN",
            "NOMINMAX",
            "SK_GAMMA_APPLY_TO_A8",
            "SK_ALLOW_STATIC_GLOBAL_INITIALIZERS=1",
            "GR_TEST_UTILS=1",
            "SKIA_IMPLEMENTATION=1",
            "SK_GL",
            "SK_ENABLE_DUMP_GPU",
            "SK_SUPPORT_PDF",
            "SK_CODEC_DECODES_JPEG",
            "SK_ENCODE_JPEG",
            "SK_SUPPORT_XPS",
            "SK_ENABLE_ANDROID_UTILS",
            "SK_USE_LIBGIFCODEC",
            "SK_HAS_HEIF_LIBRARY",
            "SK_CODEC_DECODES_PNG",
            "SK_ENCODE_PNG",
            "SK_ENABLE_SKSL_INTERPRETER",
            "SK_CODEC_DECODES_WEBP",
            "SK_ENCODE_WEBP",
            "SK_XML",

            // bin\gn desc out\Debug\ //third_party/angle2:libEGL defines
            "LIBEGL_IMPLEMENTATION",
            "_CRT_SECURE_NO_WARNINGS",
            "_HAS_EXCEPTIONS=0",
            "WIN32_LEAN_AND_MEAN",
            "NOMINMAX",
            "ANGLE_ENABLE_ESSL",
            "ANGLE_ENABLE_GLSL",
            "ANGLE_ENABLE_HLSL",
            "ANGLE_ENABLE_OPENGL",
            "EGL_EGLEXT_PROTOTYPES",
            "GL_GLEXT_PROTOTYPES",
            "ANGLE_ENABLE_D3D11",
            "ANGLE_ENABLE_D3D9",
            "GL_APICALL=",
            "GL_API=",
            "EGLAPI=",
        });

        np.IncludeDirectories.Add(skiaRoot);
        np.IncludeDirectories.Add(skiaRoot + "/third_party/externals/angle2/include");
        // np.IncludeDirectories.Add(skiaRoot + "/include/third_party/vulkan");

        np.Libraries.Add(IsWindows, c =>
        {
            var basePath = skiaRoot + "/out/Debug";
            return new PrecompiledLibrary[]
            {
                new StaticLibrary(basePath + "/skia.lib"),
                new StaticLibrary(basePath + "/skottie.lib"),
                new StaticLibrary(basePath + "/sksg.lib"),
                new StaticLibrary(basePath + "/skshaper.lib"),
                new StaticLibrary(basePath + "/libEGL.dll.lib"),
                new StaticLibrary(basePath + "/libGLESv2.dll.lib"),
                // new SystemLibrary("Opengl32.lib"), 
                new SystemLibrary("User32.lib"),
                //new SystemLibrary("D3D12.lib"), 
                //new SystemLibrary("DXGI.lib"), 
                //new SystemLibrary("d3dcompiler.lib"),
                // new SystemLibrary(basePath + "/obj/tools/trace/trace.ChromeTracingTracer.obj"),
                // new SystemLibrary(basePath + "/obj/tools/trace/trace.EventTracingPriv.obj"),
                // new SystemLibrary(basePath + "/obj/tools/trace/trace.SkDebugfTracer.obj"),
                // new SystemLibrary(basePath + "/obj/tools/flags/flags.CommandLineFlags.obj"),
            };
        });

        var basePath = skiaRoot + "/out/Debug";
        np.SupportFiles.Add(
            new DeployableFile(basePath + "/libEGL.dll"),
            new DeployableFile(basePath + "/libEGL.dll.pdb"),
            new DeployableFile(basePath + "/libGLESv2.dll"),
            new DeployableFile(basePath + "/libGLESv2.dll.pdb")
        );
    }

    // static void SetupSkiaAndroid(NativeProgram np)
    // {
    //     var skiaRoot = Environment.GetEnvironmentVariable("SKIA_ROOT");
    //     if (string.IsNullOrEmpty(skiaRoot))
    //     {
    //         skiaRoot = Environment.GetEnvironmentVariable("USERPROFILE") + "/skia_repo/skia";
    //     }
    //
    //     np.Defines.Add(new[]
    //     {
    //         // bin\gn desc out\arm64\ //:skia defines
    //         "SK_ENABLE_SPIRV_VALIDATION",
    //         "SK_GAMMA_APPLY_TO_A8",
    //         "SK_GAMMA_EXPONENT=1.4",
    //         "SK_GAMMA_CONTRAST=0.0",
    //         "SK_ALLOW_STATIC_GLOBAL_INITIALIZERS=1",
    //         "GR_TEST_UTILS=1",
    //         "SK_USE_VMA",
    //         "SKIA_IMPLEMENTATION=1",
    //         "SK_GL",
    //         "SK_VULKAN",
    //         "SK_ENABLE_VK_LAYERS",
    //         "SK_ENABLE_DUMP_GPU",
    //         "SK_SUPPORT_PDF",
    //         "SK_CODEC_DECODES_JPEG",
    //         "SK_ENCODE_JPEG",
    //         "SK_ENABLE_ANDROID_UTILS",
    //         "SK_USE_LIBGIFCODEC",
    //         "SK_HAS_HEIF_LIBRARY",
    //         "SK_CODEC_DECODES_PNG",
    //         "SK_ENCODE_PNG",
    //         "SK_CODEC_DECODES_RAW",
    //         "SK_ENABLE_SKSL_INTERPRETER",
    //         "SKVM_JIT",
    //         "SK_CODEC_DECODES_WEBP",
    //         "SK_ENCODE_WEBP",
    //         "SK_XML",
    //         "XML_STATIC"
    //     });
    //
    //     np.IncludeDirectories.Add(skiaRoot);
    //
    //     np.Libraries.Add(c =>
    //     {
    //         var basePath = skiaRoot + "/out/arm64";
    //         return new PrecompiledLibrary[]
    //         {
    //             new StaticLibrary(basePath + "/libskia.a"),
    //             new StaticLibrary(basePath + "/libskottie.a"),
    //             new StaticLibrary(basePath + "/libsksg.a"),
    //             new StaticLibrary(basePath + "/libskshaper.a"),
    //             new SystemLibrary("EGL"),
    //             new SystemLibrary("GLESv2"),
    //             new SystemLibrary("log"),
    //             new StaticLibrary(basePath + "/obj/src/utils/libskia.SkJSON.o"),
    //             new StaticLibrary(basePath + "/obj/src/core/libskia.SkCubicMap.o"),
    //             new StaticLibrary(basePath + "/obj/src/effects/libskia.SkColorMatrix.o"),
    //             new StaticLibrary(basePath + "/obj/src/pathops/libskia.SkOpBuilder.o"),
    //             new StaticLibrary(basePath + "/obj/src/utils/libskia.SkParse.o"),
    //         };
    //     });
    // }
}