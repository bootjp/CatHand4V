﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Web.Helpers;
using System.Windows;
using CatHand4V.Wrappers.User32;

namespace CatHand4V
{
    public static class Mains
    {
        [STAThread]
        private static void Main(string[] args)
        {
            
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:11400/");
            listener.Start();

            var running = true;
            Debug.WriteLine("Listening...");
            while (running)
            {
                var context = listener.GetContext();
                var request = context.Request;
                var response = context.Response;
            
                Debug.WriteLine(request.Url.AbsolutePath);
                Debug.WriteLine(request.HttpMethod);

                if (request.HttpMethod != "PUT")
                {
                    response.StatusCode = 400;
                    response.Close();
                    continue;
                }

                switch (request.Url.AbsolutePath)
                {
                    default:
                        response.StatusCode = 404;
                        break;
                    case "/shutdown":
                        running = false;
                        response.StatusCode = 204;
                        break;
                    case "/url":
                        var url = request.QueryString.Get("url");
                        if (!request.HasEntityBody)
                        {
                            response.StatusCode = 400;
                            response.Close();
                            continue;
                        }

                        System.IO.Stream body = request.InputStream;
                        System.Text.Encoding encoding = request.ContentEncoding;
                        System.IO.StreamReader reader = new System.IO.StreamReader(body, encoding);
                        string s = reader.ReadToEnd();
                        Console.WriteLine(s);
                        Query q;
                        try
                        {
                            q = Json.Decode<Query>(s);
                        } catch (Exception e)
                        {
                            response.StatusCode = 400;
                            response.Close();
                            continue;
                        }

                        Debug.WriteLine(q.Url);
                        Copy(q.Url);
                        Paste();
                        response.StatusCode = 202;
                        break;
                }
                response.Close();
            }
            listener.Stop();
        }

        private static void Copy(string url)
        {
            try
            {
                Clipboard.SetText(url);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private static void Paste()
        {
            var processes = Process.GetProcessesByName("VRChat");

            if (!processes.Any())
            {
                MessageBox.Show("VRChatが起動していません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var vrcWindowHandle = processes[0].MainWindowHandle;

            User32Wrapper.SetForegroundWindow(vrcWindowHandle);

            var inputs = User32Util.CreateKeyboardInputs(
                new (DwFlags dwFlags, VirtualKeyCode virtualKeyCode)[]
                {
                    // paste
                    (DwFlags.NONE, VirtualKeyCode.VK_LCONTROL),
                    (DwFlags.NONE, VirtualKeyCode.VK_V),
                    (DwFlags.KEYEVENTF_KEYUP, VirtualKeyCode.VK_LCONTROL),
                    (DwFlags.KEYEVENTF_KEYUP, VirtualKeyCode.VK_V),
                    
                    // enter
                    (DwFlags.NONE, VirtualKeyCode.VK_ENTER),
                    (DwFlags.KEYEVENTF_KEYUP, VirtualKeyCode.VK_ENTER)
                }).ToArray();

            User32Wrapper.SendInput(inputs.Length, inputs, Marshal.SizeOf(inputs[0]));
        }
    }
}
