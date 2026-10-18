// Sets the macOS Dock icon at runtime for the OnlineReports desktop client.
// Copyright (c) Jumpbit. Licensed under the terms in the repository LICENSE file.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace OnlineReports.Client.Platform;

/// <summary>
/// Applies the Jumpbit icon to the macOS Dock. A bare executable started with
/// <c>dotnet run</c> has no app bundle, so macOS ignores Info.plist and Avalonia's
/// <c>Window.Icon</c> and shows a generic file icon unless the application icon is set explicitly.
/// </summary>
[SupportedOSPlatform("macos")]
internal static partial class MacDockIcon
{
    private const string ObjCLibrary = "/usr/lib/libobjc.A.dylib";

    /// <summary>Loads the icon file and assigns it to <c>NSApplication.applicationIconImage</c>.</summary>
    /// <param name="iconPath">Absolute path to an .icns or .png file.</param>
    public static void Apply(string iconPath)
    {
        if (!File.Exists(iconPath))
        {
            return;
        }

        var nsString = objc_getClass("NSString");
        var nsImage = objc_getClass("NSImage");
        var nsApplication = objc_getClass("NSApplication");

        var path = IntPtr.Zero;
        var image = IntPtr.Zero;
        try
        {
            path = SendString(SendPtr(nsString, Selector("alloc")), Selector("initWithUTF8String:"), iconPath);
            image = SendPtr(SendPtr(nsImage, Selector("alloc")), Selector("initWithContentsOfFile:"), path);
            if (image == IntPtr.Zero)
            {
                return;
            }

            var app = SendPtr(nsApplication, Selector("sharedApplication"));
            SendVoid(app, Selector("setApplicationIconImage:"), image);
        }
        finally
        {
            // NSApplication retains the image, so our +1 references can be released.
            if (image != IntPtr.Zero)
            {
                SendPtr(image, Selector("release"));
            }

            if (path != IntPtr.Zero)
            {
                SendPtr(path, Selector("release"));
            }
        }
    }

    private static IntPtr Selector(string name) => sel_registerName(name);

    [LibraryImport(ObjCLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr objc_getClass(string name);

    [LibraryImport(ObjCLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr sel_registerName(string name);

    [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static partial IntPtr SendPtr(IntPtr receiver, IntPtr selector);

    [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static partial IntPtr SendPtr(IntPtr receiver, IntPtr selector, IntPtr argument);

    [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr SendString(IntPtr receiver, IntPtr selector, string argument);

    [LibraryImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static partial void SendVoid(IntPtr receiver, IntPtr selector, IntPtr argument);
}
