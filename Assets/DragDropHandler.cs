/* DragDropHandler.cs - v0.1.1 - Olaf Zwennes, 2017 - public domain
 * 
 * Handler for receiving Windows file/folder drag and drop events
 * in Unity. Works in editor (play mode) and windowed/fullscreen standalone.
 * 
 * USAGE
 *  Add to scene and add delegate to fileDropEvent. fileDropEvent is called
 *  each time the user drops files onto the Unity window, with
 *  an array of the full file paths.
 * 
 * VERSION HISTORY
 *  0.1.1 (2017-06-21)  fix issues with Unity v5.6
 *  0.1.0 (2017-06-21)  initial release
 * 
 * LICENSE
 *  See end of file.
 */

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Text;

public delegate void FileDragDropDelegate(string[] filePaths);

public class DragDropHandler : MonoBehaviour {
    public FileDragDropDelegate fileDropEvent = delegate { };
    public static DragDropHandler instance;

    delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lParam);
    
    IntPtr hMainWindow;
    IntPtr oldWndProcPtr;
    IntPtr newWndProcPtr;
    WndProcDelegate newWndProc;
    IntPtr bestHandle;

    const int WM_DROPFILES = 0x0233;
    const int MAX_PATH = 260;
#if UNITY_EDITOR
    const string UNITY_WND_CLASS = "UnityContainerWndClass";
#else
    const string UNITY_WND_CLASS = "UnityWndClass";
#endif

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    static extern System.IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    public static extern bool EnumThreadWindows(uint threadId, IntPtr callback, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern int GetClassName(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);

    [DllImport("shell32.dll")]
    static extern void DragAcceptFiles(IntPtr hWnd, bool fAccept);

    [DllImport("shell32.dll")]
    static extern uint DragQueryFile(IntPtr hDrop, uint iFile, [Out] StringBuilder filename, uint cch);

    [DllImport("shell32.dll")]
    static extern void DragFinish(IntPtr hDrop);

    void Start() {
        // singleton
        if (instance != null) {
            Debug.LogWarning("Only one instance of DragDropHandler is supported, access through DragDropHandler.instance");
            return;
        }

        // get unity window handle
        hMainWindow = GetThreadWindow();
        // check if we have window handle, try alternative get method
        if (hMainWindow == IntPtr.Zero) {
            Debug.LogWarning("Unity window class not found, trying active window");
            hMainWindow = GetActiveWindow();
        }
        if (hMainWindow == IntPtr.Zero) {
            Debug.LogError("Could not find Unity window handle");
            return;
        }
        // create new window proc message handler
        newWndProc = new WndProcDelegate(WndProc);
        newWndProcPtr = Marshal.GetFunctionPointerForDelegate(newWndProc);
        // set unity window's message handler to new window proc (store old window proc)
        oldWndProcPtr = SetWindowLongPtr(hMainWindow, -4, newWndProcPtr);
        // register for drag events
        DragAcceptFiles(hMainWindow, true);

        instance = this;
    }
    
    IntPtr GetThreadWindow() {
        // return window handle with correct class associated with current thread
        uint currentThreadId = GetCurrentThreadId();
        EnumWindowsDelegate enumDelegate = new EnumWindowsDelegate(GetWindowHandle);
        IntPtr enumDelegatePtr = Marshal.GetFunctionPointerForDelegate(enumDelegate);
        EnumThreadWindows(currentThreadId, enumDelegatePtr, IntPtr.Zero);

        return bestHandle;
    }

    bool GetWindowHandle(IntPtr hWnd, IntPtr lParam) {
        // find handle with Unity window class name
        StringBuilder className = new StringBuilder(UNITY_WND_CLASS.Length + 1);
        GetClassName(hWnd, className, className.Capacity);
        if (className.ToString() == UNITY_WND_CLASS) {
            bestHandle = hWnd;
        }
        return true;
    }

    IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) {
        if (msg == WM_DROPFILES) {
            // handle dragged file(s) dropped
            HandleFileDrop(wParam, lParam);
        }
        // send message through to old window proc
        return CallWindowProc(oldWndProcPtr, hWnd, msg, wParam, lParam);
    }

    void HandleFileDrop(IntPtr wParam, IntPtr lParam) {
        // number of dragged files dropped
        uint count = DragQueryFile(wParam, 0xFFFFFFFF, null, 0);
        string[] filePaths = new string[count];

        for (uint i = 0; i < count; ++i) {
            // file path string length
            int size = (int)DragQueryFile(wParam, i, null, 0);
            // get file path string from message
            StringBuilder path = new StringBuilder(size + 1);
            DragQueryFile(wParam, i, path, MAX_PATH);
            filePaths[i] = path.ToString();
        }
        // fire drop event delegate
        fileDropEvent(filePaths);
        // close drag and drop message (release memory)
        DragFinish(wParam);
    }

    void OnDisable() {
        if (instance == this) {
            // set window message handler back to old handler
            SetWindowLongPtr(hMainWindow, -4, oldWndProcPtr);
            // release pointers
            hMainWindow = IntPtr.Zero;
            oldWndProcPtr = IntPtr.Zero;
            newWndProcPtr = IntPtr.Zero;
            newWndProc = null;

            instance = null;
        }
    }
}

/*
------------------------------------------------------------------------------
LICENSE - Public Domain (unlicense.org)
This is free and unencumbered software released into the public domain.
Anyone is free to copy, modify, publish, use, compile, sell, or distribute this
software, either in source code form or as a compiled binary, for any purpose,
commercial or non-commercial, and by any means.
In jurisdictions that recognize copyright laws, the author or authors of this 
software dedicate any and all copyright interest in the software to the public
domain.We make this dedication for the benefit of the public at large and to
the detriment of our heirs and successors.We intend this dedication to be an
overt act of relinquishment in perpetuity of all present and future rights to 
this software under copyright law.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
------------------------------------------------------------------------------
*/
